using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KingdomDefense.Cameras
{
    /// <summary>
    /// Controller điều khiển Camera góc nhìn Isometric cho game Tower Defense / RTS.
    /// Hỗ trợ di chuyển WASD/mũi tên theo hướng nhìn camera, cuộn chuột Zoom mượt mà,
    /// rê chuột mép màn hình (Edge Pan), kéo chuột giữa và giới hạn biên bản đồ.
    /// Tương thích cả Unity New Input System lẫn Legacy Input Manager.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("--- CẤU HÌNH GÓC NHÌN ISOMETRIC ---")]
        [Tooltip("Tự động đặt góc quay isometric khi bắt đầu")]
        [SerializeField] private bool autoAlignIsometric = true;
        [Tooltip("Góc nghiêng chuẩn nhìn thẳng vào sa bàn: X = 60, Y = 0, Z = 0")]
        [SerializeField] private Vector3 isometricEulerAngles = new Vector3(60f, 0f, 0f);

        [Header("--- TỐC ĐỘ DI CHUYỂN (WASD) ---")]
        [Tooltip("Tốc độ di chuyển cơ bản")]
        [SerializeField] private float panSpeed = 20f;
        [Tooltip("Hệ số tăng tốc khi giữ phím Shift")]
        [SerializeField] private float sprintMultiplier = 2f;
        [Tooltip("Độ mượt mà khi di chuyển (càng cao càng dừng nhanh)")]
        [SerializeField] private float smoothSpeed = 10f;

        [Header("--- THIẾT LẬP ZOOM (CUỘN CHUỘT) ---")]
        [Tooltip("Tốc độ zoom khi cuộn chuột")]
        [SerializeField] private float zoomSpeed = 25f;
        [Tooltip("Độ cao camera tối thiểu (zoom gần nhất)")]
        [SerializeField] private float minHeight = 10f;
        [Tooltip("Độ cao camera tối đa (zoom xa nhất)")]
        [SerializeField] private float maxHeight = 35f;

        [Header("--- RÊ CHUỘT MÉP MÀN HÌNH (EDGE PAN) ---")]
        [Tooltip("Bật/tắt tính năng rê chuột ra mép để cuộn màn hình (mặc định tắt để tránh trôi camera trong Editor)")]
        [SerializeField] private bool enableEdgePanning = false;
        [Tooltip("Bề dày vùng mép màn hình kích hoạt cuộn (pixel)")]
        [SerializeField] private float edgePanThickness = 15f;

        [Header("--- KÉO CHUỘT GIỮA (DRAG PAN) ---")]
        [Tooltip("Bật tính năng giữ chuột giữa để kéo màn hình")]
        [SerializeField] private bool enableMouseDrag = true;
        [Tooltip("Độ nhạy khi kéo chuột giữa")]
        [SerializeField] private float dragSensitivity = 0.5f;

        [Header("--- GIỚI HẠN BIÊN BẢN ĐỒ (BOUNDS) ---")]
        [Tooltip("Giới hạn tối thiểu tọa độ (X, Z)")]
        [SerializeField] private Vector2 minBounds = new Vector2(-25f, -25f);
        [Tooltip("Giới hạn tối đa tọa độ (X, Z)")]
        [SerializeField] private Vector2 maxBounds = new Vector2(25f, 20f);

        // Vị trí mục tiêu nội suy mượt mà
        private Vector3 _targetPosition;
        private Vector3 _currentBasePosition;
        private Vector2 _lastMousePosition;
        private bool _isDragging;

        [Header("--- THIẾT LẬP RUNG MÀN HÌNH (SCREEN SHAKE) ---")]
        [Tooltip("Bật/tắt hiệu ứng rung màn hình (Hỗ trợ người chơi nhạy cảm chuyển động)")]
        [SerializeField] private bool enableScreenShake = true;
        [Tooltip("Hệ số cường độ rung màn hình (0.0: tắt hẳn, 1.0: chuẩn, 2.0: rung mạnh)")]
        [Range(0f, 2f)]
        [SerializeField] private float shakeIntensityMultiplier = 1.0f;

        private Vector3 _shakeOffset = Vector3.zero;
        private float _shakeDuration = 0f;
        private float _totalShakeDuration = 0f;
        private float _shakeMagnitude = 0.35f;
        private float _shakeTimer = 0f;

        public bool EnableScreenShake
        {
            get => enableScreenShake;
            set
            {
                enableScreenShake = value;
                if (!enableScreenShake)
                {
                    StopShake();
                }
            }
        }

        public float ShakeIntensityMultiplier
        {
            get => shakeIntensityMultiplier;
            set => shakeIntensityMultiplier = Mathf.Clamp(value, 0f, 2f);
        }

        public Vector3 CurrentShakeOffset => _shakeOffset;
        public bool IsShaking => _shakeDuration > 0f && enableScreenShake && shakeIntensityMultiplier > 0.001f;
        public float RemainingShakeDuration => _shakeDuration;
        public float ShakeMagnitude => _shakeMagnitude;
        public Vector3 CurrentBasePosition => _currentBasePosition;
        public Vector3 TargetPosition => _targetPosition;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _currentBasePosition = transform.position;
                _targetPosition = transform.position;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static void Shake(float duration = 0.4f, float magnitude = 0.35f)
        {
            if (Instance == null)
            {
                Instance = Object.FindAnyObjectByType<CameraController>();
                if (Instance == null && Camera.main != null)
                {
                    Instance = Camera.main.gameObject.AddComponent<CameraController>();
                }
            }

            if (Instance != null)
            {
                Instance.TriggerShake(duration, magnitude);
            }
        }

        /// <summary>
        /// Rung chấn cực mạnh khi thiên thạch kỹ năng Meteor va chạm mặt đất (0.5s, độ rung 0.45).
        /// </summary>
        public static void ShakeMeteor() => Shake(0.5f, 0.45f);

        /// <summary>
        /// Rung chấn cảnh báo nguy cấp khi quái vật lọt vào thành / mất mạng (0.35s, độ rung 0.35).
        /// </summary>
        public static void ShakeCastleBreach() => Shake(0.35f, 0.35f);

        /// <summary>
        /// Rung chấn uy lực vừa phải khi đạn pháo phát nổ diện rộng (0.25s, độ rung 0.20).
        /// </summary>
        public static void ShakeCannonExplosion() => Shake(0.25f, 0.20f);

        /// <summary>
        /// Rung nhịp đất nhẹ khi Boss Orcus dậm chân chấn động (0.2s, độ rung 0.15).
        /// </summary>
        public static void ShakeBossStomp() => Shake(0.2f, 0.15f);

        public void TriggerShake(float duration, float magnitude)
        {
            if (!enableScreenShake || shakeIntensityMultiplier <= 0.001f) return;

            _shakeDuration = Mathf.Max(_shakeDuration, duration);
            _totalShakeDuration = Mathf.Max(_totalShakeDuration, _shakeDuration);
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
        }

        public void StopShake()
        {
            _shakeDuration = 0f;
            _totalShakeDuration = 0f;
            _shakeMagnitude = 0f;
            _shakeOffset = Vector3.zero;
        }

        /// <summary>
        /// Cập nhật tính toán rung chấn: Trauma quadratic decay (u^2) + Perlin noise đa trục.
        /// Tách biệt hoàn toàn khỏi logic di chuyển để triệt tiêu hiện tượng trôi camera (Zero-drift).
        /// </summary>
        public void UpdateShake(float deltaTime)
        {
            if (_shakeDuration > 0f && enableScreenShake && shakeIntensityMultiplier > 0.001f)
            {
                float u = _totalShakeDuration > 0.0001f ? Mathf.Clamp01(_shakeDuration / _totalShakeDuration) : 0f;
                float trauma = u * u * _shakeMagnitude * shakeIntensityMultiplier;

                _shakeTimer += deltaTime * 25f;
                float offsetX = (Mathf.PerlinNoise(_shakeTimer, 0.15f) - 0.5f) * 2f * trauma;
                float offsetY = (Mathf.PerlinNoise(0.48f, _shakeTimer) - 0.5f) * 2f * trauma;
                float offsetZ = (Mathf.PerlinNoise(_shakeTimer, _shakeTimer) - 0.5f) * 2f * trauma * 0.5f;

                _shakeOffset = new Vector3(offsetX, offsetY, offsetZ);

                _shakeDuration -= deltaTime;
                if (_shakeDuration <= 0f)
                {
                    StopShake();
                }
            }
            else
            {
                StopShake();
            }
        }

        /// <summary>
        /// Đặt vị trí camera tức thời, reset base position và bảo toàn độ rung hiện tại.
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            _targetPosition = position;
            ClampTargetPosition();
            _currentBasePosition = _targetPosition;
            transform.position = _currentBasePosition + _shakeOffset;
        }

        private void Reset()
        {
            ApplyIsometricRotation();
        }

        private void Start()
        {
            if (autoAlignIsometric)
            {
                ApplyIsometricRotation();
            }

            _targetPosition = transform.position;

            // Đảm bảo vị trí ban đầu nằm trong giới hạn cho phép
            ClampTargetPosition();
            _currentBasePosition = _targetPosition;
            transform.position = _currentBasePosition;
        }

        private void LateUpdate()
        {
            HandleKeyboardAndEdgePan();
            HandleMouseDrag();
            HandleZoom();
            ClampTargetPosition();

            UpdateShake(Time.deltaTime);

            // Nội suy mượt mà vị trí camera đến vị trí mục tiêu (hoàn toàn zero-drift)
            _currentBasePosition = Vector3.Lerp(_currentBasePosition, _targetPosition, Time.deltaTime * smoothSpeed);
            transform.position = _currentBasePosition + _shakeOffset;
        }

        /// <summary>
        /// Áp dụng góc xoay Isometric (60°, -45°, 0°).
        /// </summary>
        public void ApplyIsometricRotation()
        {
            transform.rotation = Quaternion.Euler(isometricEulerAngles);
        }

        /// <summary>
        /// Xử lý di chuyển bằng bàn phím (WASD / Mũi tên) và rê chuột mép màn hình.
        /// Hướng di chuyển chiếu theo góc nhìn camera trên mặt phẳng ngang (XZ).
        /// </summary>
        private void HandleKeyboardAndEdgePan()
        {
            Vector2 inputDir = Vector2.zero;
            bool isSprinting = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputDir.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputDir.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputDir.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputDir.x += 1f;
                if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed) isSprinting = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            inputDir.x = Input.GetAxisRaw("Horizontal");
            inputDir.y = Input.GetAxisRaw("Vertical");
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) isSprinting = true;
#endif

            // Xử lý Edge Panning khi chuột áp sát mép màn hình (chỉ khi có focus vào Game view)
            if (enableEdgePanning && !_isDragging && Application.isFocused)
            {
                Vector2 mousePos = GetMousePosition();
                // Chỉ nhận khi con trỏ chuột nằm hoàn toàn trong phạm vi cửa sổ game
                if (mousePos.x > 0 && mousePos.x < Screen.width && mousePos.y > 0 && mousePos.y < Screen.height)
                {
                    if (mousePos.x <= edgePanThickness) inputDir.x -= 1f;
                    if (mousePos.x >= Screen.width - edgePanThickness) inputDir.x += 1f;
                    if (mousePos.y <= edgePanThickness) inputDir.y -= 1f;
                    if (mousePos.y >= Screen.height - edgePanThickness) inputDir.y += 1f;
                }
            }

            if (inputDir.sqrMagnitude > 0.001f)
            {
                inputDir.Normalize();

                // Chiếu hướng Forward và Right của camera lên mặt phẳng XZ nằm ngang
                Vector3 camForward = transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                Vector3 moveDir = camForward * inputDir.y + camRight * inputDir.x;
                float currentSpeed = isSprinting ? panSpeed * sprintMultiplier : panSpeed;

                _targetPosition += moveDir * (currentSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Xử lý kéo rê màn hình bằng chuột giữa (RTS style).
        /// </summary>
        private void HandleMouseDrag()
        {
            if (!enableMouseDrag) return;

            bool middlePressed = false;
            Vector2 currentMousePos = GetMousePosition();

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                middlePressed = Mouse.current.middleButton.isPressed;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            middlePressed = Input.GetMouseButton(2);
#endif

            if (middlePressed)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    _lastMousePosition = currentMousePos;
                }
                else
                {
                    Vector2 delta = currentMousePos - _lastMousePosition;
                    _lastMousePosition = currentMousePos;

                    Vector3 camForward = transform.forward;
                    camForward.y = 0f;
                    camForward.Normalize();

                    Vector3 camRight = transform.right;
                    camRight.y = 0f;
                    camRight.Normalize();

                    Vector3 dragVector = (-camRight * delta.x - camForward * delta.y) * (dragSensitivity * 0.05f);
                    _targetPosition += dragVector;
                }
            }
            else
            {
                _isDragging = false;
            }
        }

        /// <summary>
        /// Xử lý zoom bằng cuộn chuột.
        /// Zoom dọc theo trục nhìn (Forward) của camera để giữ chuẩn phối cảnh isometric.
        /// </summary>
        private void HandleZoom()
        {
            float scroll = GetScrollWheelDelta();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                // Chuẩn hóa input scroll (New Input System thường trả về ~120, Legacy trả về ~0.1)
                float normalizedScroll = Mathf.Sign(scroll);

                float newHeight = Mathf.Clamp(_targetPosition.y - normalizedScroll * zoomSpeed * Time.deltaTime * 10f, minHeight, maxHeight);
                float deltaY = newHeight - _targetPosition.y;

                if (Mathf.Abs(transform.forward.y) > 0.001f)
                {
                    // Tịnh tiến dọc theo tia nhìn forward tương ứng với deltaY
                    _targetPosition += (transform.forward / transform.forward.y) * deltaY;
                }
            }
        }

        /// <summary>
        /// Giới hạn vị trí mục tiêu trong biên bản đồ và phạm vi độ cao.
        /// </summary>
        private void ClampTargetPosition()
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, minBounds.x, maxBounds.x);
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, minHeight, maxHeight);
            _targetPosition.z = Mathf.Clamp(_targetPosition.z, minBounds.y, maxBounds.y);
        }

        private Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        private float GetScrollWheelDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis("Mouse ScrollWheel");
#else
            return 0f;
#endif
        }

        private void OnDrawGizmosSelected()
        {
            // Vẽ hộp giới hạn biên di chuyển của camera trong Scene View
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
            Vector3 center = new Vector3(
                (minBounds.x + maxBounds.x) * 0.5f,
                (minHeight + maxHeight) * 0.5f,
                (minBounds.y + maxBounds.y) * 0.5f
            );
            Vector3 size = new Vector3(
                maxBounds.x - minBounds.x,
                maxHeight - minHeight,
                maxBounds.y - minBounds.y
            );
            Gizmos.DrawWireCube(center, size);
        }
    }
}
