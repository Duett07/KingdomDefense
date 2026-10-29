using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using KingdomDefense.Towers;
using KingdomDefense.Build;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KingdomDefense.UI
{
    /// <summary>
    /// Quản lý Menu tương tác Nâng cấp & Bán Tháp (Tower Upgrade & Sell System).
    /// Hiển thị giao diện nổi lơ lửng trong không gian ảo 3D (World-Space Floating Menu)
    /// ngay trên đỉnh tháp được chọn, luôn xoay hướng về Camera (Billboard),
    /// vẽ vòng tròn tầm bắn tương tác và hỗ trợ:
    /// - Nâng cấp (Level 1 -> Level 2 -> Level 3): Tăng chỉ số chuẩn Bảng 2.2 và đổi giao diện nhận diện.
    /// - Bán tháp: Hoàn trả lại 70% tổng số vàng người chơi đã đầu tư vào tháp.
    /// </summary>
    public class TowerUI : MonoBehaviour
    {
        public static TowerUI Instance { get; private set; }

        private Tower _selectedTower;
        private Canvas _canvas;
        private RectTransform _panelRect;
        private Text _titleText;
        private Text _statsText;
        private Text _previewText;
        private Button _upgradeButton;
        private Text _upgradeButtonText;
        private Image _upgradeButtonImage;
        private Button _sellButton;
        private Text _sellButtonText;
        private Button _closeButton;

        public Tower SelectedTower => _selectedTower;
        public bool IsSellButtonInteractable => _sellButton != null && _sellButton.interactable;
        public Button SellButton => _sellButton;

        private readonly Color _colorPanelBg = new Color(0.08f, 0.12f, 0.18f, 0.94f); // Slate tối sang trọng
        private readonly Color _colorBorder = new Color(0.92f, 0.72f, 0.20f, 1.0f);   // Vàng đồng Hoàng gia
        private readonly Color _colorUpgradeActive = new Color(0.12f, 0.75f, 0.40f, 1.0f); // Xanh lục bảo
        private readonly Color _colorUpgradeDisabled = new Color(0.35f, 0.40f, 0.45f, 0.75f); // Xám kim loại
        private readonly Color _colorSell = new Color(0.88f, 0.24f, 0.22f, 1.0f);      // Đỏ san hô đậm

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && Object.FindAnyObjectByType<TowerUI>() == null)
            {
                GameObject uiObj = new GameObject("TowerUpgradeUI_Manager");
                uiObj.AddComponent<TowerUI>();
                Debug.Log("[TowerUI] Tự động khởi tạo TowerUI World-Space Menu!");
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

            BuildWorldSpaceCanvasUI();
            Hide();
        }

        /// <summary>
        /// Tự động dựng toàn bộ hệ thống World-Space Canvas và các nút chức năng bằng Code thuần.
        /// </summary>
        private void BuildWorldSpaceCanvasUI()
        {
            // 1. Cấu hình World Space Canvas
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 60; // Luôn hiển thị trước các vật thể 3D

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 15;

            gameObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(320, 240);
            canvasRect.localScale = Vector3.one * 0.0125f; // Tỉ lệ ~4m x 3m trong thế giới 3D

            // 2. Khung viền nền ngoài (Gold Border Frame)
            GameObject borderObj = new GameObject("Border_Frame");
            borderObj.transform.SetParent(transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.sizeDelta = new Vector2(304, 224);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = _colorBorder;

            // 3. Khung thân chính (Main Slate Background)
            GameObject panelObj = new GameObject("Main_Panel");
            panelObj.transform.SetParent(borderObj.transform, false);
            _panelRect = panelObj.AddComponent<RectTransform>();
            _panelRect.sizeDelta = new Vector2(300, 220);
            Image panelImg = panelObj.AddComponent<Image>();
            panelImg.color = _colorPanelBg;

            // 4. Tiêu đề Tháp & Cấp độ
            GameObject titleObj = new GameObject("Title_Text");
            titleObj.transform.SetParent(_panelRect, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(-15, 82);
            titleRect.sizeDelta = new Vector2(250, 30);
            _titleText = titleObj.AddComponent<Text>();
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _titleText.fontSize = 17;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = new Color(1.0f, 0.90f, 0.35f); // Vàng kim rực rỡ

            // 5. Nút đóng [✕]
            GameObject closeObj = new GameObject("Close_Button");
            closeObj.transform.SetParent(_panelRect, false);
            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchoredPosition = new Vector2(130, 84);
            closeRect.sizeDelta = new Vector2(24, 24);
            Image closeImg = closeObj.AddComponent<Image>();
            closeImg.color = new Color(0.3f, 0.35f, 0.42f, 0.9f);
            _closeButton = closeObj.AddComponent<Button>();
            _closeButton.onClick.AddListener(Deselect);

            GameObject closeTxtObj = new GameObject("Close_Txt");
            closeTxtObj.transform.SetParent(closeRect, false);
            RectTransform closeTxtRect = closeTxtObj.AddComponent<RectTransform>();
            closeTxtRect.sizeDelta = closeRect.sizeDelta;
            Text closeTxt = closeTxtObj.AddComponent<Text>();
            closeTxt.font = _titleText.font;
            closeTxt.text = "✕";
            closeTxt.fontSize = 13;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = Color.white;

            // 6. Dòng hiển thị chỉ số hiện tại
            GameObject statsObj = new GameObject("Stats_Text");
            statsObj.transform.SetParent(_panelRect, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchoredPosition = new Vector2(0, 48);
            statsRect.sizeDelta = new Vector2(280, 32);
            _statsText = statsObj.AddComponent<Text>();
            _statsText.font = _titleText.font;
            _statsText.fontSize = 12;
            _statsText.alignment = TextAnchor.MiddleCenter;
            _statsText.color = new Color(0.85f, 0.92f, 0.98f); // Trắng lam tinh khiết

            // 7. Dòng xem trước chỉ số cấp kế tiếp
            GameObject prevObj = new GameObject("Preview_Text");
            prevObj.transform.SetParent(_panelRect, false);
            RectTransform prevRect = prevObj.AddComponent<RectTransform>();
            prevRect.anchoredPosition = new Vector2(0, 18);
            prevRect.sizeDelta = new Vector2(280, 24);
            _previewText = prevObj.AddComponent<Text>();
            _previewText.font = _titleText.font;
            _previewText.fontSize = 11;
            _previewText.alignment = TextAnchor.MiddleCenter;
            _previewText.color = new Color(0.35f, 0.85f, 1.0f); // Xanh dạ quang

            // 8. Nút NÂNG CẤP (UPGRADE BUTTON)
            GameObject upObj = new GameObject("Upgrade_Button");
            upObj.transform.SetParent(_panelRect, false);
            RectTransform upRect = upObj.AddComponent<RectTransform>();
            upRect.anchoredPosition = new Vector2(0, -22);
            upRect.sizeDelta = new Vector2(270, 42);
            _upgradeButtonImage = upObj.AddComponent<Image>();
            _upgradeButtonImage.color = _colorUpgradeActive;
            _upgradeButton = upObj.AddComponent<Button>();
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);

            GameObject upTxtObj = new GameObject("Upgrade_Text");
            upTxtObj.transform.SetParent(upRect, false);
            RectTransform upTxtRect = upTxtObj.AddComponent<RectTransform>();
            upTxtRect.sizeDelta = upRect.sizeDelta;
            _upgradeButtonText = upTxtObj.AddComponent<Text>();
            _upgradeButtonText.font = _titleText.font;
            _upgradeButtonText.fontSize = 13;
            _upgradeButtonText.fontStyle = FontStyle.Bold;
            _upgradeButtonText.alignment = TextAnchor.MiddleCenter;
            _upgradeButtonText.color = Color.white;

            // 9. Nút BÁN THÁP (SELL BUTTON)
            GameObject sellObj = new GameObject("Sell_Button");
            sellObj.transform.SetParent(_panelRect, false);
            RectTransform sellRect = sellObj.AddComponent<RectTransform>();
            sellRect.anchoredPosition = new Vector2(0, -72);
            sellRect.sizeDelta = new Vector2(270, 38);
            Image sellImg = sellObj.AddComponent<Image>();
            sellImg.color = _colorSell;
            _sellButton = sellObj.AddComponent<Button>();
            _sellButton.onClick.AddListener(OnSellClicked);

            GameObject sellTxtObj = new GameObject("Sell_Text");
            sellTxtObj.transform.SetParent(sellRect, false);
            RectTransform sellTxtRect = sellTxtObj.AddComponent<RectTransform>();
            sellTxtRect.sizeDelta = sellRect.sizeDelta;
            _sellButtonText = sellTxtObj.AddComponent<Text>();
            _sellButtonText.font = _titleText.font;
            _sellButtonText.fontSize = 12;
            _sellButtonText.fontStyle = FontStyle.Bold;
            _sellButtonText.alignment = TextAnchor.MiddleCenter;
            _sellButtonText.color = Color.white;
        }

        private void Update()
        {
            if (_selectedTower == null) return;

            // 1. Luôn neo vị trí trên đỉnh tháp và hướng mặt về phía Camera (Billboard)
            if (Camera.main != null)
            {
                float bobbing = Mathf.Sin(Time.time * 3f) * 0.05f;
                transform.position = _selectedTower.transform.position + new Vector3(0f, 3.2f + bobbing, 0f);
                transform.rotation = Camera.main.transform.rotation;
            }

            // 2. Bấm phím Escape để hủy chọn
            bool esc = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) esc = true;
#else
            if (Input.GetKeyDown(KeyCode.Escape)) esc = true;
#endif
            if (esc)
            {
                Deselect();
                return;
            }

            // 3. Click chuột ra ngoài không gian để hủy chọn
            bool mouseClick = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) mouseClick = true;
#else
            if (Input.GetMouseButtonDown(0)) mouseClick = true;
#endif
            if (mouseClick)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return; // Đang click vào nút trên Menu
                }

                if (Camera.main != null)
                {
                    Vector3 mousePos =
#if ENABLE_INPUT_SYSTEM
                        Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Input.mousePosition;
#else
                        Input.mousePosition;
#endif
                    Ray ray = Camera.main.ScreenPointToRay(mousePos);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        // Nếu bấm trúng chính tháp đang chọn hoặc ô Node của nó -> giữ nguyên
                        if (hit.transform.IsChildOf(_selectedTower.transform) ||
                            (_selectedTower.ParentNode != null && hit.transform == _selectedTower.ParentNode.transform))
                        {
                            return;
                        }
                    }
                    else
                    {
                        // Click vào khoảng không -> đóng menu
                        Deselect();
                    }
                }
            }

            // 4. Tự động cập nhật khả dụng của nút Nâng cấp theo lượng vàng hiện tại
            UpdateUpgradeButtonState();
        }

        /// <summary>
        /// Kích hoạt chọn một Tháp để mở Menu Nâng cấp & Bán.
        /// </summary>
        public void SelectTower(Tower tower)
        {
            if (tower == null)
            {
                Deselect();
                return;
            }

            // Bỏ chọn tháp cũ nếu có
            if (_selectedTower != null && _selectedTower != tower)
            {
                _selectedTower.ShowRangeIndicator(false);
            }

            _selectedTower = tower;
            _selectedTower.ShowRangeIndicator(true);

            if (_sellButton != null)
            {
                _sellButton.interactable = true;
            }

            RefreshUI();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hủy chọn tháp hiện tại và ẩn Menu.
        /// </summary>
        public void Deselect()
        {
            if (_selectedTower != null)
            {
                _selectedTower.ShowRangeIndicator(false);
                _selectedTower = null;
            }

            if (_sellButton != null)
            {
                _sellButton.interactable = true;
            }

            Hide();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Làm mới nội dung hiển thị trên Menu theo trạng thái cấp độ của Tháp.
        /// </summary>
        public void RefreshUI()
        {
            if (_selectedTower == null) return;

            int currentLv = _selectedTower.Level;
            _titleText.text = $"{_selectedTower.TowerDisplayName.ToUpper()} (CẤP {currentLv})";
            _statsText.text = $"Sát thương: {_selectedTower.Damage:F0}  |  Tầm bắn: {_selectedTower.Range:F1}m  |  Tốc bắn: {_selectedTower.FireRate:F1}/s";

            int sellVal = _selectedTower.SellAmount;
            _sellButtonText.text = $"💰 BÁN THÁP (+{sellVal} VÀNG - 70%)";
            if (_sellButton != null)
            {
                _sellButton.interactable = true;
            }

            if (_selectedTower.CanUpgrade)
            {
                int upCost = _selectedTower.NextUpgradeCost;
                int nextLv = currentLv + 1;
                _upgradeButtonText.text = $"▲ NÂNG CẤP LÊN CẤP {nextLv} (+{upCost} VÀNG)";

                if (currentLv == 1)
                {
                    _previewText.text = "Cấp 2: Tăng mạnh sát thương và tầm quét mục tiêu";
                }
                else if (currentLv == 2)
                {
                    _previewText.text = "Cấp 3: Hỏa lực tối thượng & Kích hoạt hào quang Hoàng gia";
                }
            }
            else
            {
                _upgradeButtonText.text = "★ ĐÃ ĐẠT CẤP TỐI ĐA (MAX)";
                _previewText.text = "Tháp đã đạt trạng thái chiến đấu uy lực nhất!";
            }

            UpdateUpgradeButtonState();
        }

        private void UpdateUpgradeButtonState()
        {
            if (_selectedTower == null) return;

            if (!_selectedTower.CanUpgrade)
            {
                _upgradeButton.interactable = false;
                _upgradeButtonImage.color = _colorUpgradeDisabled;
                return;
            }

            int cost = _selectedTower.NextUpgradeCost;
            int playerGold = GameHUD.Instance != null ? GameHUD.Instance.CurrentGold : 999;

            bool canAfford = playerGold >= cost;
            _upgradeButton.interactable = canAfford;
            _upgradeButtonImage.color = canAfford ? _colorUpgradeActive : _colorUpgradeDisabled;
        }

        private void OnUpgradeClicked()
        {
            if (_selectedTower == null) return;
            if (_upgradeButton != null) _upgradeButton.interactable = false;

            bool success = _selectedTower.TryUpgrade();
            if (success)
            {
                RefreshUI();
            }
            else
            {
                UpdateUpgradeButtonState();
            }
        }

        private void OnSellClicked()
        {
            if (_selectedTower == null) return;
            if (_sellButton != null) _sellButton.interactable = false;

            Tower towerToSell = _selectedTower;
            Deselect();
            towerToSell.Sell();
        }
    }
}
