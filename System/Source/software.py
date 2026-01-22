# 按键操作
# "ctrl + alt + s" 保存数据
# "ctrl + alt + k" 修改基线
# 配置说明
# 均值为3*3 或者是 2*2
# MATRIX_MEAN = 3       设置为3*3均值，可以修改为2 (2*2均值)
# mean_data()           均值数据
# 鼠标方向传输数据，一次传输一个字节， 数值代表的方向如下
# 0 | 4 |  8 |  12  or   0 | 3 | 6     or        0 | 2
# 1 | 5 |  9 |  13       1 | 4 | 7               1 | 3
# 2 | 6 | 10 |  14       2 | 5 | 8
# 3 | 7 | 11 |  15
# MOUSE_MOVE_ENABLE = 1               鼠标是否移动，如不需要移动请设置为0
# SOCK_SERVER_ADDR = "127.0.0.1"      鼠标方向的传输地址
# SOCK_SERVER_PORT = 6688             鼠标方向的传输地址
# SOCK_SEND_TIME = 0.5                鼠标放行的时间传输间隔
# 接收模块配置
# SOCK_BIND_ADDR = "192.168.3.7"            与模块同一网络的IP
# SOCK_BIND_PORT = 6666                     接收端口
# allow_map["192.168.3.34"] = (255,  0,  0) 模块的IP



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

# cached data volume
allow_map = dict()
PKT_PT_NUM = 16 * 16
RB_PT_NUM  = 16 * 16 * 16
CHANNEL_NUM = 16
# Y axis range
Y_MIN = 0
Y_MAX = 3000


# Mouse move
MOUSE_MOVE_ENABLE = 0

# mean matrix
# 设置为修改3*3（数值为3，默认）的均值或者是2*2（数值为2的均值
MATRIX_MEAN = 2     # 3 * 3 matrix and suport 2 * 2 , 4 * 4

#
# Send mouse direction data to specified server
# Server ip address and port
SOCK_SERVER_ADDR = "127.0.0.1"      # local ip

SOCK_SERVER_PORT = 8686
SOCK_SEND_TIME = 0.05       # unit : second
SOCK_SEND_MAX_TIME = 3      # unit : second

# Bind ip and port  to receive data sent by node
# ip on the same network as node
SOCK_BIND_ADDR = "192.168.1.100"
SOCK_BIND_PORT = 6666

# node ip address
allow_map["192.168.1.101"] = (255,  0,  0)

'''
allow_map["192.168.1.101"] = (  0,  0,  0)
allow_map["192.168.1.102"] = (255,  0,  0)
allow_map["192.168.1.103"] = (  0,255,  0)
'''

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

#setup GUI
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
        # curve = plot.plot(name=key, pen=(R,G,B))
        curves[i] = plot.plot(pen=(R,G,B))
    acurve_dic[key] = ACurve(key, plot, curves, RB_PT_NUM, True)
    indexn += 1

# 均值数据
# 矩阵如以下
# 0 | 4 |  8 |  12  or   0 | 3 | 6     or        0 | 2
# 1 | 5 |  9 |  13       1 | 4 | 7               1 | 3
# 2 | 6 | 10 |  14       2 | 5 | 8
# 3 | 7 | 11 |  15
# 数据格式为[0, 1, 2, 3, 4, 5, 6 ,7 ,8, 9 , 10, 11, 12, 13, 14, 15,...]
# 或者[0, 1, 2, 3, 4, 5, 6 ,7 ,8, 0, 1, 2, 3, 4, 5, 6 ,7 ,8,...]
# 或者[0, 1, 2, 3, 0, 1, 2, 3, ...]
def mean_data():
    for key,acurve in acurve_dic.items():
        #转换后的矩阵数据
        acurve.matrix

def node_curve_update():
    for key,acurve in acurve_dic.items():
        # display 4 * 4 matrix
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

# mouse control handler
mouse_ctl = mouse.Controller()

# queue to transport mouse direction data
mouse_dic_queue = queue.Queue(SOCK_SEND_MAX_TIME*1000)
queue_lock = threading.Lock()

# convert matrix 4 * 4 to 3 * 3 or 2 * 2
def node_data_mean(node_data):
    # cache to save convet data
    matrix_data = np.array([], dtype = np.int32)

    #copy the data and convert it into a 16 * 16 two dimensional array
    datas = node_data.copy().reshape(16,16)
    for data in datas:
        if (MATRIX_MEAN == 2):
            # convert 4 * 4 matrix sum to 2 * 2 matrix
            data = data.reshape((4 , 4), order = 'F')
            data = data[..., 0:2] + data[..., 1:3] + data[..., 2:4]
            data = data[0:2, ...] + data[1:3, ...] + data[2:4, ...]
            data = data.flatten(order = 'F') // 9
        elif (MATRIX_MEAN == 3): 
            # convert 4 * 4 matrix sum to 3 * 3 matrix
            data = data.reshape((4 , 4), order = 'F')
            data = data[..., :3] + data[..., 1:]
            data = data[:3, ...] + data[1:, ...]
            data = data.flatten(order = 'F') // 4
        # elif (MATRIX_MEAN == 6): 
        #     t_data = []
        #     t_data.append((data[0] + data[5] + data[1] + data[4]))
        #     t_data.append((data[4] + data[5] + data[8] + data[9]))
        #     t_data.append((data[8] + data[9] + data[12] + data[13]))
        #     t_data.append((data[2] + data[3] + data[6] + data[7]))
        #     t_data.append((data[10] + data[15] + data[14] + data[11]))
        #     # t_data.append((data[9] + data[12] + data[13]))
        #     # t_data.append((data[9] + data[12] + data[8]))
        #     # t_data.append((data[0] + data[5] + data[4]))
        #     data = np.array(t_data) // 4
        # else:
        #   4 * 4 nothing

        # add data to cache
        matrix_data = np.append(matrix_data, data)
    return matrix_data

# mouse and send move direction
# 0 | 4 |  8 |  12  or   0 | 3 | 6     or        0 | 2
# 1 | 5 |  9 |  13       1 | 4 | 7               1 | 3
# 2 | 6 | 10 |  14       2 | 5 | 8
# 3 | 7 | 11 |  15
def node_move_mouse(matrix_data):
    #copy the data and convert it into a 16 * 16 two dimensional array
    datas = matrix_data.copy().reshape(-1, (MATRIX_MEAN * MATRIX_MEAN))

    for data in datas:
        # the node is pressed
        if ((data == 142).all() or (data < 100).all()):
            continue

        if ((data <= 1200).all() or (data < 100).all()):#弯曲后乱动修改第一个值
            continue
        
        # get max value index
        mouse_direction = np.argmax(data)

        if (MOUSE_MOVE_ENABLE):
            x = 0
            y = 0

            if (mouse_direction < MATRIX_MEAN):
                x = -1
            elif (mouse_direction >= MATRIX_MEAN * (MATRIX_MEAN - 1)):
                x = 1
            if (mouse_direction % MATRIX_MEAN == 0):
                y = -1
            elif (mouse_direction % MATRIX_MEAN == (MATRIX_MEAN - 1)):
                y = 1
            mouse_ctl.move(x,y)
            # time.sleep(0.5)

        # save mouse direction data
        queue_lock.acquire()
        if not mouse_dic_queue.full():
            mouse_dic_queue.put(mouse_direction+1)
        queue_lock.release()


# Start to receive udp packet
stop_event = threading.Event()
def task_recv_data(ipaddr, port):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((ipaddr, port))
    while not stop_event.is_set():
        # receive data
        c_data, (c_addr, c_port) = sock.recvfrom(16+PKT_PT_NUM*2)
        # find acurve
        acurve = acurve_dic.get(c_addr)
        if acurve == None:
            continue
        # add data to buffer
        adc_data = np.frombuffer(c_data, dtype='int16', offset=16)
        acurve.bval = adc_data
        adc_data = adc_data - acurve.baseline

        acurve.yval.extend(adc_data)
        acurve.xval.extend(np.arange(acurve.ptcnt, acurve.ptcnt+(PKT_PT_NUM)))
        acurve.ptcnt += PKT_PT_NUM

        # add convert to buffer
        matrix_data = node_data_mean(adc_data)
        acurve.matrix.extend(matrix_data)
        node_move_mouse(matrix_data)

thread_recv_data = threading.Thread(target=task_recv_data, args=(SOCK_BIND_ADDR, SOCK_BIND_PORT, ))
thread_recv_data.start()

# send mouse direction to data
client = None
def send_mouse_data():
    global client, send_mouse_timer
    # connect server
    if (client == None):
        try:
            client = socket.socket()
            client.connect((SOCK_SERVER_ADDR, SOCK_SERVER_PORT))
            print('connected to server')
        except:
            client = None
    
    # get mouse move direction data
    data = None
    queue_lock.acquire()
    if not mouse_dic_queue.empty():
        size = mouse_dic_queue.qsize()
        data = [mouse_dic_queue.get() for i in range(size)]
    queue_lock.release()

    # while 1:
    #     if (len(data) > SOCK_SEND_TIME * 500):
    #         break

    # send data mouse direct one byte
    if (client and data):
        data = np.array(data)
        counts = np.bincount(data)
        # client_data = struct.pack('b', np.argmax(counts))
        client_data = str(np.argmax(counts))
        try:
            client.send(client_data.encode())
            print(client_data)
        except:
            client = None

    # restart timer 
    
    send_mouse_timer = threading.Timer(SOCK_SEND_TIME, send_mouse_data)
    send_mouse_timer.start()

send_mouse_timer = threading.Timer(1, send_mouse_data)
send_mouse_timer.start()


# save node cache data
# save as 4 * 4 matrix format
# eg data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]
# save format = [[0, 4,  8, 12],
#                [1, 4,  9, 13],
#                [2, 5, 10, 14],
#                [3, 6, 11, 15]]
# the format is consistent with the page 4 * 4 matrix display 
def save_node_data():
    for (key, anode) in acurve_dic.items():
        filename = "./data/" + key + '.csv'
        f = open(filename, 'w')
        for i in np.arange(len(anode.yval)//16):
            for j in np.arange(4):
                f.write(str(anode.yval[16 * i +  0 + j]) + ',' + 
                        str(anode.yval[16 * i +  4 + j]) + ',' +
                        str(anode.yval[16 * i +  8 + j]) + ',' +
                        str(anode.yval[16 * i + 12 + j]) + ',' +'\n')
            f.write('\n')
        f.close()
        print("save data to ./data/{0}.csv".format(key))

# modify node baseline setting
def save_node_baseline():
    for key,acurve in acurve_dic.items():
        if (len(acurve.bval) > 0):
            data = acurve.bval.copy().reshape(16,16)
            baseline = np.sum(data, axis = 0) // 16
            acurve.baseline = np.tile(baseline, 16)
            print("moidfy {0} baseline".format(key))

# Start monitoring input control 
# "ctrl + alt + s" to save data to cvs
# "ctrl + alt + k" to modify baseline
thread_listener_key = keyboard.GlobalHotKeys({
        '<ctrl>+<alt>+s': save_node_data,
        '<ctrl>+<alt>+k': save_node_baseline
        })
thread_listener_key.start()

## Start Qt event loop unless running in interactive mode or using pyside.
if __name__ == '__main__':
    import sys

    if (sys.flags.interactive != 1) or not hasattr(QtCore, 'PYQT_VERSION'):
        QtGui.QApplication.instance().exec_()

    stop_event.set()
    thread_listener_key.stop()
    send_mouse_timer.cancel()
    send_mouse_timer = None

    thread_listener_key.join()
    thread_recv_data.join()
    if (queue_lock.acquire(True)):
        queue_lock.release()
        queue_lock = None
    if (client):
        client.close()
