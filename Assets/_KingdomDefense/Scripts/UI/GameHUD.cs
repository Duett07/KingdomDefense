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
            AutoBindTopHUDReferences();
            SetupButtonListeners();
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
        /// Tự động dò tìm các icon và thành phần Top HUD nếu chưa được gán thủ công trong Inspector.
        /// </summary>
        private void AutoBindTopHUDReferences()
        {
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

            // Tùy chỉnh màu sắc Xanh Lam Ngọc (Cyan/Azure Crystal) cho thanh Mana
            if (manaFillImage != null)
            {
                manaFillImage.color = new Color(0.06f, 0.72f, 0.94f, 1f);
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

        private void Start()
        {
            EnsureEventSystem();
            UpdateAllDisplays();
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
        }

        private void SetButtonState(Button btn, Text costText, bool enabledState)
        {
            if (btn == null) return;
            btn.interactable = enabledState;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                // Giữ màu nền thẻ sẫm màu sang trọng (Slate / Navy) thay vì biến thành màu trắng bệch
                img.color = enabledState 
                    ? new Color(0.14f, 0.17f, 0.23f, 0.95f) 
                    : new Color(0.10f, 0.11f, 0.14f, 0.60f);
            }

            if (costText != null)
            {
                costText.color = enabledState 
                    ? new Color(1f, 0.88f, 0.25f, 1f) 
                    : new Color(0.95f, 0.35f, 0.35f, 0.7f);
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
