import socket
import queue
import threading
from pyqtgraph.Qt import QtGui, QtCore
import pyqtgraph as pg
import numpy as np
from dvg_ringbuffer import RingBuffer
from pynput import mouse, keyboard
import struct
import time

# ===================== 全局配置 =====================
# 缓存数据量
allow_map = dict()
PKT_PT_NUM = 16 * 16
RB_PT_NUM  = 16 * 16 * 16
CHANNEL_NUM = 16
# Y轴范围
Y_MIN = 0
Y_MAX = 3000

# 鼠标移动开关
MOUSE_MOVE_ENABLE = 0

# 实验相关配置（动态切换）
current_experiment = 5  # 默认实验5
MATRIX_MEAN = 3         # 动态修改：实验5=3(5分区)，实验6=3(3*3)，实验7=4(4*4)

# TCP配置（发送鼠标方向到Unity）
SOCK_SERVER_ADDR = "192.168.1.100"      
SOCK_SERVER_PORT = 8686
SOCK_SEND_TIME = 0.3                
SOCK_SEND_MAX_TIME = 3              

# UDP配置（接收模块数据）
SOCK_BIND_ADDR = "192.168.1.101"
SOCK_BIND_PORT = 6666

# 模块IP
allow_map["192.168.1.102"] = (255,  0,  0)

# TCP服务端配置（接收Unity的实验序号）
EXPERIMENT_SERVER_PORT = 8687
experiment_server_socket = None

# ===================== 数据结构 =====================
class ACurve:
    def __init__(self, name, plot, curve, cap, in_use):
        matrix_num = cap // CHANNEL_NUM * MATRIX_MEAN * MATRIX_MEAN
        self.name  = name
        self.xval  = RingBuffer(cap, dtype=np.int32)
        self.yval  = RingBuffer(cap, dtype=np.int32)
        self.matrix = RingBuffer(matrix_num, dtype=np.int32)
        self.curve = curve
        self.plot  = plot
        self.ptcnt = 0
        self.bval = 0
        self.baseline = 0
        self.inuse = in_use

# ===================== GUI初始化 =====================
QtGui.QApplication([])
pg.setConfigOptions(antialias=True)
pg.setConfigOption('background', 'w')
pg.setConfigOption('foreground', 'b')
win = pg.GraphicsLayoutWidget(show=True)
win.resize(800,600)

indexn = 0
acurve_dic = {}
for key in allow_map:
    if indexn % 1 == 0:
        win.nextRow()
    R = allow_map[key][0]
    G = allow_map[key][1]
    B = allow_map[key][2]
    curves = [0 for i in np.arange(CHANNEL_NUM)]
    for i in np.arange(CHANNEL_NUM):
        if i % 4 == 0:
            win.nextRow()
        plot = win.addPlot()
        plot.addLegend()
        plot.enableAutoRange('x')
        plot.setYRange(Y_MIN, Y_MAX)
        curves[i] = plot.plot(pen=(R,G,B))
    acurve_dic[key] = ACurve(key, plot, curves, RB_PT_NUM, True)
    indexn += 1

# ===================== 定时器更新曲线 =====================
def node_curve_update():
    for key,acurve in acurve_dic.items():
        xval = acurve.xval
        yval = acurve.yval
        for i in np.arange(4):
            for j in np.arange(4):
                n = 4*j+i
                m = 4*i+j
                acurve.curve[n].setData(x=xval[m::CHANNEL_NUM], y=yval[m::CHANNEL_NUM])

timer = QtCore.QTimer()
timer.timeout.connect(node_curve_update)
timer.start(100)

# ===================== 鼠标控制 =====================
mouse_ctl = mouse.Controller()

# ===================== 队列与锁 =====================
mouse_dic_queue = queue.Queue(SOCK_SEND_MAX_TIME*1000)
queue_lock = threading.Lock()

# ===================== 数据处理逻辑（按实验序号切换） =====================
def node_data_mean(node_data):
    """根据当前实验序号处理数据均值"""
    global MATRIX_MEAN
    matrix_data = np.array([], dtype = np.int32)
    datas = node_data.copy().reshape(16,16)
    
    for data in datas:
        if current_experiment == 5:
            # 实验5：不规则5分区（第一段代码逻辑）
            data = data.reshape((4 , 4), order = 'F')
            # 区域0: 传感器0,4,1,5
            region0 = (data[0,0] + data[1,0] + data[0,1] + data[1,1]) // 4
            # 区域1: 传感器4,8,5,9
            region1 = (data[1,0] + data[2,0] + data[1,1] + data[2,1]) // 4
            # 区域2: 传感器8,12,9,13
            region2 = (data[2,0] + data[3,0] + data[2,1] + data[3,1]) // 4
            # 区域3: 传感器2,6,3,7
            region3 = (data[0,2] + data[1,2] + data[0,3] + data[1,3]) // 4
            # 区域4: 传感器10,11,14,15
            region4 = (data[2,2] + data[2,3] + data[3,2] + data[3,3]) // 4
            data = np.array([region0, region1, region2, region3, region4])
        
        elif current_experiment == 6:
            # 实验6：3*3均值
            MATRIX_MEAN = 3
            data = data.reshape((4 , 4), order = 'F')
            data = data[..., :3] + data[..., 1:]
            data = data[:3, ...] + data[1:, ...]
            data = data.flatten(order = 'F') // 4
        
        elif current_experiment == 7:
            # 实验7：4*4均值
            MATRIX_MEAN = 4
            data = data.reshape((4 , 4), order = 'F')
            data = data.flatten(order = 'F')
        
        matrix_data = np.append(matrix_data, data)
    return matrix_data

def node_move_mouse(matrix_data):
    """根据当前实验序号处理鼠标方向"""
    global current_experiment
    if current_experiment == 5:
        # 实验5：不规则5分区逻辑
        datas = matrix_data.copy().reshape(-1, 5)
    elif current_experiment == 6:
        # 实验6：3*3=9分区
        datas = matrix_data.copy().reshape(-1, 9)
    elif current_experiment == 7:
        # 实验7：4*4=16分区
        datas = matrix_data.copy().reshape(-1, 16)
    else:
        return

    for data in datas:
        # 过滤无效数据
        if ((data == 142).all() or (data < 100).all()):
            continue
        if ((data <= 1200).all() or (data < 100).all()):
            continue

        # 获取最大值得索引（方向）
        mouse_direction = np.argmax(data)
        
        # 鼠标移动控制
        if MOUSE_MOVE_ENABLE:
            x = 0
            y = 0
            if current_experiment == 5:
                # 实验5：不规则布局移动逻辑
                if mouse_direction == 0 or mouse_direction == 3:
                    x = -1
                elif mouse_direction == 2 or mouse_direction == 4:
                    x = 1
                if mouse_direction < 3:
                    y = -1
                else:
                    y = 1
            elif current_experiment == 6:
                # 实验6：3*3布局移动逻辑
                if mouse_direction < MATRIX_MEAN:
                    x = -1
                elif mouse_direction >= MATRIX_MEAN * (MATRIX_MEAN - 1):
                    x = 1
                if mouse_direction % MATRIX_MEAN == 0:
                    y = -1
                elif mouse_direction % MATRIX_MEAN == (MATRIX_MEAN - 1):
                    y = 1
            elif current_experiment == 7:
                # 实验7：4*4布局移动逻辑
                col = mouse_direction % 4
                row = mouse_direction // 4
                if col < 2:
                    x = -1
                else:
                    x = 1
                if row < 2:
                    y = -1
                else:
                    y = 1
            mouse_ctl.move(x, y)

        # 信号过滤：根据实验序号确定有效信号
        valid_signal = False
        if current_experiment == 5:
            # 实验5：0-4→1-5
            valid_signal = mouse_direction >= 0 and mouse_direction <= 4
        elif current_experiment == 6:
            # 实验6：0-4→1-5（5-8不响应）
            valid_signal = mouse_direction >= 0 and mouse_direction <= 4
        elif current_experiment == 7:
            # 实验7：0-2→1-3，4-5→5-6（其他不响应）
            valid_signal = mouse_direction in [0,1,2,4,5]
        
        if valid_signal:
            # 保存方向（+1适配Unity的1-based）
            queue_lock.acquire()
            if not mouse_dic_queue.full():
                mouse_dic_queue.put(mouse_direction + 1)
            queue_lock.release()

# ===================== UDP数据接收 =====================
stop_event = threading.Event()
def task_recv_data(ipaddr, port):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((ipaddr, port))
    while not stop_event.is_set():
        try:
            c_data, (c_addr, c_port) = sock.recvfrom(16+PKT_PT_NUM*2)
            acurve = acurve_dic.get(c_addr)
            if acurve == None:
                continue
            
            # 处理ADC数据
            adc_data = np.frombuffer(c_data, dtype='int16', offset=16)
            acurve.bval = adc_data
            adc_data = adc_data - acurve.baseline

            acurve.yval.extend(adc_data)
            acurve.xval.extend(np.arange(acurve.ptcnt, acurve.ptcnt+(PKT_PT_NUM)))
            acurve.ptcnt += PKT_PT_NUM

            # 处理均值数据
            matrix_data = node_data_mean(adc_data)
            acurve.matrix.extend(matrix_data)
            node_move_mouse(matrix_data)
        except Exception as e:
            print(f"UDP接收错误：{e}")
            continue

# ===================== TCP发送鼠标方向 =====================
client = None
def send_mouse_data():
    global client, send_mouse_timer
    # 连接Unity
    if client is None:
        try:
            client = socket.socket()
            client.connect((SOCK_SERVER_ADDR, SOCK_SERVER_PORT))
            print('Connected to Unity server')
        except:
            client = None
    
    # 获取队列数据
    data = None
    queue_lock.acquire()
    if not mouse_dic_queue.empty():
        size = mouse_dic_queue.qsize()
        data = [mouse_dic_queue.get() for i in range(size)]
    queue_lock.release()

    # 发送数据
    if client and data:
        data = np.array(data)
        counts = np.bincount(data)
        client_data = str(np.argmax(counts))
        try:
            client.send(client_data.encode())
            print(f"发送数据到Unity：{client_data}")
        except:
            client = None

    # 重启定时器
    send_mouse_timer = threading.Timer(SOCK_SEND_TIME, send_mouse_data)
    send_mouse_timer.start()

# ===================== TCP接收实验序号 =====================
def task_recv_experiment_number(port):
    global current_experiment, MATRIX_MEAN
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind(('0.0.0.0', port))
    server_socket.listen(1)
    print(f"实验序号接收服务启动：0.0.0.0:{port}")
    
    while not stop_event.is_set():
        try:
            conn, addr = server_socket.accept()
            print(f"Unity连接：{addr}")
            while True:
                data = conn.recv(1024)
                if not data:
                    break
                # 解析实验序号
                exp_num = int(data.decode().strip())
                current_experiment = exp_num
                # 更新MATRIX_MEAN
                if exp_num == 5:
                    MATRIX_MEAN = 3
                elif exp_num == 6:
                    MATRIX_MEAN = 3
                elif exp_num == 7:
                    MATRIX_MEAN = 4
                print(f"切换到实验{exp_num}，MATRIX_MEAN={MATRIX_MEAN}")
            conn.close()
        except Exception as e:
            print(f"实验序号接收错误：{e}")
            continue

# ===================== 数据保存与基线修改 =====================
def save_node_data():
    for (key, anode) in acurve_dic.items():
        filename = f"./data/{key}_exp{current_experiment}.csv"
        f = open(filename, 'w')
        for i in np.arange(len(anode.yval)//16):
            for j in np.arange(4):
                f.write(f"{anode.yval[16*i + 0 + j]},{anode.yval[16*i + 4 + j]},{anode.yval[16*i + 8 + j]},{anode.yval[16*i + 12 + j]}\n")
            f.write('\n')
        f.close()
        print(f"保存数据到：{filename}")

def save_node_baseline():
    for key,acurve in acurve_dic.items():
        if len(acurve.bval) > 0:
            data = acurve.bval.copy().reshape(16,16)
            baseline = np.sum(data, axis = 0) // 16
            acurve.baseline = np.tile(baseline, 16)
            print(f"修改{key}基线")

# ===================== 按键监听 =====================
thread_listener_key = keyboard.GlobalHotKeys({
        '<ctrl>+<alt>+s': save_node_data,
        '<ctrl>+<alt>+k': save_node_baseline
        })
thread_listener_key.start()

# ===================== 启动线程 =====================
# 1. UDP数据接收线程
thread_recv_data = threading.Thread(target=task_recv_data, args=(SOCK_BIND_ADDR, SOCK_BIND_PORT, ))
thread_recv_data.start()

# 2. 实验序号接收线程
thread_recv_exp = threading.Thread(target=task_recv_experiment_number, args=(EXPERIMENT_SERVER_PORT, ))
thread_recv_exp.start()

# 3. 鼠标方向发送定时器
send_mouse_timer = threading.Timer(1, send_mouse_data)
send_mouse_timer.start()

# ===================== 主循环 =====================
if __name__ == '__main__':
    import sys
    try:
        if (sys.flags.interactive != 1) or not hasattr(QtCore, 'PYQT_VERSION'):
            QtGui.QApplication.instance().exec_()
    finally:
        # 清理资源
        stop_event.set()
        thread_listener_key.stop()
        send_mouse_timer.cancel()
        
        thread_listener_key.join()
        thread_recv_data.join()
        thread_recv_exp.join()
        
        if queue_lock.acquire(True):
            queue_lock.release()
        
        if client:
            client.close()
        print("程序正常退出")