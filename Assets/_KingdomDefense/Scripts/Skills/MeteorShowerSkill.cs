using UnityEngine;
using KingdomDefense.UI;
using KingdomDefense.Build;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KingdomDefense.Skills
{
    /// <summary>
    /// Kỹ năng tối thượng Mưa Thiên Thạch (Meteor Shower Ultimate Skill).
    /// - Nhấn phím [Space] (hoặc nút bấm trên HUD) để bật chế độ ngắm bắn.
    /// - Hiển thị vòng tròn hồng tâm rực lửa (bán kính 4.0m) quét theo con trỏ chuột.
    /// - Click chuột trái: Trừ 60 Mana, triệu hồi thiên thạch lao xuống hủy diệt mục tiêu (400 sát thương diện rộng).
    /// - Click chuột phải hoặc phím [Esc]: Hủy chế độ ngắm.
    /// </summary>
    public class MeteorShowerSkill : MonoBehaviour
    {
        public static MeteorShowerSkill Instance { get; private set; }

        [Header("--- THIẾT LẬP KỸ NĂNG ---")]
        [Tooltip("Lượng Mana tiêu tốn mỗi lần kích hoạt")]
        [SerializeField] private float manaCost = 60f;

        [Tooltip("Sát thương diện rộng của Thiên Thạch")]
        [SerializeField] private float damage = 400f;

        [Tooltip("Bán kính nổ lan (AoE Radius)")]
        [SerializeField] private float aoeRadius = 4.0f;

        private bool _isAiming = false;
        private GameObject _reticleRoot;
        private LineRenderer _reticleCircle;
        private LineRenderer _reticleCrosshairX;
        private LineRenderer _reticleCrosshairZ;

        public bool IsAiming => _isAiming;
        public float ManaCost => manaCost;
        public float Damage => damage;
        public float AoERadius => aoeRadius;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && Object.FindAnyObjectByType<MeteorShowerSkill>() == null)
            {
                GameObject skillObj = new GameObject("MeteorShower_SkillManager");
                skillObj.AddComponent<MeteorShowerSkill>();
                Debug.Log("[MeteorShowerSkill] Tự động khởi tạo hệ thống Kỹ năng Mưa Thiên Thạch!");
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            BuildTargetingReticle();
            SetAiming(false);
        }

        /// <summary>
        /// Khởi tạo vòng tròn hồng tâm ngắm bắn rực lửa (AoE 4.0m) bằng LineRenderer.
        /// </summary>
        private void BuildTargetingReticle()
        {
            _reticleRoot = new GameObject("Targeting_Reticle_Root");
            _reticleRoot.transform.SetParent(transform, false);

            Shader lineShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material reticleMat = new Material(lineShader);
            reticleMat.color = new Color(1.0f, 0.45f, 0.05f, 0.95f); // Cam lửa bốc cháy

            // 1. Vòng tròn bán kính 4.0m
            GameObject circleObj = new GameObject("Circle_Ring");
            circleObj.transform.SetParent(_reticleRoot.transform, false);
            _reticleCircle = circleObj.AddComponent<LineRenderer>();
            _reticleCircle.useWorldSpace = false;
            _reticleCircle.loop = true;
            _reticleCircle.startWidth = 0.12f;
            _reticleCircle.endWidth = 0.12f;
            _reticleCircle.sharedMaterial = reticleMat;

            int segments = 64;
            _reticleCircle.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * aoeRadius, 0.08f, Mathf.Sin(angle) * aoeRadius);
                _reticleCircle.SetPosition(i, pos);
            }

            // 2. Hồng tâm dấu thập X
            GameObject crossX = new GameObject("Crosshair_X");
            crossX.transform.SetParent(_reticleRoot.transform, false);
            _reticleCrosshairX = crossX.AddComponent<LineRenderer>();
            _reticleCrosshairX.useWorldSpace = false;
            _reticleCrosshairX.startWidth = 0.08f;
            _reticleCrosshairX.endWidth = 0.08f;
            _reticleCrosshairX.sharedMaterial = reticleMat;
            _reticleCrosshairX.positionCount = 2;
            _reticleCrosshairX.SetPosition(0, new Vector3(-aoeRadius * 0.4f, 0.08f, 0f));
            _reticleCrosshairX.SetPosition(1, new Vector3(aoeRadius * 0.4f, 0.08f, 0f));

            // 3. Hồng tâm dấu thập Z
            GameObject crossZ = new GameObject("Crosshair_Z");
            crossZ.transform.SetParent(_reticleRoot.transform, false);
            _reticleCrosshairZ = crossZ.AddComponent<LineRenderer>();
            _reticleCrosshairZ.useWorldSpace = false;
            _reticleCrosshairZ.startWidth = 0.08f;
            _reticleCrosshairZ.endWidth = 0.08f;
            _reticleCrosshairZ.sharedMaterial = reticleMat;
            _reticleCrosshairZ.positionCount = 2;
            _reticleCrosshairZ.SetPosition(0, new Vector3(0f, 0.08f, -aoeRadius * 0.4f));
            _reticleCrosshairZ.SetPosition(1, new Vector3(0f, 0.08f, aoeRadius * 0.4f));
        }

        private void Update()
        {
            HandleHotkeyInput();

            if (_isAiming)
            {
                UpdateReticlePosition();
                HandleAimingClick();
            }
        }

        /// <summary>
        /// Bắt phím tắt Space để bật/tắt chế độ ngắm bắn.
        /// </summary>
        private void HandleHotkeyInput()
        {
            // Chặn phím tắt kỹ năng khi game đang tạm dừng (Pause)
            if ((GameHUD.Instance != null && GameHUD.Instance.IsPaused) || Time.timeScale <= 0.0001f)
            {
                return;
            }

            bool spacePressed = false;
            bool cancelPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
                cancelPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            }
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                cancelPressed = true;
            }
#else
            spacePressed = Input.GetKeyDown(KeyCode.Space);
            cancelPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1);
#endif

            if (spacePressed)
            {
                // Nếu đang chọn tháp ở BuildManager thì hủy chọn tháp trước khi ngắm thiên thạch
                if (BuildManager.Instance != null && BuildManager.Instance.SelectedTowerType != TowerType.None)
                {
                    BuildManager.Instance.SelectTowerToBuild(TowerType.None);
                }
                ToggleAiming();
            }
            else if (cancelPressed && _isAiming)
            {
                SetAiming(false);
                Debug.Log("[MeteorShower] Đã hủy chế độ ngắm bắn Thiên Thạch.");
            }
        }

        /// <summary>
        /// Bật / Tắt chế độ ngắm bắn kỹ năng (dùng cho phím tắt Space hoặc nút bấm trên HUD).
        /// </summary>
        public void ToggleAiming()
        {
            if (_isAiming)
            {
                SetAiming(false);
            }
            else
            {
                if (GameHUD.Instance != null && !GameHUD.Instance.HasEnoughMana(manaCost))
                {
                    Debug.LogWarning($"<color=#FF5722>[MeteorShower] Không đủ Mana! Cần {manaCost} MP (Hiện có: {Mathf.FloorToInt(GameHUD.Instance.CurrentMana)} MP).</color>");
                    return;
                }

                SetAiming(true);
                Debug.Log("<color=#FF9800>[MeteorShower] Đang kích hoạt chế độ ngắm Mưa Thiên Thạch! Click chuột trái để gọi thiên thạch.</color>");
            }
        }

        /// <summary>
        /// Bật hoặc tắt trạng thái hồng tâm ngắm bắn Mưa Thiên Thạch.
        /// </summary>
        public void SetAiming(bool aiming)
        {
            _isAiming = aiming;
            if (_reticleRoot != null)
            {
                _reticleRoot.SetActive(aiming);
            }
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.UpdateManaDisplay();
            }
        }

        /// <summary>
        /// Quét tia Raycast từ chuột xuống mặt đất để cập nhật vị trí hồng tâm ngắm.
        /// </summary>
        private void UpdateReticlePosition()
        {
            if (Camera.main == null) return;

            Vector3 mousePos =
#if ENABLE_INPUT_SYSTEM
                Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Input.mousePosition;
#else
                Input.mousePosition;
#endif
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            // Cắt với mặt phẳng ngang tại Y = 0
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                _reticleRoot.transform.position = new Vector3(hitPoint.x, 0.05f, hitPoint.z);

                // Hiệu ứng xoay nhẹ hồng tâm tạo cảm giác đang khóa mục tiêu
                _reticleRoot.transform.Rotate(Vector3.up, 30f * Time.deltaTime);
            }
        }

        /// <summary>
        /// Xử lý click chuột trái để xác nhận thả thiên thạch.
        /// </summary>
        private void HandleAimingClick()
        {
            bool leftClick = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                leftClick = true;
            }
#else
            leftClick = Input.GetMouseButtonDown(0);
#endif

            if (!leftClick) return;

            // Kiểm tra click vào nút UI thì bỏ qua
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Kiểm tra lại lượng Mana trước khi phóng
            if (GameHUD.Instance != null && !GameHUD.Instance.ConsumeMana(manaCost))
            {
                Debug.LogWarning($"[MeteorShower] Hết Mana! Cần {manaCost} MP.");
                SetAiming(false);
                return;
            }

            Vector3 targetPos = _reticleRoot.transform.position;
            targetPos.y = 0f;

            // Triệu hồi thiên thạch
            MeteorProjectile.Spawn(targetPos, damage, aoeRadius);

            Debug.Log($"<color=#4CAF50>[MeteorShower] Đã kích hoạt Mưa Thiên Thạch tại {targetPos}! (-{manaCost} Mana)</color>");

            // Tắt chế độ ngắm sau khi đã thả
            SetAiming(false);
        }
    }
}
