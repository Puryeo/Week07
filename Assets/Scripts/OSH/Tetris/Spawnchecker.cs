using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 블록이 스포너 영역을 완전히 벗어났을 때 다음 블록을 생성하는 트리거 체커
/// EXIT 기반: 블록이 트리거 영역을 완전히 빠져나가면 생성
/// </summary>
public class SpawnChecker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("테트리스 블록 스포너 참조")]
    [SerializeField] private TetrisBlockSpawner blockSpawner;

    [Header("Settings")]
    [Tooltip("중복 생성 방지 시간 (초)")]
    [SerializeField] private float debounceTime = 2f;

    [Tooltip("게임 시작 후 체커 활성화 대기 시간 (초) - 첫 블록 안정화")]
    [SerializeField] private float startDelay = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // 이미 처리한 블록을 추적 (중복 방지)
    private HashSet<GameObject> processedBlocks = new HashSet<GameObject>();
    private bool isActive = false; // 체커 활성화 여부

    private void Start()
    {
        ValidateComponents();

        // 일정 시간 후 체커 활성화
        Invoke(nameof(ActivateChecker), startDelay);

        if (showDebugLogs)
        {
            Debug.Log($"[SpawnChecker] {startDelay}초 후 활성화 예정");
        }
    }

    private void ActivateChecker()
    {
        isActive = true;

        if (showDebugLogs)
        {
            Debug.Log("[SpawnChecker] ✓ 체커 활성화!");
        }
    }

    private void ValidateComponents()
    {
        if (blockSpawner == null)
        {
            Debug.LogError("[SpawnChecker] TetrisBlockSpawner 참조가 없습니다!");
            enabled = false;
            return;
        }

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError("[SpawnChecker] BoxCollider 컴포넌트가 필요합니다!");
            enabled = false;
            return;
        }

        if (!boxCollider.isTrigger)
        {
            Debug.LogWarning("[SpawnChecker] BoxCollider의 'Is Trigger'를 활성화해주세요!");
        }
    }

    /// <summary>
    /// 블록이 트리거 영역을 완전히 벗어났을 때 호출
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // 체커가 활성화되지 않았으면 무시
        if (!isActive)
        {
            return;
        }

        GameObject parentBlock = null;

        // 1단계: "Cube" 태그인지 확인 (일반 테트리스 블록)
        if (other.CompareTag("Cube"))
        {
            // 2단계: 부모 블록 찾기
            Rigidbody rb = other.GetComponentInParent<Rigidbody>();
            if (rb == null)
            {
                return;
            }

            // 3단계: 부모가 "Draggable" 태그인지 확인
            if (!rb.CompareTag("Draggable"))
            {
                return;
            }

            parentBlock = rb.gameObject;
        }
        // 1-B단계: "Bomb" 태그인지 확인 (3x3 폭탄 블록 - 단일 오브젝트)
        else if (other.CompareTag("Bomb"))
        {
            // 폭탄은 자식이 없는 단일 오브젝트
            parentBlock = other.gameObject;
        }
        else
        {
            // 태그가 "Cube"도 "Bomb"도 아니면 무시
            return;
        }

        // 4단계: 중복 처리 방지
        if (processedBlocks.Contains(parentBlock))
        {
            return;
        }

        // 5단계: 처리 목록에 추가
        processedBlocks.Add(parentBlock);

        if (showDebugLogs)
        {
            string blockType = other.CompareTag("Bomb") ? "폭탄" : "블록";
            Debug.Log($"[SpawnChecker] ✓ {blockType}이 영역을 벗어남: {parentBlock.name} → 다음 블록 생성!");
        }

        // 6단계: 다음 블록 생성
        if (blockSpawner != null)
        {
            blockSpawner.SpawnBlockManually();
        }

        // 7단계: 일정 시간 후 처리 목록에서 제거 (메모리 관리)
        StartCoroutine(RemoveFromProcessedAfterDelay(parentBlock, debounceTime));
    }

    private System.Collections.IEnumerator RemoveFromProcessedAfterDelay(GameObject block, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (block != null)
        {
            processedBlocks.Remove(block);

            if (showDebugLogs)
            {
                Debug.Log($"[SpawnChecker] 블록 디바운스 해제: {block.name}");
            }
        }
    }

    /// <summary>
    /// 처리된 블록 목록 초기화 (디버그용)
    /// </summary>
    public void ClearProcessedBlocks()
    {
        processedBlocks.Clear();
        Debug.Log("[SpawnChecker] 처리된 블록 목록 초기화!");
    }

    /// <summary>
    /// 체커 즉시 활성화 (디버그용)
    /// </summary>
    public void ForceActivate()
    {
        CancelInvoke(nameof(ActivateChecker));
        isActive = true;
        Debug.Log("[SpawnChecker] 강제 활성화!");
    }

    /// <summary>
    /// 체커 비활성화 (디버그용)
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        Debug.Log("[SpawnChecker] 비활성화!");
    }

    private void OnDrawGizmos()
    {
        // 에디터에서 트리거 영역 시각화
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            // 반투명 녹색 박스
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            // 녹색 와이어프레임
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Clear Processed Blocks")]
    private void DebugClearProcessed()
    {
        ClearProcessedBlocks();
    }

    [ContextMenu("Force Activate Checker")]
    private void DebugForceActivate()
    {
        ForceActivate();
    }

    [ContextMenu("Deactivate Checker")]
    private void DebugDeactivate()
    {
        Deactivate();
    }
#endif
}