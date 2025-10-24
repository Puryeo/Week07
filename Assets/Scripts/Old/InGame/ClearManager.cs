using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] ClimaxController_Advanced climax;

    private void OnEnable()
    {
        _exitBtn.onClick.AddListener(ExitBtn);
        _nextStageBtn.onClick.AddListener(NextBtn);

        // 폭탄 개수 처리
        _maxBomb = BombManager.Instance.GetTotalBombCount();
        _remainBombText.text = $"{_maxBomb}/{_maxBomb}";
        BombManager.Instance.OnBombCountChanged += RemainBombTextUpdate;

        // 일반 개수 변경 이벤트 구독
        BombManager.Instance.OnDraggableCountChanged += ClearStarChange;
    }

    private void OnDisable()
    {
        _exitBtn.onClick.RemoveAllListeners();
        _nextStageBtn.onClick.RemoveAllListeners();

        BombManager.Instance.OnBombCountChanged -= RemainBombTextUpdate;
        BombManager.Instance.OnDraggableCountChanged -= ClearStarChange;

        if (_countDownCoroutine != null)
        {
            StopCoroutine(_countDownCoroutine);
            _countDownCoroutine = null;
        }
    }

    void RemainBombTextUpdate(int remainBomb)
    {
        _remainBombText.text = $"{remainBomb}/{_maxBomb}";

        if (remainBomb <= 0 && !_isExitClicked)
        {
            ClearChecker();
        }
    }


    void ClearStarChange(int draggable)
    {
        // 2별
        if (draggable <= _2star && draggable > _3star)
        {
            var target = _starPanel.transform.GetChild(2);
            target.GetComponent<Image>().sprite = _emptyStar;
            _clearStarCount = 2;

        }// 1별
        else if (draggable <= _1star && draggable > _2star)
        {
            var target = _starPanel.transform.GetChild(1);
            target.GetComponent<Image>().sprite = _emptyStar;
            _clearStarCount = 1;
        }// 포기
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

    void ClearChecker()
    {
        if (_countDownCoroutine != null)
        {
            Debug.Log("카운트다운 코루틴 진행 중");
            return;
        }

        _countDownCoroutine = StartCoroutine(CountDownCoroutine());

    }

    void ResetCountDownNum()
    {
        _countDownText.color = new Color(_countDownText.color.r, _countDownText.color.g, _countDownText.color.b, 1f);
    }

    void FadeOutCountDownNum()
    {
        _countDownText.color -= new Color(0f, 0f, 0f, 1f * Time.deltaTime);
    }

    void FlashingEffect()
    {
        flashAlpha = 1.5f;
        _cameraFlashImage.color = new Color(1f, 1f, 1f, 1f);
    }

    private float flashAlpha = 1f;

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

        // 스냅샷 호출

        // 카메라 찰칵 연출은 여기서
        _cameraRect.SetActive(false);
        _cameraFlashImage.gameObject.SetActive(true);
        FlashingEffect();

        SnapShot();

        // 클리어 패널 활성화
        ShowClearPanel();
    }

    // 스테이지 끝나면 스냅샷
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

    // 찍힌 스냅샷, 클리어 별 개수 보여주기
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

    // 잔여 폭탄 폭발 이후, 스냅샷 찍고 나감
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


    private void SendClearInfo()
    {
        // 블럭 업데이트 고려해서 보내기 직전 한번 더 덮어씌움
        if (_isExitClicked)
        {
            _clearStarCount = 0;
        }

        StageManager.Instance.UpdateClearData(_clearStarCount, _snapShotPath);
    }


    private void NextBtn()
    {
        SceneManager.LoadScene("STAGE");

    }

    IEnumerator WaitForClimax()
    {
        for (int i = 2; i > 0; i--)
        {
            _countDownText.text = i.ToString();
            ResetCountDownNum();
            yield return new WaitForSeconds(1);
        }
        // yield return new WaitForSeconds(_waitClimax);

        _countDownCoroutine = null;

        // 스냅샷 호출

        // 카메라 찰칵 연출은 여기서
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
