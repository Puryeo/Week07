using UnityEngine;
using System.Collections;

/// <summary>
/// IExplodable 인터페이스를 구현하는 기본 폭탄 엔티티입니다.
/// MaterialPropertyBlock 기반 점멸 효과와 폭발 위임을 처리합니다.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class NewBombEntityDefault : MonoBehaviour, IExplodable
{
    #region Serialized Fields
    [Header("Explosion Settings")]
    [Tooltip("이 폭탄의 폭발 설정 프로필입니다.")]
    [SerializeField] private ExplosionProfileSO _explosionProfile;

    [Header("Ticking Settings")]
    [Tooltip("점멸 주기(초)입니다.")]
    [SerializeField] private float _tickingInterval = 1.0f;

    [Header("Debug Settings")]
    [SerializeField] private bool _isDebugLogging = false;
    #endregion

    #region Private Fields
    private Renderer _cachedRenderer;
    private MaterialPropertyBlock _materialPropertyBlock;
    private Color _originalColor;
    private Coroutine _tickingCoroutine;
    private static readonly int ColorPropertyID = Shader.PropertyToID("_BaseColor");
    #endregion

    #region Properties
    /// <summary>디버그 로깅 활성화 여부</summary>
    public bool IsDebugLogging => _isDebugLogging;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Initialize();
    }

    private void Start()
    {
        LateInitialize();
    }

    private void OnDestroy()
    {
        Cleanup();
    }
    #endregion

    #region Initialization and Cleanup
    /// <summary>의존성이 필요 없는 내부 초기화</summary>
    public void Initialize()
    {
        _cachedRenderer = GetComponent<Renderer>();

        if (_cachedRenderer == null)
        {
            LogError("Renderer 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        _materialPropertyBlock = new MaterialPropertyBlock();
        _cachedRenderer.GetPropertyBlock(_materialPropertyBlock);

        // 원본 색상 저장
        if (_materialPropertyBlock.isEmpty)
        {
            _originalColor = _cachedRenderer.sharedMaterial.GetColor(ColorPropertyID);
        }
        else
        {
            _originalColor = _materialPropertyBlock.GetColor(ColorPropertyID);
        }

        _tickingCoroutine = null;

        Log("초기화 완료: Renderer 및 MaterialPropertyBlock 준비됨");
    }

    /// <summary>외부 의존성이 필요한 초기화</summary>
    public void LateInitialize()
    {
        if (_explosionProfile == null)
        {
            LogWarning("ExplosionProfile이 할당되지 않았습니다!");
        }

        Log("LateInitialize 완료");
    }

    /// <summary>소멸 프로세스</summary>
    public void Cleanup()
    {
        StopTicking();

        if (_cachedRenderer != null && _materialPropertyBlock != null)
        {
            _materialPropertyBlock.SetColor(ColorPropertyID, _originalColor);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        Log("Cleanup: 원본 색상 복원 완료");
    }
    #endregion

    #region IExplodable Implementation
    /// <summary>
    /// 이 객체의 폭발 설정 프로필을 반환합니다.
    /// </summary>
    public ExplosionProfileSO GetExplosionProfile()
    {
        return _explosionProfile;
    }

    /// <summary>
    /// 점멸 효과를 위한 Renderer를 반환합니다.
    /// </summary>
    public Renderer GetRenderer()
    {
        return _cachedRenderer;
    }

    /// <summary>
    /// 이 객체를 폭발시킵니다.
    /// </summary>
    public void Explode()
    {
        if (NewBombExplodeSystem.Instance == null)
        {
            LogError("NewBombExplodeSystem 인스턴스를 찾을 수 없습니다!");
            return;
        }

        Log($"{gameObject.name} 폭발 요청");
        NewBombExplodeSystem.Instance.Execute(this);
    }

    /// <summary>
    /// 폭발 후 호출되는 콜백입니다.
    /// </summary>
    public void AfterExploded()
    {
        gameObject.SetActive(false);
        Log($"{gameObject.name} 비활성화");
    }

    /// <summary>
    /// 점멸 효과를 시작합니다.
    /// </summary>
    /// <param name="duration">점멸 지속 시간(초)</param>
    public void StartTicking(float duration)
    {
        if (_tickingCoroutine != null)
        {
            StopCoroutine(_tickingCoroutine);
        }

        _tickingCoroutine = StartCoroutine(TickingCoroutine(duration));
        Log($"점멸 시작: {duration}초");
    }

    /// <summary>
    /// 점멸 효과를 중지합니다.
    /// </summary>
    public void StopTicking()
    {
        if (_tickingCoroutine != null)
        {
            StopCoroutine(_tickingCoroutine);
            _tickingCoroutine = null;
        }

        if (_cachedRenderer != null && _materialPropertyBlock != null)
        {
            _materialPropertyBlock.SetColor(ColorPropertyID, _originalColor);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        Log("점멸 중지");
    }
    #endregion

    #region Private Methods - Coroutine
    /// <summary>
    /// 점멸 효과 코루틴
    /// </summary>
    private IEnumerator TickingCoroutine(float duration)
    {
        if (_cachedRenderer == null || _materialPropertyBlock == null || _explosionProfile == null)
        {
            LogError("점멸 효과에 필요한 컴포넌트가 누락되었습니다!");
            yield break;
        }

        Color tickingColor = _explosionProfile.TickingColor;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // 점멸 색상으로 변경
            _materialPropertyBlock.SetColor(ColorPropertyID, tickingColor);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
            yield return new WaitForSeconds(_tickingInterval / 2);

            // 원본 색상으로 복원
            _materialPropertyBlock.SetColor(ColorPropertyID, _originalColor);
            _cachedRenderer.SetPropertyBlock(_materialPropertyBlock);
            yield return new WaitForSeconds(_tickingInterval / 2);

            elapsedTime += _tickingInterval;
        }

        Log("점멸 완료");
    }
    #endregion

    #region Private Methods - Debug Logging
    /// <summary>일반 로그 출력</summary>
    /// <param name="message">로그 메시지</param>
    private void Log(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.Log($"<color=cyan>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>경고 로그 출력</summary>
    /// <param name="message">경고 메시지</param>
    private void LogWarning(string message, bool forcely = false)
    {
        if (_isDebugLogging || forcely)
            Debug.LogWarning($"<color=cyan>[{GetType().Name}]</color> {message}", this);
    }

    /// <summary>에러 로그 출력 - 항상 강제 출력</summary>
    /// <param name="message">에러 메시지</param>
    private void LogError(string message)
    {
        Debug.LogError($"<color=cyan>[{GetType().Name}]</color> {message}", this);
    }
    #endregion
}