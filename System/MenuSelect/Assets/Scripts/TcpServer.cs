using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class TcpServer : MonoBehaviour {

    public string msg;
    public int tcp_port;

    //以下默认都是私有的成员
    Socket serverSocket;                //服务器端socket
    Socket clientSocket;                //客户端socket
    IPEndPoint ipEnd; 					//侦听端口
    string recvStr; 					//接收的字符串
    string sendStr;						//发送的字符串
    byte[] recvData;                    //接收的数据，必须为字节
    byte[] sendData = new byte[1024];   //发送的数据，必须为字节
    int recvLen;                        //接收的数据长度

	// Use this for initialization
	void Start () {
		InitSocket();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnApplicationQuit(){

        SocketQuit();
    }

    void OnDestroy(){

        SocketQuit();
    }

	//初始化
    void InitSocket(){

        //定义侦听端口,侦听任何IP
        ipEnd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), tcp_port);
        //定义套接字类型,在主线程中定义
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //连接
        serverSocket.Bind(ipEnd);
        //开始侦听,最大5个连接
        serverSocket.Listen(5);

        //开启一个线程连接，必须的，否则主线程卡死
        Thread serverThread = new Thread(StartServer);
        serverThread.Start(serverSocket);
    }

    //等待连接
    void StartServer(object obj){
        
        string str;
        while (true)
        {
            //等待接收客户端连接 Accept方法返回一个用于和该客户端通信的Socket
            Socket recviceSocket = ((Socket)obj).Accept();
            //获取客户端ip和端口号
            str = recviceSocket.RemoteEndPoint.ToString();
            // socketList.Add(str, recviceSocket);
            Debug.Log("Connect with " + str);
            //Accept()执行过后 当前线程会阻塞 只有在有客户端连接时才会继续执行
            //创建新线程,监控接收新客户端的请求数据
            Thread thread = new Thread(SocketReceive);
            thread.IsBackground = true;
            thread.Start(recviceSocket);
        }
    }

	void SocketSend(string sendStr){

        //清空发送缓存
        sendData = new byte[1024];
        //数据类型转换
        sendData = Encoding.ASCII.GetBytes(sendStr);
        //发送
        clientSocket.Send(sendData, sendData.Length, SocketFlags.None);
    }

	//服务器接收
    void SocketReceive(object obj){

        while (true)
        {
            recvData = new byte[1024];
            //获取收到的数据的长度
            recvLen = ((Socket)obj).Receive(recvData);
            if (recvLen == 0)
			{
                Debug.Log("no connection");
                ((Socket)obj).Close();
                break;
            }
            else
            {
                msg = Encoding.UTF8.GetString(recvData, 0, recvLen);
                Debug.Log(msg);
                // posArray = pos.Split(' ');
                // num_pos = int.Parse(posArray[0]);
            }
        }
    }

	//连接关闭
    void SocketQuit(){

        //关闭服务器
        serverSocket.Close();
        Debug.Log("socket closed");
    }
}
