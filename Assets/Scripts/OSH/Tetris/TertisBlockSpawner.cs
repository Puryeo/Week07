using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테트리스 7-Bag 시스템을 사용한 블록 스포너
/// - 트리거 기반 생성: SpawnChecker가 SpawnBlockManually() 호출
/// - 일반 모드와 폭탄 모드 지원
/// </summary>
public class TetrisBlockSpawner : MonoBehaviour
{
    #region Serialized Fields

    [Header("Block Prefabs")]
    [Tooltip("7개의 테트리스 블록 프리팹 (I, O, T, S, Z, J, L)")]
    [SerializeField] private GameObject[] blockPrefabs = new GameObject[7];

    [Tooltip("폭탄 블록 프리팹 (1종류)")]
    [SerializeField] private GameObject bombBlockPrefab;

    [Header("Spawn Settings")]
    [Tooltip("실제 블록이 소환될 위치")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("스폰 구역 검사 반지름")]
    [SerializeField] private float spawnCheckRadius = 1f;

    [Tooltip("첫 블록 소환 대기 시간 (초)")]
    [SerializeField] private float initialSpawnDelay = 0.5f;

    [Header("Bomb Mode Settings")]
    [Tooltip("폭탄 블록 스폰 위치")]
    [SerializeField] private Transform bombSpawnPoint;

    [Tooltip("폭탄 블록 소환 간격 (초)")]
    [SerializeField] private float bombSpawnInterval = 1f;

    [Header("Preview Settings")]
    [Tooltip("미리보기 블록이 표시될 4개의 위치")]
    [SerializeField] private Transform[] previewPoints = new Transform[4];

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region Private Fields

    // 7-Bag 시스템 데이터
    private List<int> currentBag = new List<int>();
    private int bagIndex = 0;

    // 블록 큐 시스템
    private Queue<GameObject> blockQueue = new Queue<GameObject>();
    private GameObject[] previewBlocks = new GameObject[4];

    // 폭탄 큐 시스템
    private Queue<GameObject> bombQueue = new Queue<GameObject>();
    private bool isBombSpawning = false;

    // 스폰 제어
    private bool canSpawn = true; // 일반 블록 스폰 가능 여부

    // 생성된 폭탄 블록 리스트
    private List<GameObject> spawnedBombBlocks = new List<GameObject>();

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ValidateSettings();
        InitializeQueue();

        // 첫 블록 자동 소환
        if (showDebugLogs)
        {
            Debug.Log("[BlockSpawner] 초기화 완료, 첫 블록 생성 예약");
        }

        Invoke(nameof(SpawnFirstBlock), initialSpawnDelay);
    }

    #endregion

    #region Validation

    private void ValidateSettings()
    {
        if (blockPrefabs == null || blockPrefabs.Length != 7)
        {
            Debug.LogError("[BlockSpawner] 블록 프리팹이 7개가 아닙니다!");
            enabled = false;
            return;
        }

        for (int i = 0; i < blockPrefabs.Length; i++)
        {
            if (blockPrefabs[i] == null)
            {
                Debug.LogError($"[BlockSpawner] 블록 프리팹 {i}번이 없습니다!");
                enabled = false;
                return;
            }
        }

        if (spawnPoint == null)
        {
            Debug.LogError("[BlockSpawner] Spawn Point가 없습니다!");
            enabled = false;
            return;
        }

        if (previewPoints == null || previewPoints.Length != 4)
        {
            Debug.LogError("[BlockSpawner] Preview Points가 4개가 아닙니다!");
            enabled = false;
            return;
        }

        for (int i = 0; i < previewPoints.Length; i++)
        {
            if (previewPoints[i] == null)
            {
                Debug.LogError($"[BlockSpawner] Preview Point {i}번이 없습니다!");
                enabled = false;
                return;
            }
        }
    }

    #endregion

    #region 7-Bag System

    private void CreateNewBag()
    {
        currentBag.Clear();
        for (int i = 0; i < 7; i++)
        {
            currentBag.Add(i);
        }
        bagIndex = 0;
    }

    private void ShuffleBag()
    {
        for (int i = currentBag.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = currentBag[i];
            currentBag[i] = currentBag[randomIndex];
            currentBag[randomIndex] = temp;
        }
    }

    private int GetNextBlockFromBag()
    {
        if (bagIndex >= 7)
        {
            CreateNewBag();
            ShuffleBag();

            if (showDebugLogs)
            {
                Debug.Log("[BlockSpawner] 새로운 Bag 생성 및 셔플 완료");
            }
        }

        int blockIndex = currentBag[bagIndex];
        bagIndex++;

        return blockIndex;
    }

    #endregion

    #region Queue Management

    private void InitializeQueue()
    {
        CreateNewBag();
        ShuffleBag();

        // 큐에 5개의 블록 미리 채우기
        for (int i = 0; i < 5; i++)
        {
            int blockIndex = GetNextBlockFromBag();
            GameObject blockPrefab = blockPrefabs[blockIndex];
            blockQueue.Enqueue(blockPrefab);
        }

        UpdatePreviewDisplay();

        if (showDebugLogs)
        {
            Debug.Log($"[BlockSpawner] 큐 초기화 완료: {blockQueue.Count}개 블록 대기 중");
        }
    }

    private void AddNewBlockToQueue()
    {
        int blockIndex = GetNextBlockFromBag();
        GameObject blockPrefab = blockPrefabs[blockIndex];
        blockQueue.Enqueue(blockPrefab);
    }

    #endregion

    #region Bomb Block Spawning

    /// <summary>
    /// 폭탄 블록 1개 소환
    /// </summary>
    public void SpawnBombBlock()
    {
        if (bombBlockPrefab == null)
        {
            Debug.LogWarning("[BlockSpawner] 폭탄 블록 프리팹이 없습니다!");
            return;
        }

        if (bombSpawnPoint == null)
        {
            Debug.LogWarning("[BlockSpawner] 폭탄 스폰 포인트가 없습니다!");
            return;
        }

        GameObject bombBlock = Instantiate(
            bombBlockPrefab,
            bombSpawnPoint.position,
            bombSpawnPoint.rotation
        );

        spawnedBombBlocks.Add(bombBlock);

        if (showDebugLogs)
        {
            Debug.Log($"[BlockSpawner] 💣 폭탄 블록 생성! (총 {spawnedBombBlocks.Count}개)");
        }
    }

    /// <summary>
    /// 폭탄 블록을 큐에 추가
    /// </summary>
    public void QueueBombBlock()
    {
        if (bombBlockPrefab == null || bombSpawnPoint == null)
        {
            Debug.LogWarning("[BlockSpawner] 폭탄 블록 프리팹 또는 스폰 포인트가 설정되지 않았습니다!");
            return;
        }

        bombQueue.Enqueue(bombBlockPrefab);

        if (!isBombSpawning)
        {
            StartCoroutine(SpawnBombBlocksSequentially());
        }
    }

    private IEnumerator SpawnBombBlocksSequentially()
    {
        isBombSpawning = true;

        while (bombQueue.Count > 0)
        {
            GameObject bombBlock = Instantiate(
                bombQueue.Dequeue(),
                bombSpawnPoint.position,
                bombSpawnPoint.rotation
            );

            spawnedBombBlocks.Add(bombBlock);

            if (showDebugLogs)
            {
                Debug.Log($"[BlockSpawner] 💣 폭탄 블록 생성! (총 {spawnedBombBlocks.Count}개)");
            }

            yield return new WaitForSeconds(bombSpawnInterval);
        }

        isBombSpawning = false;
    }

    #endregion

    #region Spawning

    /// <summary>
    /// 일반 블록 생성 허용
    /// </summary>
    public void EnableSpawning()
    {
        canSpawn = true;

        if (showDebugLogs)
        {
            Debug.Log("[BlockSpawner] 일반 블록 생성 활성화");
        }
    }

    /// <summary>
    /// 일반 블록 생성 중지
    /// </summary>
    public void DisableSpawning()
    {
        canSpawn = false;

        if (showDebugLogs)
        {
            Debug.Log("[BlockSpawner] 일반 블록 생성 비활성화");
        }
    }

    /// <summary>
    /// 첫 블록 소환 (게임 시작 시)
    /// </summary>
    private void SpawnFirstBlock()
    {
        if (showDebugLogs)
        {
            Debug.Log("[BlockSpawner] 🎮 첫 블록 생성!");
        }

        SpawnBlock();
    }

    /// <summary>
    /// 블록 생성 (내부 메서드)
    /// </summary>
    private void SpawnBlock()
    {
        // 생성 불가 상태면 중단
        if (!canSpawn)
        {
            if (showDebugLogs)
            {
                Debug.Log("[BlockSpawner] 생성 중지 상태 - 블록 생성 취소");
            }
            return;
        }

        // 스폰 구역 검사
        Collider[] colliders = Physics.OverlapSphere(spawnPoint.position, spawnCheckRadius);
        bool hasBlock = false;
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Bomb") || collider.CompareTag("Block"))
            {
                hasBlock = true;
                break;
            }
        }
        if (hasBlock)
        {
            if (showDebugLogs)
            {
                Debug.Log("[BlockSpawner] 스폰 구역에 블록이 있어 생성 취소");
            }
            return;
        }

        // 큐가 비어있으면 중단
        if (blockQueue.Count == 0)
        {
            Debug.LogWarning("[BlockSpawner] 블록 큐가 비어있습니다!");
            return;
        }

        // 큐에서 블록 꺼내기
        GameObject blockPrefab = blockQueue.Dequeue();

        // 블록 생성
        GameObject spawnedBlock = Instantiate(
            blockPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (showDebugLogs)
        {
            Debug.Log($"[BlockSpawner] ✓ 블록 생성: {spawnedBlock.name}");
        }

        // 큐에 새 블록 추가
        AddNewBlockToQueue();

        // 미리보기 업데이트
        UpdatePreviewDisplay();
    }

    /// <summary>
    /// 외부에서 블록 생성 요청 (SpawnChecker에서 호출)
    /// </summary>
    public void SpawnBlockManually()
    {
        if (showDebugLogs)
        {
            Debug.Log("[BlockSpawner] 트리거 기반 블록 생성 요청");
        }

        SpawnBlock();
    }

    #endregion

    #region Preview System

    private void UpdatePreviewDisplay()
    {
        ClearPreviewBlocks();

        GameObject[] queueArray = blockQueue.ToArray();

        for (int i = 0; i < 4 && i < queueArray.Length; i++)
        {
            if (previewPoints[i] != null)
            {
                GameObject previewBlock = Instantiate(
                    queueArray[i],
                    previewPoints[i].position,
                    previewPoints[i].rotation
                );

                // 프리뷰는 물리 비활성화
                Rigidbody rb = previewBlock.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                Rigidbody2D rb2d = previewBlock.GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    rb2d.simulated = false;
                }

                previewBlocks[i] = previewBlock;
            }
        }
    }

    private void ClearPreviewBlocks()
    {
        for (int i = 0; i < previewBlocks.Length; i++)
        {
            if (previewBlocks[i] != null)
            {
                Destroy(previewBlocks[i]);
                previewBlocks[i] = null;
            }
        }
    }

    #endregion

    #region Public Utilities

    /// <summary>
    /// 큐 정보 반환
    /// </summary>
    public string GetQueueInfo()
    {
        return $"Queue: {blockQueue.Count}개 | Bag: {bagIndex}/7 | 폭탄: {spawnedBombBlocks.Count}개 | 생성가능: {canSpawn}";
    }

    /// <summary>
    /// 생성된 폭탄 블록 리스트 반환
    /// </summary>
    public List<GameObject> GetSpawnedBombBlocks()
    {
        return new List<GameObject>(spawnedBombBlocks);
    }

    /// <summary>
    /// 현재 생성 가능 여부 반환
    /// </summary>
    public bool CanSpawn()
    {
        return canSpawn;
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        ClearPreviewBlocks();
    }

    #endregion

    #region Debug

#if UNITY_EDITOR
    [ContextMenu("Test: Spawn Block Now")]
    private void DebugSpawnBlock()
    {
        if (Application.isPlaying)
        {
            SpawnBlockManually();
        }
    }

    [ContextMenu("Test: Spawn Bomb Block")]
    private void DebugSpawnBombBlock()
    {
        if (Application.isPlaying)
        {
            SpawnBombBlock();
        }
    }

    [ContextMenu("Test: Print Queue Info")]
    private void DebugPrintQueueInfo()
    {
        if (Application.isPlaying)
        {
            Debug.Log(GetQueueInfo());
        }
    }

    [ContextMenu("Test: Toggle Spawning")]
    private void DebugToggleSpawning()
    {
        if (Application.isPlaying)
        {
            if (canSpawn)
            {
                DisableSpawning();
            }
            else
            {
                EnableSpawning();
            }
        }
    }
#endif

    #endregion
}