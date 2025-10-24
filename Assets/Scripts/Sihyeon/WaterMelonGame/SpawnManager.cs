using UnityEngine;

/// <summary>
/// 수박 게임의 과일 스폰 전담 매니저입니다.
/// 스폰 위치 계산, 검증, 과일 생성을 담당합니다.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("과일이 생성될 위치입니다.")]
    [SerializeField] private Transform spawnPoint;
    
    [Tooltip("스폰 지점의 기본 높이입니다. (Y축)")]
    [SerializeField] private float spawnHeight = 10f;
    
    [Tooltip("스폰 가능한 X축 범위입니다. (-range ~ +range)")]
    [SerializeField] private float spawnRangeX = 3f;
    
    [Tooltip("스폰 가능한 Z축 범위입니다. (-range ~ +range)")]
    [SerializeField] private float spawnRangeZ = 3f;
    
    [Tooltip("생성할 과일 타입 범위입니다. (Grape ~ 설정한 타입까지만 생성)")]
    [SerializeField] private FruitMergeData.FruitType maxSpawnFruitType = FruitMergeData.FruitType.Lemon;
    
    [Header("Spawn Validation Settings")]
    [Tooltip("스폰 위치 검증 시 과일 간 최소 여유 거리입니다.")]
    [SerializeField] private float spawnMargin = 0.2f;
    
    [Tooltip("스폰 위치 찾기 최대 시도 횟수입니다.")]
    [SerializeField] private int maxSpawnAttempts = 20;
    
    [Tooltip("스폰 위치 검증을 수행합니다. (false면 검증 없이 즉시 스폰)")]
    [SerializeField] private bool enableSpawnValidation = true;
    
    [Header("Debug Settings")]
    [Tooltip("디버그 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false;
    
    [Tooltip("스폰 지점과 범위를 Gizmo로 표시합니다.")]
    [SerializeField] private bool showGizmos = true;
    
    // 참조
    private WatermelonObjectPool objectPool;
    
    public void Initialize(WatermelonObjectPool pool)
    {
        objectPool = pool;
        
        if (spawnPoint == null)
        {
            Debug.LogWarning("[SpawnManager] SpawnPoint가 설정되지 않았습니다. 매니저 위치를 사용합니다.");
            spawnPoint = transform;
        }
        
        Debug.Log("[SpawnManager] 스폰 매니저 초기화 완료");
    }
    
    /// <summary>
    /// 랜덤한 위치에 랜덤한 과일을 생성합니다.
    /// </summary>
    public GameObject SpawnRandomFruit()
    {
        if (objectPool == null)
        {
            Debug.LogError("[SpawnManager] ObjectPool이 null입니다!");
            return null;
        }
        
        // 랜덤 과일 타입 선택
        int randomTypeIndex = Random.Range(0, (int)maxSpawnFruitType + 1);
        FruitMergeData.FruitType randomType = (FruitMergeData.FruitType)randomTypeIndex;
        
        return SpawnFruit(randomType);
    }
    
    /// <summary>
    /// 특정 타입의 과일을 생성합니다.
    /// </summary>
    public GameObject SpawnFruit(FruitMergeData.FruitType fruitType)
    {
        if (objectPool == null)
        {
            Debug.LogError("[SpawnManager] ObjectPool이 null입니다!");
            return null;
        }
        
        Vector3 spawnPos = Vector3.zero;
        bool foundValidPosition = false;
        
        if (enableSpawnValidation)
        {
            float fruitRadius = GetFruitRadius(fruitType);
            
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector3 candidatePos = GetRandomSpawnPosition();
                
                if (IsValidSpawnPosition(candidatePos, fruitRadius))
                {
                    spawnPos = candidatePos;
                    foundValidPosition = true;
                    
                    if (showDebugLogs)
                    {
                        Debug.Log($"[SpawnManager] 유효한 스폰 위치 찾음 (시도 {attempt + 1}회): {spawnPos}");
                    }
                    break;
                }
            }
            
            if (!foundValidPosition)
            {
                Debug.LogWarning($"[SpawnManager] {maxSpawnAttempts}회 시도 후에도 유효한 스폰 위치를 찾지 못했습니다.");
                return null;
            }
        }
        else
        {
            spawnPos = GetRandomSpawnPosition();
            foundValidPosition = true;
        }
        
        if (!foundValidPosition) return null;
        
        GameObject fruit = objectPool.GetFruit(fruitType, spawnPos);
        
        if (fruit != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[SpawnManager] {fruitType} 생성 완료 at {spawnPos}");
            }
        }
        else
        {
            Debug.LogWarning($"[SpawnManager] {fruitType} 생성 실패!");
        }
        
        return fruit;
    }
    
    /// <summary>
    /// 특정 과일 타입의 반경을 반환합니다.
    /// </summary>
    private float GetFruitRadius(FruitMergeData.FruitType type)
    {
        if (objectPool == null)
        {
            Debug.LogWarning("[SpawnManager] ObjectPool이 null입니다. 기본 반경(0.5f) 사용.");
            return 0.5f;
        }
        
        GameObject prefab = objectPool.GetFruitPrefab(type);
        
        if (prefab == null)
        {
            Debug.LogWarning($"[SpawnManager] {type} 프리팹을 찾을 수 없습니다. 기본 반경(0.5f) 사용.");
            return 0.5f;
        }
        
        SphereCollider sphereCollider = prefab.GetComponent<SphereCollider>();
        
        if (sphereCollider != null)
        {
            float maxScale = Mathf.Max(prefab.transform.localScale.x, 
                                       prefab.transform.localScale.y, 
                                       prefab.transform.localScale.z);
            float actualRadius = sphereCollider.radius * maxScale;
            
            if (showDebugLogs)
            {
                Debug.Log($"[SpawnManager] {type} 반경: {actualRadius:F2}");
            }
            
            return actualRadius;
        }
        
        Collider collider = prefab.GetComponent<Collider>();
        
        if (collider != null)
        {
            float radius = Mathf.Max(collider.bounds.extents.x, 
                                     collider.bounds.extents.y, 
                                     collider.bounds.extents.z);
            
            if (showDebugLogs)
            {
                Debug.Log($"[SpawnManager] {type} 반경 (Bounds): {radius:F2}");
            }
            
            return radius;
        }
        
        Debug.LogWarning($"[SpawnManager] {type} 프리팹에 Collider가 없습니다. 기본 반경(0.5f) 사용.");
        return 0.5f;
    }
    
    /// <summary>
    /// 지정된 위치가 스폰하기에 유효한지 검증합니다.
    /// </summary>
    private bool IsValidSpawnPosition(Vector3 position, float fruitRadius)
    {
        FruitMergeData[] activeFruits = FindObjectsByType<FruitMergeData>(FindObjectsSortMode.None);
        
        foreach (var fruit in activeFruits)
        {
            if (!fruit.gameObject.activeSelf) continue;
            
            Vector3 fruitPos = fruit.transform.position;
            float distanceXZ = Vector2.Distance(
                new Vector2(position.x, position.z),
                new Vector2(fruitPos.x, fruitPos.z)
            );
            
            float otherRadius = GetFruitRadius(fruit.CurrentFruitType);
            float minDistance = fruitRadius + otherRadius + spawnMargin;
            
            if (distanceXZ < minDistance)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[SpawnManager] 스폰 위치 검증 실패: {fruit.CurrentFruitType}와 거리 {distanceXZ:F2} (최소 {minDistance:F2} 필요)");
                }
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 랜덤한 스폰 위치를 반환합니다.
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        float randomZ = Random.Range(-spawnRangeZ, spawnRangeZ);
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        return new Vector3(basePos.x + randomX, spawnHeight, basePos.z + randomZ);
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spawnHeight <= 0f)
        {
            spawnHeight = 10f;
            Debug.LogWarning("[SpawnManager] spawnHeight는 0보다 커야 합니다.");
        }
        
        if (spawnRangeX < 0f) spawnRangeX = 3f;
        if (spawnRangeZ < 0f) spawnRangeZ = 3f;
        if (spawnMargin < 0f) spawnMargin = 0.2f;
        if (maxSpawnAttempts < 1) maxSpawnAttempts = 20;
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        Gizmos.color = Color.green;
        Vector3 spawnPos = new Vector3(basePos.x, spawnHeight, basePos.z);
        Gizmos.DrawWireSphere(spawnPos, 0.5f);
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Vector3 boxSize = new Vector3(spawnRangeX * 2, 0.1f, spawnRangeZ * 2);
        Gizmos.DrawCube(spawnPos, boxSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnPos, boxSize);
    }
#endif
}