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

# 基础配置
allow_map = dict()
PKT_PT_NUM = 16 * 16
RB_PT_NUM  = 16 * 16 * 16
CHANNEL_NUM = 16
Y_MIN = 0
Y_MAX = 3000  

# 鼠标移动开关
MOUSE_MOVE_ENABLE = 0

# 动态配置：MATRIX_MEAN（默认2）
MATRIX_MEAN = 2  
current_experiment = 0  # 当前实验序号（由Unity传入）

# 鼠标方向发送配置
SOCK_SERVER_ADDR = "192.168.1.100"      # Unity的TCP服务地址（接收信号）
SOCK_SERVER_PORT = 8686             # Unity的TCP服务端口
SOCK_SEND_TIME = 0.3               
SOCK_SEND_MAX_TIME = 3              

# 硬件数据接收配置
SOCK_BIND_ADDR = "192.168.1.101"    # 和硬件同网段的IP
SOCK_BIND_PORT = 6666               # 接收硬件UDP数据的端口

# Unity实验序号接收配置（TCP）
UNITY_TCP_PORT = 8687               # 接收Unity实验序号的端口
unity_tcp_server = None
unity_tcp_running = True

# 硬件节点IP配置
allow_map["192.168.1.102"] = (255,  0,  0)

# 实验信号过滤规则（只保留对应有效信号）
experiment_signal_rules = {
    1: [1,2,3,4],
    2: [1,3,4],
    3: [1,4,5],
    4: [1,5,6]
}

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

# 初始化GUI
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

# 更新曲线显示
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

# 鼠标控制
mouse_ctl = mouse.Controller()
mouse_dic_queue = queue.Queue(SOCK_SEND_MAX_TIME*1000)
queue_lock = threading.Lock()

# 根据实验序号更新MATRIX_MEAN
def update_matrix_mean(experiment_num):
    global MATRIX_MEAN, current_experiment
    current_experiment = experiment_num
    if experiment_num == 1 or experiment_num == 2:
        MATRIX_MEAN = 2
    elif experiment_num == 3:
        MATRIX_MEAN = 3
    elif experiment_num == 4:
        MATRIX_MEAN = 4
    else:
        MATRIX_MEAN = 2  # 默认值
    print(f"当前实验序号：{current_experiment}，MATRIX_MEAN已设置为：{MATRIX_MEAN}")

# 矩阵均值转换（根据MATRIX_MEAN动态转换）
def node_data_mean(node_data):
    matrix_data = np.array([], dtype = np.int32)
    datas = node_data.copy().reshape(16,16)
    for data in datas:
        if MATRIX_MEAN == 2:
            # 4x4→2x2均值
            data = data.reshape((4 , 4), order = 'F')
            data = data[..., 0:2] + data[..., 1:3] + data[..., 2:4]
            data = data[0:2, ...] + data[1:3, ...] + data[2:4, ...]
            data = data.flatten(order = 'F') // 9
        elif MATRIX_MEAN == 3:
            # 4x4→3x3均值
            data = data.reshape((4 , 4), order = 'F')
            data = data[..., :3] + data[..., 1:]
            data = data[:3, ...] + data[1:, ...]
            data = data.flatten(order = 'F') // 4
        elif MATRIX_MEAN == 4:
            # 4x4→4x4（不转换）
            data = data.reshape((4,4), order='F').flatten(order='F')
        matrix_data = np.append(matrix_data, data)
    return matrix_data

# 鼠标方向控制（过滤无效信号）
def node_move_mouse(matrix_data):
    global current_experiment
    datas = matrix_data.copy().reshape(-1, (MATRIX_MEAN * MATRIX_MEAN))
    valid_signals = experiment_signal_rules.get(current_experiment, [])

    for data in datas:
        if ((data == 142).all() or (data < 100).all()):
            continue
        if ((data <= 500).all() or (data < 100).all()):
            continue
        
        mouse_direction = np.argmax(data) + 1  # 方向从1开始
        
        # 过滤无效信号
        if mouse_direction not in valid_signals:
            return

        # 鼠标移动控制
        if MOUSE_MOVE_ENABLE:
            x = 0
            y = 0
            if mouse_direction < MATRIX_MEAN:
                x = -1
            elif mouse_direction >= MATRIX_MEAN * (MATRIX_MEAN - 1):
                x = 1
            if mouse_direction % MATRIX_MEAN == 0:
                y = -1
            elif mouse_direction % MATRIX_MEAN == (MATRIX_MEAN - 1):
                y = 1
            mouse_ctl.move(x,y)

        # 存入队列发送给Unity
        queue_lock.acquire()
        if not mouse_dic_queue.full():
            mouse_dic_queue.put(mouse_direction)
        queue_lock.release()

# 接收硬件UDP数据
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
            
            # 解析硬件数据
            adc_data = np.frombuffer(c_data, dtype='int16', offset=16)
            acurve.bval = adc_data
            adc_data = adc_data - acurve.baseline

            # 更新数据缓冲区
            acurve.yval.extend(adc_data)
            acurve.xval.extend(np.arange(acurve.ptcnt, acurve.ptcnt+(PKT_PT_NUM)))
            acurve.ptcnt += PKT_PT_NUM

            # 矩阵转换+鼠标方向识别
            matrix_data = node_data_mean(adc_data)
            acurve.matrix.extend(matrix_data)
            node_move_mouse(matrix_data)
        except Exception as e:
            print(f"接收硬件数据出错：{e}")
            continue

# 接收Unity发送的实验序号（TCP服务）
def task_recv_unity_experiment_num(port):
    global unity_tcp_server
    unity_tcp_server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    unity_tcp_server.bind(('0.0.0.0', port))
    unity_tcp_server.listen(1)
    print(f"Unity实验序号接收服务已启动，监听端口：{port}")

    while unity_tcp_running:
        try:
            client_socket, addr = unity_tcp_server.accept()
            print(f"Unity客户端已连接：{addr}")
            while True:
                data = client_socket.recv(1024)
                if not data:
                    break
                experiment_num = int(data.decode('utf-8'))
                update_matrix_mean(experiment_num)
            client_socket.close()
        except Exception as e:
            if not unity_tcp_running:
                break
            print(f"接收实验序号出错：{e}")

# 向Unity发送鼠标方向数据
client = None
def send_mouse_data():
    global client, send_mouse_timer
    # 连接Unity TCP服务
    if client is None:
        try:
            client = socket.socket()
            client.connect((SOCK_SERVER_ADDR, SOCK_SERVER_PORT))
            print(f"连接Unity TCP服务成功：{SOCK_SERVER_ADDR}:{SOCK_SERVER_PORT}")
        except:
            client = None
    
    # 读取队列中的方向数据
    data = None
    queue_lock.acquire()
    if not mouse_dic_queue.empty():
        size = mouse_dic_queue.qsize()
        data = [mouse_dic_queue.get() for i in range(size)]
    queue_lock.release()

    # 发送数据（取出现次数最多的方向）
    if client and data:
        data = np.array(data)
        counts = np.bincount(data)
        send_data = str(np.argmax(counts))
        try:
            client.send(send_data.encode('utf-8'))
            print(f"向Unity发送信号：{send_data}")
        except:
            client = None

    # 定时重复发送
    send_mouse_timer = threading.Timer(SOCK_SEND_TIME, send_mouse_data)
    send_mouse_timer.start()

# 保存数据（热键：ctrl+alt+s）
def save_node_data():
    import os
    if not os.path.exists("./data"):
        os.makedirs("./data")
    for (key, anode) in acurve_dic.items():
        filename = f"./data/{key}_experiment_{current_experiment}.csv"
        f = open(filename, 'w')
        for i in np.arange(len(anode.yval)//16):
            for j in np.arange(4):
                f.write(f"{anode.yval[16 * i +  0 + j]},{anode.yval[16 * i +  4 + j]},{anode.yval[16 * i +  8 + j]},{anode.yval[16 * i + 12 + j]}\n")
            f.write('\n')
        f.close()
        print(f"保存数据到：{filename}")

# 修改基线（热键：ctrl+alt+k）
def save_node_baseline():
    for key,acurve in acurve_dic.items():
        if len(acurve.bval) > 0:
            data = acurve.bval.copy().reshape(16,16)
            baseline = np.sum(data, axis = 0) // 16
            acurve.baseline = np.tile(baseline, 16)
            print(f"修改{key}基线完成")

# 启动线程
if __name__ == '__main__':
    import sys

    # 1. 启动硬件数据接收线程
    thread_recv_data = threading.Thread(target=task_recv_data, args=(SOCK_BIND_ADDR, SOCK_BIND_PORT, ))
    thread_recv_data.start()

    # 2. 启动Unity实验序号接收线程
    thread_recv_unity = threading.Thread(target=task_recv_unity_experiment_num, args=(UNITY_TCP_PORT, ))
    thread_recv_unity.start()

    # 3. 启动鼠标方向发送定时器
    send_mouse_timer = threading.Timer(1, send_mouse_data)
    send_mouse_timer.start()

    # 4. 启动热键监听
    thread_listener_key = keyboard.GlobalHotKeys({
            '<ctrl>+<alt>+s': save_node_data,
            '<ctrl>+<alt>+k': save_node_baseline
            })
    thread_listener_key.start()

    # 启动Qt GUI事件循环
    if (sys.flags.interactive != 1) or not hasattr(QtCore, 'PYQT_VERSION'):
        QtGui.QApplication.instance().exec_()

    # 程序退出清理
    stop_event.set()
    unity_tcp_running = False
    thread_listener_key.stop()
    send_mouse_timer.cancel()

    # 等待线程结束
    thread_recv_data.join()
    thread_recv_unity.join()
    thread_listener_key.join()

    # 关闭连接
    if unity_tcp_server:
        unity_tcp_server.close()
    if client:
        client.close()
    print("程序正常退出")