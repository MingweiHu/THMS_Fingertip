using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class Manager2 : MonoBehaviour
{
    [Header("TCP基础配置")]
    [Tooltip("拖入场景中的TCP Server脚本挂载对象（接收Python信号）")]
    public TcpServer tcpServer;
    [Tooltip("Python端IP地址（发送实验序号）")]
    public string pythonIp = "127.0.0.1";
    [Tooltip("Python端接收实验序号的端口")]
    public int pythonExperimentPort = 8687;
    [Tooltip("TCP连接超时时间（毫秒）")] // 新增：连接超时配置
    public int tcpConnectTimeout = 1000;

    [Header("按钮配置")]
    public Button btn1;
    public Button btn2;
    public Button btn3;
    public Button btn4;
    public Button btn5;
    [Tooltip("按钮触发后透明度（0-1）")]
    public float targetAlpha = 0.5f;
    [Tooltip("按钮高亮颜色")]
    public Color highlightColor = new Color(0.8f, 0.8f, 0.8f);
    [Tooltip("按钮反馈持续时间（秒）")]
    public float feedbackDuration = 0.3f;
    [Tooltip("信号锁定时间（秒）")]
    public float lockDuration = 0.5f;

    [Header("音量控制")]
    public TextMeshProUGUI volumeText;
    private int currentVolume = 50;
    private const int VOLUME_STEP = 10;
    private const int MIN_VOLUME = 0;
    private const int MAX_VOLUME = 100;

    [Header("UI显示")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI experimentText;
    public Image resultCircle;

    [Header("实验核心配置")]
    [Tooltip("实验序列（5/6/7）")]
    public int[] experimentSequence = new int[] { 5, 6, 7 };
    [Tooltip("总实验轮数（每个实验）")]
    public int totalTrials = 50;
    [Tooltip("随机数种子")]
    public int randomSeed = 123;
    [Tooltip("起始实验序号（5/6/7），程序会从该序号开始执行")]
    public int currentExperiment = 5; // 改为public，可在Inspector直接配置
    [Tooltip("被试名称")]
    public string participantName = "participant_001";


    [Header("内部状态（无需修改）")]
    private bool isExperimentRunning = false;
    private bool isSignalLocked = false;
    private int currentTrial = 0;
    private int targetButtonNum = 0;
    private System.Random randomGenerator;
    private Coroutine currentFeedbackCoroutine;
    private const float RESULT_SHOW_TIME = 0.3f;

    // 按钮原始颜色缓存
    private Color btn1OriginalColor;
    private Color btn2OriginalColor;
    private Color btn3OriginalColor;
    private Color btn4OriginalColor;
    private Color btn5OriginalColor;

    // 数据记录
    private string csvPath;
    private float trialStartTime;
    private List<int> targetNumberPool = new List<int>();
    private int poolIndex = 0;

    // TCP客户端（发送实验序号）
    private TcpClient tcpClient;
    private NetworkStream networkStream;

    // 新增：实验序列索引
    private int currentExperimentIndex = 0;
    // 修复：新增实验启动保护锁，防止启动瞬间处理旧消息
    private bool isExperimentInStartup = false;

    void Awake()
    {
        // 初始化随机数
        int seed = randomSeed == 0 ? (int)DateTime.Now.Ticks : randomSeed;
        randomGenerator = new System.Random(seed);
        Debug.Log($"随机数种子初始化：{seed}");

        // 缓存按钮原始颜色
        CacheButtonOriginalColors();

        // 关键修改：根据指定的currentExperiment，找到其在实验序列中的索引
        currentExperimentIndex = GetExperimentIndexInSequence(currentExperiment);
        // 如果指定的起始实验不在序列中，默认用第一个
        if (currentExperimentIndex == -1)
        {
            currentExperimentIndex = 0;
            currentExperiment = experimentSequence[0];
            Debug.LogWarning($"指定的起始实验{currentExperiment}不在实验序列中，默认使用序列第一个：{experimentSequence[0]}");
        }

        // 初始化UI
        InitUI();

        // 初始化目标数字池
        ResetTargetPool();

        // 尝试连接Python TCP服务（提前建立连接）
        TryConnectToPython();
    }

    void Update()
    {
        // 按Enter开始/重置实验（仅当实验未运行时）
        if (Input.GetKeyDown(KeyCode.Return) && !isExperimentRunning)
        {
            StartExperiment();
            return;
        }

        // 实验未运行/启动中/无TCP服务时跳过
        if (!isExperimentRunning || tcpServer == null || isExperimentInStartup) return;

        // 处理TCP信号
        ProcessTcpSignal();
    }

    #region 核心逻辑
    /// <summary>
    /// 初始化UI显示
    /// </summary>
    private void InitUI()
    {
        // 初始化状态文本
        if (statusText != null)
        {
            statusText.text = "Press [Enter]";
            statusText.color = Color.yellow;
        }

        // 初始化实验序号显示
        if (experimentText != null)
        {
            experimentText.text = $"Experiment: {currentExperiment}";
            experimentText.color = Color.blue;
        }

        // 初始化音量显示
        UpdateVolumeDisplay();

        // 初始化结果圆
        if (resultCircle != null)
        {
            resultCircle.color = Color.white;
            resultCircle.enabled = true;
        }

        // 给按钮添加CanvasGroup组件
        AddCanvasGroupToButton(btn1);
        AddCanvasGroupToButton(btn2);
        AddCanvasGroupToButton(btn3);
        AddCanvasGroupToButton(btn4);
        AddCanvasGroupToButton(btn5);
    }

    /// <summary>
    /// 缓存按钮原始颜色
    /// </summary>
    private void CacheButtonOriginalColors()
    {
        if (btn1 != null && btn1.image != null) btn1OriginalColor = btn1.image.color;
        if (btn2 != null && btn2.image != null) btn2OriginalColor = btn2.image.color;
        if (btn3 != null && btn3.image != null) btn3OriginalColor = btn3.image.color;
        if (btn4 != null && btn4.image != null) btn4OriginalColor = btn4.image.color;
        if (btn5 != null && btn5.image != null) btn5OriginalColor = btn5.image.color;
    }

    /// <summary>
    /// 重置目标数字池
    /// </summary>
    private void ResetTargetPool()
    {
        targetNumberPool.Clear();
        poolIndex = 0;

        // 根据实验序号确定有效目标
        List<int> validTargets = GetValidTargets();
        int countPerTarget = totalTrials / validTargets.Count;
        int remainder = totalTrials % validTargets.Count;

        // 填充目标池
        foreach (int target in validTargets)
        {
            for (int i = 0; i < countPerTarget; i++)
            {
                targetNumberPool.Add(target);
            }
        }

        // 补充余数
        for (int i = 0; i < remainder; i++)
        {
            targetNumberPool.Add(validTargets[i]);
        }

        // 打乱顺序
        ShuffleList(targetNumberPool);
        Debug.Log($"目标池初始化完成 | 实验{currentExperiment} | 总数量：{targetNumberPool.Count} | 有效目标：{string.Join(",", validTargets)}");
    }

    /// <summary>
    /// 获取当前实验的有效目标数字
    /// </summary>
    private List<int> GetValidTargets()
    {
        switch (currentExperiment)
        {
            case 5:
                return new List<int> { 1, 2, 3, 4, 5 };
            case 6:
                return new List<int> { 1, 2, 3, 4, 5 };
            case 7:
                return new List<int> { 1, 2, 3, 4, 5 };
            default:
                return new List<int> { 1, 2, 3, 4, 5 };
        }
    }

    /// <summary>
    /// 开始实验（核心修改：确保每次启动必发实验序号到Python）
    /// </summary>
    private void StartExperiment()
    {
        // 修复：标记实验启动中，防止启动瞬间处理旧消息
        isExperimentInStartup = true;

        isExperimentRunning = true;
        currentTrial = 0;
        poolIndex = 0;
        isSignalLocked = false;

        // 修复：启动前清空TCP Server旧消息缓存
        if (tcpServer != null)
        {
            tcpServer.msg = string.Empty;
            tcpServer.hasNewMessage = false;
            Debug.Log("启动实验前已清空TCP Server旧消息缓存");
        }

        // 重置目标池
        ResetTargetPool();

        // 创建CSV文件（每个实验单独生成）
        CreateCSVFile();

        // 生成第一个目标
        GenerateNextTarget();
        currentTrial++;

        // 更新UI
        UpdateStatusText(targetButtonNum.ToString(), Color.white);

        // ========== 关键修改1：强制发送实验序号（带重试） ==========
        bool sendSuccess = SendExperimentNumberToPythonWithRetry(currentExperiment, 3); // 重试3次
        if (sendSuccess)
        {
            Debug.Log($"✅ 实验{currentExperiment}序号已成功发送到Python");
        }
        else
        {
            Debug.LogError($"❌ 实验{currentExperiment}序号发送失败（重试3次后），请检查Python端TCP服务是否启动");
        }

        // 修复：延迟解除启动保护，确保初始化完成
        StartCoroutine(EndStartupProtectionAfterDelay());

        Debug.Log($"实验{currentExperiment}开始 | 被试：{participantName} | 总轮数：{totalTrials}");
    }

    /// <summary>
    /// 延迟解除实验启动保护
    /// </summary>
    private IEnumerator EndStartupProtectionAfterDelay()
    {
        // 短暂延迟0.1秒，确保所有初始化完成
        yield return new WaitForSeconds(0.1f);
        isExperimentInStartup = false;
        Debug.Log("实验启动保护已解除，开始接收TCP消息");
    }

    /// <summary>
    /// 处理TCP信号
    /// </summary>
    private void ProcessTcpSignal()
    {
        // 检查是否有新消息
        if (!tcpServer.hasNewMessage) return;

        // 锁定状态直接丢弃信号
        if (isSignalLocked)
        {
            Debug.LogWarning($"[锁定中] 丢弃信号：{tcpServer.msg}");
            tcpServer.ResetNewMessageFlag();
            return;
        }

        // 解析信号
        string signalStr = tcpServer.msg.Trim();
        tcpServer.ResetNewMessageFlag();

        if (!int.TryParse(signalStr, out int signalNum))
        {
            Debug.LogWarning($"无效信号（非数字）：{signalStr}");
            return;
        }

        // 检查信号是否有效
        if (!IsSignalValid(signalNum))
        {
            Debug.LogWarning($"实验{currentExperiment}无效信号：{signalNum}");
            return;
        }

        // 处理有效信号
        HandleValidSignal(signalNum);
    }

    /// <summary>
    /// 检查信号是否有效
    /// </summary>
    private bool IsSignalValid(int signalNum)
    {
        switch (currentExperiment)
        {
            case 5:
                return signalNum >= 1 && signalNum <= 5;
            case 6:
                return signalNum >= 1 && signalNum <= 5; // 6-9不响应
            case 7:
                // 1-3、5-6有效，其他无效
                return signalNum == 1 || signalNum == 2 || signalNum == 3 ||
                       signalNum == 5 || signalNum == 6;
            default:
                return false;
        }
    }

    /// <summary>
    /// 处理有效信号
    /// </summary>
    private void HandleValidSignal(int signalNum)
    {
        // 立即锁定信号
        isSignalLocked = true;
        StartCoroutine(UnlockSignalAfterDelay());

        // 获取信号对应的按钮和物体编号
        Button targetBtn = GetButtonForSignal(signalNum);
        int mappedObjNum = GetMappedObjectNumber(signalNum);

        if (targetBtn == null || mappedObjNum == -1)
        {
            Debug.LogWarning($"信号{signalNum}无对应按钮");
            return;
        }

        // 按钮反馈
        PlayButtonFeedback(targetBtn);

        // 音量调整
        AdjustVolume(mappedObjNum);

        // 判断对错
        bool isCorrect = mappedObjNum == targetButtonNum;

        // 显示结果反馈
        StartCoroutine(ShowResultFeedback(isCorrect));

        // 计算耗时
        float elapsedTime = Time.realtimeSinceStartup - trialStartTime;
        elapsedTime = Mathf.Max(0f, elapsedTime); // 防止负数

        // 写入CSV（修复IsCorrect值反转问题）
        WriteToCSV(currentTrial, mappedObjNum, isCorrect, elapsedTime);

        // 日志输出
        Debug.Log($"第{currentTrial}轮 | 信号{signalNum}→物体{mappedObjNum} | 目标{targetButtonNum} | {(isCorrect ? "正确" : "错误")} | 耗时：{elapsedTime:F3}s");

        // 生成下一个目标
        GenerateNextTarget();
        currentTrial++;

        // 更新状态文本
        if (currentTrial <= totalTrials)
        {
            UpdateStatusText(targetButtonNum.ToString(), Color.white);
        }
    }

    /// <summary>
    /// 获取信号对应的按钮
    /// </summary>
    private Button GetButtonForSignal(int signalNum)
    {
        switch (currentExperiment)
        {
            case 5:
            case 6:
                switch (signalNum)
                {
                    case 1: return btn1;
                    case 2: return btn2;
                    case 3: return btn3;
                    case 4: return btn4;
                    case 5: return btn5;
                    default: return null;
                }
            case 7:
                switch (signalNum)
                {
                    case 1: return btn1;
                    case 2: return btn2;
                    case 3: return btn3;
                    case 5: return btn4;
                    case 6: return btn5;
                    default: return null;
                }
            default:
                return null;
        }
    }

    /// <summary>
    /// 获取信号映射的物体编号
    /// </summary>
    private int GetMappedObjectNumber(int signalNum)
    {
        switch (currentExperiment)
        {
            case 5:
            case 6:
                return signalNum;
            case 7:
                switch (signalNum)
                {
                    case 1: return 1;
                    case 2: return 2;
                    case 3: return 3;
                    case 5: return 4;
                    case 6: return 5;
                    default: return -1;
                }
            default:
                return -1;
        }
    }

    /// <summary>
    /// 生成下一个目标数字
    /// </summary>
    private void GenerateNextTarget()
    {
        if (poolIndex >= targetNumberPool.Count)
        {
            EndExperiment();
            return;
        }

        targetButtonNum = targetNumberPool[poolIndex];
        poolIndex++;
        trialStartTime = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// 结束当前实验（新增：支持切换到下一个实验）
    /// </summary>
    private void EndExperiment()
    {
        isExperimentRunning = false;

        // 修复：实验结束时清空TCP缓存
        if (tcpServer != null)
        {
            tcpServer.msg = string.Empty;
            tcpServer.hasNewMessage = false;
        }

        // 检查是否有下一个实验
        if (currentExperimentIndex < experimentSequence.Length - 1)
        {
            // 切换到下一个实验
            currentExperimentIndex++;
            currentExperiment = experimentSequence[currentExperimentIndex];
            UpdateStatusText($"Exp {currentExperiment} Ready\nPress [Enter]", Color.cyan);
            experimentText.text = $"Experiment: {currentExperiment}";
            Debug.Log($"实验{experimentSequence[currentExperimentIndex - 1]}结束 | 切换到实验{currentExperiment}，等待Enter开始");
        }
        else
        {
            // 所有实验完成
            UpdateStatusText("All Experiments End", Color.green);
            Debug.Log($"所有实验完成 | 最后数据保存至：{csvPath}");
        }

        // 重置音量
        currentVolume = 50;
        UpdateVolumeDisplay();
    }

    /// <summary>
    /// 辅助方法：根据实验序号找到其在序列中的索引
    /// </summary>
    private int GetExperimentIndexInSequence(int expNum)
    {
        for (int i = 0; i < experimentSequence.Length; i++)
        {
            if (experimentSequence[i] == expNum)
            {
                return i;
            }
        }
        return -1;
    }
    #endregion

    #region 辅助功能
    /// <summary>
    /// 打乱列表顺序
    /// </summary>
    private void ShuffleList(List<int> list)
    {
        int count = list.Count;
        while (count > 1)
        {
            count--;
            int randomIndex = randomGenerator.Next(count + 1);
            int temp = list[randomIndex];
            list[randomIndex] = list[count];
            list[count] = temp;
        }
    }

    /// <summary>
    /// 延时解锁信号
    /// </summary>
    private IEnumerator UnlockSignalAfterDelay()
    {
        yield return new WaitForSecondsRealtime(lockDuration);
        isSignalLocked = false;
    }

    /// <summary>
    /// 播放按钮反馈
    /// </summary>
    private void PlayButtonFeedback(Button btn)
    {
        if (btn == null) return;

        // 停止之前的反馈
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
        }

        currentFeedbackCoroutine = StartCoroutine(ButtonFeedbackCoroutine(btn));
    }

    /// <summary>
    /// 按钮反馈协程
    /// </summary>
    private IEnumerator ButtonFeedbackCoroutine(Button btn)
    {
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        Image img = btn.image;

        if (cg == null || img == null) yield break;

        // 保存原始状态
        float originalAlpha = cg.alpha;
        Color originalColor = img.color;

        // 高亮显示
        cg.alpha = targetAlpha;
        img.color = highlightColor;

        // 等待反馈时间
        yield return new WaitForSeconds(feedbackDuration);

        // 恢复原始状态
        cg.alpha = originalAlpha;
        img.color = originalColor;

        currentFeedbackCoroutine = null;
    }

    /// <summary>
    /// 显示结果反馈（红绿圆）
    /// </summary>
    private IEnumerator ShowResultFeedback(bool isCorrect)
    {
        if (resultCircle == null) yield break;

        Color originalColor = resultCircle.color;
        resultCircle.color = isCorrect ? Color.green : Color.red;

        yield return new WaitForSeconds(RESULT_SHOW_TIME);

        resultCircle.color = originalColor;
    }

    /// <summary>
    /// 调整音量
    /// </summary>
    private void AdjustVolume(int objNum)
    {
        if (objNum == 4)
        {
            currentVolume = Mathf.Max(MIN_VOLUME, currentVolume - VOLUME_STEP);
            UpdateVolumeDisplay();
        }
        else if (objNum == 5)
        {
            currentVolume = Mathf.Min(MAX_VOLUME, currentVolume + VOLUME_STEP);
            UpdateVolumeDisplay();
        }
    }

    /// <summary>
    /// 更新音量显示
    /// </summary>
    private void UpdateVolumeDisplay()
    {
        if (volumeText != null)
        {
            volumeText.text = $"Volume: {currentVolume}%";
        }
    }

    /// <summary>
    /// 更新状态文本
    /// </summary>
    private void UpdateStatusText(string text, Color color)
    {
        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = color;
        }
    }

    /// <summary>
    /// 给按钮添加CanvasGroup组件
    /// </summary>
    private void AddCanvasGroupToButton(Button btn)
    {
        if (btn == null) return;

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = btn.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.alpha = 1f;
        }
    }
    #endregion

    #region 数据记录
    /// <summary>
    /// 创建CSV文件
    /// </summary>
    private void CreateCSVFile()
    {
        // 创建目录
        string rootPath = Path.Combine(Application.dataPath, "DataRecord");
        string participantPath = Path.Combine(rootPath, participantName);

        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }

        if (!Directory.Exists(participantPath))
        {
            Directory.CreateDirectory(participantPath);
        }

        // 生成文件名
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{participantName}_{currentExperiment}_mp3_{timestamp}.csv";
        csvPath = Path.Combine(participantPath, fileName);

        // 写入表头
        string header = "Trial,Experiment,Target,Selected,IsCorrect,ElapsedTime(s)\n";
        File.WriteAllText(csvPath, header, Encoding.UTF8);

        Debug.Log($"CSV文件创建成功：{csvPath}");
    }

    /// <summary>
    /// 写入CSV数据（修复IsCorrect值反转问题）
    /// </summary>
    private void WriteToCSV(int trial, int selected, bool isCorrect, float elapsedTime)
    {
        if (!File.Exists(csvPath)) return;

        // 修复：正确时IsCorrect=0，错误时=1（原逻辑是反过来的）
        int isCorrectInt = isCorrect ? 0 : 1;
        string row = $"{trial},{currentExperiment},{targetButtonNum},{selected},{isCorrectInt},{elapsedTime:F3}\n";

        // 追加写入
        File.AppendAllText(csvPath, row, Encoding.UTF8);
    }
    #endregion

    #region TCP通信（发送实验序号）- 核心修改区域
    /// <summary>
    /// 同步尝试连接Python（带超时）
    /// </summary>
    private bool TryConnectToPythonSync()
    {
        try
        {
            // 关闭旧连接
            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }

            tcpClient = new TcpClient();
            // 同步连接（带超时）
            IAsyncResult result = tcpClient.BeginConnect(pythonIp, pythonExperimentPort, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(tcpConnectTimeout, true);

            if (success && tcpClient.Connected)
            {
                tcpClient.EndConnect(result);
                networkStream = tcpClient.GetStream();
                Debug.Log($"✅ 同步连接Python成功：{pythonIp}:{pythonExperimentPort}");
                return true;
            }
            else
            {
                tcpClient.Close();
                tcpClient = null;
                Debug.LogWarning($"❌ 同步连接Python超时/失败（超时：{tcpConnectTimeout}ms）");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"❌ 同步连接Python异常：{e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 异步尝试连接Python（备用）
    /// </summary>
    private void TryConnectToPython()
    {
        try
        {
            if (tcpClient != null && tcpClient.Connected) return;

            tcpClient = new TcpClient();
            tcpClient.BeginConnect(pythonIp, pythonExperimentPort, ConnectCallback, tcpClient);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"❌ 异步连接Python异常：{e.Message}");
        }
    }

    /// <summary>
    /// 连接回调
    /// </summary>
    private void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            TcpClient client = (TcpClient)ar.AsyncState;
            client.EndConnect(ar);
            networkStream = client.GetStream();
            Debug.Log($"✅ 异步连接Python成功：{pythonIp}:{pythonExperimentPort}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"❌ 异步连接Python失败：{e.Message}");
        }
    }

    /// <summary>
    /// 发送实验序号到Python（基础版）
    /// </summary>
    private bool SendExperimentNumberToPython(int expNum)
    {
        try
        {
            // 检查连接状态
            if (tcpClient == null || !tcpClient.Connected || networkStream == null)
            {
                Debug.LogWarning("❌ TCP未连接，尝试同步重连...");
                // 尝试同步重连
                if (!TryConnectToPythonSync())
                {
                    return false;
                }
            }

            // 发送数据（UTF8编码，末尾加换行符方便Python解析）
            string sendStr = expNum.ToString() + "\n";
            byte[] data = Encoding.UTF8.GetBytes(sendStr);
            networkStream.Write(data, 0, data.Length);
            networkStream.Flush(); // 强制刷新缓冲区
            Debug.Log($"📤 已发送实验序号：{expNum}（内容：{sendStr.Trim()}）");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 发送实验序号异常：{e.Message}");
            // 重置连接
            if (networkStream != null) networkStream.Close();
            if (tcpClient != null) tcpClient.Close();
            tcpClient = null;
            networkStream = null;
            return false;
        }
    }

    /// <summary>
    /// 发送实验序号到Python（带重试）- 每次Enter必调这个
    /// </summary>
    private bool SendExperimentNumberToPythonWithRetry(int expNum, int retryCount)
    {
        // 重试逻辑
        for (int i = 0; i < retryCount; i++)
        {
            if (SendExperimentNumberToPython(expNum))
            {
                return true;
            }
            Debug.LogWarning($"⚠️ 第{i + 1}次发送失败，{(i < retryCount - 1 ? "重试中..." : "重试结束")}");
            // 重试间隔
            System.Threading.Thread.Sleep(100);
        }
        return false;
    }
    #endregion

    #region 公开方法（供UI调用）
    /// <summary>
    /// 重置实验（可绑定到UI按钮）
    /// </summary>
    public void ResetExperiment()
    {
        isExperimentRunning = false;
        currentTrial = 0;
        poolIndex = 0;
        isSignalLocked = false;
        // 修复：重置时也清空启动保护
        isExperimentInStartup = false;

        // 重置实验序列索引到起始实验的位置
        currentExperimentIndex = GetExperimentIndexInSequence(currentExperiment);
        if (currentExperimentIndex == -1)
        {
            currentExperimentIndex = 0;
            currentExperiment = experimentSequence[0];
        }

        // 重置按钮状态
        ResetButtonState(btn1, btn1OriginalColor);
        ResetButtonState(btn2, btn2OriginalColor);
        ResetButtonState(btn3, btn3OriginalColor);
        ResetButtonState(btn4, btn4OriginalColor);
        ResetButtonState(btn5, btn5OriginalColor);

        // 重置结果圆
        if (resultCircle != null)
        {
            resultCircle.color = Color.white;
        }

        // 重置音量
        currentVolume = 50;
        UpdateVolumeDisplay();

        // 重置UI
        UpdateStatusText("Press [Enter] to Start", Color.yellow);
        if (experimentText != null)
        {
            experimentText.text = $"Experiment: {currentExperiment}";
        }

        // 重置目标池
        ResetTargetPool();

        // 修复：重置时清空TCP缓存
        if (tcpServer != null)
        {
            tcpServer.msg = string.Empty;
            tcpServer.hasNewMessage = false;
        }
    }

    /// <summary>
    /// 重置按钮状态
    /// </summary>
    private void ResetButtonState(Button btn, Color originalColor)
    {
        if (btn == null) return;

        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
        }

        if (btn.image != null)
        {
            btn.image.color = originalColor;
        }
    }
    #endregion

    /// <summary>
    /// 应用退出时清理资源
    /// </summary>
    private void OnApplicationQuit()
    {
        // 停止协程
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
        }

        // 关闭TCP连接
        if (networkStream != null)
        {
            networkStream.Close();
        }

        if (tcpClient != null)
        {
            tcpClient.Close();
        }
    }
}