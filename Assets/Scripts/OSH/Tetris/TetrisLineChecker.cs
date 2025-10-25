using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 테트리스 라인 체크 및 제거 시스템 (개선 버전)
/// 
/// [개선 사항]
/// - 라인 제거 안정성 향상 (일부만 제거되는 문제 해결)
/// - 블록 분리 시스템 통합
/// - 물리 동기화 강화
/// </summary>
public class TetrisLineChecker : MonoBehaviour
{
    #region Serialized Fields

    [Header("Line Check Settings")]
    [Tooltip("체크할 Y 높이 배열")]
    [SerializeField] private float[] checkHeights = { 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f, 10.5f, 11.5f, 12.5f, 13.5f, 14.5f };

    [Tooltip("레이캐스트 시작 X 좌표")]
    [SerializeField] private float rayStartX = -5.5f;

    [Tooltip("레이캐스트 길이")]
    [SerializeField] private float rayLength = 11f;

    [Tooltip("라인 체크 주기 (초)")]
    [SerializeField] private float checkInterval = 0.1f;

    [Tooltip("블록이 정지했다고 판단하는 속도 임계값")]
    [SerializeField] private float stopThreshold = 0.01f; // 0.001에서 0.01로 완화

    [Header("Line Removal Settings")]
    [Tooltip("라인 제거 전 대기 시간 (물리 안정화)")]
    [SerializeField] private float removalDelay = 0.1f;

    [Tooltip("라인 제거 시 부모 블록도 함께 제거 (비활성화가 아닌 파괴)")]
    [SerializeField] private bool destroyEmptyParents = false;

    [Header("Block Fragmentation")]
    [Tooltip("블록 분리 시스템 사용 여부")]
    [SerializeField] private bool enableFragmentation = true;

    [Tooltip("분리된 조각에 적용할 질량")]
    [SerializeField] private float fragmentMass = 0.1f;

    [Tooltip("분리 시 추가 속도 (분산 효과)")]
    [SerializeField] private float fragmentationForce = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private bool showDebugLogs = false;
    [SerializeField] private Color debugRayColor = Color.red;

    #endregion

    #region Events

    /// <summary>
    /// 라인 제거 이벤트
    /// float: 제거된 라인의 높이
    /// bool: 폭탄 라인 여부
    /// </summary>
    [System.Serializable]
    public class LineRemovedEvent : UnityEvent<float, bool> { }

    [Header("Events")]
    public LineRemovedEvent onLineRemoved = new LineRemovedEvent();

    #endregion

    #region Private Fields

    private float checkTimer = 0f;
    private HashSet<float> processingHeights = new HashSet<float>(); // 중복 처리 방지

    #endregion

    #region Unity Lifecycle

    void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckAllLines();
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugRays) return;

        Gizmos.color = debugRayColor;
        foreach (float height in checkHeights)
        {
            Vector3 startPos = new Vector3(rayStartX, height, 0f);
            Vector3 endPos = startPos + Vector3.right * rayLength;
            Gizmos.DrawLine(startPos, endPos);
        }
    }

    #endregion

    #region Line Check Methods

    /// <summary>
    /// 모든 설정된 높이에서 라인 체크
    /// </summary>
    private void CheckAllLines()
    {
        foreach (float height in checkHeights)
        {
            // 이미 처리 중인 높이는 스킵
            if (processingHeights.Contains(height))
                continue;

            CheckLineAtHeight(height);
        }
    }

    /// <summary>
    /// 특정 높이에서 라인 체크 및 제거
    /// 
    /// [로직]
    /// 1. 수평 레이캐스트로 모든 큐브 감지
    /// 2. 정확히 10개의 큐브가 있는지 확인
    /// 3. 모든 블록이 정지 상태인지 확인
    /// 4. 조건 만족 시 라인 제거 시작
    /// </summary>
    private void CheckLineAtHeight(float yHeight)
    {
        Vector3 rayStart = new Vector3(rayStartX, yHeight, 0f);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.right, rayLength);

        // 이 라인에 있는 큐브들을 저장
        HashSet<GameObject> cubesInLine = new HashSet<GameObject>();
        // 부모 블록들도 따로 저장 (정지 체크용)
        HashSet<GameObject> blocksInLine = new HashSet<GameObject>();

        foreach (RaycastHit hit in hits)
        {
            // "Cube" 태그인지 확인
            if (!hit.collider.CompareTag("Cube"))
                continue;

            // 자식 큐브 추가
            cubesInLine.Add(hit.collider.gameObject);

            // 부모 블록 가져오기
            Transform parent = hit.collider.transform.parent;
            if (parent == null)
                continue;

            GameObject block = parent.gameObject;

            // Rigidbody가 있는지 확인
            if (block.GetComponent<Rigidbody>() == null)
                continue;

            blocksInLine.Add(block);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[LineChecker] 높이 {yHeight}에서 감지된 큐브: {cubesInLine.Count}개");
        }

        // 큐브가 정확히 10개인지 확인 (테트리스 가로 라인)
        if (cubesInLine.Count != 10)
            return;

        // 모든 블록이 정지 상태인지 확인
        if (!AreAllBlocksStopped(blocksInLine))
        {
            if (showDebugLogs)
            {
                Debug.Log($"[LineChecker] 높이 {yHeight} - 블록들이 아직 움직이는 중");
            }
            return;
        }

        // 조건을 모두 만족하면 라인 제거 시작
        StartCoroutine(RemoveLineWithDelay(cubesInLine, blocksInLine, yHeight));
    }

    /// <summary>
    /// 모든 블록이 정지 상태인지 확인
    /// 
    /// [체크 항목]
    /// - 선형 속도 (linearVelocity)
    /// - 각속도 (angularVelocity) - 회전 중인지 확인
    /// </summary>
    private bool AreAllBlocksStopped(HashSet<GameObject> blocks)
    {
        foreach (GameObject block in blocks)
        {
            Rigidbody rb = block.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            // 선형 속도와 각속도 모두 체크
            if (rb.linearVelocity.magnitude >= stopThreshold ||
                rb.angularVelocity.magnitude >= stopThreshold)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Line Removal

    /// <summary>
    /// 지연 후 라인 제거 (물리 안정화 대기)
    /// 
    /// [처리 순서]
    /// 1. 중복 처리 방지 플래그 설정
    /// 2. 물리 안정화 대기
    /// 3. 큐브 제거
    /// 4. 블록 분리 처리
    /// 5. 빈 부모 블록 정리
    /// 6. 물리 시스템 동기화
    /// </summary>
    private System.Collections.IEnumerator RemoveLineWithDelay(
        HashSet<GameObject> cubes,
        HashSet<GameObject> blocks,
        float height)
    {
        // 중복 처리 방지
        processingHeights.Add(height);

        // 물리 안정화 대기
        yield return new WaitForSeconds(removalDelay);

        if (showDebugLogs)
        {
            Debug.Log($"[LineChecker] 라인 제거 시작 - 높이: {height}, 큐브 수: {cubes.Count}");
        }

        // 1단계: 큐브 제거 및 부모 블록별로 분류
        Dictionary<GameObject, List<GameObject>> blockToCubes = new Dictionary<GameObject, List<GameObject>>();

        foreach (GameObject cube in cubes)
        {
            if (cube == null) continue;

            Transform parent = cube.transform.parent;
            if (parent == null)
            {
                // 부모가 없는 경우 바로 비활성화
                cube.SetActive(false);
                continue;
            }

            GameObject parentBlock = parent.gameObject;

            // 부모 블록별로 제거된 큐브 추적
            if (!blockToCubes.ContainsKey(parentBlock))
            {
                blockToCubes[parentBlock] = new List<GameObject>();
            }
            blockToCubes[parentBlock].Add(cube);

            // 큐브 비활성화
            cube.SetActive(false);
        }

        // 2단계: 각 블록 처리 (분리 또는 제거)
        foreach (var kvp in blockToCubes)
        {
            GameObject block = kvp.Key;
            List<GameObject> removedCubes = kvp.Value;

            if (block == null) continue;

            // 블록 분리 처리
            if (enableFragmentation)
            {
                ProcessBlockFragmentation(block, removedCubes);
            }

            // 빈 블록 제거
            if (destroyEmptyParents && IsBlockEmpty(block))
            {
                Destroy(block);
            }
            else
            {
                // Rigidbody 깨우기 (슬립 상태 해제)
                Rigidbody rb = block.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.WakeUp();
                }
            }
        }

        // 3단계: 물리 시스템 동기화
        Physics.SyncTransforms();

        // 이벤트 발생
        onLineRemoved?.Invoke(height, false);

        // 처리 완료
        processingHeights.Remove(height);
    }

    /// <summary>
    /// 블록이 비어있는지 확인 (활성화된 자식 큐브가 없는지)
    /// </summary>
    private bool IsBlockEmpty(GameObject block)
    {
        if (block == null) return true;

        foreach (Transform child in block.transform)
        {
            if (child.CompareTag("Cube") && child.gameObject.activeSelf)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 블록 분리 처리
    /// 
    /// [동작 원리]
    /// 1. 활성화된 큐브들을 그룹으로 분류 (연결된 큐브끼리)
    /// 2. 각 그룹을 독립적인 새 블록으로 분리
    /// 3. 원본 블록이 비어있으면 제거
    /// </summary>
    private void ProcessBlockFragmentation(GameObject block, List<GameObject> removedCubes)
    {
        if (block == null) return;

        // 활성화된 큐브들만 수집
        List<GameObject> activeCubes = new List<GameObject>();
        foreach (Transform child in block.transform)
        {
            if (child.CompareTag("Cube") && child.gameObject.activeSelf)
            {
                activeCubes.Add(child.gameObject);
            }
        }

        // 활성 큐브가 없으면 처리 중단
        if (activeCubes.Count == 0)
            return;

        // 연결된 큐브 그룹 찾기
        List<List<GameObject>> connectedGroups = FindConnectedGroups(activeCubes);

        // 그룹이 1개면 분리 불필요
        if (connectedGroups.Count <= 1)
            return;

        if (showDebugLogs)
        {
            Debug.Log($"[LineChecker] 블록 분리: {block.name} -> {connectedGroups.Count}개 조각");
        }

        Rigidbody originalRb = block.GetComponent<Rigidbody>();

        // 각 그룹을 새로운 블록으로 분리
        for (int i = 0; i < connectedGroups.Count; i++)
        {
            List<GameObject> group = connectedGroups[i];

            // 첫 번째 그룹은 원본 블록 사용
            if (i == 0)
            {
                // 다른 그룹의 큐브들은 원본에서 제거될 것이므로 아무것도 안 함
                continue;
            }

            // 새 블록 생성
            GameObject newBlock = new GameObject($"{block.name}_Fragment_{i}");
            newBlock.transform.position = block.transform.position;
            newBlock.transform.rotation = block.transform.rotation;

            // "Cube" 태그 자동 할당
            newBlock.tag = block.tag;

            // Rigidbody 추가
            Rigidbody newRb = newBlock.AddComponent<Rigidbody>();
            newRb.mass = fragmentMass;
            newRb.linearDamping = originalRb != null ? originalRb.linearDamping : 0.05f;
            newRb.angularDamping = originalRb != null ? originalRb.angularDamping : 0.05f;

            // 큐브들을 새 블록으로 이동
            foreach (GameObject cube in group)
            {
                cube.transform.SetParent(newBlock.transform, true);
            }

            // 분리 효과: 약간의 힘 추가
            if (originalRb != null && fragmentationForce > 0)
            {
                Vector3 randomDirection = Random.insideUnitSphere.normalized;
                newRb.AddForce(randomDirection * fragmentationForce, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// 연결된 큐브들을 그룹으로 분류
    /// 
    /// [알고리즘]
    /// - BFS(너비 우선 탐색)를 사용하여 인접한 큐브들을 찾음
    /// - 거리 1.1 이내에 있는 큐브들을 연결된 것으로 판단
    /// </summary>
    private List<List<GameObject>> FindConnectedGroups(List<GameObject> cubes)
    {
        List<List<GameObject>> groups = new List<List<GameObject>>();
        HashSet<GameObject> visited = new HashSet<GameObject>();

        foreach (GameObject cube in cubes)
        {
            if (visited.Contains(cube))
                continue;

            // 새 그룹 시작 (BFS)
            List<GameObject> group = new List<GameObject>();
            Queue<GameObject> queue = new Queue<GameObject>();
            queue.Enqueue(cube);
            visited.Add(cube);

            while (queue.Count > 0)
            {
                GameObject current = queue.Dequeue();
                group.Add(current);

                // 인접한 큐브 찾기
                foreach (GameObject other in cubes)
                {
                    if (visited.Contains(other))
                        continue;

                    // 거리 체크 (인접 판단)
                    float distance = Vector3.Distance(current.transform.position, other.transform.position);
                    if (distance < 1.1f) // 큐브 크기가 1이므로 1.1로 설정
                    {
                        queue.Enqueue(other);
                        visited.Add(other);
                    }
                }
            }

            groups.Add(group);
        }

        return groups;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 특정 높이의 라인 체크를 강제로 실행
    /// (디버깅 또는 외부 트리거용)
    /// </summary>
    public void ForceCheckLine(float height)
    {
        CheckLineAtHeight(height);
    }

    /// <summary>
    /// 모든 높이의 라인 체크를 강제로 실행
    /// </summary>
    public void ForceCheckAllLines()
    {
        CheckAllLines();
    }

    #endregion
}