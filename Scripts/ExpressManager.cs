using System.Collections;
using UnityEngine;
using LabFrame2023;
using Wave.Native;
using Wave.OpenXR;

#if USE_PICO
using Unity.XR.PXR;
#endif

public class ExpressManager : LabSingleton<ExpressManager>, IManager
{
    protected bool _doWriteLabData = false;
    protected string _currentExpressTag = "express";

    protected JawData _jawData;
    protected MouthData _mouthData;
    protected CheekData _cheekData;
    protected TongueData _tongueData;
    protected EyeData _eyeData;

#if USE_VIVE_ANDROID
    private float[] lipWeights = new float[37];
    private float[] eyeWeights = new float[14];
    private bool isLipTrackingActive = false;
    private bool isEyeTrackingActive = false;
#endif

    void Start()
    {
        Debug.Log("Data Path: " + Application.persistentDataPath);
        AutoWriteLabData(true);
        ManagerInit();
    }

    void Awake()
    {
        Debug.Log("[ExpressManager] Awake");
    }

#region IManager 實現

    public void ManagerInit()
    {
#if USE_PICO

#elif USE_VIVE_ANDROID
        StartCoroutine(WaitForLabDataManagerAndInit());
#endif
    }

    public IEnumerator ManagerDispose()
    {
        _doWriteLabData = false;
        _jawData = null;
        _mouthData = null;
        _cheekData = null;
        _tongueData = null;
        if (isLipTrackingActive)
        {
            Interop.WVR_StopLipExp();
            isLipTrackingActive = false;
        }
        _eyeData = null;
        if (isEyeTrackingActive)
        {
            InputDeviceEye.ActivateEyeExpression(false);
            isEyeTrackingActive = false;
        }
        yield break;
    }

#endregion

#region 初始化流程

    private IEnumerator WaitForLabDataManagerAndInit()
    {
        int waitCount = 0;
        // 等待 LabDataManager 實例存在
        while (LabDataManager.Instance == null)
        {
            waitCount++;
            if (waitCount % 60 == 0)
                Debug.Log($"[ExpressManager] 等待 LabDataManager 實例... ({waitCount / 60}秒)");
            yield return null;
        }


        // 初始化 LabData 系統
        LabDataManager.Instance.LabDataInit("DefaultUser", "express_exp");
        Debug.Log("[ExpressManager] LabDataManager is ready.");

        // 啟動嘴部追蹤硬件
        if (Interop.WVR_StartLipExp() == WVR_Result.WVR_Success)
        {
            isLipTrackingActive = true;
            Debug.Log("[ExpressManager] Lip tracking started successfully.");
        }
        else
        {
            Debug.LogError("[ExpressManager] Failed to start Lip tracking.");
        }

        // 啟動眼部表情追蹤
        if (InputDeviceEye.IsEyeExpressionAvailable())
        {
            InputDeviceEye.ActivateEyeExpression(true);
            isEyeTrackingActive = true;
            Debug.Log("[ExpressManager] Eye Expression started successfully.");
        }
        else
        {
            Debug.LogError("[ExpressManager] Failed to start Eye Expression.");
        }

        // 初始化數據對象
        _jawData = new JawData();
        _mouthData = new MouthData();
        _cheekData = new CheekData();
        _tongueData = new TongueData();
        _eyeData = new EyeData();
    }

#endregion

#region Unity 生命周期

    void Update()
    {
#if USE_PICO

#elif USE_VIVE_ANDROID
        // 只在 LabDataManager 就續、追蹤激活時才獲取數據
        if (LabDataManager.Instance == null || !LabDataManager.Instance.IsInited)
            return;

        if (isLipTrackingActive)
        {
            var result = Interop.WVR_GetLipExpData(lipWeights);
            if (result == WVR_Result.WVR_Success)
            {
                // 更新已有數據對象
                UpdateJawData();
                UpdateMouthData();
                UpdateCheekData();
                UpdateTongueData();
            }
        }

        if (isEyeTrackingActive && InputDeviceEye.HasEyeExpressionValue())
        {
            for (int i = 0; i < eyeWeights.Length; i++)
            {
                eyeWeights[i] = InputDeviceEye.GetEyeExpressionValue((InputDeviceEye.Expressions)i);
            }
            UpdateEyeData();
        }

        // 僅在需要寫入時才寫入 LabDataManager
        if (_doWriteLabData)
        {
            LabDataManager.Instance.WriteData(_jawData, _currentExpressTag);
            LabDataManager.Instance.WriteData(_mouthData, _currentExpressTag);
            LabDataManager.Instance.WriteData(_cheekData, _currentExpressTag);
            LabDataManager.Instance.WriteData(_tongueData, _currentExpressTag);
            LabDataManager.Instance.WriteData(_eyeData, _currentExpressTag);
        }
#endif
    }

    void OnDestroy()
    {
        if (isLipTrackingActive)
        {
            Interop.WVR_StopLipExp();
            isLipTrackingActive = false;
        }
        if (isEyeTrackingActive)
        {
            InputDeviceEye.ActivateEyeExpression(false);
            isEyeTrackingActive = false;
        }
    }

#endregion

#region 數據更新方法

    private void UpdateJawData()
    {
        _jawData.JawRight = lipWeights[0];
        _jawData.JawLeft = lipWeights[1];
        _jawData.JawForward = lipWeights[2];
        _jawData.JawOpen = lipWeights[3];
    }

    private void UpdateMouthData()
    {
        _mouthData.MouthApeShape = lipWeights[4];
        _mouthData.MouthUpperRight = lipWeights[5];
        _mouthData.MouthUpperLeft = lipWeights[6];
        _mouthData.MouthLowerRight = lipWeights[7];
        _mouthData.MouthLowerLeft = lipWeights[8];
        _mouthData.MouthUpperOverturn = lipWeights[9];
        _mouthData.MouthLowerOverturn = lipWeights[10];
        _mouthData.MouthPout = lipWeights[11];
        _mouthData.MouthRaiserRight = lipWeights[12];
        _mouthData.MouthRaiserLeft = lipWeights[13];
        _mouthData.MouthStretcherRight = lipWeights[14];
        _mouthData.MouthStretcherLeft = lipWeights[15];
        _mouthData.MouthUpperUpRight = lipWeights[19];
        _mouthData.MouthUpperUpLeft = lipWeights[20];
        _mouthData.MouthLowerDownRight = lipWeights[21];
        _mouthData.MouthLowerDownLeft = lipWeights[22];
        _mouthData.MouthUpperInside = lipWeights[23];
        _mouthData.MouthLowerInside = lipWeights[24];
        _mouthData.MouthLowerOverlay = lipWeights[25];
    }

    private void UpdateCheekData()
    {
        _cheekData.CheekPuffRight = lipWeights[16];
        _cheekData.CheekPuffLeft = lipWeights[17];
        _cheekData.CheekSuck = lipWeights[18];
    }

    private void UpdateTongueData()
    {
        _tongueData.TongueLongStep1 = lipWeights[26];
        _tongueData.TongueLeft = lipWeights[27];
        _tongueData.TongueRight = lipWeights[28];
        _tongueData.TongueUp = lipWeights[29];
        _tongueData.TongueDown = lipWeights[30];
        _tongueData.TongueRoll = lipWeights[31];
        _tongueData.TongueLongStep2 = lipWeights[32];
        _tongueData.TongueUpRightMorph = lipWeights[33];
        _tongueData.TongueUpLeftMorph = lipWeights[34];
        _tongueData.TongueDownRightMorph = lipWeights[35];
        _tongueData.TongueDownLeftMorph = lipWeights[36];
    }

    private void UpdateEyeData()
    {
        _eyeData.LeftBlink = eyeWeights[0];
        _eyeData.LeftWide = eyeWeights[1];
        _eyeData.RightBlink = eyeWeights[2];
        _eyeData.RightWide = eyeWeights[3];
        _eyeData.LeftSqueeze = eyeWeights[4];
        _eyeData.RightSqueeze = eyeWeights[5];
        _eyeData.LeftDown = eyeWeights[6];
        _eyeData.RightDown = eyeWeights[7];
        _eyeData.LeftOut = eyeWeights[8];
        _eyeData.RightIn = eyeWeights[9];
        _eyeData.LeftIn = eyeWeights[10];
        _eyeData.RightOut = eyeWeights[11];
        _eyeData.LeftUp = eyeWeights[12];
        _eyeData.RightUp = eyeWeights[13];
    }

#endregion

#region Public Methods

    /// <summary>
    /// 開啟/關閉自動寫入 LabData 數據
    /// </summary>
    /// <param name="enable">是否啟用寫入</param>
    /// <param name="tag">數據標籤</param>
    public void AutoWriteLabData(bool enable = true, string tag = "express")
    {
        _doWriteLabData = enable;
        if (!string.IsNullOrEmpty(tag))
            _currentExpressTag = tag;
        Debug.Log($"[ExpressManager] AutoWriteLabData set to {enable}, tag = {_currentExpressTag}");
    }

    /// <summary>
    /// 動態修改當前數據標籤
    /// </summary>
    /// <param name="tag">新標籤</param>
    public void SetExpressTag(string tag)
    {
        _currentExpressTag = string.IsNullOrEmpty(tag) ? "express" : tag;
        Debug.Log($"[ExpressManager] Express data tag changed to: {_currentExpressTag}");
    }

    public JawData GetJawData() => _jawData;
    public MouthData GetMouthData() => _mouthData;
    public CheekData GetCheekData() => _cheekData;
    public TongueData GetTongueData() => _tongueData;
    public EyeData GetEyeData() => _eyeData;

#endregion
}