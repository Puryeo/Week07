using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테트리스 7-Bag 시스템을 사용한 블록 스포너
/// 일반 모드와 폭탄 모드 지원
/// </summary>
public class TetrisBlockSpawner : MonoBehaviour
{
    #region Enums

    // SpawnMode 제거 - 항상 일반 모드로만 동작

    #endregion

    #region Serialized Fields

    [Header("Block Prefabs")]
    [Tooltip("7개의 테트리스 블록 프리팹 (I, O, T, S, Z, J, L)")]
    [SerializeField] private GameObject[] blockPrefabs = new GameObject[7];

    [Tooltip("폭탄 블록 프리팹 (1종류)")]
    [SerializeField] private GameObject bombBlockPrefab;

    [Header("Spawn Settings")]
    [Tooltip("실제 블록이 소환될 위치")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("블록 소환 간격 (초)")]
    [SerializeField] private float spawnInterval = 1.0f;

    [Header("Bomb Mode Settings")]
    [Tooltip("폭탄 블록 스폰 위치")]
    [SerializeField] private Transform bombSpawnPoint;

    [Header("Preview Settings")]
    [Tooltip("미리보기 블록이 표시될 4개의 위치")]
    [SerializeField] private Transform[] previewPoints = new Transform[4];

    #endregion

    #region Private Fields

    // 7-Bag 시스템 데이터
    private List<int> currentBag = new List<int>();
    private int bagIndex = 0;

    // 블록 큐 시스템
    private Queue<GameObject> blockQueue = new Queue<GameObject>();
    private GameObject[] previewBlocks = new GameObject[4];

    // 타이머
    private float spawnTimer = 0f;
    private bool isSpawning = false;

    // 생성된 폭탄 블록 리스트
    private List<GameObject> spawnedBombBlocks = new List<GameObject>();

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ValidateSettings();
        InitializeQueue();
        StartSpawning();
    }

    private void Update()
    {
        if (!isSpawning) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnBlock();
        }
    }

    #endregion

    #region Validation

    private void ValidateSettings()
    {
        if (blockPrefabs == null || blockPrefabs.Length != 7)
        {
            Debug.LogError("[TetrisBlockSpawner] blockPrefabs는 정확히 7개여야 합니다!");
            enabled = false;
            return;
        }

        for (int i = 0; i < blockPrefabs.Length; i++)
        {
            if (blockPrefabs[i] == null)
            {
                Debug.LogError($"[TetrisBlockSpawner] blockPrefabs[{i}]가 null입니다!");
                enabled = false;
                return;
            }
        }

        if (bombBlockPrefab == null)
        {
            Debug.LogWarning("[TetrisBlockSpawner] bombBlockPrefab이 설정되지 않았습니다. 폭탄 블록을 소환할 수 없습니다.");
        }

        if (spawnPoint == null)
        {
            Debug.LogError("[TetrisBlockSpawner] spawnPoint가 설정되지 않았습니다!");
            enabled = false;
            return;
        }

        if (previewPoints == null || previewPoints.Length != 4)
        {
            Debug.LogError("[TetrisBlockSpawner] previewPoints는 정확히 4개여야 합니다!");
            enabled = false;
            return;
        }

        for (int i = 0; i < previewPoints.Length; i++)
        {
            if (previewPoints[i] == null)
            {
                Debug.LogError($"[TetrisBlockSpawner] previewPoints[{i}]가 null입니다!");
                enabled = false;
                return;
            }
        }

        if (bombSpawnPoint == null)
        {
            Debug.LogWarning("[TetrisBlockSpawner] bombSpawnPoint가 설정되지 않았습니다. 폭탄 블록을 소환할 수 없습니다.");
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

        for (int i = 0; i < 5; i++)
        {
            int blockIndex = GetNextBlockFromBag();
            GameObject blockPrefab = blockPrefabs[blockIndex];
            blockQueue.Enqueue(blockPrefab);
        }

        UpdatePreviewDisplay();
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
            Debug.LogWarning("[TetrisBlockSpawner] bombBlockPrefab이 설정되지 않았습니다!");
            return;
        }

        if (bombSpawnPoint == null)
        {
            Debug.LogWarning("[TetrisBlockSpawner] bombSpawnPoint가 설정되지 않았습니다!");
            return;
        }

        GameObject bombBlock = Instantiate(
            bombBlockPrefab,
            bombSpawnPoint.position,
            bombSpawnPoint.rotation
        );

        spawnedBombBlocks.Add(bombBlock);
        Debug.Log($"[TetrisBlockSpawner] 폭탄 블록 소환됨 (총 {spawnedBombBlocks.Count}개)");
    }

    #endregion

    #region Spawning

    public void StartSpawning()
    {
        isSpawning = true;
        spawnTimer = 0f;
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    private void SpawnBlock()
    {
        if (blockQueue.Count == 0) return;

        GameObject blockPrefab = blockQueue.Dequeue();
        GameObject spawnedBlock = Instantiate(blockPrefab, spawnPoint.position, spawnPoint.rotation);

        AddNewBlockToQueue();
        UpdatePreviewDisplay();
    }

    public void SpawnBlockManually()
    {
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

                Rigidbody rb = previewBlock.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                Rigidbody2D rb2d = previewBlock.GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    rb2d.simulated = true;
                    rb2d.gravityScale = 1.0f;
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

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.1f, interval);
        Debug.Log($"[TetrisBlockSpawner] 소환 간격 변경: {spawnInterval}초");
    }

    public string GetQueueInfo()
    {
        return $"Queue 크기: {blockQueue.Count}, Bag 위치: {bagIndex}/7, 폭탄 블록: {spawnedBombBlocks.Count}개";
    }

    /// <summary>
    /// 생성된 폭탄 블록 리스트 반환
    /// </summary>
    public List<GameObject> GetSpawnedBombBlocks()
    {
        return new List<GameObject>(spawnedBombBlocks);
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
        else
        {
            Debug.LogWarning("[TetrisBlockSpawner] 플레이 모드에서만 사용 가능합니다.");
        }
    }

    [ContextMenu("Test: Spawn Bomb Block")]
    private void DebugSpawnBombBlock()
    {
        if (Application.isPlaying)
        {
            SpawnBombBlock();
        }
        else
        {
            Debug.LogWarning("[TetrisBlockSpawner] 플레이 모드에서만 사용 가능합니다.");
        }
    }

    [ContextMenu("Test: Print Queue Info")]
    private void DebugPrintQueueInfo()
    {
        if (Application.isPlaying)
        {
            Debug.Log($"[TetrisBlockSpawner] {GetQueueInfo()}");
            Debug.Log($"[TetrisBlockSpawner] 현재 Bag: [{string.Join(", ", currentBag)}]");
        }
        else
        {
            Debug.LogWarning("[TetrisBlockSpawner] 플레이 모드에서만 사용 가능합니다.");
        }
    }
#endif

    #endregion
}