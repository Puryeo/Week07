using UnityEngine;
using System.Collections;

/// <summary>
/// 수박 게임의 과일 합치기 로직을 담당하는 컨트롤러입니다.
/// 같은 종류의 과일이 충돌하면 합치기 애니메이션을 재생하고 다음 단계 과일을 생성합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(FruitMergeData))]
public class FruitMergeController : MonoBehaviour
{
    [Header("Merge Animation Settings")]
    [Tooltip("두 과일이 합쳐지는 지점으로 이동하는 속도입니다.")]
    [SerializeField] private float mergeSpeed = 5f;
    
    [Tooltip("합치기 애니메이션 지속 시간(초)입니다.")]
    [SerializeField] private float mergeDuration = 0.3f;
    
    [Tooltip("합치기 애니메이션 커브입니다.")]
    [SerializeField] private AnimationCurve mergeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Collision Settings")]
    [Tooltip("같은 과일과의 충돌 쿨다운 시간(초)입니다. 중복 충돌 방지용입니다.")]
    [SerializeField] private float collisionCooldown = 0.1f;
    
    [Header("Visual Effects")]
    [Tooltip("충돌 시 생성할 VFX 프리팹입니다. (선택사항)")]
    [SerializeField] private GameObject collisionVFXPrefab;
    
    [Tooltip("VFX 자동 소멸 시간(초)입니다.")]
    [SerializeField] private float vfxLifetime = 2.0f;
    
    [Header("Debug Settings")]
    [Tooltip("충돌 및 합치기 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false;
    
    private Rigidbody rb;
    private Collider fruitCollider;
    private FruitMergeData fruitData;
    
    // 충돌 쿨다운 관리
    private GameObject lastCollisionObject;
    private float lastCollisionTime;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fruitCollider = GetComponent<Collider>();
        fruitData = GetComponent<FruitMergeData>();
        
        if (fruitData == null)
        {
            Debug.LogError($"[FruitMergeController] {gameObject.name}에 FruitMergeData 컴포넌트가 없습니다!");
            enabled = false;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // 합치기 진행 중이면 무시
        if (fruitData.IsMerging)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeController] {gameObject.name}은(는) 이미 합치기 진행 중입니다.");
            }
            return;
        }
        
        // 쿨다운 체크 (같은 오브젝트와 연속 충돌 방지)
        if (collision.gameObject == lastCollisionObject && 
            Time.time - lastCollisionTime < collisionCooldown)
        {
            return;
        }
        
        lastCollisionObject = collision.gameObject;
        lastCollisionTime = Time.time;
        
        // 상대방도 과일인지 확인
        FruitMergeController otherController = collision.gameObject.GetComponent<FruitMergeController>();
        
        if (otherController == null)
        {
            // 과일이 아닌 오브젝트와 충돌 (벽, 바닥 등)
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeController] {gameObject.name}이(가) {collision.gameObject.name}에 충돌 (과일 아님)");
            }
            
            // 일반 충돌 VFX
            if (collisionVFXPrefab != null && collision.contacts.Length > 0)
            {
                SpawnCollisionVFX(collision.contacts[0].point);
            }
            
            return;
        }
        
        FruitMergeData otherData = otherController.fruitData;
        
        // 같은 종류의 과일인지 확인
        if (fruitData.CurrentFruitType != otherData.CurrentFruitType)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeController] {gameObject.name}({fruitData.CurrentFruitType})과(와) " +
                         $"{collision.gameObject.name}({otherData.CurrentFruitType})은(는) 종류가 다릅니다.");
            }
            return;
        }
        
        // 합치기 가능 여부 확인
        if (!fruitData.CanMerge || !otherData.CanMerge)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeController] {gameObject.name} 또는 {collision.gameObject.name}은(는) 합치기 불가능합니다.");
            }
            return;
        }
        
        // 이미 상대방이 합치기 진행 중이면 무시
        if (otherData.IsMerging)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeController] {collision.gameObject.name}은(는) 이미 합치기 진행 중입니다.");
            }
            return;
        }
        
        // 중복 방지: 인스턴스 ID가 작은 쪽만 처리
        if (GetInstanceID() > otherController.GetInstanceID())
        {
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeController] {otherController.gameObject.name}이(가) 합치기를 처리합니다.");
            }
            return;
        }
        
        // 합치기 시작
        if (showDebugLogs)
        {
            Debug.Log($"[FruitMergeController] {gameObject.name}과(와) {collision.gameObject.name} 합치기 시작! " +
                     $"타입: {fruitData.CurrentFruitType} → {fruitData.NextFruitType}");
        }
        
        Vector3 contactPoint = collision.contacts.Length > 0 ? 
            collision.contacts[0].point : 
            (transform.position + collision.transform.position) / 2f;
        
        StartCoroutine(MergeCoroutine(otherController, contactPoint));
    }
    
    private IEnumerator MergeCoroutine(FruitMergeController other, Vector3 contactPoint)
    {
        fruitData.SetMerging(true);
        other.fruitData.SetMerging(true);
        
        // 콜라이더 비활성화
        fruitCollider.enabled = false;
        other.fruitCollider.enabled = false;
        
        rb.isKinematic = true;
        other.rb.isKinematic = true;
        
        // Z축 고정 제거 - 중간 지점을 3D로 계산
        Vector3 midPoint = (transform.position + other.transform.position) / 2f;
        
        Vector3 startPos1 = transform.position;
        Vector3 startPos2 = other.transform.position;
        
        float elapsed = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log($"[FruitMergeController] 합치기 애니메이션 시작: {mergeDuration}초 동안 {midPoint}로 이동 (3D)");
        }
        
        // 합치기 애니메이션
        while (elapsed < mergeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / mergeDuration;
            float curveValue = mergeCurve.Evaluate(t);
            
            if (this != null && gameObject.activeSelf)
            {
                transform.position = Vector3.Lerp(startPos1, midPoint, curveValue);
            }
            
            if (other != null && other.gameObject.activeSelf)
            {
                other.transform.position = Vector3.Lerp(startPos2, midPoint, curveValue);
            }
            
            yield return null;
        }
        
        // 최종 위치 설정
        if (this != null && gameObject.activeSelf)
        {
            transform.position = midPoint;
        }
        
        if (other != null && other.gameObject.activeSelf)
        {
            other.transform.position = midPoint;
        }
        
        // 이펙트 생성
        GameObject mergeFX = fruitData.MergeFXPrefab;
        if (mergeFX != null)
        {
            GameObject vfxInstance = Instantiate(mergeFX, midPoint, Quaternion.identity);
            
            // ParticleSystem 자동 재생
            ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
            else
            {
                // 자식 ParticleSystem 재생
                ParticleSystem[] particleSystems = vfxInstance.GetComponentsInChildren<ParticleSystem>();
                foreach (var system in particleSystems)
                {
                    system.Play();
                }
            }
            
            if (vfxLifetime > 0)
            {
                Destroy(vfxInstance, vfxLifetime);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeController] 합치기 이펙트 생성: {midPoint}");
            }
        }
        
        // 다음 단계 과일 생성 (폭탄이 아닐 경우만)
        if (fruitData.CurrentFruitType != FruitMergeData.FruitType.Bomb)
        {
            if (WatermelonObjectPool.Instance != null)
            {
                WatermelonObjectPool.Instance.GetFruit(fruitData.NextFruitType, midPoint);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[FruitMergeController] 다음 단계 과일 생성: {fruitData.NextFruitType} at {midPoint}");
                }
            }
            else
            {
                Debug.LogError("[FruitMergeController] WatermelonObjectPool이 없습니다! 다음 과일을 생성할 수 없습니다.");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeController] 폭탄은 최종 단계입니다. 다음 과일을 생성하지 않습니다.");
            }
        }
        
        // 풀로 반환 (또는 파괴)
        if (WatermelonObjectPool.Instance != null)
        {
            WatermelonObjectPool.Instance.ReturnFruit(gameObject);
            WatermelonObjectPool.Instance.ReturnFruit(other.gameObject);
            
            if (showDebugLogs)
            {
                Debug.Log($"[FruitMergeController] 두 과일을 풀로 반환했습니다.");
            }
        }
        else
        {
            // 풀이 없으면 파괴
            Destroy(gameObject);
            Destroy(other.gameObject);
            
            if (showDebugLogs)
            {
                Debug.LogWarning($"[FruitMergeController] 풀이 없어 두 과일을 파괴했습니다.");
            }
        }
    }
    
    /// <summary>
    /// 충돌 VFX를 생성합니다.
    /// </summary>
    private void SpawnCollisionVFX(Vector3 position)
    {
        GameObject vfxInstance = Instantiate(collisionVFXPrefab, position, Quaternion.identity);
        
        // ParticleSystem 자동 재생
        ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
        
        if (vfxLifetime > 0)
        {
            Destroy(vfxInstance, vfxLifetime);
        }
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // mergeDuration 검증
        if (mergeDuration <= 0f)
        {
            mergeDuration = 0.3f;
            Debug.LogWarning($"[FruitMergeController] {gameObject.name}: mergeDuration은 0보다 커야 합니다. 기본값(0.3초)으로 설정합니다.");
        }
        
        // collisionCooldown 검증
        if (collisionCooldown < 0f)
        {
            collisionCooldown = 0.1f;
            Debug.LogWarning($"[FruitMergeController] {gameObject.name}: collisionCooldown은 음수일 수 없습니다. 기본값(0.1초)로 설정합니다.");
        }
    }
#endif
}