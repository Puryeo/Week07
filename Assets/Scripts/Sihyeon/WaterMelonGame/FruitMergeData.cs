using UnityEngine;

/// <summary>
/// 수박 게임의 과일 데이터 컴포넌트입니다.
/// 각 과일의 타입, 다음 단계, 합치기 가능 여부 등을 관리합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FruitMergeData : MonoBehaviour
{
    /// <summary>
    /// 과일 종류를 정의하는 열거형입니다.
    /// </summary>
    public enum FruitType
    {
        Grape = 0,      // 포도 (0단계) ← 새로 추가
        Apple = 1,      // 사과 (1단계)
        Orange = 2,     // 오렌지 (2단계)
        Lemon = 3,      // 레몬 (3단계)
        Melon = 4,      // 멜론 (4단계)
        Durian = 5,     // 두리안 (5단계) ← 새로 추가
        Watermelon = 6, // 수박 (6단계)
        Bomb = 7        // 폭탄 (7단계, 최종)
    }
    
    [Header("Fruit Settings")]
    [Tooltip("현재 과일의 종류입니다.")]
    [SerializeField] private FruitType fruitType = FruitType.Grape;
    
    [Tooltip("합쳐졌을 때 생성될 다음 단계 과일의 종류입니다.")]
    [SerializeField] private FruitType nextFruitType = FruitType.Apple;
    
    [Header("Merge Settings")]
    [Tooltip("합치기 가능 여부입니다. 폭탄(최종 단계)은 false로 설정합니다.")]
    [SerializeField] private bool canMerge = true;
    
    [Tooltip("합쳐질 때 생성할 파티클 이펙트 프리팹입니다.")]
    [SerializeField] private GameObject mergeFXPrefab;
    
    [Header("Physics Settings")]
    [Tooltip("과일의 질량입니다. (Rigidbody.mass)")]
    [SerializeField] private float mass = 1f;
    
    [Tooltip("과일의 물리 재질입니다. (튕김, 마찰 등)")]
    [SerializeField] private PhysicsMaterial physicsMaterial;
    
    [Header("Debug")]
    [Tooltip("디버그 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false;
    
    // 합치기 진행 중인지 여부 (중복 방지용)
    private bool isMerging = false;
    
    /// <summary>
    /// 현재 과일의 타입을 반환합니다.
    /// </summary>
    public FruitType CurrentFruitType => fruitType;
    
    /// <summary>
    /// 다음 단계 과일의 타입을 반환합니다.
    /// </summary>
    public FruitType NextFruitType => nextFruitType;
    
    /// <summary>
    /// 합치기 가능 여부를 반환합니다.
    /// </summary>
    public bool CanMerge => canMerge;
    
    /// <summary>
    /// 합치기 이펙트 프리팹을 반환합니다.
    /// </summary>
    public GameObject MergeFXPrefab => mergeFXPrefab;
    
    /// <summary>
    /// 현재 합치기 진행 중인지 여부를 반환합니다.
    /// </summary>
    public bool IsMerging => isMerging;
    
    /// <summary>
    /// 합치기 상태를 설정합니다.
    /// </summary>
    public void SetMerging(bool value)
    {
        isMerging = value;
        
        if (showDebugLogs)
        {
            Debug.Log($"[FruitMergeData] {gameObject.name} 합치기 상태 변경: {value}");
        }
    }
    
    private void Awake()
    {
        InitializePhysics();
    }
    
    /// <summary>
    /// 물리 속성을 초기화합니다.
    /// </summary>
    private void InitializePhysics()
    {
        // Rigidbody 설정
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = mass;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Z축 위치 자유롭게, 회전만 고정 (완전한 3D)
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeData] {gameObject.name} Rigidbody 초기화 완료 (mass: {mass}, 3D 모드)");
            }
        }
        
        // Collider에 PhysicsMaterial 적용 (있으면)
        Collider col = GetComponent<Collider>();
        if (col != null && physicsMaterial != null)
        {
            col.material = physicsMaterial;
            
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeData] {gameObject.name} Collider 초기화 완료 (Type: {col.GetType().Name})");
            }
        }
    }
    
    /// <summary>
    /// 과일을 활성화하고 초기 상태로 리셋합니다.
    /// </summary>
    public void Activate(Vector3 position)
    {
        transform.position = position;
        isMerging = false;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 🔥 수정: isKinematic을 먼저 false로 설정
            rb.isKinematic = false;
            
            // 이제 안전하게 velocity 설정
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
        
        gameObject.SetActive(true);
        
        if (showDebugLogs)
        {
            Debug.Log($"[FruitMergeData] {gameObject.name} 활성화됨 at {position}");
        }
    }
    
    /// <summary>
    /// 과일을 비활성화합니다.
    /// </summary>
    public void Deactivate()
    {
        gameObject.SetActive(false);
        
        if (showDebugLogs)
        {
            Debug.Log($"[FruitMergeData] {gameObject.name} 비활성화됨");
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 폭탄은 합치기 불가능
        if (fruitType == FruitType.Bomb)
        {
            canMerge = false;
        }
        
        // 다음 단계 자동 설정
        if (fruitType != FruitType.Bomb)
        {
            nextFruitType = (FruitType)((int)fruitType + 1);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Collider 범위 시각화
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        
        // 과일 타입 레이블
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, 
            $"{fruitType} (Lv.{(int)fruitType})");
    }
#endif
}