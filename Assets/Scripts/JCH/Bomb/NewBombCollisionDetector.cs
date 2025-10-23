using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

/// <summary>
/// 폭탄 태그 오브젝트 의 충돌을 감지하고
/// NewBombManager에 폭발을 요청합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class NewBombCollisionDetector : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Tag Settings")]
    [Tooltip("폭발을 트리거할 오브젝트의 태그입니다.")]
    [SerializeField] private string _bombTag = "Bomb";

    [Header("Debug")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private HashSet<GameObject> _triggeredDraggables;
    private Collider _triggerCollider;
    #endregion

    #region Properties
    /// <summary>트리거된 Draggable 오브젝트의 개수를 반환합니다.</summary>
    public int TriggeredDraggableCount => _triggeredDraggables?.Count ?? 0;

    /// <summary>디버그 로깅 활성화 여부</summary>
    public bool IsDebugLogging => _isDebugLogging;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        LateInitialize();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(other.gameObject.tag) || other.gameObject.tag == "Untagged")
            return;

        // Bomb 태그 체크
        if (other.CompareTag(_bombTag))
        {
            GameObject bomb = other.gameObject;
            Vector3 contactWorldPosition = other.ClosestPoint(transform.position);
            HandleBombTrigger(bomb, contactWorldPosition);
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _triggeredDraggables = new HashSet<GameObject>();
        _triggerCollider = GetComponent<Collider>();

        // Collider 검증 및 Trigger 설정 확인
        if (_triggerCollider == null)
        {
            LogError($"{gameObject.name}에 Collider가 없습니다!");
        }
        else if (!_triggerCollider.isTrigger)
        {
            LogWarning($"{gameObject.name}의 Collider가 Trigger로 설정되어 있지 않습니다. 자동으로 Trigger를 활성화합니다.", true);
            _triggerCollider.isTrigger = true;
        }

        Log("초기화 완료: 충돌 감지 시스템 준비됨");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        Log("LateInitialize 완료");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        _triggeredDraggables?.Clear();
        Log("Cleanup: 충돌 감지 시스템 정리 완료");
    }
    #endregion

    #region Public Methods - Draggable Management
    /// <summary>트리거된 Draggable 개수를 초기화합니다.</summary>
    public void ResetDraggableCount()
    {
        // 빈 구현
    }
    #endregion

    #region Private Methods - Trigger Handling
    /// <summary>폭탄 트리거 처리</summary>
    /// <param name="bomb">감지된 폭탄 GameObject</param>
    /// <param name="contactWorldPosition">충돌 지점 월드 좌표</param>
    private void HandleBombTrigger(GameObject bomb, Vector3 contactWorldPosition)
    {
        Log($"{gameObject.name}이(가) 폭탄 {bomb.name}을(를) 감지! 폭발 요청 전송.");

        // NewBombManager에 폭발 요청
        if (NewBombManager.Instance != null)
        {
            NewBombController bombController = bomb.GetComponent<NewBombController>();
            if (bombController != null)
            {
                NewBombManager.Instance.RequestExplosion(bombController);
                Log($"NewBombManager에 폭발 요청: {bomb.name}");
            }
            else
            {
                LogWarning($"{bomb.name}에 NewBombController 컴포넌트가 없습니다!");
            }
        }
        else
        {
            LogError("NewBombManager 인스턴스를 찾을 수 없습니다!");
        }
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.Log($"<color=purple>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.LogWarning($"<color=purple>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력 - 항상 강제 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=purple>[{GetType().Name}]</color> {message}", this);
    }
    #endregion

#if UNITY_EDITOR
    #region Editor Validation
    private void OnValidate()
    {
        // 태그 존재 여부 확인
        if (!IsTagValid(_bombTag))
        {
            LogWarning($"'{_bombTag}' 태그가 Tag Manager에 등록되어 있지 않습니다.", true);
        }
        // Collider가 Trigger인지 확인
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            LogWarning("Collider의 'Is Trigger'를 활성화해야 합니다!", true);
        }
    }

    private bool IsTagValid(string tag)
    {
        try
        {
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 트리거 범위 시각화 (Collider 기준)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);

            // Box Collider
            if (col is BoxCollider boxCol)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            }
            // Sphere Collider
            else if (col is SphereCollider sphereCol)
            {
                Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius * transform.lossyScale.x);
            }
            // Capsule Collider
            else if (col is CapsuleCollider capsuleCol)
            {
                Gizmos.DrawWireSphere(transform.position + capsuleCol.center, capsuleCol.radius * transform.lossyScale.x);
            }
            // 기타 Collider
            else
            {
                Gizmos.DrawWireSphere(transform.position, col.bounds.extents.magnitude);
            }
        }
    }
    #endregion
#endif
}