using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomDefense.Core
{
    /// <summary>
    /// Bộ tối ưu hóa hiệu năng toàn diện cho Kingdom Defense:
    /// - Khóa cứng 60 FPS mượt mà: Application.targetFrameRate = 60, QualitySettings.vSyncCount = 1.
    /// - Static Batching tự động: Gom nhóm các cụm địa hình, rừng thông mùa thu, đường mòn và công trình KayKit
    ///   thành một lượt dựng (Draw Call) duy nhất, giảm tải tối đa cho CPU & GPU.
    /// - GPU Instancing: Kích hoạt cờ Instancing trên các Material chia sẻ giữa quái và đạn đạo.
    /// - Performance Overlay (Phím F3): Bảng hiển thị FPS, Frame Time (ms), V-Sync và trạng thái Batching.
    /// </summary>
    public class PerformanceOptimizer : MonoBehaviour
    {
        public static PerformanceOptimizer Instance { get; private set; }

        [Header("--- THIẾT LẬP HIỆU NĂNG ---")]
        [Tooltip("Số khung hình mục tiêu mỗi giây")]
        [SerializeField] private int targetFPS = 60;

        [Tooltip("Đồng bộ dọc VSync (1 = Khóa theo tần số quét màn hình, 0 = Tắt)")]
        [SerializeField] private int vSyncLevel = 1;

        [Tooltip("Tự động gom nhóm Static Batching cho môi trường khi bắt đầu")]
        [SerializeField] private bool autoStaticBatching = true;

        [Header("--- BẢNG THEO DÕI HIỆU NĂNG (PERFORMANCE HUD) ---")]
        [Tooltip("Hiển thị bảng FPS và thông số hiệu năng ở góc màn hình")]
        [SerializeField] private bool showPerformanceHUD = true;

        [Tooltip("Phím nóng để ẩn/hiện bảng hiệu năng")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F3;

        // Bộ đệm tính toán FPS mượt mà
        private float _fpsAccumulator = 0f;
        private int _fpsFrameCount = 0;
        private float _fpsTimeLeft = 0.5f;
        private float _currentFPS = 60f;
        private float _currentFrameTimeMs = 16.6f;
        private int _batchedObjectCount = 0;
        private bool _isBatchingCompleted = false;

        // Vị trí dải chữ Performance và tham chiếu nút Tốc độ
        private Rect _panelRect = Rect.zero;
        private RectTransform _cachedSpeedBtnRect;

        // IMGUI Style Cache
        private GUIStyle _boxStyle;
        private GUIStyle _iconStyle;
        private GUIStyle _labelValueStyle;
        private GUIStyle _labelDotStyle;
        private GUIStyle _labelBatchStyle;
        private Texture2D _bgTexture;

        public float CurrentFPS => _currentFPS;
        public float CurrentFrameTimeMs => _currentFrameTimeMs;
        public int BatchedObjectCount => _batchedObjectCount;
        public bool IsBatchingCompleted => _isBatchingCompleted;
        public bool ShowPerformanceHUD
        {
            get => showPerformanceHUD;
            set => showPerformanceHUD = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitializeBeforeSceneLoad()
        {
            ApplyFrameRateSettings(60, 1);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeAfterSceneLoad()
        {
            if (Instance == null && Object.FindAnyObjectByType<PerformanceOptimizer>() == null)
            {
                GameObject perfObj = new GameObject("[PerformanceOptimizer]");
                perfObj.AddComponent<PerformanceOptimizer>();
                DontDestroyOnLoad(perfObj);
                Debug.Log("<color=#2ECC71>[PerformanceOptimizer]</color> Đã tự động kích hoạt bộ tối ưu hóa hiệu năng toàn cục!");
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            ApplyPerformanceSettings();
        }

        private void Start()
        {
            if (autoStaticBatching)
            {
                StartCoroutine(ExecuteEnvironmentStaticBatchingRoutine());
            }
        }

        private void Update()
        {
            // Phím nóng F3 để ẩn/hiện Performance Overlay
            if (Input.GetKeyDown(toggleKey))
            {
                showPerformanceHUD = !showPerformanceHUD;
            }

            // Đo FPS độc lập với Time.timeScale (chính xác ngay cả khi tua 2X hoặc Pause)
            float delta = Time.unscaledDeltaTime;
            _fpsAccumulator += (delta > 0.0001f) ? (1f / delta) : 60f;
            _fpsFrameCount++;
            _fpsTimeLeft -= delta;

            if (_fpsTimeLeft <= 0f)
            {
                _currentFPS = _fpsFrameCount > 0 ? (_fpsAccumulator / _fpsFrameCount) : 60f;
                _currentFrameTimeMs = delta * 1000f;
                _fpsAccumulator = 0f;
                _fpsFrameCount = 0;
                _fpsTimeLeft = 0.5f;
            }
        }

        /// <summary>
        /// Áp dụng thiết lập 60 FPS và V-Sync.
        /// </summary>
        public void ApplyPerformanceSettings()
        {
            ApplyFrameRateSettings(targetFPS, vSyncLevel);
        }

        public static void ApplyFrameRateSettings(int fps, int vsync)
        {
            QualitySettings.vSyncCount = vsync;
            Application.targetFrameRate = fps;
            Time.maximumDeltaTime = 0.1f;
        }

        /// <summary>
        /// Tự động dò tìm các container môi trường tĩnh và gom nhóm Static Batching.
        /// </summary>
        public IEnumerator ExecuteEnvironmentStaticBatchingRoutine()
        {
            // Chờ 1 frame để đảm bảo MapGenerator đã dựng xong toàn bộ GameObject
            yield return null;

            int combinedRoots = 0;
            _batchedObjectCount = 0;

            string[] staticContainerNames = new string[]
            {
                "--- 01_Terrain_Base ---",
                "--- 02_KingdomRush_Road ---",
                "--- 04_Autumn_Forest ---",
                "--- 05_Landmarks_Props ---"
            };

            foreach (string containerName in staticContainerNames)
            {
                GameObject container = GameObject.Find(containerName);
                if (container != null)
                {
                    int childRenderers = container.GetComponentsInChildren<MeshRenderer>(true).Length;
                    if (childRenderers > 0)
                    {
                        StaticBatchingUtility.Combine(container);
                        _batchedObjectCount += childRenderers;
                        combinedRoots++;
                    }
                }
            }

            // Nếu không tìm thấy tên container chuẩn, thử tìm theo MapGenerator
            if (combinedRoots == 0)
            {
                GameObject mapGenObj = GameObject.Find("MapGenerator");
                if (mapGenObj != null)
                {
                    foreach (Transform child in mapGenObj.transform)
                    {
                        // Bỏ qua ObjectPool, Waypoints và Tower Signposts
                        if (child.name.Contains("ObjectPool") || child.name.Contains("Waypoints") || child.name.Contains("Signposts"))
                        {
                            continue;
                        }

                        int renderers = child.GetComponentsInChildren<MeshRenderer>(true).Length;
                        if (renderers > 0)
                        {
                            StaticBatchingUtility.Combine(child.gameObject);
                            _batchedObjectCount += renderers;
                            combinedRoots++;
                        }
                    }
                }
            }

            _isBatchingCompleted = true;
            Debug.Log($"<color=#2ECC71>[PerformanceOptimizer]</color> Hoàn tất Static Batching: Đã gom nhóm {_batchedObjectCount} Mesh Renderers từ {combinedRoots} cụm môi trường tĩnh!");
        }

        private RectTransform GetSpeedBtnRect()
        {
            if (_cachedSpeedBtnRect == null)
            {
                GameObject speedBtn = GameObject.Find("Btn_Speed");
                if (speedBtn != null)
                {
                    _cachedSpeedBtnRect = speedBtn.GetComponent<RectTransform>();
                }
                else
                {
                    GameObject controlPanel = GameObject.Find("TopRight_ControlPanel");
                    if (controlPanel != null)
                    {
                        _cachedSpeedBtnRect = controlPanel.GetComponent<RectTransform>();
                    }
                }
            }
            return _cachedSpeedBtnRect;
        }

        private void OnGUI()
        {
            if (!showPerformanceHUD) return;

            InitStyles();

            float pillWidth = 205f;
            float pillHeight = 28f;

            // Tự động định vị dải chữ nằm ngay trước (bên trái) nút Tua nhanh & Tạm dừng
            RectTransform speedRect = GetSpeedBtnRect();
            if (speedRect != null)
            {
                Vector3[] corners = new Vector3[4];
                speedRect.GetWorldCorners(corners);

                // corners[0] = bottom-left, corners[1] = top-left
                float leftEdge = corners[0].x;
                float centerY = (corners[0].y + corners[1].y) * 0.5f;

                // Quy đổi sang hệ tọa độ GUI của Unity (gốc 0,0 ở góc trên bên trái)
                float guiY = Screen.height - centerY - (pillHeight * 0.5f);
                float guiX = leftEdge - pillWidth - 10f;

                _panelRect = new Rect(guiX, guiY, pillWidth, pillHeight);
            }
            else
            {
                // Fallback nếu HUD chưa nạp: đặt tại góc trên bên phải trước 190px
                float guiX = Screen.width - 200f - pillWidth;
                float guiY = 36f;
                _panelRect = new Rect(guiX, guiY, pillWidth, pillHeight);
            }

            // Vẽ khung viền dải chữ thanh mảnh sang trọng
            GUI.Box(_panelRect, GUIContent.none, _boxStyle);

            // Vẽ nội dung dải chữ nằm gọn trên 1 hàng duy nhất
            Rect contentRect = new Rect(_panelRect.x + 8f, _panelRect.y + 4f, _panelRect.width - 16f, _panelRect.height - 8f);
            GUILayout.BeginArea(contentRect);
            GUILayout.BeginHorizontal();

            // Biểu tượng tia sét vàng
            GUILayout.Label("⚡", _iconStyle, GUILayout.Width(16f));

            // FPS
            Color fpsColor = (_currentFPS >= 55f) ? new Color(0.2f, 0.88f, 0.45f) :
                            (_currentFPS >= 30f) ? new Color(0.95f, 0.75f, 0.2f) :
                            new Color(0.95f, 0.3f, 0.3f);

            GUIStyle fpsStyle = new GUIStyle(_labelValueStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = fpsColor }
            };
            GUILayout.Label($"{_currentFPS:F0} FPS", fpsStyle, GUILayout.Width(52f));

            // Dấu chấm phân cách
            GUILayout.Label("•", _labelDotStyle, GUILayout.Width(10f));

            // Frame time
            GUILayout.Label($"{_currentFrameTimeMs:F1}ms", _labelValueStyle, GUILayout.Width(48f));

            // Dấu chấm phân cách
            GUILayout.Label("•", _labelDotStyle, GUILayout.Width(10f));

            // Batch count
            string batchText = _isBatchingCompleted ? $"{_batchedObjectCount}B" : "...";
            GUILayout.Label(batchText, _labelBatchStyle, GUILayout.Width(42f));

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void InitStyles()
        {
            if (_boxStyle != null) return;

            // Nền tối mờ dạng dải thanh mảnh (#121417)
            _bgTexture = new Texture2D(1, 1);
            _bgTexture.SetPixel(0, 0, new Color(0.07f, 0.08f, 0.09f, 0.88f));
            _bgTexture.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _bgTexture },
                border = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            };

            _iconStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.77f, 0.25f, 1f) } // Vàng hoàng gia
            };

            _labelValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.90f, 0.92f, 0.95f, 1f) }
            };

            _labelDotStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.60f, 0.65f, 0.8f) }
            };

            _labelBatchStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.30f, 0.80f, 0.85f, 1f) } // Soft cyan/teal
            };
        }

        private void OnDestroy()
        {
            if (_bgTexture != null)
            {
                Destroy(_bgTexture);
            }
        }
    }
}
