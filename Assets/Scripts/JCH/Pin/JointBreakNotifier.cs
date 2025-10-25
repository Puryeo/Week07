using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// Joint 연결 파괴를 감지하고 통지하는 컴포넌트
/// </summary>
public class JointBreakNotifier : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Debug")]
    [SerializeField] private bool _isDebugLogging = true;
    #endregion

    #region Fields
    [TabGroup("Debug")]
    [ShowInInspector,ReadOnly]
    private List<Joint> _jointList;

    private int _initialJointCount;
    private int _brokenJointCount;
    #endregion

    #region Event
    public event System.Action<int, int> OnJointBreakEvent;
    #endregion

    #region Properties
    /// <summary>디버그 로그 출력 여부</summary>
    public bool IsDebugLogging => _isDebugLogging;

    /// <summary>초기 Joint 개수</summary>
    public int InitialJointCount => _initialJointCount;

    /// <summary>파괴된 Joint 개수</summary>
    public int BrokenJointCount => _brokenJointCount;

    /// <summary>현재 남은 Joint 개수</summary>
    public int RemainingJointCount => _initialJointCount - _brokenJointCount;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void OnJointBreak(float breakForce)
    {
        _brokenJointCount++;
        int remainingCount = RemainingJointCount;

        Log($"Joint 파괴 감지 - 파괴력: {breakForce:F2}, 남은 Joint: {remainingCount}/{_initialJointCount}");

        NotifyJointBreak(remainingCount, _initialJointCount);
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _jointList = new List<Joint>();
        _brokenJointCount = 0;

        // 자신에게 붙은 모든 Joint 컴포넌트 수집
        GetComponents(_jointList);
        _initialJointCount = _jointList.Count;

        Log($"초기화 완료 - 총 Joint 개수: {_initialJointCount}");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        if (_jointList != null)
        {
            _jointList.Clear();
            _jointList = null;
        }

        Log("정리 완료");
    }
    #endregion

    #region Protected Methods - Notification
    /// <summary>Joint 파괴 통지</summary>
    /// <param name="nowCount">현재 남은 Joint 개수</param>
    /// <param name="maxCount">최대 Joint 개수</param>
    protected virtual void NotifyJointBreak(int nowCount, int maxCount)
    {
        Log($"NotifyJointBreak 호출 - 남은 개수: {nowCount}, 최대 개수: {maxCount}");
        OnJointBreakEvent?.Invoke(nowCount, maxCount);
    }
    #endregion

    #region Private Methods - Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (IsDebugLogging || forcely)
            Debug.Log($"<color=orange>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (IsDebugLogging || forcely)
            Debug.LogWarning($"<color=orange>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=orange>[{GetType().Name}]</color> {message}", this);
    }
    #endregion
}