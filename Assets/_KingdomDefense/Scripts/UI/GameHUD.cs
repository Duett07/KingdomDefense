using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KingdomDefense.Build;
using KingdomDefense.Core;
using KingdomDefense.Cameras;
using KingdomDefense.Skills;
using KingdomDefense.Audio;
using KingdomDefense.Enemy;

namespace KingdomDefense.UI
{
    /// <summary>
    /// Quản lý giao diện HUD phong cách Kingdom Rush kinh điển.
    /// Bao gồm:
    /// - Top-Left: Máu (Lives), Vàng (Gold), Đợt quái (Wave)
    /// - Top-Center: Thanh Năng lượng Mana (0-100 MP)
    /// - Top-Right: Nút Tua nhanh (1X / 2X), Nút Tạm dừng (Pause)
    /// - Bottom-Dock: 4 Nút chọn mua công trình: Tháp Cung [1], Tháp Pháo [2], Tháp Phép [3], Tường Thành [4]
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        [Header("--- THIẾT LẬP CHỈ SỐ BAN ĐẦU ---")]
        [SerializeField] private int initialLives = 20;
        [SerializeField] private int initialGold = 350;
        [SerializeField] private int totalWaves = 10;
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float manaRegenPerSecond = 2f;

        [Header("--- TOP HUD THAM CHIẾU ---")]
        [SerializeField] private Text livesText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text waveText;
        [SerializeField] private Text speedText;
        [SerializeField] private Slider manaSlider;
        [SerializeField] private Text manaText;

        [Header("--- TOP HUD ICONS & FX ---")]
        [SerializeField] private Image livesIcon;
        [SerializeField] private Image goldIcon;
        [SerializeField] private Image waveIcon;
        [SerializeField] private Image manaFillImage;
        [SerializeField] private Color normalLivesColor = Color.white;
        [SerializeField] private Color criticalLivesColor = new Color(0.98f, 0.28f, 0.28f, 1f); // Đỏ thẫm cảnh báo nguy cấp
        [SerializeField] private Color manaReadyColor = new Color(0.35f, 0.88f, 1f, 1f); // Cyan phát sáng khi đủ 60 MP

        [Header("--- BOTTOM DOCK THAM CHIẾU ---")]
        [SerializeField] private Button archerBtn;
        [SerializeField] private Button bombBtn;
        [SerializeField] private Button mageBtn;
        [SerializeField] private Button frostBtn;
        [SerializeField] private Button wallBtn;
        [SerializeField] private Button knightBtn;

        [SerializeField] private Image archerHighlight;
        [SerializeField] private Image bombHighlight;
        [SerializeField] private Image mageHighlight;
        [SerializeField] private Image frostHighlight;
        [SerializeField] private Image wallHighlight;
        [SerializeField] private Image knightHighlight;

        [SerializeField] private Text archerCostText;
        [SerializeField] private Text bombCostText;
        [SerializeField] private Text mageCostText;
        [SerializeField] private Text frostCostText;
        [SerializeField] private Text wallCostText;
        [SerializeField] private Text knightCostText;

        private float _knightCooldown = 0f;
        private const float KNIGHT_COOLDOWN_TIME = 15f;

        [Header("--- NÚT ĐIỀU KHIỂN & TẠM DỪNG (PAUSE / SPEED) ---")]
        [SerializeField] private Button speedBtn;
        [SerializeField] private Button pauseBtn;
        [SerializeField] private Text pauseText;
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private Sprite iconPause;
        [SerializeField] private Sprite iconResume;

        private int _currentLives;
        private int _currentGold;
        private int _currentWave = 1;
        private float _currentMana;
        private bool _isFastForward;
        private bool _isPaused;
        private Text _bgmToggleText;
        private Text _sfxToggleText;

        // Trạng thái hiệu ứng và Coroutines phản hồi thị giác
        private Coroutine _heartbeatRoutine;
        private Coroutine _goldPunchRoutine;
        private Coroutine _livesPunchRoutine;
        private Coroutine _wavePunchRoutine;
        private bool _isCriticalLives = false;
        private Text _notificationText;
        private Coroutine _notificationRoutine;

        public int CurrentLives => _currentLives;
        public int CurrentGold => _currentGold;
        public int CurrentWave => _currentWave;
        public float CurrentMana => _currentMana;
        public bool IsCriticalLives => _isCriticalLives;
        public bool IsPaused => _isPaused;
        public bool IsFastForward => _isFastForward;

        public event System.Action<int> OnLivesChanged;
        public event System.Action<int> OnGoldChanged;
        public event System.Action<bool> OnPauseToggled;
        public event System.Action<bool> OnFastForwardToggled;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            _currentLives = initialLives;
            _currentGold = initialGold;
            _currentWave = 1;
            _currentMana = maxMana;

            EnsureEventSystem();
            EnsurePauseOverlay();
            AutoBindHUDReferences();
            AlignAndEnforceHUDLayout();
            SetupButtonListeners();
            UpdateAllDisplays();
        }

        private void Start()
        {
            EnsureEventSystem();
            EnsurePauseOverlay();
            AlignAndEnforceHUDLayout();
            UpdateAllDisplays();
        }

        private void OnDisable()
        {
            Time.timeScale = 1.0f;
            StopAllTopHUDCoroutines();
        }

        private void StopAllTopHUDCoroutines()
        {
            if (_heartbeatRoutine != null) { StopCoroutine(_heartbeatRoutine); _heartbeatRoutine = null; }
            if (_goldPunchRoutine != null) { StopCoroutine(_goldPunchRoutine); _goldPunchRoutine = null; }
            if (_livesPunchRoutine != null) { StopCoroutine(_livesPunchRoutine); _livesPunchRoutine = null; }
            if (_wavePunchRoutine != null) { StopCoroutine(_wavePunchRoutine); _wavePunchRoutine = null; }
        }

        /// <summary>
        /// Chuẩn hóa và căn chỉnh tự động tỷ lệ pixel-perfect cho Top HUD và Bottom Dock:
        /// - Top HUD: Căn đều 3 huy hiệu Máu, Vàng, Wave trong khung 510x75, triệt tiêu hiện tượng lệch góc và tràn lề.
        /// - Bottom Dock: Mở rộng khung viền lên 840x125 để ôm trọn 4 thẻ tháp (710px) với lề đệm sang trọng, không bị nhỏ hơn các nút.
        /// </summary>
        public void AlignAndEnforceHUDLayout()
        {
            // 1. CĂN CHỈNH TOP-LEFT STATUS PANEL
            Transform tlPanel = transform.Find("TopLeft_StatusPanel");
            if (tlPanel != null)
            {
                RectTransform tlRect = tlPanel.GetComponent<RectTransform>();
                if (tlRect != null)
                {
                    tlRect.anchorMin = new Vector2(0f, 1f);
                    tlRect.anchorMax = new Vector2(0f, 1f);
                    tlRect.pivot = new Vector2(0f, 1f);
                    tlRect.anchoredPosition = new Vector2(25f, -25f);
                    tlRect.sizeDelta = new Vector2(510f, 75f);
                }

                // Căn chỉnh 3 Badge con (Máu, Vàng, Wave)
                Vector2[] badgePositions = new Vector2[]
                {
                    new Vector2(18f, 0f),   // Máu
                    new Vector2(150f, 0f),  // Vàng
                    new Vector2(295f, 0f)   // Wave
                };

                Vector2[] badgeSizes = new Vector2[]
                {
                    new Vector2(110f, 50f),
                    new Vector2(125f, 50f),
                    new Vector2(190f, 50f)
                };

                int badgeIndex = 0;
                for (int i = 0; i < tlPanel.childCount && badgeIndex < 3; i++)
                {
                    Transform child = tlPanel.GetChild(i);
                    if (child.name.Contains("BadgeItem"))
                    {
                        RectTransform br = child.GetComponent<RectTransform>();
                        if (br != null)
                        {
                            br.anchorMin = new Vector2(0f, 0.5f);
                            br.anchorMax = new Vector2(0f, 0.5f);
                            br.pivot = new Vector2(0f, 0.5f);
                            br.anchoredPosition = badgePositions[badgeIndex];
                            br.sizeDelta = badgeSizes[badgeIndex];
                        }

                        // Căn chỉnh icon và text bên trong Badge
                        Transform iconTr = child.Find("Icon");
                        if (iconTr != null)
                        {
                            RectTransform ir = iconTr.GetComponent<RectTransform>();
                            if (ir != null)
                            {
                                ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(0f, 0.5f);
                                ir.anchoredPosition = Vector2.zero;
                                ir.sizeDelta = new Vector2(38f, 38f);
                            }
                        }

                        Transform valTr = child.Find("ValueText");
                        if (valTr != null)
                        {
                            RectTransform vr = valTr.GetComponent<RectTransform>();
                            if (vr != null)
                            {
                                vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(0f, 0.5f);
                                vr.anchoredPosition = new Vector2(44f, 0f);
                                vr.sizeDelta = new Vector2(badgeSizes[badgeIndex].x - 44f, 38f);
                            }
                        }

                        badgeIndex++;
                    }
                }
            }

            // 2. CĂN CHỈNH BOTTOM-CENTER BUILD DOCK (Khung rộng 920x125 ôm trọn các thẻ nút)
            Transform bdPanel = transform.Find("BottomCenter_BuildDock");
            if (bdPanel != null)
            {
                RectTransform bdRect = bdPanel.GetComponent<RectTransform>();
                if (bdRect != null)
                {
                    bdRect.anchorMin = new Vector2(0.5f, 0f);
                    bdRect.anchorMax = new Vector2(0.5f, 0f);
                    bdRect.pivot = new Vector2(0.5f, 0f);
                    bdRect.anchoredPosition = new Vector2(0f, 20f);
                    bdRect.sizeDelta = new Vector2(920f, 125f);
                }

                // Loại bỏ / ẩn thẻ Tường Thành khỏi dock bottom theo yêu cầu
                for (int i = 0; i < bdPanel.childCount; i++)
                {
                    Transform child = bdPanel.GetChild(i);
                    string cName = child.name.ToLower();
                    if (cName.Contains("tường") || cName.Contains("tuong") || cName.Contains("wall"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
                wallBtn = null;
                wallHighlight = null;
                wallCostText = null;

                int cardCount = 0;
                for (int i = 0; i < bdPanel.childCount; i++)
                {
                    Transform c = bdPanel.GetChild(i);
                    if (c.gameObject.activeSelf && c.name.StartsWith("Card_")) cardCount++;
                }

                float[] cardXPositions;
                float cardWidth;
                if (cardCount >= 6)
                {
                    bdRect.sizeDelta = new Vector2(1100f, 125f);
                    cardXPositions = new float[] { -425f, -255f, -85f, 85f, 255f, 425f };
                    cardWidth = 150f;
                }
                else if (cardCount >= 5)
                {
                    bdRect.sizeDelta = new Vector2(920f, 125f);
                    cardXPositions = new float[] { -340f, -170f, 0f, 170f, 340f };
                    cardWidth = 155f;
                }
                else
                {
                    bdRect.sizeDelta = new Vector2(750f, 125f);
                    cardXPositions = new float[] { -270f, -90f, 90f, 270f };
                    cardWidth = 170f;
                }

                int cardIndex = 0;
                for (int i = 0; i < bdPanel.childCount && cardIndex < cardXPositions.Length; i++)
                {
                    Transform card = bdPanel.GetChild(i);
                    if (card.gameObject.activeSelf && card.name.StartsWith("Card_"))
                    {
                        RectTransform cr = card.GetComponent<RectTransform>();
                        if (cr != null)
                        {
                            cr.anchorMin = new Vector2(0.5f, 0.5f);
                            cr.anchorMax = new Vector2(0.5f, 0.5f);
                            cr.pivot = new Vector2(0.5f, 0.5f);
                            cr.anchoredPosition = new Vector2(cardXPositions[cardIndex], 0f);
                            cr.sizeDelta = new Vector2(cardWidth, 95f);
                        }
                        cardIndex++;
                    }
                }
            }

            // 3. CĂN CHỈNH TOP-CENTER MANA BAR
            Transform manaBar = transform.Find("TopCenter_ManaBar");
            if (manaBar != null)
            {
                RectTransform mr = manaBar.GetComponent<RectTransform>();
                if (mr != null)
                {
                    mr.anchorMin = new Vector2(0.5f, 1f);
                    mr.anchorMax = new Vector2(0.5f, 1f);
                    mr.pivot = new Vector2(0.5f, 1f);
                    mr.anchoredPosition = new Vector2(0f, -25f);
                    mr.sizeDelta = new Vector2(380f, 48f);
                }
            }

            // 4. CĂN CHỈNH TOP-RIGHT CONTROL PANEL (Speed x1/x2, Pause)
            Transform trPanel = transform.Find("TopRight_ControlPanel");
            if (trPanel != null)
            {
                RectTransform trRect = trPanel.GetComponent<RectTransform>();
                if (trRect != null)
                {
                    trRect.anchorMin = new Vector2(1f, 1f);
                    trRect.anchorMax = new Vector2(1f, 1f);
                    trRect.pivot = new Vector2(1f, 1f);
                    trRect.anchoredPosition = new Vector2(-25f, -25f);
                    trRect.sizeDelta = new Vector2(165f, 75f);
                }

                Transform speedTr = trPanel.Find("Btn_Speed") ?? (trPanel.childCount > 0 ? trPanel.GetChild(0) : null);
                if (speedTr != null)
                {
                    speedTr.name = "Btn_Speed";
                    RectTransform sr = speedTr.GetComponent<RectTransform>();
                    if (sr != null)
                    {
                        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
                        sr.anchoredPosition = new Vector2(-38f, 0f);
                        sr.sizeDelta = new Vector2(62f, 52f);
                    }
                }

                Transform pauseTr = trPanel.Find("Btn_Pause") ?? (trPanel.childCount > 1 ? trPanel.GetChild(1) : null);
                if (pauseTr != null)
                {
                    pauseTr.name = "Btn_Pause";
                    RectTransform pr = pauseTr.GetComponent<RectTransform>();
                    if (pr != null)
                    {
                        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
                        pr.anchoredPosition = new Vector2(38f, 0f);
                        pr.sizeDelta = new Vector2(62f, 52f);
                    }
                }
            }
        }

        /// <summary>
        /// Tự động dò tìm các icon Top HUD và các nút Bottom Dock nếu chưa được gán thủ công trong Inspector.
        /// </summary>
        public void AutoBindHUDReferences()
        {
            // 1. Tự động liên kết Top HUD
            if (livesIcon == null && livesText != null && livesText.transform.parent != null)
            {
                Transform iconTr = livesText.transform.parent.Find("Icon");
                if (iconTr != null) livesIcon = iconTr.GetComponent<Image>();
            }

            if (goldIcon == null && goldText != null && goldText.transform.parent != null)
            {
                Transform iconTr = goldText.transform.parent.Find("Icon");
                if (iconTr != null) goldIcon = iconTr.GetComponent<Image>();
            }

            if (waveIcon == null && waveText != null && waveText.transform.parent != null)
            {
                Transform iconTr = waveText.transform.parent.Find("Icon");
                if (iconTr != null) waveIcon = iconTr.GetComponent<Image>();
            }

            if (manaSlider != null && manaFillImage == null && manaSlider.fillRect != null)
            {
                manaFillImage = manaSlider.fillRect.GetComponent<Image>();
            }

            if (manaFillImage != null)
            {
                manaFillImage.color = new Color(0.06f, 0.72f, 0.94f, 1f);
            }

            // 2. Tự động liên kết Bottom Dock nếu chưa gán
            Transform dockTr = transform.Find("BottomCenter_BuildDock");
            if (dockTr != null)
            {
                for (int i = 0; i < dockTr.childCount; i++)
                {
                    Transform card = dockTr.GetChild(i);
                    Button btn = card.GetComponent<Button>();
                    if (btn == null) continue;

                    string cardName = card.name.ToLower();
                    Transform hlTr = card.Find("Highlight") ?? card.Find("HighlightBorder");
                    Transform costTr = card.Find("CostText") ?? card.Find("Cost");

                    if (cardName.Contains("cung") || cardName.Contains("archer"))
                    {
                        if (archerBtn == null) archerBtn = btn;
                        if (archerHighlight == null) archerHighlight = hlTr?.GetComponent<Image>();
                        if (archerCostText == null) archerCostText = costTr?.GetComponent<Text>();
                    }
                    else if (cardName.Contains("pháo") || cardName.Contains("phao") || cardName.Contains("bomb") || cardName.Contains("cannon"))
                    {
                        if (bombBtn == null) bombBtn = btn;
                        if (bombHighlight == null) bombHighlight = hlTr?.GetComponent<Image>();
                        if (bombCostText == null) bombCostText = costTr?.GetComponent<Text>();
                    }
                    else if (cardName.Contains("phép") || cardName.Contains("phep") || cardName.Contains("mage"))
                    {
                        if (mageBtn == null) mageBtn = btn;
                        if (mageHighlight == null) mageHighlight = hlTr?.GetComponent<Image>();
                        if (mageCostText == null) mageCostText = costTr?.GetComponent<Text>();
                    }
                    else if (cardName.Contains("băng") || cardName.Contains("bang") || cardName.Contains("frost"))
                    {
                        if (frostBtn == null) frostBtn = btn;
                        if (frostHighlight == null) frostHighlight = hlTr?.GetComponent<Image>();
                        if (frostCostText == null) frostCostText = costTr?.GetComponent<Text>();
                    }
                    else if (cardName.Contains("tường") || cardName.Contains("tuong") || cardName.Contains("wall"))
                    {
                        // Thẻ Tường Thành đã loại bỏ khỏi dock bottom
                        card.gameObject.SetActive(false);
                        wallBtn = null;
                        wallHighlight = null;
                        wallCostText = null;
                    }
                }
            }

#if UNITY_EDITOR
            if (iconPause == null)
            {
                iconPause = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_KingdomDefense/Sprites/UI/btn_pause.png");
            }
            if (iconResume == null)
            {
                iconResume = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_KingdomDefense/Sprites/UI/btn_resume.png");
            }
#endif

            // 3. Tự động liên kết TopRight Control Panel (Speed, Pause)
            Transform trPanel = transform.Find("TopRight_ControlPanel");
            if (trPanel != null)
            {
                Transform speedTr = trPanel.Find("Btn_Speed") ?? (trPanel.childCount > 0 ? trPanel.GetChild(0) : null);
                if (speedTr != null)
                {
                    if (speedBtn == null) speedBtn = speedTr.GetComponent<Button>();
                    if (speedText == null) speedText = speedTr.Find("Label")?.GetComponent<Text>() ?? speedTr.GetComponentInChildren<Text>();
                }

                Transform pauseTr = trPanel.Find("Btn_Pause") ?? (trPanel.childCount > 1 ? trPanel.GetChild(1) : null);
                if (pauseTr != null)
                {
                    if (pauseBtn == null) pauseBtn = pauseTr.GetComponent<Button>();
                    if (pauseText == null)
                    {
                        Transform lbl = pauseTr.Find("Label");
                        if (lbl != null) pauseText = lbl.GetComponent<Text>();
                        else pauseText = pauseTr.GetComponentInChildren<Text>();
                    }

                    Image pauseImg = pauseTr.GetComponent<Image>();
                    if (pauseImg != null && iconPause != null && pauseImg.sprite == null)
                    {
                        pauseImg.sprite = _isPaused ? iconResume : iconPause;
                        pauseImg.color = Color.white;
                    }

                    if (pauseText == null && iconPause == null)
                    {
                        GameObject lblObj = new GameObject("Label");
                        lblObj.transform.SetParent(pauseTr, false);
                        RectTransform lr = lblObj.AddComponent<RectTransform>();
                        lr.anchorMin = Vector2.zero;
                        lr.anchorMax = Vector2.one;
                        lr.sizeDelta = Vector2.zero;
                        pauseText = lblObj.AddComponent<Text>();
                        pauseText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                        pauseText.fontSize = 24;
                        pauseText.fontStyle = FontStyle.Bold;
                        pauseText.color = Color.white;
                        pauseText.alignment = TextAnchor.MiddleCenter;
                        pauseText.raycastTarget = false;

                        Outline o = lblObj.AddComponent<Outline>();
                        o.effectColor = Color.black;
                        o.effectDistance = new Vector2(1.5f, -1.5f);
                    }
                }
            }

            if (pauseOverlay == null)
            {
                Transform overlayTr = transform.Find("Pause_Overlay");
                if (overlayTr != null) pauseOverlay = overlayTr.gameObject;
            }
        }

        /// <summary>
        /// Khởi tạo hoặc tìm kiếm màn che phủ mờ (Dark Tint Overlay) khi game Tạm dừng.
        /// <summary>
        /// Khởi tạo hoặc tìm kiếm Modal Menu Tạm Dừng (Pause Modal Dialog) khi game Tạm dừng:
        /// - Lớp phủ mờ điện ảnh tối (Dark Backdrop Overlay) chặn click xuyên thấu nhưng không đóng nhầm modal.
        /// - Dialog Card Hoàng gia (480x380) với viền vàng kim sang trọng (Royal Gold Outline #F59E0B).
        /// - 3 Nút bấm: [TIẾP TỤC], [CHƠI LẠI TRẬN], [VỀ MENU / THOÁT].
        /// - Hiệu ứng bung nở nảy nhẹ (Punch Scale Tween) mượt mà chạy trên Time.unscaledDeltaTime.
        /// </summary>
        public void EnsurePauseOverlay()
        {
            if (pauseOverlay == null)
            {
                Transform existing = transform.Find("Pause_Overlay");
                if (existing != null)
                {
                    pauseOverlay = existing.gameObject;
                }
            }

            if (pauseOverlay == null)
            {
                BuildPauseOverlay();
            }
            else
            {
                BindPauseModalButtons(pauseOverlay);
            }
        }

        private void BuildPauseOverlay()
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Backdrop Overlay (Lớp phủ làm mờ nền màn hình)
            GameObject overlayObj = new GameObject("Pause_Overlay");
            overlayObj.transform.SetParent(transform, false);
            overlayObj.transform.SetAsLastSibling();

            RectTransform r = overlayObj.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.sizeDelta = Vector2.zero;
            r.anchoredPosition = Vector2.zero;

            Image darkTint = overlayObj.AddComponent<Image>();
            darkTint.color = new Color(0.04f, 0.06f, 0.08f, 0.75f); // Phủ mờ điện ảnh
            darkTint.raycastTarget = true; // Chặn click chuột xuyên thấu xuống bàn chơi khi đang tạm dừng

            // 2. Dialog Panel (Hộp thoại chính phong cách Hoàng gia 480x380)
            GameObject centerBox = new GameObject("PauseModal_Panel");
            centerBox.transform.SetParent(overlayObj.transform, false);
            RectTransform cbr = centerBox.AddComponent<RectTransform>();
            cbr.anchorMin = new Vector2(0.5f, 0.5f);
            cbr.anchorMax = new Vector2(0.5f, 0.5f);
            cbr.pivot = new Vector2(0.5f, 0.5f);
            cbr.anchoredPosition = Vector2.zero;
            cbr.sizeDelta = new Vector2(480f, 440f);

            Image boxBg = centerBox.AddComponent<Image>();
            boxBg.color = new Color(0.10f, 0.13f, 0.18f, 0.98f); // Đá phiến hoàng gia sẫm

            Outline panelOutline = centerBox.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.96f, 0.72f, 0.15f, 1f); // Viền vàng kim rực rỡ
            panelOutline.effectDistance = new Vector2(3f, -3f);

            // 3. Tiêu đề: ⏸ TRÒ CHƠI TẠM DỪNG
            GameObject titleObj = new GameObject("Title_Text");
            titleObj.transform.SetParent(centerBox.transform, false);
            RectTransform tr = titleObj.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 1f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0f, -24f);
            tr.sizeDelta = new Vector2(460f, 45f);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.text = "⏸ TRÒ CHƠI TẠM DỪNG";
            titleText.fontSize = 26;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.88f, 0.25f, 1f); // Vàng kim hoàng gia
            titleText.alignment = TextAnchor.MiddleCenter;

            Outline to = titleObj.AddComponent<Outline>();
            to.effectColor = new Color(0f, 0f, 0f, 0.9f);
            to.effectDistance = new Vector2(2f, -2f);

            // 4. Phụ đề & Hướng dẫn
            GameObject subObj = new GameObject("Subtitle_Text");
            subObj.transform.SetParent(centerBox.transform, false);
            RectTransform sr = subObj.AddComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 1f);
            sr.anchorMax = new Vector2(1f, 1f);
            sr.pivot = new Vector2(0.5f, 1f);
            sr.anchoredPosition = new Vector2(0f, -70f);
            sr.sizeDelta = new Vector2(440f, 30f);

            Text subText = subObj.AddComponent<Text>();
            subText.font = defaultFont;
            subText.text = "Vương quốc đang đợi lệnh! Nhấn [Esc] hoặc [P] để tiếp tục";
            subText.fontSize = 14;
            subText.color = new Color(0.82f, 0.86f, 0.92f, 0.9f);
            subText.alignment = TextAnchor.MiddleCenter;

            // 5. Ba nút chức năng: [TIẾP TỤC], [CHƠI LẠI TRẬN], [VỀ MENU / THOÁT]
            CreateModalButton(centerBox.transform, new Vector2(0f, -135f), "▶  TIẾP TỤC (RESUME)", new Color(0.12f, 0.58f, 0.32f, 1f), defaultFont, () => TogglePause());
            CreateModalButton(centerBox.transform, new Vector2(0f, -205f), "🔄  CHƠI LẠI TRẬN (RESTART)", new Color(0.85f, 0.52f, 0.12f, 1f), defaultFont, () => {
                if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
                else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            });
            CreateModalButton(centerBox.transform, new Vector2(0f, -275f), "🏠  VỀ MENU / THOÁT", new Color(0.22f, 0.26f, 0.34f, 1f), defaultFont, () => QuitOrReturnToMenu());

            // 6. Thanh điều khiển Âm thanh nhanh (BGM & SFX Toggles)
            CreateAudioToggleButtons(centerBox.transform, new Vector2(0f, -345f), defaultFont);

            pauseOverlay = overlayObj;
            pauseOverlay.SetActive(false);
        }

        private void BindPauseModalButtons(GameObject overlay)
        {
            if (overlay == null) return;
            Transform panelTr = overlay.transform.Find("PauseModal_Panel");
            if (panelTr == null) return;

            // Gán lại chính xác sự kiện cho nút Về Menu / Thoát
            Button btnQuit = panelTr.Find("Btn_VỀ_MENU__THOÁT")?.GetComponent<Button>();
            if (btnQuit != null)
            {
                btnQuit.onClick.RemoveAllListeners();
                btnQuit.onClick.AddListener(() => QuitOrReturnToMenu());
            }

            // Gán lại cho nút Tiếp tục
            Button btnResume = panelTr.Find("Btn_TIẾP_TỤC_RESUME")?.GetComponent<Button>();
            if (btnResume != null)
            {
                btnResume.onClick.RemoveAllListeners();
                btnResume.onClick.AddListener(() => TogglePause());
            }

            // Gán lại cho nút Chơi lại
            Button btnRestart = panelTr.Find("Btn_CHƠI_LẠI_TRẬN_RESTART")?.GetComponent<Button>();
            if (btnRestart != null)
            {
                btnRestart.onClick.RemoveAllListeners();
                btnRestart.onClick.AddListener(() => {
                    if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
                    else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                });
            }
        }

        private void CreateModalButton(Transform parent, Vector2 anchoredPos, string label, Color bgColor, Font font, UnityEngine.Events.UnityAction onClickAction)
        {
            string shortName = label.Replace(" ", "_").Replace("/", "_").Replace("(", "").Replace(")", "").Replace("▶", "").Replace("🔄", "").Replace("🏠", "").Trim('_');
            GameObject btnObj = new GameObject("Btn_" + shortName);
            btnObj.transform.SetParent(parent, false);
            RectTransform br = btnObj.AddComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 1f);
            br.anchorMax = new Vector2(0.5f, 1f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.anchoredPosition = anchoredPos;
            br.sizeDelta = new Vector2(360f, 52f);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.25f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
            btn.colors = cb;

            if (onClickAction != null)
            {
                btn.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayUIClick();
                    onClickAction.Invoke();
                });
            }

            Outline bo = btnObj.AddComponent<Outline>();
            bo.effectColor = new Color(0f, 0f, 0f, 0.8f);
            bo.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = textObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            Text txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 17;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
        }

        private void CreateAudioToggleButtons(Transform parent, Vector2 anchoredPos, Font font)
        {
            GameObject container = new GameObject("Audio_Controls_Row");
            container.transform.SetParent(parent, false);
            RectTransform cr = container.AddComponent<RectTransform>();
            cr.anchorMin = new Vector2(0.5f, 1f);
            cr.anchorMax = new Vector2(0.5f, 1f);
            cr.pivot = new Vector2(0.5f, 0.5f);
            cr.anchoredPosition = anchoredPos;
            cr.sizeDelta = new Vector2(360f, 44f);

            // Nút BGM (-95f)
            CreateAudioPillButton(container.transform, new Vector2(-95f, 0f), "Btn_Toggle_BGM",
                GetBgmButtonLabel(), new Color(0.18f, 0.38f, 0.52f, 1f), font, () =>
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.ToggleBGM();
                        UpdateAudioButtonsVisual();
                        AudioManager.Instance.PlayUIClick();
                    }
                }, out _bgmToggleText);

            // Nút SFX (+95f)
            CreateAudioPillButton(container.transform, new Vector2(95f, 0f), "Btn_Toggle_SFX",
                GetSfxButtonLabel(), new Color(0.48f, 0.32f, 0.16f, 1f), font, () =>
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.ToggleSFX();
                        UpdateAudioButtonsVisual();
                        AudioManager.Instance.PlayUIClick();
                    }
                }, out _sfxToggleText);
        }

        private void CreateAudioPillButton(Transform parent, Vector2 anchoredPos, string name, string label, Color bgColor, Font font, UnityEngine.Events.UnityAction onClickAction, out Text outLabel)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform br = btnObj.AddComponent<RectTransform>();
            br.anchorMin = new Vector2(0.5f, 0.5f);
            br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.anchoredPosition = anchoredPos;
            br.sizeDelta = new Vector2(170f, 42f);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.25f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
            btn.colors = cb;

            if (onClickAction != null)
            {
                btn.onClick.AddListener(onClickAction);
            }

            Outline bo = btnObj.AddComponent<Outline>();
            bo.effectColor = new Color(0f, 0f, 0f, 0.8f);
            bo.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = textObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            Text txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            outLabel = txt;
        }

        public string GetBgmButtonLabel()
        {
            bool isMuted = AudioManager.Instance != null && (AudioManager.Instance.IsMuted || AudioManager.Instance.IsBgmMuted);
            return isMuted ? "🔇 BGM: TẮT" : "🎵 BGM: BẬT";
        }

        public string GetSfxButtonLabel()
        {
            bool isMuted = AudioManager.Instance != null && (AudioManager.Instance.IsMuted || AudioManager.Instance.IsSfxMuted);
            return isMuted ? "🔇 SFX: TẮT" : "🔊 SFX: BẬT";
        }

        public void UpdateAudioButtonsVisual()
        {
            if (_bgmToggleText != null) _bgmToggleText.text = GetBgmButtonLabel();
            if (_sfxToggleText != null) _sfxToggleText.text = GetSfxButtonLabel();
        }

        /// <summary>
        /// Hiệu ứng bung nở nảy nhẹ (Punch Scale Tween) cho Dialog Panel (0.85x -> 1.05x -> 1.0x).
        /// Chạy mượt mà trên Time.unscaledDeltaTime bất chấp Time.timeScale = 0.
        /// </summary>
        private IEnumerator AnimateModalPopRoutine(Transform panel)
        {
            if (panel == null) yield break;

            panel.localScale = Vector3.one * 0.85f;
            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = 0.85f + Mathf.Sin(t * Mathf.PI * 0.7f) * 0.22f;
                panel.localScale = Vector3.one * scale;
                yield return null;
            }

            panel.localScale = Vector3.one;
        }

        /// <summary>
        /// Đảm bảo EventSystem luôn tồn tại để các nút UI Button nhận được sự kiện Click chuột.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null && Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
                Debug.Log("[GameHUD] Đã tự động tạo EventSystem để các nút UI nhận được click chuột!");
            }
        }

        private void Update()
        {
            // Tự hồi phục Mana theo thời gian (chỉ khi không tạm dừng)
            if (_currentMana < maxMana && !_isPaused)
            {
                _currentMana = Mathf.Min(maxMana, _currentMana + manaRegenPerSecond * Time.deltaTime);
                UpdateManaDisplay();
            }

            // Đếm ngược thời gian hồi chiêu xuất lính Hiệp Sĩ Hoàng Gia
            if (_knightCooldown > 0f && !_isPaused)
            {
                _knightCooldown -= Time.deltaTime;
                if (knightCostText != null)
                {
                    knightCostText.text = $"{Mathf.CeilToInt(_knightCooldown)}s";
                    knightCostText.color = new Color(0.95f, 0.45f, 0.2f);
                }
                if (knightBtn != null && knightBtn.interactable) knightBtn.interactable = false;
            }
            else if (_knightCooldown <= 0f)
            {
                if (knightCostText != null && knightCostText.text != "SẴN SÀNG")
                {
                    knightCostText.text = "SẴN SÀNG";
                    knightCostText.color = new Color(0.25f, 0.90f, 0.4f);
                }
                if (knightBtn != null && !knightBtn.interactable) knightBtn.interactable = true;
            }

            // Lắng nghe phím tắt điều khiển: [Esc]/[P] cho Pause, [Tab] cho 1X/2X Speed, [R]/[Space] xuất lính
            HandleControlHotkeys();
        }

        /// <summary>
        /// Xử lý hành vi phím [Esc] theo thứ tự ưu tiên:
        /// 1. Hủy ngắm bắn Thiên Thạch (nếu đang ngắm).
        /// 2. Hủy chọn tháp (nếu đang chọn tháp).
        /// 3. Bật / Tắt Menu Tạm dừng (Pause Modal).
        /// </summary>
        public string HandleEscAction()
        {
            if (MeteorShowerSkill.Instance != null && MeteorShowerSkill.Instance.IsAiming)
            {
                MeteorShowerSkill.Instance.SetAiming(false);
                return "CancelMeteorAiming";
            }

            if (BuildManager.Instance != null && BuildManager.Instance.SelectedTowerType != TowerType.None)
            {
                BuildManager.Instance.SelectTowerToBuild(TowerType.None);
                return "CancelTowerSelection";
            }

            TogglePause();
            return _isPaused ? "OpenPauseMenu" : "ClosePauseMenu";
        }

        private void HandleControlHotkeys()
        {
            bool escPressed = false;
            bool pPressed = false;
            bool toggleSpeed = false;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                escPressed = UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
                pPressed = UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame;
                toggleSpeed = UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame ||
                              UnityEngine.InputSystem.Keyboard.current.backquoteKey.wasPressedThisFrame;
            }
#endif
            if (!escPressed && !pPressed)
            {
                try
                {
                    escPressed = Input.GetKeyDown(KeyCode.Escape);
                    pPressed = Input.GetKeyDown(KeyCode.P);
                }
                catch (System.InvalidOperationException) { }
            }

            if (!toggleSpeed)
            {
                try
                {
                    toggleSpeed = Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.BackQuote);
                }
                catch (System.InvalidOperationException) { }
            }

            if (escPressed)
            {
                HandleEscAction();
            }
            else if (pPressed)
            {
                TogglePause();
            }
            else if (toggleSpeed)
            {
                ToggleFastForward();
            }

            // Phím tắt [R] để triệu hồi Hiệp sĩ hoàng gia
            bool rPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                rPressed = UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
            }
#else
            rPressed = Input.GetKeyDown(KeyCode.R);
#endif
            if (rPressed)
            {
                OnSummonKnightClicked();
            }

#if UNITY_EDITOR
            HandleDebugHotkeys();
#endif
        }

        private void SetupButtonListeners()
        {
            if (speedBtn != null)
            {
                speedBtn.onClick.RemoveAllListeners();
                speedBtn.onClick.AddListener(ToggleFastForward);
            }
            if (pauseBtn != null)
            {
                pauseBtn.onClick.RemoveAllListeners();
                pauseBtn.onClick.AddListener(TogglePause);
            }

            if (archerBtn != null)
            {
                archerBtn.onClick.RemoveAllListeners();
                archerBtn.onClick.AddListener(() => OnSelectTower(TowerType.Archer));
            }
            if (bombBtn != null)
            {
                bombBtn.onClick.RemoveAllListeners();
                bombBtn.onClick.AddListener(() => OnSelectTower(TowerType.Bomb));
            }
            if (mageBtn != null)
            {
                mageBtn.onClick.RemoveAllListeners();
                mageBtn.onClick.AddListener(() => OnSelectTower(TowerType.Mage));
            }
            if (frostBtn != null)
            {
                frostBtn.onClick.RemoveAllListeners();
                frostBtn.onClick.AddListener(() => OnSelectTower(TowerType.Frost));
            }
            if (wallBtn != null && wallBtn.gameObject.activeInHierarchy)
            {
                wallBtn.onClick.RemoveAllListeners();
                wallBtn.onClick.AddListener(() => OnSelectTower(TowerType.Wall));
            }
            if (knightBtn != null)
            {
                knightBtn.onClick.RemoveAllListeners();
                knightBtn.onClick.AddListener(OnSummonKnightClicked);
            }
        }

        /// <summary>
        /// Người chơi chủ động bấm nút trên Bottom Dock để xuất 1 Hiệp sĩ hoàng gia.
        /// </summary>
        public void OnSummonKnightClicked()
        {
            if (_knightCooldown > 0f)
            {
                ShowNotification($"Hiệp sĩ đang hồi chiêu ({Mathf.CeilToInt(_knightCooldown)}s)...", new Color(1.0f, 0.45f, 0.45f));
                return;
            }

            if (Allies.CastleBarracks.Instance != null)
            {
                var knight = Allies.CastleBarracks.Instance.SpawnPlayerKnight();
                if (knight != null)
                {
                    _knightCooldown = KNIGHT_COOLDOWN_TIME;
                    ShowNotification("⚔️ HIỆP SĨ XUẤT TRẬN! Hành quân về Cổng Thành Quái!", new Color(0.40f, 0.90f, 1.0f));
                    AudioManager.Instance?.PlayTowerPlaced();
                }
            }
            else
            {
                Debug.LogWarning("[GameHUD] Không tìm thấy CastleBarracks trong Scene!");
            }
        }

        private void OnSelectTower(TowerType type)
        {
            if (BuildManager.Instance != null)
            {
                BuildManager.Instance.SelectTowerToBuild(type);
            }
        }

        /// <summary>
        /// Cập nhật hiển thị viền highlight khi người chơi chọn một loại tháp.
        /// </summary>
        public void UpdateSelectedTowerUI(TowerType type)
        {
            Color activeColor = new Color(0.2f, 1f, 0.45f, 0.95f); // Xanh lá dạ quang rực rỡ
            Color normalColor = new Color(1f, 1f, 1f, 0f); // Ẩn hoàn toàn

            SetHighlight(archerHighlight, type == TowerType.Archer, activeColor, normalColor);
            SetHighlight(bombHighlight, type == TowerType.Bomb, activeColor, normalColor);
            SetHighlight(mageHighlight, type == TowerType.Mage, activeColor, normalColor);
            SetHighlight(frostHighlight, type == TowerType.Frost, activeColor, normalColor);
            SetHighlight(wallHighlight, type == TowerType.Wall, activeColor, normalColor);
        }

        private void SetHighlight(Image hl, bool isSelected, Color activeColor, Color normalColor)
        {
            if (hl == null) return;
            hl.color = isSelected ? activeColor : normalColor;
            hl.raycastTarget = false; // Không chặn tia click chuột
        }

        /// <summary>
        /// Cập nhật số sinh mệnh (Trừ khi quái lọt vào thành).
        /// Kích hoạt cảnh báo nguy cấp khi Lives < 5 (Heartbeat pulse FX) và giật nảy khi mất máu.
        /// </summary>
        public void SetLives(int lives)
        {
            int prevLives = _currentLives;
            _currentLives = Mathf.Max(0, lives);
            if (livesText != null) livesText.text = _currentLives.ToString();

            // Hiệu ứng giật nảy và rung màn hình cảnh báo khi bị quái vật trừ máu
            if (_currentLives < prevLives)
            {
                if (_livesPunchRoutine != null) StopCoroutine(_livesPunchRoutine);
                _livesPunchRoutine = StartCoroutine(PunchScaleRoutine(livesText.transform, 1.35f, 0.20f));
                if (livesIcon != null) StartCoroutine(PunchScaleRoutine(livesIcon.transform, 1.30f, 0.20f));

                CameraController.ShakeCastleBreach();
            }

            // Xử lý Cảnh báo nguy cấp: Lives < 5
            if (_currentLives < 5 && _currentLives > 0)
            {
                if (!_isCriticalLives)
                {
                    _isCriticalLives = true;
                    if (livesText != null) livesText.color = criticalLivesColor;
                    if (_heartbeatRoutine != null) StopCoroutine(_heartbeatRoutine);
                    _heartbeatRoutine = StartCoroutine(HeartbeatRoutine());
                }
            }
            else
            {
                if (_isCriticalLives)
                {
                    _isCriticalLives = false;
                    if (livesText != null) livesText.color = normalLivesColor;
                    if (_heartbeatRoutine != null)
                    {
                        StopCoroutine(_heartbeatRoutine);
                        _heartbeatRoutine = null;
                    }
                    if (livesIcon != null) livesIcon.transform.localScale = Vector3.one;
                }
            }

            OnLivesChanged?.Invoke(_currentLives);

            if (_currentLives <= 0)
            {
                Debug.LogWarning("[GameHUD] THUA TRẬN! Vương quốc đã thất thủ!");
            }
        }

        public void ModifyLives(int delta)
        {
            SetLives(_currentLives + delta);
        }

        /// <summary>
        /// Cập nhật số vàng (Tăng khi diệt quái, giảm khi xây/nâng cấp tháp).
        /// Hiệu ứng punch scale nảy nhẹ khi thu được vàng từ quái vật.
        /// </summary>
        public void SetGold(int gold)
        {
            int prevGold = _currentGold;
            _currentGold = Mathf.Max(0, gold);

            if (goldText != null)
            {
                goldText.text = _currentGold.ToString();
                if (_currentGold > prevGold)
                {
                    if (_goldPunchRoutine != null) StopCoroutine(_goldPunchRoutine);
                    _goldPunchRoutine = StartCoroutine(PunchScaleRoutine(goldText.transform, 1.25f, 0.18f));
                    if (goldIcon != null) StartCoroutine(PunchScaleRoutine(goldIcon.transform, 1.20f, 0.18f));
                }
            }

            OnGoldChanged?.Invoke(_currentGold);
            UpdateTowerButtonsAvailability();
        }

        public void AddGold(int amount)
        {
            SetGold(_currentGold + amount);
        }

        public bool SpendGold(int cost)
        {
            if (_currentGold >= cost)
            {
                SetGold(_currentGold - cost);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cập nhật hiển thị đợt quái chuẩn quy cách 2 chữ số (Ví dụ: Wave 04 / 10).
        /// Hiệu ứng nổi bật khi bước sang làn sóng quái mới.
        /// </summary>
        public void SetWave(int wave, int total = -1)
        {
            int prevWave = _currentWave;
            _currentWave = wave;
            if (total > 0) totalWaves = total;

            if (waveText != null)
            {
                waveText.text = $"WAVE {_currentWave:D2} / {totalWaves:D2}";
                if (_currentWave > prevWave)
                {
                    if (_wavePunchRoutine != null) StopCoroutine(_wavePunchRoutine);
                    _wavePunchRoutine = StartCoroutine(PunchScaleRoutine(waveText.transform, 1.30f, 0.22f));
                    if (waveIcon != null) StartCoroutine(PunchScaleRoutine(waveIcon.transform, 1.25f, 0.22f));
                }
            }
        }

        public float MaxMana => maxMana;
        public float ManaRegenPerSecond => manaRegenPerSecond;

        /// <summary>
        /// Nạp thêm Mana cho người chơi (ví dụ thưởng diệt quái tinh anh hoặc ngọc hồi năng lượng).
        /// </summary>
        public void AddMana(float amount)
        {
            _currentMana = Mathf.Clamp(_currentMana + amount, 0f, maxMana);
            UpdateManaDisplay();
        }

        /// <summary>
        /// Kiểm tra xem người chơi có đủ lượng Mana yêu cầu hay không.
        /// </summary>
        public bool HasEnoughMana(float amount)
        {
            return _currentMana >= amount;
        }

        /// <summary>
        /// Trừ Mana khi kích hoạt kỹ năng Mưa Thiên Thạch (Yêu cầu 60 MP).
        /// </summary>
        public bool ConsumeMana(float amount)
        {
            if (_currentMana >= amount)
            {
                _currentMana -= amount;
                UpdateManaDisplay();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cập nhật hiển thị Thanh năng lượng Mana (Top-Center):
        /// - Thanh slider pha lê xanh ngọc Cyan/Azure.
        /// - Hiển thị chữ 'MANA 100 / 100 MP [Space]' với hiệu ứng phát sáng khi >= 60 MP (sẵn sàng chiêu cuối).
        /// </summary>
        public void UpdateManaDisplay()
        {
            if (manaSlider != null)
            {
                manaSlider.value = _currentMana / maxMana;
            }
            if (manaText != null)
            {
                if (MeteorShowerSkill.Instance != null && MeteorShowerSkill.Instance.IsAiming)
                {
                    manaText.text = "☄️ ĐANG NGẮM [Space/Esc]";
                    manaText.color = manaReadyColor;
                }
                else if (_currentMana >= 60f)
                {
                    manaText.text = $"MANA {Mathf.FloorToInt(_currentMana)} / {Mathf.FloorToInt(maxMana)} MP [Space]";
                    manaText.color = manaReadyColor;
                }
                else
                {
                    manaText.text = $"MANA {Mathf.FloorToInt(_currentMana)} / {Mathf.FloorToInt(maxMana)} MP";
                    manaText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Hiệu ứng bung nở nảy nhẹ (Punch Scale Tween) cho chữ số và icon.
        /// </summary>
        private IEnumerator PunchScaleRoutine(Transform target, float punchMultiplier, float duration)
        {
            if (target == null) yield break;
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scaleFactor = 1f + Mathf.Sin(t * Mathf.PI) * (punchMultiplier - 1f);
                target.localScale = originalScale * scaleFactor;
                yield return null;
            }

            target.localScale = originalScale;
        }

        /// <summary>
        /// Hiệu ứng nhịp tim đập dồn dập (Heartbeat Pulse) khi thành trì còn dưới 5 Máu.
        /// </summary>
        private IEnumerator HeartbeatRoutine()
        {
            Vector3 originalScale = Vector3.one;
            Transform iconTr = livesIcon != null ? livesIcon.transform : (livesText != null ? livesText.transform : null);
            if (iconTr == null) yield break;

            while (_isCriticalLives && _currentLives > 0)
            {
                // Nhịp 1 (Mạnh)
                float t = 0f;
                while (t < 0.12f)
                {
                    t += Time.unscaledDeltaTime;
                    iconTr.localScale = originalScale * Mathf.Lerp(1.0f, 1.28f, t / 0.12f);
                    yield return null;
                }
                t = 0f;
                while (t < 0.10f)
                {
                    t += Time.unscaledDeltaTime;
                    iconTr.localScale = originalScale * Mathf.Lerp(1.28f, 1.05f, t / 0.10f);
                    yield return null;
                }

                // Nhịp 2 (Nhẹ)
                t = 0f;
                while (t < 0.10f)
                {
                    t += Time.unscaledDeltaTime;
                    iconTr.localScale = originalScale * Mathf.Lerp(1.05f, 1.18f, t / 0.10f);
                    yield return null;
                }
                t = 0f;
                while (t < 0.14f)
                {
                    t += Time.unscaledDeltaTime;
                    iconTr.localScale = originalScale * Mathf.Lerp(1.18f, 1.0f, t / 0.14f);
                    yield return null;
                }

                // Khoảng nghỉ giữa 2 nhịp tim
                yield return new WaitForSecondsRealtime(0.40f);
            }

            if (iconTr != null) iconTr.localScale = originalScale;
        }

        /// <summary>
        /// Tự động làm mờ / vô hiệu hóa nút mua tháp nếu người chơi không đủ vàng.
        /// Đồng thời kiểm tra nếu đang chọn tháp mà số vàng bị hụt xuống dưới chi phí mua, tự động hủy chọn (Edge Case 1).
        /// </summary>
        public void UpdateTowerButtonsAvailability()
        {
            if (BuildManager.Instance == null) return;

            int gold = _currentGold;
            SetButtonState(archerBtn, archerCostText, gold >= BuildManager.Instance.ArcherCost);
            SetButtonState(bombBtn, bombCostText, gold >= BuildManager.Instance.BombCost);
            SetButtonState(mageBtn, mageCostText, gold >= BuildManager.Instance.MageCost);
            SetButtonState(frostBtn, frostCostText, gold >= BuildManager.Instance.FrostCost);
            if (wallBtn != null && wallBtn.gameObject.activeInHierarchy)
            {
                SetButtonState(wallBtn, wallCostText, gold >= BuildManager.Instance.WallCost);
            }

            // Trường hợp biên (Edge Case 1): Nếu đang chọn tháp mà số vàng bị hụt xuống dưới chi phí mua, tự động hủy chọn
            if (BuildManager.Instance.SelectedTowerType != TowerType.None && !BuildManager.Instance.HasEnoughMoney(BuildManager.Instance.SelectedTowerType))
            {
                BuildManager.Instance.SelectTowerToBuild(TowerType.None);
            }
        }

        /// <summary>
        /// Thiết lập trạng thái hiển thị của nút mua công trình:
        /// - Khi ĐỦ VÀNG: Nút tương tác được (interactable=true), nền Slate/Navy sang trọng, giá Vàng rực rỡ (#FBBF24), chữ sắc nét.
        /// - Khi THIẾU VÀNG: Làm tối mờ cao cấp (interactable=false), nền sẫm mờ (Alpha 0.45), giá Vàng chuyển Đỏ cảnh báo (#EF4444), icon mờ nhẹ.
        /// </summary>
        private void SetButtonState(Button btn, Text costText, bool enabledState)
        {
            if (btn == null) return;
            btn.interactable = enabledState;

            // 1. Nền thẻ nút
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = enabledState 
                    ? new Color(0.14f, 0.18f, 0.25f, 0.96f)  // Slate / Navy sang trọng
                    : new Color(0.08f, 0.09f, 0.12f, 0.45f); // Tối mờ cao cấp
            }

            // 2. Chữ giá tiền vàng
            if (costText != null)
            {
                costText.color = enabledState 
                    ? new Color(1.0f, 0.88f, 0.25f, 1f)     // Vàng hoàng gia rực rỡ
                    : new Color(0.95f, 0.28f, 0.28f, 0.75f); // Đỏ thẫm cảnh báo thiếu tiền
            }

            // 3. Làm mờ / nổi bật các dòng chữ nhãn trong thẻ (ví dụ: '🏹 CUNG [1]')
            Text[] allTexts = btn.GetComponentsInChildren<Text>(true);
            foreach (var txt in allTexts)
            {
                if (txt != costText)
                {
                    txt.color = enabledState
                        ? new Color(1f, 0.96f, 0.88f, 1f)       // Trắng ngà nổi bật
                        : new Color(0.50f, 0.55f, 0.62f, 0.50f); // Xám bạc tối mờ
                }
            }

            // 4. Làm mờ icon minh họa trong thẻ nếu có
            Image[] allImages = btn.GetComponentsInChildren<Image>(true);
            foreach (var iconImg in allImages)
            {
                if (iconImg != img && !iconImg.name.Contains("Highlight"))
                {
                    Color c = iconImg.color;
                    c.a = enabledState ? 1f : 0.35f;
                    iconImg.color = c;
                }
            }
        }

        /// <summary>
        /// Nút chuyển đổi tốc độ game (1x <-> 2x Speed).
        /// </summary>
        public void ToggleFastForward()
        {
            _isFastForward = !_isFastForward;
            if (!_isPaused)
            {
                Time.timeScale = _isFastForward ? 2.0f : 1.0f;
            }

            AudioManager.Instance?.PlayUIClick();

            if (speedText != null)
            {
                speedText.text = _isFastForward ? "2X" : "1X";
                speedText.color = _isFastForward ? new Color(1.0f, 0.88f, 0.25f, 1f) : Color.white;
            }

            if (speedBtn != null)
            {
                Image btnImg = speedBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = _isFastForward 
                        ? new Color(0.18f, 0.25f, 0.36f, 0.98f) 
                        : new Color(0.12f, 0.15f, 0.20f, 0.92f);
                }
            }

            OnFastForwardToggled?.Invoke(_isFastForward);
            Debug.Log($"[GameHUD] Tốc độ game: {(_isFastForward ? "2X" : "1X")} (Time.timeScale={Time.timeScale})");
        }

        /// <summary>
        /// Thiết lập trực tiếp chế độ tăng tốc độ game.
        /// </summary>
        public void SetFastForward(bool fastForward)
        {
            if (_isFastForward == fastForward) return;
            ToggleFastForward();
        }

        /// <summary>
        /// Cập nhật hiển thị đồ họa của Nút Tạm dừng / Tiếp tục:
        /// - Khi game ĐANG CHƠI: Hiển thị icon ⏸ (hai vạch dọc Pause sắc nét).
        /// - Khi game ĐANG TẠM DỪNG: Hiển thị icon ▶ (tam giác Play màu vàng kim nổi bật).
        /// </summary>
        public void UpdatePauseVisuals()
        {
            if (pauseBtn != null)
            {
                Image btnImg = pauseBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    if (iconPause != null && iconResume != null)
                    {
                        btnImg.sprite = _isPaused ? iconResume : iconPause;
                        btnImg.color = Color.white;
                    }
                    else
                    {
                        btnImg.color = _isPaused 
                            ? new Color(0.22f, 0.28f, 0.40f, 0.98f) 
                            : new Color(0.12f, 0.15f, 0.20f, 0.92f);
                    }
                }
            }

            if (pauseText != null)
            {
                if (iconPause != null && iconResume != null)
                {
                    // Khi đã có sprite chuẩn hiển thị trực tiếp trên nút, ẩn text để icon rõ đẹp nhất
                    pauseText.text = "";
                }
                else
                {
                    // Fallback an toàn khi chưa gán sprite: Dùng ký tự chuẩn ASCII "||" và "▶"
                    pauseText.text = _isPaused ? "▶" : "||";
                    pauseText.color = _isPaused ? new Color(1.0f, 0.88f, 0.25f, 1f) : Color.white;
                }
            }
        }

        /// <summary>
        /// Tạm dừng hoặc tiếp tục game.
        /// </summary>
        public void TogglePause()
        {
            _isPaused = !_isPaused;
            AudioManager.Instance?.PlayUIClick();

            if (_isPaused)
            {
                Time.timeScale = 0f;
                EnsurePauseOverlay();
                UpdateAudioButtonsVisual();
                if (pauseOverlay != null)
                {
                    pauseOverlay.SetActive(true);
                    Transform panelTr = pauseOverlay.transform.Find("PauseModal_Panel");
                    if (panelTr != null)
                    {
                        StartCoroutine(AnimateModalPopRoutine(panelTr));
                    }
                }
                Debug.Log("[GameHUD] Đã mở Pause Modal Dialog (Pause).");
            }
            else
            {
                Time.timeScale = _isFastForward ? 2.0f : 1.0f;
                if (pauseOverlay != null) pauseOverlay.SetActive(false);
                Debug.Log($"[GameHUD] Tiếp tục game (Resume, timeScale={Time.timeScale}).");
            }

            UpdatePauseVisuals();
            OnPauseToggled?.Invoke(_isPaused);
        }

        /// <summary>
        /// Đóng Pause Modal một cách an toàn (dùng khi kết thúc trận đấu hoặc cần đóng từ bên ngoài).
        /// </summary>
        public void ClosePauseModal()
        {
            if (!_isPaused) return;
            _isPaused = false;
            Time.timeScale = _isFastForward ? 2.0f : 1.0f;
            if (pauseOverlay != null) pauseOverlay.SetActive(false);
            UpdatePauseVisuals();
            OnPauseToggled?.Invoke(false);
        }

        /// <summary>
        /// Xử lý khi người chơi bấm nút "VỀ MENU / THOÁT":
        /// - Đặt lại Time.timeScale = 1f
        /// - Đóng Pause Modal
        /// - Trong Unity Editor: Dừng chế độ Play Mode.
        /// - Trong bản Build độc lập (Windows .exe / Android APK): Thoát ứng dụng ra hệ điều hành.
        /// </summary>
        public void QuitOrReturnToMenu()
        {
            Debug.Log("[GameHUD] Người chơi chọn VỀ MENU / THOÁT...");
            Time.timeScale = 1f;

            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(false);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
                return;
            }

            // Fallback nếu không có GameManager trong Scene
            if (UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings > 1)
            {
                string firstScenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(0);
                if (!string.IsNullOrEmpty(firstScenePath) && firstScenePath.ToLower().Contains("menu"))
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                    return;
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Thiết lập trực tiếp trạng thái Tạm dừng game.
        /// </summary>
        public void SetPaused(bool pause)
        {
            if (_isPaused == pause) return;
            TogglePause();
        }

        private void UpdateAllDisplays()
        {
            if (livesText != null)
            {
                livesText.text = _currentLives.ToString();
                livesText.color = (_currentLives < 5 && _currentLives > 0) ? criticalLivesColor : normalLivesColor;
            }
            if (goldText != null) goldText.text = _currentGold.ToString();
            if (waveText != null) waveText.text = $"WAVE {_currentWave:D2} / {totalWaves:D2}";
            
            if (speedText != null)
            {
                speedText.text = _isFastForward ? "2X" : "1X";
                speedText.color = _isFastForward ? new Color(1.0f, 0.88f, 0.25f, 1f) : Color.white;
            }
            
            UpdatePauseVisuals();

            if (archerCostText != null) archerCostText.text = "100";
            if (bombCostText != null) bombCostText.text = "125";
            if (mageCostText != null) mageCostText.text = "150";
            if (frostCostText != null) frostCostText.text = "175";
            if (wallCostText != null) wallCostText.text = "70";

            UpdateManaDisplay();
            UpdateTowerButtonsAvailability();
        }

        #region --- NOTIFICATION & BALANCE DEBUG TOOLS ---

        /// <summary>
        /// Hiển thị thông báo biểu ngữ nổi bật (Wave Clear, thưởng vàng, debug info) trên đỉnh màn hình.
        /// </summary>
        public void ShowNotification(string message, Color textColor)
        {
            EnsureNotificationText();
            if (_notificationText == null) return;

            if (_notificationRoutine != null) StopCoroutine(_notificationRoutine);
            _notificationRoutine = StartCoroutine(NotificationRoutine(message, textColor));
        }

        private void EnsureNotificationText()
        {
            if (_notificationText != null) return;

            GameObject notifObj = new GameObject("HUD_Notification_Banner");
            notifObj.transform.SetParent(transform, false);

            RectTransform rt = notifObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -72f);
            rt.sizeDelta = new Vector2(600f, 44f);

            _notificationText = notifObj.AddComponent<Text>();
            Font f = (livesText != null && livesText.font != null)
                ? livesText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans" }, 20);
            _notificationText.font = f;
            _notificationText.fontSize = 20;
            _notificationText.fontStyle = FontStyle.Bold;
            _notificationText.alignment = TextAnchor.MiddleCenter;
            _notificationText.raycastTarget = false;

            Outline outline = notifObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.8f, -1.8f);

            notifObj.SetActive(false);
        }

        private IEnumerator NotificationRoutine(string message, Color textColor)
        {
            _notificationText.text = message;
            _notificationText.color = textColor;
            _notificationText.gameObject.SetActive(true);

            Transform target = _notificationText.transform;
            target.localScale = Vector3.one * 1.30f;

            float punchTime = 0.16f;
            float elapsed = 0f;
            while (elapsed < punchTime)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(Vector3.one * 1.30f, Vector3.one, elapsed / punchTime);
                yield return null;
            }
            target.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(1.3f);

            float fadeTime = 0.45f;
            elapsed = 0f;
            Color baseCol = textColor;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                baseCol.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                _notificationText.color = baseCol;
                yield return null;
            }

            _notificationText.gameObject.SetActive(false);
            _notificationRoutine = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Bộ phím tắt kiểm thử cân bằng nhanh (Quick-Balance Debug Hotkeys):
        /// [G]: Nhận ngay +500 Vàng
        /// [M]: Hồi đầy 100 Mana
        /// [K]: Tiêu diệt toàn bộ quái vật đang có trên bản đồ
        /// [L]: Hồi đầy 20 Mạng thành trì
        /// [N]: Kích hoạt ngay đợt quái tiếp theo
        /// </summary>
        private void HandleDebugHotkeys()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.G))
                {
                    AddGold(500);
                    ShowNotification("⚡ DEBUG: +500 VÀNG!", new Color(1.0f, 0.88f, 0.20f));
                    AudioManager.Instance?.PlayTowerPlaced();
                }

                if (Input.GetKeyDown(KeyCode.M))
                {
                    AddMana(100f);
                    ShowNotification("⚡ DEBUG: HỒI ĐẦY 100 MANA!", new Color(0.35f, 0.88f, 1.0f));
                    AudioManager.Instance?.PlayMagicZap();
                }

                if (Input.GetKeyDown(KeyCode.K))
                {
                    Enemy.Enemy.KillAllActiveEnemies();
                    ShowNotification("⚡ DEBUG: TIÊU DIỆT TOÀN BỘ QUÁI!", new Color(1.0f, 0.35f, 0.35f));
                    AudioManager.Instance?.PlayMeteorImpact();
                }

                if (Input.GetKeyDown(KeyCode.L))
                {
                    SetLives(20);
                    ShowNotification("⚡ DEBUG: HỒI ĐẦY 20 MẠNG!", new Color(0.40f, 1.0f, 0.40f));
                }

                if (Input.GetKeyDown(KeyCode.N))
                {
                    if (WaveSpawner.Instance != null)
                    {
                        WaveSpawner.Instance.ForceNextWave();
                        ShowNotification($"⚡ DEBUG: KÍCH HOẠT WAVE {WaveSpawner.Instance.CurrentWaveIndex}!", new Color(0.85f, 0.70f, 1.0f));
                    }
                }
            }
            catch (System.InvalidOperationException) { }
        }
#endif

        #endregion
    }
}
