using UnityEngine;

/// <summary>
/// 수박 게임의 전체 흐름을 관리하는 게임 매니저입니다.
/// 과일 생성, 게임 오버 등을 관리합니다.
/// 싱글톤 패턴으로 구현되었습니다.
/// </summary>
public class WatermelonGameManager : MonoBehaviour
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
    
    [Header("Fruit Generation Settings")]
    [Tooltip("자동으로 생성할 과일 타입 범위입니다. (Grape ~ 설정한 타입까지만 생성)")]
    [SerializeField] private FruitMergeData.FruitType maxSpawnFruitType = FruitMergeData.FruitType.Lemon;
    
    [Tooltip("게임 시작 시 자동으로 과일을 생성합니다.")]
    [SerializeField] private bool autoSpawnOnStart = false;
    
    [Tooltip("자동 생성 지연 시간(초)입니다.")]
    [SerializeField] private float autoSpawnDelay = 1f;
    
    [Header("Game Settings")]
    [Tooltip("게임 오버 라인의 Y 좌표입니다.")]
    [SerializeField] private float gameOverLineY = 8f;
    
    [Tooltip("게임 오버 체크 간격(초)입니다.")]
    [SerializeField] private float gameOverCheckInterval = 1f;
    
    [Header("Debug Settings")]
    [Tooltip("디버그 로그를 출력합니다.")]
    [SerializeField] private bool showDebugLogs = false;
    
    [Tooltip("스폰 지점과 게임오버 라인을 Gizmo로 표시합니다.")]
    [SerializeField] private bool showGizmos = true;
    
    // 싱글톤 인스턴스
    private static WatermelonGameManager instance;
    public static WatermelonGameManager Instance => instance;
    
    // 게임 상태
    private bool isGameOver = false;
    private float gameOverCheckTimer = 0f;
    
    // 참조
    private WatermelonObjectPool objectPool;
    
    /// <summary>
    /// 게임 오버 상태를 반환합니다.
    /// </summary>
    public bool IsGameOver => isGameOver;
    
    private void Awake()
    {
        // 싱글톤 설정
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[WatermelonGameManager] 중복된 인스턴스 감지! {gameObject.name}를 파괴합니다.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
    }
    
    private void Start()
    {
        // ObjectPool 참조 가져오기
        objectPool = WatermelonObjectPool.Instance;
        
        if (objectPool == null)
        {
            Debug.LogError("[WatermelonGameManager] WatermelonObjectPool을 찾을 수 없습니다!");
            return;
        }
        
        // 스폰 지점 검증
        if (spawnPoint == null)
        {
            Debug.LogWarning("[WatermelonGameManager] SpawnPoint가 설정되지 않았습니다. 매니저 위치를 사용합니다.");
            spawnPoint = transform;
        }
        
        // 자동 생성
        if (autoSpawnOnStart)
        {
            Invoke(nameof(SpawnRandomFruit), autoSpawnDelay);
        }
        
        Debug.Log("[WatermelonGameManager] 게임 매니저 초기화 완료 (3D 모드, 8단계)");
    }
    
    private void Update()
    {
        // 게임 오버 체크
        if (!isGameOver)
        {
            gameOverCheckTimer += Time.deltaTime;
            
            if (gameOverCheckTimer >= gameOverCheckInterval)
            {
                gameOverCheckTimer = 0f;
                CheckGameOver();
            }
        }
        
        // 테스트 입력
        HandleTestInput();
    }
    
    /// <summary>
    /// 테스트용 입력을 처리합니다.
    /// </summary>
    private void HandleTestInput()
    {
        // 스페이스바: 랜덤 과일 생성
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnRandomFruit();
        }
        
        // 숫자 키 0~7: 특정 과일 생성
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SpawnFruit(FruitMergeData.FruitType.Grape, GetRandomSpawnPosition());
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SpawnFruit(FruitMergeData.FruitType.Apple, GetRandomSpawnPosition());
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SpawnFruit(FruitMergeData.FruitType.Orange, GetRandomSpawnPosition());
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SpawnFruit(FruitMergeData.FruitType.Lemon, GetRandomSpawnPosition());
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SpawnFruit(FruitMergeData.FruitType.Melon, GetRandomSpawnPosition());
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SpawnFruit(FruitMergeData.FruitType.Durian, GetRandomSpawnPosition());
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SpawnFruit(FruitMergeData.FruitType.Watermelon, GetRandomSpawnPosition());
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SpawnFruit(FruitMergeData.FruitType.Bomb, GetRandomSpawnPosition());
        }
        
        // R키: 모든 과일 반환
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }
        
        // G키: 게임 오버 테스트
        if (Input.GetKeyDown(KeyCode.G))
        {
            GameOver();
        }
    }
    
    /// <summary>
    /// 랜덤한 위치에 랜덤한 과일을 생성합니다.
    /// </summary>
    public void SpawnRandomFruit()
    {
        if (isGameOver)
        {
            Debug.LogWarning("[WatermelonGameManager] 게임 오버 상태에서는 과일을 생성할 수 없습니다.");
            return;
        }
        
        // 랜덤 과일 타입 선택 (Grape ~ maxSpawnFruitType)
        int randomTypeIndex = Random.Range(0, (int)maxSpawnFruitType + 1);
        FruitMergeData.FruitType randomType = (FruitMergeData.FruitType)randomTypeIndex;
        
        // 랜덤 스폰 위치
        Vector3 spawnPos = GetRandomSpawnPosition();
        
        SpawnFruit(randomType, spawnPos);
    }
    
    /// <summary>
    /// 특정 타입의 과일을 지정된 위치에 생성합니다.
    /// </summary>
    public void SpawnFruit(FruitMergeData.FruitType type, Vector3 position)
    {
        if (isGameOver)
        {
            Debug.LogWarning("[WatermelonGameManager] 게임 오버 상태에서는 과일을 생성할 수 없습니다.");
            return;
        }
        
        if (objectPool == null)
        {
            Debug.LogError("[WatermelonGameManager] ObjectPool이 null입니다!");
            return;
        }
        
        GameObject fruit = objectPool.GetFruit(type, position);
        
        if (fruit != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[WatermelonGameManager] {type} 생성 완료 at {position}");
            }
        }
        else
        {
            Debug.LogWarning($"[WatermelonGameManager] {type} 생성 실패!");
        }
    }
    
    /// <summary>
    /// 랜덤한 스폰 위치를 반환합니다. (3D)
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        float randomZ = Random.Range(-spawnRangeZ, spawnRangeZ);
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        return new Vector3(basePos.x + randomX, spawnHeight, basePos.z + randomZ);
    }
    
    /// <summary>
    /// 게임 오버를 체크합니다.
    /// </summary>
    private void CheckGameOver()
    {
        // 게임 오버 라인을 넘은 과일이 있는지 확인
        GameObject[] allFruits = GameObject.FindGameObjectsWithTag("Untagged"); // 모든 활성 오브젝트 확인
        
        foreach (var obj in allFruits)
        {
            FruitMergeData fruitData = obj.GetComponent<FruitMergeData>();
            
            if (fruitData != null && obj.activeSelf)
            {
                // 과일이 게임 오버 라인을 넘었는지 확인
                if (obj.transform.position.y > gameOverLineY)
                {
                    // Rigidbody의 속도가 거의 0이면 (정지 상태)
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null && rb.linearVelocity.magnitude < 0.1f)
                    {
                        GameOver();
                        return;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 게임 오버를 처리합니다.
    /// </summary>
    private void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        
        Debug.Log($"[WatermelonGameManager] ===== 게임 오버! =====");
        
        // TODO: 게임 오버 UI 표시
        // TODO: 게임 오버 사운드 재생
        
        // 3초 후 자동 리셋 (테스트용)
        Invoke(nameof(ResetGame), 3f);
    }
    
    /// <summary>
    /// 게임을 리셋합니다.
    /// </summary>
    public void ResetGame()
    {
        if (objectPool != null)
        {
            objectPool.ReturnAllFruits();
        }
        
        isGameOver = false;
        gameOverCheckTimer = 0f;
        
        Debug.Log("[WatermelonGameManager] 게임 리셋 완료");
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 스폰 높이 검증
        if (spawnHeight <= 0f)
        {
            spawnHeight = 10f;
            Debug.LogWarning("[WatermelonGameManager] spawnHeight는 0보다 커야 합니다. 기본값(10)으로 설정합니다.");
        }
        
        // 스폰 범위 검증
        if (spawnRangeX < 0f)
        {
            spawnRangeX = 3f;
            Debug.LogWarning("[WatermelonGameManager] spawnRangeX는 0 이상이어야 합니다. 기본값(3)으로 설정합니다.");
        }
        
        if (spawnRangeZ < 0f)
        {
            spawnRangeZ = 3f;
            Debug.LogWarning("[WatermelonGameManager] spawnRangeZ는 0 이상이어야 합니다. 기본값(3)으로 설정합니다.");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        // 스폰 지점 시각화
        Gizmos.color = Color.green;
        Vector3 spawnPos = new Vector3(basePos.x, spawnHeight, basePos.z);
        Gizmos.DrawWireSphere(spawnPos, 0.5f);
        
        // 스폰 범위 시각화 (3D 박스)
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Vector3 boxSize = new Vector3(spawnRangeX * 2, 0.1f, spawnRangeZ * 2);
        Gizmos.DrawCube(spawnPos, boxSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnPos, boxSize);
        
        // 게임 오버 라인 시각화 (평면)
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 gameOverPlaneSize = new Vector3(spawnRangeX * 2 + 4f, 0.1f, spawnRangeZ * 2 + 4f);
        Vector3 gameOverPlanePos = new Vector3(basePos.x, gameOverLineY, basePos.z);
        Gizmos.DrawCube(gameOverPlanePos, gameOverPlaneSize);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(gameOverPlanePos, gameOverPlaneSize);
        
        // 라벨
        UnityEditor.Handles.Label(spawnPos + Vector3.up * 1f, "Spawn Point (3D)");
        UnityEditor.Handles.Label(gameOverPlanePos + Vector3.up * 0.5f, "Game Over Line");
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || !Application.isPlaying) return;
        
        // 게임 상태 표시
        Vector3 labelPos = transform.position + Vector3.up * 5f;
        string gameInfo = $"=== Game Status (3D, 8 Stages) ===\n" +
                         $"Game Over: {isGameOver}\n" +
                         $"Max Spawn Type: {maxSpawnFruitType}";
        
        UnityEditor.Handles.Label(labelPos, gameInfo);
    }
#endif
}