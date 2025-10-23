using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using VFolders.Libs;

public class LineChecker : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 10; // X축 그리드 너비
    [SerializeField] private int gridDepth = 10; // Z축 그리드 깊이
    [SerializeField] private float gridSpacing = 1f; // 큐브 간격
    [SerializeField] private float checkHeight = 20f; // 체크할 최대 높이

    [Header("Check Settings")]
    [SerializeField] private float checkInterval = 0.5f; // 체크 주기
    [SerializeField] private float rayLength = 100f; // 레이 길이
    [SerializeField] private Vector3 gridOrigin = Vector3.zero; // 그리드 시작점

    [Header("Visual Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private float debugRayDuration = 1f;
    [SerializeField] private bool showDetailedLogs = true; // 상세 로그 표시 여부

    private void Update()
    {
        CheckAndClearLines();
    }

    private void CheckAndClearLines()
    {
        // Y축의 각 높이를 체크
        for (float y = gridOrigin.y; y < gridOrigin.y + checkHeight; y += gridSpacing)
        {
            if (CheckLineAtHeight(y))
            {
                ClearLineAtHeight(y);
            }
        }
    }

    private bool CheckLineAtHeight(float yPosition)
    {
        List<Collider> cubesInLine = new List<Collider>();

        if (showDetailedLogs)
        {
            Debug.Log($"=== Checking line at Y={yPosition} ===");
        }

        // X-Z 평면에서 그리드의 모든 위치를 체크
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                Vector3 startPos = gridOrigin + new Vector3(
                    x * gridSpacing,
                    yPosition + rayLength / 2f,
                    z * gridSpacing
                );

                Vector3 direction = Vector3.right;

                // 디버그용 레이 표시
                if (showDebugRays)
                {
                    Debug.DrawRay(startPos, direction * rayLength, Color.yellow, debugRayDuration);
                }

                // 레이캐스트로 해당 위치의 큐브 감지
                RaycastHit[] hits = Physics.RaycastAll(startPos, direction, rayLength);

                if (showDetailedLogs && hits.Length > 0)
                {
                    Debug.Log($"Ray at X={x}, y={z}: Hit {hits.Length} object(s)");
                }

                foreach (RaycastHit hit in hits)
                {
                    if (showDetailedLogs)
                    {
                        Debug.Log($"  - Hit: {hit.collider.name} at position {hit.collider.transform.position}");
                    }

                    float size = 0.1f;
                    Debug.DrawLine(hit.point + Vector3.up * size, hit.point - Vector3.up * size, Color.red, debugRayDuration);
                    Debug.DrawLine(hit.point + Vector3.right * size, hit.point - Vector3.right * size, Color.red, debugRayDuration);
                    Debug.DrawLine(hit.point + Vector3.forward * size, hit.point - Vector3.forward * size, Color.red, debugRayDuration);


                    // 큐브인지 확인 (태그나 레이어로 필터링 가능)
                    if (hit.collider.CompareTag("Cube"))
                    {
                        // float size = 0.1f;
                        Debug.DrawLine(hit.point + Vector3.up * size, hit.point - Vector3.up * size, Color.blue, debugRayDuration);
                        Debug.DrawLine(hit.point + Vector3.right * size, hit.point - Vector3.right * size, Color.blue, debugRayDuration);
                        Debug.DrawLine(hit.point + Vector3.forward * size, hit.point - Vector3.forward * size, Color.blue, debugRayDuration);
                        // Y 위치가 체크하는 높이와 일치하는지 확인
                        float yDiff = Mathf.Abs(hit.collider.transform.position.y - yPosition);

                        if (showDetailedLogs)
                        {
                            Debug.Log($"    ✓ Is Cube! Y difference: {yDiff} (threshold: {gridSpacing * 0.5f})");
                        }

                        if (yDiff < gridSpacing * 0.5f)
                        {
                            if (!cubesInLine.Contains(hit.collider))
                            {
                                cubesInLine.Add(hit.collider);

                                if (showDetailedLogs)
                                {
                                    Debug.Log($"    ✅ Added to line! Total cubes in line: {cubesInLine.Count}");
                                }
                            }
                        }
                        else
                        {
                            if (showDetailedLogs)
                            {
                                Debug.Log($"    ❌ Too far from target Y position");
                            }
                        }

                        //Todo: 테트리스 부시는 로직
                        // var rb = hit.collider.gameObject.transform.parent.gameObject.GetComponent<Rigidbody>();

                        // if (rb != null)
                        // {
                        //     Debug.Log($"rb linear velocity magnitude: {rb.linearVelocity.magnitude}");
                        // }

                        // if (rb != null && rb.linearVelocity.magnitude < 0.001f)
                        // {
                        //     Debug.Log($"rb linear velocity magnitude: {rb.linearVelocity.magnitude}");
                        //     hit.collider.gameObject.SetActive(false);
                        //     // 콜라이더 구조가 변경되었을 때 물리 시스템 갱신
                        //     rb.WakeUp(); // 슬립 상태 해제
                        //     Physics.SyncTransforms(); // 물리 시스템과 Transform 동기화

                        // }



                    }
                    else
                    {
                        if (showDetailedLogs)
                        {
                            Debug.Log($"    ⚠ Not a cube (wrong tag/name)");
                        }
                    }
                }
            }
        }

        if (showDetailedLogs)
        {
            Debug.Log($"📊 Total cubes found at Y={yPosition}: {cubesInLine.Count}/10");
        }

        // 큐브가 정확히 10개인지 확인
        if (cubesInLine.Count == 10)
        {
            Debug.Log($"🎯 Found exactly 10 cubes at Y={yPosition}! Checking sleep state...");

            // 모든 큐브가 슬립 상태인지 확인
            bool allSleeping = AreAllCubesSleeping(cubesInLine);

            if (allSleeping)
            {
                Debug.Log($"💤 All cubes are sleeping! Ready to clear!");
            }
            else
            {
                Debug.Log($"⏳ Some cubes are still moving...");
            }

            return allSleeping;
        }

        return false;
    }

    private bool AreAllCubesSleeping(List<Collider> cubes)
    {
        foreach (Collider cube in cubes)
        {
            Rigidbody rb = cube.GetComponent<Rigidbody>();

            if (rb == null)
            {
                Debug.LogWarning($"⚠ Rigidbody not found on {cube.name}");
                return false;
            }

            // Rigidbody가 슬립 상태가 아니면 false 반환
            if (!rb.IsSleeping())
            {
                if (showDetailedLogs)
                {
                    Debug.Log($"  {cube.name} is NOT sleeping (velocity: {rb.linearVelocity.magnitude:F3})");
                }
                return false;
            }
            else
            {
                if (showDetailedLogs)
                {
                    Debug.Log($"  {cube.name} is sleeping ✓");
                }
            }
        }

        return true;
    }

    private void ClearLineAtHeight(float yPosition)
    {
        Debug.Log($"✨ Clearing line at height: {yPosition}");

        List<GameObject> cubesToDestroy = new List<GameObject>();

        // 해당 높이의 모든 큐브 찾기
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                Vector3 startPos = gridOrigin + new Vector3(
                    x * gridSpacing,
                    yPosition + rayLength / 2f,
                    z * gridSpacing
                );

                RaycastHit[] hits = Physics.RaycastAll(startPos, Vector3.down, rayLength);
                Debug.Log($"Raycasting down from {startPos} found {hits.Length} hits.");
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.CompareTag("Cube") || hit.collider.name.Contains("Cube"))
                    {
                        if (Mathf.Abs(hit.collider.transform.position.y - yPosition) < gridSpacing * 0.5f)
                        {
                            if (!cubesToDestroy.Contains(hit.collider.gameObject))
                            {
                                cubesToDestroy.Add(hit.collider.gameObject);
                                Debug.Log($"  🗑 Marked for destruction: {hit.collider.name}");
                            }
                        }
                    }
                }
            }
        }

        // 이펙트와 함께 큐브 제거
        foreach (GameObject cube in cubesToDestroy)
        {
            // 여기에 파티클 이펙트나 사운드 추가 가능
            Destroy(cube);
        }

        Debug.Log($"💥 Destroyed {cubesToDestroy.Count} cubes at Y={yPosition}");
    }

    // 수동으로 체크하고 싶을 때 사용
    public void ManualCheck()
    {
        CheckAndClearLines();
    }

    // 디버그용: 그리드 시각화
    private void OnDrawGizmos()
    {
        if (!showDebugRays) return;

        Gizmos.color = Color.green;

        // 그리드 경계 표시
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = gridOrigin + new Vector3(x * gridSpacing, 0, 0);
            Vector3 end = gridOrigin + new Vector3(x * gridSpacing, 0, gridDepth * gridSpacing);
            Gizmos.DrawLine(start, end);
        }

        for (int z = 0; z <= gridDepth; z++)
        {
            Vector3 start = gridOrigin + new Vector3(0, 0, z * gridSpacing);
            Vector3 end = gridOrigin + new Vector3(gridWidth * gridSpacing, 0, z * gridSpacing);
            Gizmos.DrawLine(start, end);
        }
    }
}