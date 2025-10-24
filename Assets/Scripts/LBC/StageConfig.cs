using UnityEngine;

/// <summary>
/// 스테이지별 설정을 관리하는 스크립트입니다.
/// 각 스테이지 씬마다 1개씩 배치하여 사용합니다.
/// 목표 폭탄 개수, 자동 카운팅 모드 등을 설정할 수 있습니다.
/// </summary>
public class StageConfig : MonoBehaviour
{
    private static StageConfig instance;
    private static bool isQuitting = false;

    /// <summary>
    /// StageConfig의 싱글톤 인스턴스입니다.
    /// 씬마다 1개만 존재해야 합니다.
    /// </summary>
    public static StageConfig Instance
    {
        get
        {
            if (isQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                instance = FindFirstObjectByType<StageConfig>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("StageConfig");
                    instance = obj.AddComponent<StageConfig>();
                }
            }
            return instance;
        }
    }

    [Header("Goal Bomb Settings")]
    [Tooltip("목표 폭탄 개수 설정 모드를 선택합니다.")]
    [SerializeField] private GoalBombMode goalBombMode = GoalBombMode.AutoCount;

    [Tooltip("Manual 모드일 때 목표로 삼을 폭탄 개수입니다.")]
    [SerializeField] private int manualGoalBombCount = 5;

    [Header("Debug Settings")]
    [Tooltip("스테이지 설정 관련 로그를 출력합니다.")]
    [SerializeField] private bool enableDebugLog = true;

    /// <summary>
    /// 목표 폭탄 개수 설정 모드
    /// </summary>
    public enum GoalBombMode
    {
        [Tooltip("씬에 있는 모든 Bomb 태그 오브젝트를 자동으로 목표로 설정합니다.")]
        AutoCount,

        [Tooltip("Inspector에서 설정한 개수를 목표로 사용합니다. 동적 생성 스테이지에 유용합니다.")]
        Manual,

        [Tooltip("BombManager에 등록된 폭탄만 목표로 삼습니다. RegisterGoalBomb()로 등록 필요.")]
        RegisterOnly
    }

    /// <summary>
    /// 현재 설정된 목표 폭탄 개수 모드를 반환합니다.
    /// </summary>
    public GoalBombMode CurrentGoalBombMode => goalBombMode;

    /// <summary>
    /// Manual 모드일 때 설정된 목표 폭탄 개수를 반환합니다.
    /// </summary>
    public int ManualGoalBombCount => manualGoalBombCount;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (instance == null)
        {
            instance = this;
            isQuitting = false;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log($"<color=green>[StageConfig]</color> 스테이지 설정 초기화 완료\n" +
                      $"모드: {goalBombMode} | 수동 목표: {manualGoalBombCount}");
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            isQuitting = true;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    /// <summary>
    /// 현재 스테이지의 목표 폭탄 개수를 반환합니다.
    /// BombManager와 연동하여 모드에 따라 적절한 값을 계산합니다.
    /// </summary>
    /// <returns>목표 폭탄 개수</returns>
    public int GetGoalBombCount()
    {
        if (BombManager.Instance == null)
        {
            Debug.LogWarning("[StageConfig] BombManager를 찾을 수 없습니다.");
            return 0;
        }

        int goalCount = 0;

        switch (goalBombMode)
        {
            case GoalBombMode.AutoCount:
                // 씬에 있는 모든 Bomb 태그 오브젝트를 목표로 설정
                goalCount = BombManager.Instance.GetTotalBombCount();
                break;

            case GoalBombMode.Manual:
                // Inspector에서 설정한 개수 사용
                goalCount = manualGoalBombCount;
                break;

            case GoalBombMode.RegisterOnly:
                // 등록된 폭탄만 카운트
                goalCount = BombManager.Instance.GetRegisteredGoalBombCount();
                break;
        }

        if (enableDebugLog)
        {
            Debug.Log($"<color=green>[StageConfig]</color> 목표 폭탄 개수: {goalCount} (모드: {goalBombMode})");
        }

        return goalCount;
    }

    /// <summary>
    /// 현재 남은 목표 폭탄 개수를 반환합니다.
    /// </summary>
    /// <returns>남은 목표 폭탄 개수</returns>
    public int GetRemainingGoalBombCount()
    {
        if (BombManager.Instance == null)
        {
            return 0;
        }

        int remainingCount = 0;

        switch (goalBombMode)
        {
            case GoalBombMode.AutoCount:
                // 활성화된 모든 폭탄
                remainingCount = BombManager.Instance.GetActiveBombCount();
                break;

            case GoalBombMode.Manual:
                // 수정: Manual 모드도 씬의 전체 폭탄을 카운트
                remainingCount = BombManager.Instance.GetActiveBombCount();
                break;

            case GoalBombMode.RegisterOnly:
                // 등록된 폭탄 중 활성화된 것만
                remainingCount = BombManager.Instance.GetActiveRegisteredGoalBombCount();
                break;
        }

        return remainingCount;
    }

#if UNITY_EDITOR
    [ContextMenu("목표 폭탄 개수 확인")]
    private void DebugGoalBombCount()
    {
        int goalCount = GetGoalBombCount();
        int remainingCount = GetRemainingGoalBombCount();

        Debug.Log($"<color=green>[StageConfig]</color> 목표: {goalCount} | 남은 개수: {remainingCount}");
    }

    private void OnValidate()
    {
        // Manual 모드일 때 목표 개수가 0 이하면 경고
        if (goalBombMode == GoalBombMode.Manual && manualGoalBombCount <= 0)
        {
            Debug.LogWarning("[StageConfig] Manual 모드에서 목표 폭탄 개수는 1 이상이어야 합니다.");
        }
    }
#endif
}