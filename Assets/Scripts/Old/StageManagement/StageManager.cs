using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    #region Public Fields
    public static StageManager Instance { get; private set; }
    public StageDataSO CurrentStageData { get; private set; }
    #endregion

    #region Public Methods
    public void SetStageData(StageDataSO data, List<StageDataSO> datum)
    {
        LogSystem.PushLog(LogLevel.INFO, "StageBegin", data.StageName);
        CurrentStageData = data;
        data.IsTried = true;

        // 전체 Save가 아니라, isTried만 갱신
        StageSaveManager.SaveSingleStage(data);
    }

    // 스테이지 이미지 및 별 업데이트
    public void UpdateClearData(int starCount, string snapShotPath)
    {
        StageSaveManager.UpdateStageData(CurrentStageData, starCount, snapShotPath);
        
        LogSystem.PushLog(LogLevel.INFO, "StageStar", starCount );
        LogSystem.PushLog(LogLevel.INFO, "StageClear", CurrentStageData.StageName);
    }
    #endregion

    #region Private Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion
}