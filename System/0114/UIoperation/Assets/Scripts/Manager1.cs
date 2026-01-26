using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using System.Text;
using System.Net.Sockets;

public class Manager1 : MonoBehaviour
{
    [Header("TCP相关")]
    [Tooltip("拖入场景中的TCP Server脚本挂载对象")]
    public TcpServer tcpServer;
    [Tooltip("Python端TCP服务地址（发送实验序号用）")]
    public string pythonTcpIp = "127.0.0.1";
    [Tooltip("Python端TCP服务端口（发送实验序号用）")]
    public int pythonTcpPort = 8687;
    private TcpClient tcpClientToPython;
    private NetworkStream networkStream;

    [Header("按钮映射与反馈配置")]
    public Button btn1; // 物体1对应按钮
    public Button btn2; // 物体2对应按钮
    public Button btn3; // 物体3对应按钮
    public float targetAlpha = 0.5f;
    public Color btnHighlightColor = new Color(0.8f, 0.8f, 0.8f);
    public float feedbackDuration = 0.3f;
    public float repeatCoolDownTime = 0.5f;


    [Header("实验流程配置")]
    public TextMeshProUGUI statusText; // 显示目标数字
    public TextMeshProUGUI experimentText; // 显示当前实验序号
    public Image resultCircle;
    public int totalTrialsPerExperiment = 50; // 每个实验固定50轮
    public int randomSeed = 123;
    [Tooltip("当前实验序号（可修改，1-4），无需固定从实验1开始")]
    public int currentExperiment = 1; // 改为Public，支持Inspector修改
    private int totalExperiments = 4; // 总实验数
    public string participantName = "participantName";

    [Header("内部状态")]
    private bool isExperimentStarted = false;
    private int currentTrialNumber = 0;
    private int targetNumber = 0;
    private Color originalTextColor;
    private System.Random random;
    private Color btnOriginalColor1, btnOriginalColor2, btnOriginalColor3;
    private Coroutine currentFeedbackCoroutine;
    private const float RESULT_HINT_DURATION = 0.3f;
    private Dictionary<int, Button> numberButtonMap; // 动态信号映射表
    private bool isResponseLocked = false;
    private float lastProcessTime = 0f;
    private string csvFilePath;
    private float trialStartTime; // 改用Time.realtimeSinceStartup计时，避免时间缩放影响
    private List<int> targetNumberPool;
    private int poolIndex = 0;
    // 新增：实验启动保护锁，防止启动瞬间处理旧消息
    private bool isExperimentInStartup = false;

    void Awake()
    {
        // 限制实验序号范围（避免输入无效值）
        currentExperiment = Mathf.Clamp(currentExperiment, 1, totalExperiments);

        // 初始化随机数
        int seed = randomSeed == 0 ? (int)DateTime.Now.Ticks : randomSeed;
        random = new System.Random(seed);
        Debug.Log($"随机数种子：{seed}");

        // 初始化按钮CanvasGroup
        AddCanvasGroupIfMissing(btn1);
        AddCanvasGroupIfMissing(btn2);
        AddCanvasGroupIfMissing(btn3);

        // 记录按钮原始颜色
        if (btn1 != null && btn1.image != null) btnOriginalColor1 = btn1.image.color;
        if (btn2 != null && btn2.image != null) btnOriginalColor2 = btn2.image.color;
        if (btn3 != null && btn3.image != null) btnOriginalColor3 = btn3.image.color;

        // 初始化文本
        if (statusText != null)
        {
            originalTextColor = statusText.color;
            InitStartHint();
        }
        if (experimentText != null)
        {
            UpdateExperimentText(); // 显示初始实验序号
        }

        // 初始化圆形图标
        if (resultCircle != null)
        {
            resultCircle.color = Color.white;
            resultCircle.enabled = true;
        }

        // 初始化TCP客户端（向Python发实验序号）
        ConnectToPythonTcp();
    }

    void Update()
    {
        // 实验未开始时，按Enter启动当前实验
        if (!isExperimentStarted && Input.GetKeyDown(KeyCode.Return))
        {
            StartCurrentExperiment();
        }

        // 实验未开始/无TCP服务/实验正在启动中，不处理消息
        if (!isExperimentStarted || tcpServer == null || isExperimentInStartup) return;

        // 处理Python发来的TCP消息
        if (tcpServer.hasNewMessage)
        {
            string newMsg = tcpServer.msg;
            tcpServer.ResetNewMessageFlag();
            ProcessReceivedMessage(newMsg);
        }
    }

    #region 实验流程控制
    // 初始化当前实验的开始提示
    private void InitStartHint()
    {
        statusText.text = $"Press [Enter]";
        statusText.color = Color.yellow;
    }

    // 更新实验序号显示文本
    private void UpdateExperimentText()
    {
        experimentText.text = $"Experiment: {currentExperiment}";
        experimentText.color = Color.blue;
    }

    // 启动当前实验
    private void StartCurrentExperiment()
    {
        // 标记实验正在启动，防止启动过程中处理消息
        isExperimentInStartup = true;

        // 再次限制实验序号范围（防止运行中修改无效值）
        currentExperiment = Mathf.Clamp(currentExperiment, 1, totalExperiments);

        isExperimentStarted = true;
        currentTrialNumber = 0;
        poolIndex = 0;
        isResponseLocked = false;

        // 关键修复1：清空TCP Server的旧消息，重置消息标志
        if (tcpServer != null)
        {
            tcpServer.msg = string.Empty;
            tcpServer.hasNewMessage = false;
            Debug.Log("启动实验前已清空TCP Server旧消息缓存");
        }

        // 初始化当前实验的信号映射表
        InitExperimentSignalMap();
        // 初始化随机数池
        InitTargetNumberPool();
        // 生成第一个目标数字（赋值真实开始时间）
        GenerateNewTargetNumber();
        currentTrialNumber++;
        UpdateStatusText(targetNumber.ToString(), Color.white);

        // 创建当前实验的CSV文件
        CreateCSVFile();
        // 向Python发送当前实验序号
        SendExperimentNumberToPython(currentExperiment);

        // 关键修复2：延迟解锁启动保护，确保启动完成后再处理消息
        StartCoroutine(EndStartupProtectionAfterShortDelay());

        Debug.Log($"实验{currentExperiment}开始，总轮数：{totalTrialsPerExperiment}");
    }

    // 延迟结束启动保护（确保启动流程完全完成）
    private IEnumerator EndStartupProtectionAfterShortDelay()
    {
        // 短暂延迟0.1秒，确保所有初始化完成
        yield return new WaitForSeconds(0.1f);
        isExperimentInStartup = false;
        Debug.Log("实验启动保护已解除，开始接收TCP消息");
    }

    // 初始化当前实验的信号映射规则
    private void InitExperimentSignalMap()
    {
        numberButtonMap = new Dictionary<int, Button>();
        switch (currentExperiment)
        {
            case 1:
                // 1/2→物体1，3→物体2，4→物体3
                numberButtonMap.Add(1, btn1);
                numberButtonMap.Add(2, btn1);
                numberButtonMap.Add(3, btn2);
                numberButtonMap.Add(4, btn3);
                break;
            case 2:
                // 1→物体1，2→无响应，3→物体2，4→物体3
                numberButtonMap.Add(1, btn1);
                numberButtonMap.Add(3, btn2);
                numberButtonMap.Add(4, btn3);
                break;
            case 3:
                // 1→物体1，4→物体2，5→物体3，其余无响应
                numberButtonMap.Add(1, btn1);
                numberButtonMap.Add(4, btn2);
                numberButtonMap.Add(5, btn3);
                break;
            case 4:
                // 1→物体1，5→物体2，6→物体3，其余无响应
                numberButtonMap.Add(1, btn1);
                numberButtonMap.Add(5, btn2);
                numberButtonMap.Add(6, btn3);
                break;
        }
        Debug.Log($"实验{currentExperiment}信号映射表初始化完成，有效信号数：{numberButtonMap.Count}");
    }

    // 结束当前实验，进入下一个实验
    private void EndCurrentExperiment()
    {
        isExperimentStarted = false;
        // 实验结束时也清空TCP消息缓存
        if (tcpServer != null)
        {
            tcpServer.msg = string.Empty;
            tcpServer.hasNewMessage = false;
        }
        UpdateStatusText($"Experiment {currentExperiment} Completed! Press [Enter] for next experiment", Color.green);

        // 实验序号+1，判断是否全部完成
        currentExperiment++;
        // 限制实验序号不超过总实验数
        currentExperiment = Mathf.Clamp(currentExperiment, 1, totalExperiments);

        if (currentExperiment > totalExperiments)
        {
            statusText.text = "All Experiments Completed!";
            experimentText.text = "Experiment Finished";
            Debug.Log("所有实验完成！");
            return;
        }

        // 更新实验序号显示，等待下一个实验启动
        UpdateExperimentText();
        InitStartHint();
        Debug.Log($"实验{currentExperiment - 1}完成，等待启动实验{currentExperiment}");
    }
    #endregion

    #region 随机数池与目标数字生成
    private void InitTargetNumberPool()
    {
        targetNumberPool = new List<int>();
        int countPerNumber = totalTrialsPerExperiment / 3;
        int remainder = totalTrialsPerExperiment % 3;

        for (int i = 0; i < countPerNumber; i++)
        {
            targetNumberPool.Add(1);
            targetNumberPool.Add(2);
            targetNumberPool.Add(3);
        }
        for (int i = 0; i < remainder; i++)
        {
            targetNumberPool.Add(i + 1);
        }
        ShuffleList(targetNumberPool);
        Debug.Log($"实验{currentExperiment}随机数池初始化完成，总数量：{targetNumberPool.Count}");
    }

    private void ShuffleList(List<int> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    private void GenerateNewTargetNumber()
    {
        if (poolIndex >= targetNumberPool.Count)
        {
            EndCurrentExperiment();
            return;
        }
        targetNumber = targetNumberPool[poolIndex];
        poolIndex++;
        // 关键修改：改用Time.realtimeSinceStartup（真实时间，不受Time.timeScale影响）
        trialStartTime = Time.realtimeSinceStartup;
    }
    #endregion

    #region TCP通信（向Python发送实验序号）
    private void ConnectToPythonTcp()
    {
        try
        {
            tcpClientToPython = new TcpClient();
            tcpClientToPython.Connect(pythonTcpIp, pythonTcpPort);
            networkStream = tcpClientToPython.GetStream();
            Debug.Log($"成功连接Python TCP服务：{pythonTcpIp}:{pythonTcpPort}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"连接Python TCP失败：{e.Message}，将在发送时重试");
        }
    }

    private void SendExperimentNumberToPython(int experimentNum)
    {
        try
        {
            // 连接断开则重试连接
            if (tcpClientToPython == null || !tcpClientToPython.Connected)
            {
                ConnectToPythonTcp();
            }
            byte[] data = Encoding.UTF8.GetBytes(experimentNum.ToString());
            networkStream.Write(data, 0, data.Length);
            Debug.Log($"向Python发送实验序号：{experimentNum}");
        }
        catch (Exception e)
        {
            Debug.LogError($"发送实验序号失败：{e.Message}");
        }
    }
    #endregion

    #region 数据记录（CSV）
    private void CreateDataDirectory()
    {
        string rootPath = Path.Combine(Application.dataPath, "DataRecord");
        string participantPath = Path.Combine(rootPath, participantName);
        if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
        if (!Directory.Exists(participantPath)) Directory.CreateDirectory(participantPath);
    }

    private void CreateCSVFile()
    {
        CreateDataDirectory();
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{participantName}_{currentExperiment}_recording_{timestamp}.csv";
        string participantPath = Path.Combine(Application.dataPath, "DataRecord", participantName);
        csvFilePath = Path.Combine(participantPath, fileName);

        string header = "Number,Experiment,Instruction,Selected,IsCorrect,ElapsedTime(s)\n";
        File.WriteAllText(csvFilePath, header, Encoding.UTF8);
        Debug.Log($"CSV文件创建成功：{csvFilePath}");
    }

    private void WriteCSVRow(int number, int experiment, int instruction, int selected, int isCorrect, float elapsedTime)
    {
        if (!File.Exists(csvFilePath)) return;
        string row = $"{number},{experiment},{instruction},{selected},{isCorrect},{elapsedTime:F3}\n";
        File.AppendAllText(csvFilePath, row, Encoding.UTF8);
    }
    #endregion

    #region 消息处理与反馈
    private void ProcessReceivedMessage(string msg)
    {
        if (currentTrialNumber > totalTrialsPerExperiment || !isExperimentStarted) return;
        if (isResponseLocked)
        {
            Debug.LogWarning($"锁定状态，丢弃信号：{msg}");
            return;
        }

        if (!int.TryParse(msg.Trim(), out int receivedNumber))
        {
            Debug.LogWarning($"非数字消息，忽略：{msg}");
            return;
        }

        // 检查当前实验的有效信号
        if (!numberButtonMap.ContainsKey(receivedNumber))
        {
            Debug.LogWarning($"实验{currentExperiment}无效信号：{receivedNumber}，忽略");
            return;
        }

        // 锁定响应，启动延时解锁
        isResponseLocked = true;
        StartCoroutine(UnlockAfterDelay());

        // 触发按钮反馈
        Button targetButton = numberButtonMap[receivedNumber];
        TriggerButtonFeedback(targetButton);

        // 判断对错
        int mappedButtonNumber = GetMappedButtonNumber(receivedNumber);
        bool isCorrect = mappedButtonNumber == targetNumber;
        StartCoroutine(ShowCircleResultHint(isCorrect));

        // 关键修改：用真实时间计算耗时，保留3位小数
        float elapsedTime = Time.realtimeSinceStartup - trialStartTime;
        // 防止耗时为负数（极端情况）
        elapsedTime = Mathf.Max(0f, elapsedTime);

        // 写入CSV数据
        WriteCSVRow(
            currentTrialNumber,
            currentExperiment,
            targetNumber,
            mappedButtonNumber,
            isCorrect ? 0 : 1,
            elapsedTime
        );

        Debug.Log($"实验{currentExperiment}第{currentTrialNumber}轮：选择{receivedNumber}→物体{mappedButtonNumber}，目标{targetNumber}，{(isCorrect ? "正确" : "错误")}，耗时{elapsedTime:F3}s");

        // 生成下一个目标数字（更新下一轮开始时间）
        GenerateNewTargetNumber();

        // 更新轮数
        if (isExperimentStarted)
        {
            currentTrialNumber++;
            if (currentTrialNumber <= totalTrialsPerExperiment)
            {
                UpdateStatusText(targetNumber.ToString(), Color.white);
            }
        }
    }

    private int GetMappedButtonNumber(int receivedNumber)
    {
        if (numberButtonMap[receivedNumber] == btn1) return 1;
        if (numberButtonMap[receivedNumber] == btn2) return 2;
        if (numberButtonMap[receivedNumber] == btn3) return 3;
        return -1;
    }

    private IEnumerator UnlockAfterDelay()
    {
        float unlockTime = Time.realtimeSinceStartup + repeatCoolDownTime;
        while (Time.realtimeSinceStartup < unlockTime) yield return null;
        isResponseLocked = false;
    }

    private IEnumerator ShowCircleResultHint(bool isCorrect)
    {
        if (resultCircle == null) yield break;
        Color originalColor = resultCircle.color;
        resultCircle.color = isCorrect ? Color.green : Color.red;
        yield return new WaitForSeconds(RESULT_HINT_DURATION);
        resultCircle.color = originalColor;
    }

    private void TriggerButtonFeedback(Button targetButton)
    {
        if (targetButton == null) return;
        if (currentFeedbackCoroutine != null) StopCoroutine(currentFeedbackCoroutine);
        currentFeedbackCoroutine = StartCoroutine(ButtonFeedbackCoroutine(targetButton));
    }

    private IEnumerator ButtonFeedbackCoroutine(Button button)
    {
        CanvasGroup cg = button.GetComponent<CanvasGroup>();
        Image btnImage = button.GetComponent<Image>();
        if (cg == null || btnImage == null) yield break;

        Color originalColor = btnImage.color;
        float originalAlpha = cg.alpha;

        cg.alpha = targetAlpha;
        btnImage.color = btnHighlightColor;
        yield return new WaitForSeconds(feedbackDuration);

        cg.alpha = originalAlpha;
        btnImage.color = originalColor;
        currentFeedbackCoroutine = null;
    }
    #endregion

    #region 工具方法
    private void AddCanvasGroupIfMissing(Button button)
    {
        if (button == null) return;
        if (button.GetComponent<CanvasGroup>() == null)
        {
            CanvasGroup cg = button.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.alpha = 1f;
        }
    }

    private void UpdateStatusText(string text, Color color)
    {
        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = color;
        }
    }

    private void OnApplicationQuit()
    {
        // 关闭TCP连接
        if (networkStream != null) networkStream.Close();
        if (tcpClientToPython != null) tcpClientToPython.Close();
        if (currentFeedbackCoroutine != null) StopCoroutine(currentFeedbackCoroutine);
    }
    #endregion
}