// StageUIManager.cs (Step 3 리팩터링 완료 + 주석 코드 복원)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageUIManager : MonoBehaviour
{
    #region Serialized Fields
    // --- [사용되지 않음] 기존 2D UI 필드 ---
    [Header("!! [사용되지 않음] !!")]
    [SerializeField] private GameObject _stagePrefab; // (더 이상 사용되지 않음)
    [SerializeField] private GameObject _stageTarget; // (더 이상 사용되지 않음)
    // ------------------------------------

    [Header("스테이지 데이터")]
    [SerializeField] private StageGroupSO _stageGroupSO;
    [SerializeField] private Button _exitBtn;

    [Header("Hidden Stage Lock")]
    [SerializeField] private GameObject _hiddenStageLock;  // Scene의 3D 가림막 오브젝트
    [SerializeField] private string _hiddenStageName = "JMKey";
    [SerializeField] private GameObject _unlockObject1;  // 해금 시 활성화할 오브젝트 1
    [SerializeField] private GameObject _unlockObject2;  // 해금 시 활성화할 오브젝트 2

    [Header("Hidden Object Monitoring")]
    [SerializeField] private GameObject _hiddenObj2;  // HiddenObj_2 오브젝트
    [SerializeField] private float positionChangeThreshold = 1.0f;  // 위치 변화 감지 임계값 (1cm)

    [Header("Debug Settings")]
    [SerializeField] private float starFillDelay = 0.3f;  // 별 채우기 간격 (초)
    #endregion

    #region Private Fields
    private List<StageDataSO> stageDataSOs = new();

    // --- 3D 상호작용용 ---
    private Camera _mainCamera; // Raycast를 위한 카메라

    // --- (추후 히든 스테이지/디버그 기능 복구 시 필요한 필드들) ---
    // private List<Button> stageBtns = new();
    // private bool isAllCleared = false;  // 히든 스테이지 해금용도
    // private GameObject hiddenStage;
    // private Button hiddenStageButton;
    // 
    // // 가림막 관련
    // private Material lockMaterial;
    // private Renderer lockRenderer;
    // private bool lockDeactivated = false;
    // 
    // // HiddenObj_2 모니터링 관련
    // private Vector3 lastHiddenObj2Position;
    // private bool isMonitoringHiddenObj2 = false;
    // private Coroutine unlockDelayCoroutine;
    // 
    // // 디버그 관련
    // private Coroutine fillStarsCoroutine;
    #endregion

    #region Initialize Methods

    // StageGroupSO 호출
    void OnEnable()
    {
        stageDataSOs = _stageGroupSO.stages;
        StageSaveManager.Load(stageDataSOs);

        // SetStageUI(); // ★ 삭제: UI는 씬에 수동 배치됨

        _exitBtn.onClick.AddListener(ExitGame);
    }

    private void OnDisable()
    {
        _exitBtn.onClick.RemoveAllListeners();

        // (추후 기능 복구 시 코루틴 정리 필요)
        // if (unlockDelayCoroutine != null) ...
        // if (fillStarsCoroutine != null) ...
    }

    private void Start()
    {
        // 3D 클릭(Raycast)을 위해 메인 카메라 할당
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("[StageUIManager] 씬에 'MainCamera' 태그가 달린 카메라가 없습니다!");
        }
    }

    private void Update()
    {
        // 매 프레임 마우스 클릭 감지
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }

        // (추후 기능 복구 시 Update 로직 필요)
        // #if UNITY_EDITOR
        //     HandleDebugInput();
        // #endif
        // if (isMonitoringHiddenObj2 && _hiddenObj2 != null) ...
    }
    #endregion

    #region Private Methods

    /// <summary>
    /// ★ [신규] 3D 오브젝트 클릭을 감지하는 Raycast 메서드
    /// </summary>
    private void HandleClick()
    {
        if (_mainCamera == null) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // 2D UI (예: Exit 버튼)를 클릭했다면 3D Raycast 무시
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Raycast에 맞은 오브젝트에서 WorldStageObject 컴포넌트 탐색
            WorldStageObject stageObject = hit.collider.GetComponent<WorldStageObject>();

            if (stageObject != null)
            {
                // 컴포넌트를 찾았다면, 해당 오브젝트의 선택 메서드 호출
                stageObject.SelectStage();
            }
        }
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion


    #region 추후 추가될 기능 (히든 스테이지, 디버그)
    /* // 해금 로직 비활성화
    #region Hidden Stage Lock Management
    /// <summary>
    /// 가림막 투명도 업데이트 및 비활성화
    /// 전체 별 개수 대비 현재 별 개수 비율로 투명도 계산
    /// </summary>
    void UpdateHiddenStageLock(int currentStars, int maxStars)
    {
        if (_hiddenStageLock == null)
        {
            Debug.LogWarning("[StageUIManager] 가림막 오브젝트가 null입니다.");
            return;
        }

        // 가림막이 비활성화 상태면 업데이트 건너뜀
        if (!_hiddenStageLock.activeSelf)
        {
            Debug.Log("[StageUIManager] 가림막이 비활성화 상태입니다. 업데이트 건너뜀.");
            return;
        }

        // 전체 별 개수 대비 현재 별 개수 비율
        float progress = maxStars > 0 ? (float)currentStars / maxStars : 0f;

        Debug.Log($"[StageUIManager] 가림막 업데이트: {currentStars}/{maxStars} (진행도: {progress:P0})");

        if (progress >= 1.0f)
        {
            // 해금 완료: 가림막 비활성화
            DeactivateLock();
            return;
        }

        // 투명도 계산: 진행도에 따라 선형 보간
        // 0% = 0.85 (85% 불투명), 100% = 0.0 (완전 투명)
        float alpha = Mathf.Lerp(0.85f, 0.0f, progress);
        ApplyLockTransparency(alpha);
    }

    /// <summary>
    /// 가림막의 Material을 Transparent 모드로 초기화
    /// </summary>
    void InitializeLockMaterial()
    {
        if (lockMaterial != null) return;

        lockRenderer = _hiddenStageLock.GetComponent<Renderer>();
        if (lockRenderer == null)
        {
            Debug.LogWarning("[StageUIManager] 가림막에 Renderer 컴포넌트가 없습니다.");
            return;
        }

        // Material 인스턴스 생성 (원본 보호)
        lockMaterial = lockRenderer.material;

        // Material을 Transparent 모드로 강제 설정
        if (lockMaterial.HasProperty("_Mode"))
        {
            lockMaterial.SetFloat("_Mode", 3); // Transparent 모드
            lockMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lockMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lockMaterial.SetInt("_ZWrite", 0);
            lockMaterial.DisableKeyword("_ALPHATEST_ON");
            lockMaterial.EnableKeyword("_ALPHABLEND_ON");
            lockMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            lockMaterial.renderQueue = 3000;
        }

        Debug.Log($"[StageUIManager] Material 초기화 완료: {lockMaterial.name}");
    }

    /// <summary>
    /// 가림막에 투명도 적용
    /// </summary>
    /// <param name="alpha">투명도 값 (0.0 = 완전 투명, 1.0 = 불투명)</param>
    void ApplyLockTransparency(float alpha)
    {
        // Material 초기화 (최초 1회)
        if (lockMaterial == null)
        {
            InitializeLockMaterial();
        }

        if (lockMaterial == null) return;

        // 투명도 적용
        Color color = lockMaterial.color;
        color.a = alpha;
        lockMaterial.color = color;

        Debug.Log($"[StageUIManager] 투명도 적용: Alpha = {alpha:F2} (색상: {color})");
    }

    /// <summary>
    /// 가림막 비활성화 및 추가 오브젝트 활성화, HiddenObj_2 모니터링 시작
    /// </summary>
    void DeactivateLock()
    {
        _hiddenStageLock.SetActive(false);
        lockDeactivated = true;

        Debug.Log("[StageUIManager] ✅ 가림막 비활성화 완료 (모든 별 획득)");

        // ✅ 추가: 해금 시 오브젝트들 활성화
        ActivateUnlockObjects();

        // HiddenObj_2 모니터링 시작
        StartMonitoringHiddenObj2();
    }

    /// <summary>
    /// ✅ 신규: 해금 시 추가 오브젝트들 활성화
    /// </summary>
    void ActivateUnlockObjects()
    {
        if (_unlockObject1 != null)
        {
            _unlockObject1.SetActive(true);
            Debug.Log($"[StageUIManager] 🔓 UnlockObject1 활성화: {_unlockObject1.name}");
        }
        else
        {
            Debug.LogWarning("[StageUIManager] UnlockObject1이 할당되지 않았습니다.");
        }

        if (_unlockObject2 != null)
        {
            _unlockObject2.SetActive(true);
            Debug.Log($"[StageUIManager] 🔓 UnlockObject2 활성화: {_unlockObject2.name}");
        }
        else
        {
            Debug.LogWarning("[StageUIManager] UnlockObject2가 할당되지 않았습니다.");
        }
    }

    /// <summary>
    /// ✅ 신규: 해금 오브젝트들 비활성화 (디버그용)
    /// </summary>
    void DeactivateUnlockObjects()
    {
        if (_unlockObject1 != null)
        {
            _unlockObject1.SetActive(false);
            Debug.Log($"[StageUIManager] 🔒 UnlockObject1 비활성화: {_unlockObject1.name}");
        }

        if (_unlockObject2 != null)
        {
            _unlockObject2.SetActive(false);
            Debug.Log($"[StageUIManager] 🔒 UnlockObject2 비활성화: {_unlockObject2.name}");
        }
    }

    /// <summary>
    /// 가림막 재활성화 (디버그용)
    /// </summary>
    void ReactivateLock()
    {
        if (_hiddenStageLock == null) return;

        if (!_hiddenStageLock.activeSelf)
        {
            _hiddenStageLock.SetActive(true);
            lockDeactivated = false;

            // Material 참조 초기화 (재생성을 위해)
            lockMaterial = null;
            lockRenderer = null;

            // ✅ 추가: 해금 오브젝트들도 비활성화
            DeactivateUnlockObjects();

            Debug.Log("[StageUIManager] 가림막 재활성화 완료");
        }
    }
    #endregion
    */ // 해금 로직 비활성화

    /* // 해금 로직 비활성화
    #region Hidden Object Monitoring
    /// <summary>
    /// HiddenObj_2 위치 모니터링 시작
    /// </summary>
    void StartMonitoringHiddenObj2()
    {
        if (_hiddenObj2 == null)
        {
            Debug.LogWarning("[StageUIManager] HiddenObj_2가 할당되지 않았습니다. Inspector에서 할당해주세요.");
            return;
        }

        lastHiddenObj2Position = _hiddenObj2.transform.position;
        isMonitoringHiddenObj2 = true;

        Debug.Log($"[StageUIManager] HiddenObj_2 위치 모니터링 시작 (초기 위치: {lastHiddenObj2Position})");
    }

    /// <summary>
    /// HiddenObj_2의 위치 변화를 감지
    /// </summary>
    void MonitorHiddenObj2Position()
    {
        Vector3 currentPosition = _hiddenObj2.transform.position;

        // 위치 변화 감지
        float distance = Vector3.Distance(currentPosition, lastHiddenObj2Position);
        if (distance > positionChangeThreshold)
        {
            Debug.Log($"[StageUIManager] HiddenObj_2 위치 변화 감지! 거리: {distance:F4}m");
            Debug.Log($"[StageUIManager] 이전: {lastHiddenObj2Position} → 현재: {currentPosition}");

            // 0.5초 후 버튼 활성화 시작
            if (unlockDelayCoroutine != null)
            {
                StopCoroutine(unlockDelayCoroutine);
            }
            unlockDelayCoroutine = StartCoroutine(UnlockHiddenStageAfterDelay(0.5f));

            // 모니터링 중지 (한 번만 실행)
            isMonitoringHiddenObj2 = false;
        }
    }

    /// <summary>
    /// 지정된 시간 후 히든 스테이지 버튼 활성화
    /// </summary>
    IEnumerator UnlockHiddenStageAfterDelay(float delay)
    {
        Debug.Log($"[StageUIManager] {delay}초 후 히든 스테이지 버튼 활성화...");

        yield return new WaitForSeconds(delay);

        if (hiddenStageButton != null)
        {
            hiddenStageButton.interactable = true;
            Debug.Log("[StageUIManager] ✅ 히든 스테이지 버튼 활성화 완료!");
        }
        else
        {
            Debug.LogError("[StageUIManager] hiddenStageButton이 null입니다.");
        }

        unlockDelayCoroutine = null;
    }
    #endregion
    */ // 해금 로직 비활성화

    /* // 해금 로직 비활성화
    #region Debug
    /// <summary>
    /// 디버깅용 키 입력 처리
    /// F1: 히든 스테이지 강제 해금
    /// F2: 모든 스테이지 별 3개로 채우기 (순차적)
    /// F3: 모든 스테이지 초기화
    /// </summary>
    private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugUnlockHiddenStage();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            DebugFillAllStars();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            DebugResetAllStars();
        }
    }

    /// <summary>
    /// [F1] 히든 스테이지 강제 해금
    /// </summary>
    private void DebugUnlockHiddenStage()
    {
        if (hiddenStageButton == null)
        {
            Debug.LogWarning("[Debug] 히든 스테이지가 존재하지 않습니다.");
            return;
        }

        isAllCleared = true;
        hiddenStageButton.interactable = true;

        // 가림막 비활성화
        if (_hiddenStageLock != null && !lockDeactivated)
        {
            DeactivateLock();
        }

        Debug.Log("[Debug] F1: 히든 스테이지 강제 해금 완료");
    }

    /// <summary>
    /// [F2] 모든 스테이지 별 3개로 채우기 (순차적)
    /// </summary>
    private void DebugFillAllStars()
    {
        if (stageDataSOs == null || stageDataSOs.Count == 0)
        {
            Debug.LogWarning("[Debug] stageDataSOs가 비어있습니다.");
            return;
        }

        // 이미 진행 중이면 중지
        if (fillStarsCoroutine != null)
        {
            StopCoroutine(fillStarsCoroutine);
        }

        fillStarsCoroutine = StartCoroutine(FillAllStarsSequentially());
    }

    /// <summary>
    /// 순차적으로 별 채우기 코루틴
    /// </summary>
    IEnumerator FillAllStarsSequentially()
    {
        Debug.Log("[Debug] F2: 모든 스테이지의 별을 순차적으로 3개로 설정합니다.");

        int stageIndex = 0;
        foreach (var stage in stageDataSOs)
        {
            // 히든 스테이지는 건너뛰기
            if (stage.SceneName == _hiddenStageName)
            {
                Debug.Log($"[Debug] {stage.StageName} (히든 스테이지) 건너뛰기");
                continue;
            }

            // 이미 3별이면 건너뛰기
            if (stage.ClearStar >= 3)
            {
                Debug.Log($"[Debug] {stage.StageName} 이미 3별 (건너뛰기)");
                stageIndex++;
                continue;
            }

            // 별을 하나씩 채우기
            for (int star = stage.ClearStar + 1; star <= 3; star++)
            {
                stage.ClearStar = star;
                stage.IsTried = true;
                StageSaveManager.UpdateStageData(stage, star, stage.StageImagePath ?? "");

                Debug.Log($"[Debug] {stage.StageName}: ⭐ {star}/3");

                // UI 갱신 (부분 갱신)
                DebugPartialRefreshUI(stageIndex, stage);

                // 가림막 투명도 실시간 업데이트
                // AllStagesCleared(); // (이 함수는 새 구조에서 다시 작성 필요)

                // 딜레이
                yield return new WaitForSeconds(starFillDelay);
            }

            stageIndex++;
        }

        Debug.Log("[Debug] 모든 스테이지 별 채우기 완료!");

        // 전체 UI 최종 갱신
        DebugRefreshStageUI();

        fillStarsCoroutine = null;
    }

    /// <summary>
    /// 특정 스테이지 UI만 부분 갱신 (★ 새 구조에서는 작동 안 함)
    /// </summary>
    private void DebugPartialRefreshUI(int stageIndex, StageDataSO stage)
    {
        // (이 로직은 stageBtns 리스트에 의존하므로 새 구조에서는 다시 작성해야 함)
        // if (stageIndex < 0 || stageIndex >= stageBtns.Count)
        //     return;
        // 
        // GameObject stageObj = stageBtns[stageIndex].transform.parent?.gameObject;
        // ...
    }

    /// <summary>
    /// [F3] 모든 스테이지 초기화
    /// </summary>
    private void DebugResetAllStars()
    {
        if (stageDataSOs == null || stageDataSOs.Count == 0)
        {
            Debug.LogWarning("[Debug] stageDataSOs가 비어있습니다.");
            return;
        }

        // 진행 중인 별 채우기 중지
        if (fillStarsCoroutine != null)
        {
            StopCoroutine(fillStarsCoroutine);
            fillStarsCoroutine = null;
        }

        Debug.Log("[Debug] F3: 모든 스테이지를 초기화합니다.");

        foreach (var stage in stageDataSOs)
        {
            stage.ClearStar = 0;
            stage.IsTried = false;
            StageSaveManager.UpdateStageData(stage, 0, "");
        }

        // UI 갱신 (★ 새 구조에서는 작동 안 함)
        DebugRefreshStageUI();

        Debug.Log("[Debug] 모든 스테이지 초기화 완료.");
    }

    /// <summary>
    /// 디버깅용: 스테이지 UI 갱신 (★ 새 구조에서는 작동 안 함)
    /// </summary>
    private void DebugRefreshStageUI()
    {
        // (이 로직은 _stageTarget에 UI를 재생성하는 로직이라 새 구조에서는 작동 안 함)
        // foreach (var btn in stageBtns) ...
        // foreach (Transform child in _stageTarget.transform) ...
        // SetStageUI();
    }
    #endregion
    */ // 해금 로직 비활성화
    #endregion
}