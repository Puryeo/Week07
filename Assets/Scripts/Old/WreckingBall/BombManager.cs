using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 씬에 존재하는 폭탄 및 Draggable 오브젝트의 개수를 관리하는 싱글톤 매니저입니다.
/// UI 등에서 현재 남은 폭탄 개수 및 트리거된 Draggable 개수를 확인할 때 사용합니다.
/// StageConfig와 연동하여 목표 폭탄 관리 기능을 제공합니다.
/// </summary>
public class BombManager : MonoBehaviour
{
    private static BombManager instance;
    private static bool isQuitting = false;

    public static BombManager Instance
    {
        get
        {
            // Don't create new instance if application is quitting or during scene unload
            if (isQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                instance = FindFirstObjectByType<BombManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("BombManager");
                    instance = obj.AddComponent<BombManager>();
                }
            }
            return instance;
        }
    }

    [Header("Bomb Settings")]
    [Tooltip("폭탄으로 인식할 GameObject의 태그입니다.")]
    [SerializeField] private string bombTag = "Bomb";

    [Header("Draggable Settings")]
    [Tooltip("Draggable로 인식할 GameObject의 태그입니다.")]
    [SerializeField] private string draggableTag = "Draggable";

    [Header("Debug Settings")]
    [Tooltip("폭탄 개수 변경 시 자동으로 로그를 출력합니다.")]
    [SerializeField] private bool enableAutoDebugLog = true;

    private int lastActiveBombCount = -1;
    private HashSet<GameObject> triggeredDraggables = new HashSet<GameObject>();

    // 목표 폭탄 등록 시스템
    private HashSet<GameObject> registeredGoalBombs = new HashSet<GameObject>();

    // 폭탄 개수 변경 이벤트
    public event Action<int> OnBombCountChanged;
    public event Action<int> OnDraggableCountChanged;

    /// <summary>
    /// 트리거된 Draggable 오브젝트의 개수를 반환합니다.
    /// </summary>
    public int TriggeredDraggableCount => triggeredDraggables.Count;

    /// <summary>
    /// 등록된 목표 폭탄의 개수를 반환합니다.
    /// </summary>
    public int RegisteredGoalBombCount => registeredGoalBombs.Count;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            isQuitting = false;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
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

    private void Start()
    {
        // 초기 상태 로그
        if (enableAutoDebugLog)
        {
            LogBombStatus("게임 시작");
            lastActiveBombCount = GetActiveBombCount();
        }
    }

    private void Update()
    {
        // 매 프레임 폭탄 개수 변경 감지
        if (enableAutoDebugLog)
        {
            int currentCount = GetActiveBombCount();
            if (currentCount != lastActiveBombCount)
            {
                LogBombStatus("폭탄 개수 변경 감지");
                lastActiveBombCount = currentCount;

                // 폭탄 개수 변경 이벤트 발생
                OnBombCountChanged?.Invoke(currentCount);
            }
        }
    }

    #region Draggable Management

    /// <summary>
    /// Draggable 오브젝트가 트리거되었음을 알립니다.
    /// </summary>
    /// <param name="draggable">트리거된 Draggable 오브젝트</param>
    public void NotifyDraggableTriggered(GameObject draggable)
    {
        if (draggable != null && !triggeredDraggables.Contains(draggable))
        {
            triggeredDraggables.Add(draggable);

            if (enableAutoDebugLog)
            {
                int active = GetActiveDraggableCount();
                int triggered = TriggeredDraggableCount;

                Debug.Log($"<color=cyan>[BombManager]</color> [Draggable 트리거 감지] 활성 Draggable: <color=green>{active}</color> | " +
                          $"트리거된 개수: <color=orange>{triggered}</color>");

                // Draggable 개수 변경 이벤트 발생
                OnDraggableCountChanged?.Invoke(triggered);
            }
        }
    }

    /// <summary>
    /// 트리거된 Draggable 카운트를 초기화합니다.
    /// </summary>
    public void ResetDraggableCount()
    {
        triggeredDraggables.Clear();

        if (enableAutoDebugLog)
        {
            Debug.Log($"<color=cyan>[BombManager]</color> Draggable 카운트 초기화됨.");
        }
    }

    #endregion

    #region Basic Bomb Counting (씬 전체 기준)

    /// <summary>
    /// 현재 씬에 존재하는 활성화된 폭탄의 개수를 반환합니다.
    /// </summary>
    public int GetActiveBombCount()
    {
        GameObject[] bombs = GameObject.FindGameObjectsWithTag(bombTag);

        if (bombs == null || bombs.Length == 0)
        {
            return 0;
        }

        int activeCount = 0;
        foreach (var bomb in bombs)
        {
            if (bomb != null && bomb.activeInHierarchy)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    /// <summary>
    /// 현재 씬에 존재하는 전체 폭탄의 개수를 반환합니다. (비활성화 포함)
    /// </summary>
    public int GetTotalBombCount()
    {
        GameObject[] bombs = GameObject.FindGameObjectsWithTag(bombTag);
        return bombs != null ? bombs.Length : 0;
    }

    /// <summary>
    /// 폭발한 폭탄의 개수를 반환합니다.
    /// </summary>
    public int GetExplodedBombCount()
    {
        return GetTotalBombCount() - GetActiveBombCount();
    }

    #endregion

    #region Goal Bomb Registration System (목표 폭탄 등록 시스템)

    /// <summary>
    /// 폭탄을 목표 폭탄으로 등록합니다.
    /// 동적으로 생성되는 폭탄이나 특정 폭탄만 목표로 삼을 때 사용합니다.
    /// </summary>
    /// <param name="bomb">등록할 폭탄 GameObject</param>
    public void RegisterGoalBomb(GameObject bomb)
    {
        if (bomb == null)
        {
            Debug.LogWarning("[BombManager] 등록하려는 폭탄이 null입니다.");
            return;
        }

        if (!bomb.CompareTag(bombTag))
        {
            Debug.LogWarning($"[BombManager] {bomb.name}은(는) '{bombTag}' 태그가 아닙니다.");
            return;
        }

        if (registeredGoalBombs.Add(bomb))
        {
            if (enableAutoDebugLog)
            {
                Debug.Log($"<color=yellow>[BombManager]</color> 목표 폭탄 등록: {bomb.name} | 총 등록 개수: {registeredGoalBombs.Count}");
            }
        }
    }

    /// <summary>
    /// 폭탄을 목표 폭탄에서 제거합니다.
    /// </summary>
    /// <param name="bomb">제거할 폭탄 GameObject</param>
    public void UnregisterGoalBomb(GameObject bomb)
    {
        if (bomb == null) return;

        if (registeredGoalBombs.Remove(bomb))
        {
            if (enableAutoDebugLog)
            {
                Debug.Log($"<color=yellow>[BombManager]</color> 목표 폭탄 제거: {bomb.name} | 총 등록 개수: {registeredGoalBombs.Count}");
            }
        }
    }

    /// <summary>
    /// 등록된 목표 폭탄의 총 개수를 반환합니다.
    /// </summary>
    public int GetRegisteredGoalBombCount()
    {
        // null 체크 및 정리
        registeredGoalBombs.RemoveWhere(bomb => bomb == null);
        return registeredGoalBombs.Count;
    }

    /// <summary>
    /// 등록된 목표 폭탄 중 현재 활성화된 폭탄의 개수를 반환합니다.
    /// </summary>
    public int GetActiveRegisteredGoalBombCount()
    {
        // null 체크 및 정리
        registeredGoalBombs.RemoveWhere(bomb => bomb == null);

        int activeCount = 0;
        foreach (var bomb in registeredGoalBombs)
        {
            if (bomb != null && bomb.activeInHierarchy)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    /// <summary>
    /// 등록된 모든 목표 폭탄을 초기화합니다.
    /// </summary>
    public void ClearRegisteredGoalBombs()
    {
        registeredGoalBombs.Clear();

        if (enableAutoDebugLog)
        {
            Debug.Log($"<color=yellow>[BombManager]</color> 등록된 목표 폭탄 전체 초기화됨.");
        }
    }

    #endregion

    #region Draggable Counting

    /// <summary>
    /// 현재 씬에 존재하는 활성화된 Draggable 오브젝트의 개수를 반환합니다.
    /// </summary>
    public int GetActiveDraggableCount()
    {
        GameObject[] draggables = GameObject.FindGameObjectsWithTag(draggableTag);

        if (draggables == null || draggables.Length == 0)
        {
            return 0;
        }

        int activeCount = 0;
        foreach (var draggable in draggables)
        {
            if (draggable != null && draggable.activeInHierarchy)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    #endregion

    #region Debug Logging

    /// <summary>
    /// 폭탄 상태를 로그로 출력합니다.
    /// </summary>
    /// <param name="eventName">이벤트 이름 (예: "폭발", "게임 시작")</param>
    public void LogBombStatus(string eventName = "")
    {
        int total = GetTotalBombCount();
        int active = GetActiveBombCount();
        int exploded = GetExplodedBombCount();

        string prefix = string.IsNullOrEmpty(eventName) ? "" : $"[{eventName}] ";
        Debug.Log($"<color=yellow>[BombManager]</color> {prefix}전체: <color=cyan>{total}</color> | " +
                  $"남은 폭탄: <color=green>{active}</color> | " +
                  $"폭발한 폭탄: <color=red>{exploded}</color>");
    }

    /// <summary>
    /// Draggable 상태를 로그로 출력합니다.
    /// </summary>
    /// <param name="eventName">이벤트 이름</param>
    public void LogDraggableStatus(string eventName = "")
    {
        int active = GetActiveDraggableCount();
        int triggered = TriggeredDraggableCount;

        string prefix = string.IsNullOrEmpty(eventName) ? "" : $"[{eventName}] ";
        Debug.Log($"<color=cyan>[BombManager]</color> {prefix}활성 Draggable: <color=green>{active}</color> | " +
                  $"트리거된 개수: <color=orange>{triggered}</color>");
    }

    /// <summary>
    /// 등록된 목표 폭탄 상태를 로그로 출력합니다.
    /// </summary>
    /// <param name="eventName">이벤트 이름</param>
    public void LogGoalBombStatus(string eventName = "")
    {
        int registered = GetRegisteredGoalBombCount();
        int active = GetActiveRegisteredGoalBombCount();

        string prefix = string.IsNullOrEmpty(eventName) ? "" : $"[{eventName}] ";
        Debug.Log($"<color=yellow>[BombManager]</color> {prefix}등록된 목표 폭탄: <color=cyan>{registered}</color> | " +
                  $"활성 상태: <color=green>{active}</color>");
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("폭탄 상태 확인")]
    public void DebugBombStatus()
    {
        LogBombStatus("수동 확인");
    }

    [ContextMenu("Draggable 상태 확인")]
    public void DebugDraggableStatus()
    {
        LogDraggableStatus("수동 확인");
    }

    [ContextMenu("목표 폭탄 상태 확인")]
    public void DebugGoalBombStatus()
    {
        LogGoalBombStatus("수동 확인");
    }
#endif
}