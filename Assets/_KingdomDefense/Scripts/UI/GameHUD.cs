using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KingdomDefense.Build;

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

        [SerializeField] private Image archerHighlight;
        [SerializeField] private Image bombHighlight;
        [SerializeField] private Image mageHighlight;
        [SerializeField] private Image frostHighlight;
        [SerializeField] private Image wallHighlight;

        [SerializeField] private Text archerCostText;
        [SerializeField] private Text bombCostText;
        [SerializeField] private Text mageCostText;
        [SerializeField] private Text frostCostText;
        [SerializeField] private Text wallCostText;

        private int _currentLives;
        private int _currentGold;
        private int _currentWave = 1;
        private float _currentMana;
        private bool _isFastForward;

        // Trạng thái hiệu ứng và Coroutines phản hồi thị giác
        private Coroutine _heartbeatRoutine;
        private Coroutine _goldPunchRoutine;
        private Coroutine _livesPunchRoutine;
        private Coroutine _wavePunchRoutine;
        private bool _isCriticalLives = false;

        public int CurrentLives => _currentLives;
        public int CurrentGold => _currentGold;
        public int CurrentWave => _currentWave;
        public float CurrentMana => _currentMana;
        public bool IsCriticalLives => _isCriticalLives;

        public event System.Action<int> OnLivesChanged;
        public event System.Action<int> OnGoldChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            _currentLives = initialLives;
            _currentGold = initialGold;
            _currentWave = 1;
            _currentMana = maxMana;

            EnsureEventSystem();
            AutoBindHUDReferences();
            AlignAndEnforceHUDLayout();
            SetupButtonListeners();
            UpdateAllDisplays();
        }

        private void Start()
        {
            EnsureEventSystem();
            AlignAndEnforceHUDLayout();
            UpdateAllDisplays();
        }

        private void OnDisable()
        {
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

            // 2. CĂN CHỈNH BOTTOM-CENTER BUILD DOCK (Khung rộng 840x125 ôm trọn 4 thẻ nút 710px)
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
                    bdRect.sizeDelta = new Vector2(840f, 125f);
                }

                // Căn chỉnh đều 4 thẻ nút: -270, -90, 90, 270 (mỗi thẻ rộng 170px)
                float[] cardXPositions = new float[] { -270f, -90f, 90f, 270f };
                int cardIndex = 0;

                for (int i = 0; i < bdPanel.childCount && cardIndex < 4; i++)
                {
                    Transform card = bdPanel.GetChild(i);
                    if (card.name.StartsWith("Card_"))
                    {
                        RectTransform cr = card.GetComponent<RectTransform>();
                        if (cr != null)
                        {
                            cr.anchorMin = new Vector2(0.5f, 0.5f);
                            cr.anchorMax = new Vector2(0.5f, 0.5f);
                            cr.pivot = new Vector2(0.5f, 0.5f);
                            cr.anchoredPosition = new Vector2(cardXPositions[cardIndex], 0f);
                            cr.sizeDelta = new Vector2(170f, 95f);
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
        }

        /// <summary>
        /// Tự động dò tìm các icon Top HUD và các nút Bottom Dock nếu chưa được gán thủ công trong Inspector.
        /// </summary>
        private void AutoBindHUDReferences()
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
                    if (cardName.Contains("cung") || cardName.Contains("archer"))
                    {
                        if (archerBtn == null) archerBtn = btn;
                        if (archerHighlight == null) archerHighlight = card.Find("Highlight")?.GetComponent<Image>();
                        if (archerCostText == null) archerCostText = card.Find("CostText")?.GetComponent<Text>();
                    }
                    else if (cardName.Contains("pháo") || cardName.Contains("phao") || cardName.Contains("bomb") || cardName.Contains("cannon"))
                    {
                        if (bombBtn == null) bombBtn = btn;
                        if (bombHighlight == null) bombHighlight = card.Find("Highlight")?.GetComponent<Image>();
                        if (bombCostText == null) bombCostText = card.Find("CostText")?.GetComponent<Text>();
                    }
                    else if (cardName.Contains("phép") || cardName.Contains("phep") || cardName.Contains("mage"))
                    {
                        if (mageBtn == null) mageBtn = btn;
                        if (mageHighlight == null) mageHighlight = card.Find("Highlight")?.GetComponent<Image>();
                        if (mageCostText == null) mageCostText = card.Find("CostText")?.GetComponent<Text>();
                    }
                    else if (cardName.Contains("băng") || cardName.Contains("bang") || cardName.Contains("frost"))
                    {
                        if (frostBtn == null) frostBtn = btn;
                        if (frostHighlight == null) frostHighlight = card.Find("Highlight")?.GetComponent<Image>();
                        if (frostCostText == null) frostCostText = card.Find("CostText")?.GetComponent<Text>();
                    }
                    else if (cardName.Contains("tường") || cardName.Contains("tuong") || cardName.Contains("wall"))
                    {
                        if (wallBtn == null) wallBtn = btn;
                        if (wallHighlight == null) wallHighlight = card.Find("Highlight")?.GetComponent<Image>();
                        if (wallCostText == null) wallCostText = card.Find("CostText")?.GetComponent<Text>();
                    }
                }
            }
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
            // Tự hồi phục Mana theo thời gian
            if (_currentMana < maxMana)
            {
                _currentMana = Mathf.Min(maxMana, _currentMana + manaRegenPerSecond * Time.deltaTime);
                UpdateManaDisplay();
            }
        }

        private void SetupButtonListeners()
        {
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
            if (wallBtn != null)
            {
                wallBtn.onClick.RemoveAllListeners();
                wallBtn.onClick.AddListener(() => OnSelectTower(TowerType.Wall));
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

            // Hiệu ứng giật nảy khi bị quái vật trừ máu
            if (_currentLives < prevLives)
            {
                if (_livesPunchRoutine != null) StopCoroutine(_livesPunchRoutine);
                _livesPunchRoutine = StartCoroutine(PunchScaleRoutine(livesText.transform, 1.35f, 0.20f));
                if (livesIcon != null) StartCoroutine(PunchScaleRoutine(livesIcon.transform, 1.30f, 0.20f));
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
        /// - Hiển thị chữ 'MANA 100 / 100 MP' với hiệu ứng phát sáng khi >= 60 MP (sẵn sàng chiêu cuối).
        /// </summary>
        private void UpdateManaDisplay()
        {
            if (manaSlider != null)
            {
                manaSlider.value = _currentMana / maxMana;
            }
            if (manaText != null)
            {
                manaText.text = $"MANA {Mathf.FloorToInt(_currentMana)} / {Mathf.FloorToInt(maxMana)} MP";

                // Phát sáng xanh rực rỡ khi tích đủ >= 60 Mana
                if (_currentMana >= 60f)
                {
                    manaText.color = manaReadyColor;
                }
                else
                {
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
            SetButtonState(wallBtn, wallCostText, gold >= BuildManager.Instance.WallCost);

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
            Time.timeScale = _isFastForward ? 2.0f : 1.0f;

            if (speedText != null)
            {
                speedText.text = _isFastForward ? "2X" : "1X";
                speedText.color = _isFastForward ? new Color(1f, 0.85f, 0.2f) : Color.white;
            }
            Debug.Log($"[GameHUD] Tốc độ game: {Time.timeScale}x");
        }

        /// <summary>
        /// Tạm dừng hoặc tiếp tục game.
        /// </summary>
        public void TogglePause()
        {
            if (Time.timeScale > 0.01f)
            {
                Time.timeScale = 0f;
                Debug.Log("[GameHUD] Đã tạm dừng game (Pause).");
            }
            else
            {
                Time.timeScale = _isFastForward ? 2.0f : 1.0f;
                Debug.Log("[GameHUD] Tiếp tục game.");
            }
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
            if (speedText != null) speedText.text = _isFastForward ? "2X" : "1X";

            if (archerCostText != null) archerCostText.text = "100";
            if (bombCostText != null) bombCostText.text = "125";
            if (mageCostText != null) mageCostText.text = "150";
            if (frostCostText != null) frostCostText.text = "175";
            if (wallCostText != null) wallCostText.text = "70";

            UpdateManaDisplay();
            UpdateTowerButtonsAvailability();
        }
    }
}
