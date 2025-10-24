using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TetrisLineChecker : MonoBehaviour
{
    #region Serialized Fields

    [Header("Line Check Settings")]
    [SerializeField] private float[] checkHeights = { 1.25f, 2.25f, 3.25f, 4.25f, 5.25f, 6.25f, 7.25f, 8.25f, 9.25f, 10.25f, 11.25f, 12.25f, 13.25f, 14.25f };
    [SerializeField] private float rayStartX = -5.5f;
    [SerializeField] private float rayLength = 11f;
    [SerializeField] private float checkInterval = 0.1f;
    [SerializeField] private float stopThreshold = 0.001f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private Color debugRayColor = Color.red;

    #endregion

    #region Events

    /// <summary>
    /// 라인 제거 이벤트 (높이)
    /// </summary>
    [System.Serializable]
    public class LineRemovedEvent : UnityEvent<float, bool> { }

    [Header("Events")]
    public LineRemovedEvent onLineRemoved = new LineRemovedEvent();

    #endregion

    #region Private Fields

    private float checkTimer = 0f;

    #endregion

    #region Unity Lifecycle

    void Update()
    {
        // 타이머 업데이트
        checkTimer += Time.deltaTime;

        // 설정된 간격마다 체크
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckAllLines();
        }
    }

    void OnDrawGizmos()
    {
        // 디버그 레이 표시
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
            CheckLineAtHeight(height);
        }
    }

    /// <summary>
    /// 특정 높이에서 라인 체크
    /// </summary>
    private void CheckLineAtHeight(float yHeight)
    {
        // 레이 시작 위치
        Vector3 rayStart = new Vector3(rayStartX, yHeight, 0f);

        // 수평 레이캐스트 실행 (모든 hit 수집)
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.right, rayLength);

        // 이 라인에 있는 큐브들을 저장
        HashSet<GameObject> cubesInLine = new HashSet<GameObject>();
        // 부모 블록들도 따로 저장 (정지 체크용)
        HashSet<GameObject> blocksInLine = new HashSet<GameObject>();

        // 각 hit 처리
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

            // 부모 블록 추가
            blocksInLine.Add(block);
        }

        // 디버그 로그
        Debug.Log($"높이 {yHeight}에서 감지된 큐브: {cubesInLine.Count}개");

        // 큐브가 정확히 10개인지 확인 (테트리스 가로 라인)
        if (cubesInLine.Count != 10)
            return;

        // 모든 블록이 정지 상태인지 확인
        if (!AreAllBlocksStopped(blocksInLine))
            return;

        // 조건을 모두 만족하면 라인 제거
        RemoveLine(cubesInLine, yHeight, false);
    }

    /// <summary>
    /// 모든 블록이 정지 상태인지 확인
    /// </summary>
    private bool AreAllBlocksStopped(HashSet<GameObject> blocks)
    {
        foreach (GameObject block in blocks)
        {
            Rigidbody rb = block.GetComponent<Rigidbody>();
            if (rb == null)
                continue;

            // linearVelocity의 크기가 임계값 이상이면 아직 움직이는 중
            if (rb.linearVelocity.magnitude >= stopThreshold)
                return false;
        }

        // 모든 블록이 정지 상태
        return true;
    }

    #endregion

    #region Line Removal

    private void RemoveLine(HashSet<GameObject> cubes, float height, bool isBombLine)
    {
        Debug.Log($"라인 제거! 높이: {height}, 큐브 수: {cubes.Count}, 폭탄라인: {isBombLine}");

        // 모든 큐브 비활성화
        foreach (GameObject cube in cubes)
        {
            // 부모의 Rigidbody 가져오기
            var rb = cube.transform.parent?.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Debug.Log($"rb linear velocity magnitude: {rb.linearVelocity.magnitude}");
            }

            if (rb != null && rb.linearVelocity.magnitude < 0.001f)
            {
                Debug.Log($"큐브 비활성화: {cube.name}");
                cube.SetActive(false);

                // 콜라이더 구조가 변경되었을 때 물리 시스템 갱신
                rb.WakeUp(); // 슬립 상태 해제
                Physics.SyncTransforms(); // 물리 시스템과 Transform 동기화
            }
        }

        // 이벤트 발생
        onLineRemoved?.Invoke(height, isBombLine);

        // 여기에 추가 효과 가능:
        // - 파티클 이펙트
        // - 사운드 재생
        // - 점수 추가
        // - UI 업데이트
    }

    #endregion
}