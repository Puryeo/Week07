using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스테이지 클리어 조건 체크 및 결과 화면을 관리하는 스크립트입니다.
/// StageConfig와 연동하여 목표 폭탄 개수를 확인하고, 별점 시스템을 관리합니다.
/// </summary>
public class ClearManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _exitBtn;
    [SerializeField] private Button _nextStageBtn;
    [SerializeField] private GameObject _starPanel;
    [SerializeField] private TextMeshProUGUI _remainBombText;
    [SerializeField] private GameObject _clearPanel;
    [SerializeField] private Image _clearImage;
    [SerializeField] private GameObject _clearStarPanel;
    [SerializeField] private Sprite _emptyStar;
    private int _maxBomb;

    [Header("CountDown")]
    [SerializeField] private GameObject _countDownPanel;
    [SerializeField] private TextMeshProUGUI _countDownText;
    [SerializeField] private GameObject _cameraRect;
    [SerializeField] private Image _cameraFlashImage;
    private Coroutine _countDownCoroutine;

    [Header("Snapshot")]
    [SerializeField] private Camera _snapshotCamera;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private bool _isSnapshotPivot = false;
    private string _snapShotPath;

    [Header("Clear")]
    [SerializeField] private int _3star = 0;
    [SerializeField] private int _2star = 5;
    [SerializeField] private int _1star = 10;
    [SerializeField] private float _waitClimax = 2;
    private int _clearStarCount = 3;
    private bool _isExitClicked = false;

    [Header("References")]
    [SerializeField] private ClimaxController_Advanced climax;

    /// <summary>
    /// 초기화 시 StageConfig와 BombManager를 통해 목표 폭탄 개수를 설정하고,
    /// 필요한 이벤트를 구독합니다.
    /// </summary>
    private void Awake()
    {
        _exitBtn.onClick.AddListener(ExitBtn);
        _nextStageBtn.onClick.AddListener(NextBtn);

        // StageConfig를 통해 목표 폭탄 개수 가져오기
        if (StageConfig.Instance != null)
        {
            _maxBomb = StageConfig.Instance.GetGoalBombCount();
        }
        else
        {
            // StageConfig가 없으면 기존 방식 사용 (하위 호환성)
            Debug.LogWarning("[ClearManager] StageConfig를 찾을 수 없습니다. 기존 방식으로 폭탄 개수를 계산합니다.");
            _maxBomb = BombManager.Instance.GetTotalBombCount();
        }

        // 초기 텍스트를 현재 실제 폭탄 개수로 설정
        // 동적 생성 스테이지에서 0/2 같은 형태로 올바르게 표시
        int initialRemaining = StageConfig.Instance != null
            ? StageConfig.Instance.GetRemainingGoalBombCount()
            : BombManager.Instance.GetActiveBombCount();

        _remainBombText.text = $"{initialRemaining}/{_maxBomb}";

        // 폭탄 개수 변경 이벤트 구독
        BombManager.Instance.OnBombCountChanged += RemainBombTextUpdate;

        // Draggable 개수 변경 이벤트 구독
        BombManager.Instance.OnDraggableCountChanged += ClearStarChange;
    }

    private void OnDisable()
    {
        _exitBtn.onClick.RemoveAllListeners();
        _nextStageBtn.onClick.RemoveAllListeners();

        if (BombManager.Instance != null)
        {
            BombManager.Instance.OnBombCountChanged -= RemainBombTextUpdate;
            BombManager.Instance.OnDraggableCountChanged -= ClearStarChange;
        }

        if (_countDownCoroutine != null)
        {
            StopCoroutine(_countDownCoroutine);
            _countDownCoroutine = null;
        }
    }

    /// <summary>
    /// 남은 폭탄 개수 텍스트를 업데이트합니다.
    /// 목표 폭탄이 모두 폭발하면 클리어 체크를 시작합니다.
    /// 동적 생성 스테이지를 위해 최소 1개 이상 폭발했는지도 확인합니다.
    /// </summary>
    /// <param name="remainBomb">남은 폭탄 개수</param>
    void RemainBombTextUpdate(int remainBomb)
    {
        // StageConfig 모드에 따라 남은 폭탄 개수 계산
        int actualRemaining = remainBomb;

        if (StageConfig.Instance != null)
        {
            actualRemaining = StageConfig.Instance.GetRemainingGoalBombCount();
        }

        _remainBombText.text = $"{actualRemaining}/{_maxBomb}";

        // 폭발한 폭탄 개수 계산
        int explodedCount = _maxBomb - actualRemaining;

        // 디버그 로그 추가
        Debug.Log($"<color=cyan>[ClearManager]</color> 폭탄 개수 업데이트\n" +
                  $"목표: {_maxBomb} | 남은 개수: {actualRemaining} | 폭발: {explodedCount}\n" +
                  $"조건 체크 - 남은개수<=0: {actualRemaining <= 0}, 폭발>0: {explodedCount > 0}, 퇴장X: {!_isExitClicked}");

        // 클리어 조건
        if (actualRemaining <= 0 && explodedCount > 0 && !_isExitClicked)
        {
            Debug.Log("<color=green>[ClearManager]</color> 클리어 조건 만족! ClearChecker 호출");
            ClearChecker();
        }
    }

    /// <summary>
    /// Draggable 오브젝트가 낙하할 때마다 별점을 계산합니다.
    /// 낙하 개수에 따라 3성 -> 2성 -> 1성 -> 0성으로 감소합니다.
    /// </summary>
    /// <param name="draggable">트리거된 Draggable 개수</param>
    void ClearStarChange(int draggable)
    {
        // 2별
        if (draggable <= _2star && draggable > _3star)
        {
            var target = _starPanel.transform.GetChild(2);
            target.GetComponent<Image>().sprite = _emptyStar;
            _clearStarCount = 2;
        }
        // 1별
        else if (draggable <= _1star && draggable > _2star)
        {
            var target = _starPanel.transform.GetChild(1);
            target.GetComponent<Image>().sprite = _emptyStar;
            _clearStarCount = 1;
        }
        // 0별
        else if (draggable > _1star)
        {
            for (int i = 0; i < 3; i++)
            {
                var target = _starPanel.transform.GetChild(i);
                target.GetComponent<Image>().sprite = _emptyStar;
            }
            _clearStarCount = 0;
        }
        // 포기 (중도 퇴장)
        else if (draggable == 10000)
        {
            for (int i = 0; i < 3; i++)
            {
                var target = _starPanel.transform.GetChild(i);
                target.GetComponent<Image>().sprite = _emptyStar;
                _clearStarCount = 0;
            }
        }
    }

    /// <summary>
    /// 클리어 조건이 만족되었는지 확인하고 카운트다운을 시작합니다.
    /// </summary>
    void ClearChecker()
    {
        if (_countDownCoroutine != null)
        {
            Debug.Log("카운트다운 코루틴 진행 중");
            return;
        }

        _countDownCoroutine = StartCoroutine(CountDownCoroutine());
    }

    /// <summary>
    /// 카운트다운 텍스트의 알파값을 초기화합니다.
    /// </summary>
    void ResetCountDownNum()
    {
        _countDownText.color = new Color(_countDownText.color.r, _countDownText.color.g, _countDownText.color.b, 1f);
    }

    /// <summary>
    /// 카운트다운 텍스트를 서서히 페이드아웃 시킵니다.
    /// </summary>
    void FadeOutCountDownNum()
    {
        _countDownText.color -= new Color(0f, 0f, 0f, 1f * Time.deltaTime);
    }

    /// <summary>
    /// 카메라 플래시 효과를 시작합니다.
    /// </summary>
    void FlashingEffect()
    {
        flashAlpha = 1.5f;
        _cameraFlashImage.color = new Color(1f, 1f, 1f, 1f);
    }

    private float flashAlpha = 1f;

    /// <summary>
    /// 플래시 이미지를 서서히 페이드아웃 시킵니다.
    /// </summary>
    void FaseOutFlashImage()
    {
        if (flashAlpha > 1f)
            _cameraFlashImage.color = new Color(1f, 1f, 1f, 1f);
        else
            _cameraFlashImage.color = new Color(1f, 1f, 1f, flashAlpha);

        flashAlpha -= 0.8f * Time.deltaTime;
        if (flashAlpha < 0f)
            flashAlpha = 0f;
    }

    void Update()
    {
        FadeOutCountDownNum();
        FaseOutFlashImage();
    }

    /// <summary>
    /// 클리어 시 3초 카운트다운 후 스냅샷을 찍고 클리어 패널을 표시합니다.
    /// </summary>
    IEnumerator CountDownCoroutine()
    {
        _countDownPanel.SetActive(true);
        _cameraRect.SetActive(true);

        // 3초 카운트 다운
        for (int i = 3; i > 0; i--)
        {
            _countDownText.text = i.ToString();
            ResetCountDownNum();
            yield return new WaitForSeconds(1);
        }

        _countDownCoroutine = null;

        // 카메라 찰칵 연출
        _cameraRect.SetActive(false);
        _cameraFlashImage.gameObject.SetActive(true);
        FlashingEffect();

        SnapShot();

        // 클리어 패널 활성화
        ShowClearPanel();
    }

    /// <summary>
    /// 현재 화면을 스냅샷으로 캡처하여 파일로 저장합니다.
    /// 저장된 이미지는 StageManager를 통해 스테이지 선택 화면에서 표시됩니다.
    /// </summary>
    void SnapShot()
    {
        if (_snapshotCamera == null || _mainCamera == null)
        {
            Debug.LogError("SnapShotCamera or MainCamera 미설정");
            return;
        }

        // 스냅샷 전용 카메라 or 메인 카메라 기준
        if (!_isSnapshotPivot)
        {
            // 메인 카메라로 transform 변경
            _snapshotCamera.transform.position = _mainCamera.transform.position;
            _snapshotCamera.transform.rotation = _mainCamera.transform.rotation;
        }

        // 렌더링용 텍스처 생성
        int width = Screen.width;
        int height = Screen.height;
        RenderTexture rt = new RenderTexture(width, height, 24);
        _snapshotCamera.targetTexture = rt;

        // 스크린샷용 텍스처 준비
        Texture2D screenTex = new Texture2D(width, height, TextureFormat.RGB24, false);

        // 카메라 렌더링 실행
        _snapshotCamera.Render();

        // RenderTexture -> Texture2D 복사
        RenderTexture.active = rt;
        screenTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenTex.Apply();

        // 카메라와 렌더텍스처 정리
        _snapshotCamera.targetTexture = null;
        RenderTexture.active = null;

        // RenderTexture 안전하게 해제
        rt.Release();
        Destroy(rt);

        // 저장 폴더 지정
        string folderPath = Path.Combine(Application.persistentDataPath, "StageImages");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // 파일명: 현재 씬 이름
        string sceneName = SceneManager.GetActiveScene().name;
        string fileName = $"{sceneName}.png";
        _snapShotPath = Path.Combine(folderPath, fileName);

        // PNG로 저장
        byte[] bytes = screenTex.EncodeToPNG();
        File.WriteAllBytes(_snapShotPath, bytes);

        Debug.Log($"스냅샷 저장 완료: {_snapShotPath}");

        // 메모리 정리
        Destroy(screenTex);

        // StageManager에 클리어 정보 업데이트
        SendClearInfo();
    }

    /// <summary>
    /// 촬영한 스냅샷과 획득한 별 개수를 클리어 패널에 표시합니다.
    /// </summary>
    void ShowClearPanel()
    {
        // 스냅샷 로드
        _snapShotPath = _snapShotPath.Replace("\\", "/");
        Debug.Log("클리어 패널 활성화");
        if (!string.IsNullOrEmpty(_snapShotPath))
        {
#if UNITY_EDITOR
            string fullPath = Path.IsPathRooted(_snapShotPath)
                ? _snapShotPath
                : Path.Combine(Application.dataPath, _snapShotPath);
#else
            string fullPath = Path.IsPathRooted(_snapShotPath)
                ? _snapShotPath
                : Path.Combine(Application.persistentDataPath, _snapShotPath);
#endif
            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);

            _clearImage.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );
        }
        Debug.Log("클리어 패널 활성화 직전");
        _clearPanel.SetActive(true);

        // 별 개수 그리기
        int _emptyStarCount = 3 - _clearStarCount;

        for (int i = 0; i < _emptyStarCount; i++)
        {
            int index = _clearStarPanel.transform.childCount - 1 - i; // 뒤에서부터
            _clearStarPanel.transform.GetChild(index).GetComponent<Image>().sprite = _emptyStar;
        }
    }

    /// <summary>
    /// 중도 포기 버튼입니다.
    /// 잔여 폭탄을 모두 폭발시킨 후 스냅샷을 찍고 스테이지 선택 화면으로 이동합니다.
    /// </summary>
    private void ExitBtn()
    {
        _countDownPanel.SetActive(true);
        _cameraRect.SetActive(true);

        // 잔여 폭탄 폭발
        climax.StartClimaxSequence();
        StartCoroutine(WaitForClimax());

        // 3,2,1 코루틴 방지
        _isExitClicked = true;

        // 중도 포기 코드 값 10000
        ClearStarChange(10000);
    }

    /// <summary>
    /// 클리어 정보(별 개수, 스냅샷 경로)를 StageManager에 전송합니다.
    /// </summary>
    private void SendClearInfo()
    {
        // 블럭 업데이트 고려해서 보내기 직전 한번 더 덮어씌움
        if (_isExitClicked)
        {
            _clearStarCount = 0;
        }

        StageManager.Instance.UpdateClearData(_clearStarCount, _snapShotPath);
    }

    /// <summary>
    /// 다음 스테이지로 이동하는 버튼입니다.
    /// </summary>
    private void NextBtn()
    {
        SceneManager.LoadScene("STAGE");
    }

    /// <summary>
    /// 중도 포기 시 클라이맥스 폭발 대기 후 스냅샷을 찍고 스테이지 선택 화면으로 이동합니다.
    /// </summary>
    IEnumerator WaitForClimax()
    {
        for (int i = 2; i > 0; i--)
        {
            _countDownText.text = i.ToString();
            ResetCountDownNum();
            yield return new WaitForSeconds(1);
        }

        _countDownCoroutine = null;

        // 카메라 찰칵 연출
        _cameraRect.SetActive(false);
        _cameraFlashImage.gameObject.SetActive(true);
        FlashingEffect();

        SnapShot();
        ShowClearPanel();
        _nextStageBtn.gameObject.SetActive(false);
        yield return new WaitForSeconds(_waitClimax);

        SceneManager.LoadScene("STAGE");
    }
}