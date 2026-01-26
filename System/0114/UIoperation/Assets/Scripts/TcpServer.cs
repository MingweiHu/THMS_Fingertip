using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class TcpServer : MonoBehaviour
{
    [Header("TCP配置")]
    public int tcp_port = 8888; // 默认端口，可在Inspector修改

    [Header("运行时数据")]
    public string msg;          // 接收到的消息
    public bool hasNewMessage;  // 标记是否有新消息待处理

    // 私有网络相关变量
    private Socket serverSocket;
    private IPEndPoint ipEnd;
    private byte[] recvData = new byte[1024];
    private int recvLen;

    void Start()
    {
        InitSocket();
    }

    void OnApplicationQuit()
    {
        SocketQuit();
    }

    void OnDestroy()
    {
        SocketQuit();
    }

    /// <summary>
    /// 初始化TCP服务器
    /// </summary>
    private void InitSocket()
    {
        try
        {
            // 绑定本地IP和端口
            ipEnd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), tcp_port);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(ipEnd);
            serverSocket.Listen(5); // 最大5个连接

            // 开启线程监听客户端连接（避免卡死主线程）
            Thread serverThread = new Thread(StartServer);
            serverThread.IsBackground = true;
            serverThread.Start();
            Debug.Log($"TCP服务器已启动，监听端口：{tcp_port}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TCP服务器初始化失败：{e.Message}");
        }
    }

    /// <summary>
    /// 监听客户端连接
    /// </summary>
    private void StartServer()
    {
        while (true)
        {
            try
            {
                // 阻塞等待客户端连接
                Socket clientSocket = serverSocket.Accept();
                Debug.Log($"客户端已连接：{clientSocket.RemoteEndPoint}");

                // 开启线程处理该客户端的消息接收
                Thread receiveThread = new Thread(() => SocketReceive(clientSocket));
                receiveThread.IsBackground = true;
                receiveThread.Start();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"客户端连接失败：{e.Message}");
                break;
            }
        }
    }

    /// <summary>
    /// 接收客户端消息
    /// </summary>
    /// <param name="clientSocket">客户端Socket</param>
    private void SocketReceive(Socket clientSocket)
    {
        while (clientSocket.Connected)
        {
            try
            {
                recvLen = clientSocket.Receive(recvData);
                if (recvLen <= 0)
                {
                    Debug.Log($"客户端断开连接：{clientSocket.RemoteEndPoint}");
                    clientSocket.Close();
                    break;
                }

                // 解析消息并标记为新消息
                msg = Encoding.UTF8.GetString(recvData, 0, recvLen);
                hasNewMessage = true;
                Debug.Log($"收到消息：{msg}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"接收消息出错：{e.Message}");
                clientSocket.Close();
                break;
            }
        }
    }

    /// <summary>
    /// 重置新消息标记（处理完消息后调用）
    /// </summary>
    public void ResetNewMessageFlag()
    {
        hasNewMessage = false;
    }

    /// <summary>
    /// 关闭TCP服务器
    /// </summary>
    private void SocketQuit()
    {
        if (serverSocket != null && serverSocket.Connected)
        {
            serverSocket.Close();
        }
        Debug.Log("TCP服务器已关闭");
    }
}