using UnityEngine;

/// <summary>
/// 마우스와 키보드(WASD/QE) 입력을 사용해 타겟 주위를 공전하고 줌하는 카메라 스크립트입니다.
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    [Header("타겟 설정")]
    [Tooltip("카메라가 바라볼 타겟 오브젝트입니다.")]
    [SerializeField] private Transform target;

    [Header("궤도 설정")]
    [Tooltip("타겟으로부터의 초기 거리입니다.")]
    [SerializeField] private float distance = 5.0f;
    [Tooltip("마우스를 사용한 수평/수직 회전 속도입니다.")]
    [SerializeField] private float xSpeed = 120.0f;
    [SerializeField] private float ySpeed = 120.0f;

    [Header("키보드 설정")]
    // <<< 1번 요청: WASD 회전 속도 변수 추가
    [Tooltip("WASD 키를 사용한 수평/수직 회전 속도입니다.")]
    [SerializeField] private float keyOrbitSpeed = 60.0f;

    [Header("줌 설정")]
    // <<< 3, 4번 요청: 줌 속도 툴팁 변경
    [Tooltip("마우스 휠 및 QE 키를 사용한 줌 속도입니다.")]
    [SerializeField] private float zoomSpeed = 5.0f;
    [SerializeField] private float keyZoomSpeed = 20.0f;

    [Header("제한 값")]
    [Tooltip("카메라의 최소/최대 고도(수직 각도)입니다.")]
    [SerializeField] private float yMinLimit = -20f;
    [SerializeField] private float yMaxLimit = 80f;
    [Tooltip("카메라의 최소/최대 줌 거리입니다.")]
    [SerializeField] private float distanceMin = .5f;
    [SerializeField] private float distanceMax = 15f;

    // 현재 카메라의 회전 각도를 저장하는 변수
    private float x = 0.0f;
    private float y = 0.0f;

    // 스크립트가 시작될 때 한 번 호출됩니다.
    void Start()
    {
        // 현재 카메라의 오일러 각도를 초기값으로 설정합니다.
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    // 모든 Update 함수가 호출된 후 프레임마다 호출됩니다.
    void LateUpdate()
    {
        // 타겟이 설정되어 있는지 확인합니다.
        if (target)
        {
            // --- 1. 마우스 궤도 회전 (우클릭) ---
            if (Input.GetMouseButton(1))
            {
                x += Input.GetAxis("Mouse X") * xSpeed;
                y -= Input.GetAxis("Mouse Y") * ySpeed;

                // <<< 2번 요청: 마우스 우클릭 중 휠 줌 기능 제거 (해당 코드 삭제)
            }

            // --- 2. 키보드 궤도 회전 (WASD) ---
            // <<< 1번 요청: WASD로 궤도 회전 기능 추가
            if (Input.GetKey(KeyCode.W))
            {
                y += keyOrbitSpeed * Time.deltaTime; // 상
            }
            if (Input.GetKey(KeyCode.S))
            {
                y -= keyOrbitSpeed * Time.deltaTime; // 하
            }
            if (Input.GetKey(KeyCode.A))
            {
                x += keyOrbitSpeed * Time.deltaTime; // 좌
            }
            if (Input.GetKey(KeyCode.D))
            {
                x -= keyOrbitSpeed * Time.deltaTime; // 우
            }

            // --- 3. 줌 (휠 & QE) ---

            // <<< 3번 요청: 좌클릭을 안 할 때 마우스 휠 줌
            if (!CursorManager.Instance.isGrabbed || (CursorManager.Instance.isGrabbed && Input.GetMouseButton(1)))
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                distance -= scroll * zoomSpeed;
            }

            // <<< 4번 요청: QE 키로 줌
            if (Input.GetKey(KeyCode.Q))
            {
                distance += keyZoomSpeed * Time.deltaTime; // 줌 아웃
            }
            if (Input.GetKey(KeyCode.E))
            {
                distance -= keyZoomSpeed * Time.deltaTime; // 줌 인
            }

            // --- 4. 값 제한 (Clamping) ---

            // y(수직) 회전 각도를 지정된 최소/최대 값 사이로 제한합니다. (마우스, 키보드 입력 모두 적용)
            y = ClampAngle(y, yMinLimit, yMaxLimit);

            // 거리를 최소/최대 값 사이로 제한합니다. (휠, QE 입력 모두 적용)
            distance = Mathf.Clamp(distance, distanceMin, distanceMax);

            // --- 5. 카메라 위치/회전 최종 적용 ---

            // 계산된 회전 값으로 Quaternion을 생성합니다.
            Quaternion rotation = Quaternion.Euler(y, x, 0);

            // 타겟 위치에서 계산된 거리와 회전 값을 적용하여 카메라의 목표 위치를 계산합니다.
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position;

            // 계산된 위치와 회전 값을 카메라의 transform에 적용합니다.
            transform.rotation = rotation;
            transform.position = position;
        }
    }

    /// <summary>
    /// 각도를 주어진 최소값과 최대값 사이로 제한하는 헬퍼 함수입니다.
    /// </summary>
    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}