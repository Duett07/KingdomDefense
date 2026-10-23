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

        [Header("--- BOTTOM DOCK THAM CHIẾU ---")]
        [SerializeField] private Button archerBtn;
        [SerializeField] private Button bombBtn;
        [SerializeField] private Button mageBtn;
        [SerializeField] private Button wallBtn;

        [SerializeField] private Image archerHighlight;
        [SerializeField] private Image bombHighlight;
        [SerializeField] private Image mageHighlight;
        [SerializeField] private Image wallHighlight;

        [SerializeField] private Text archerCostText;
        [SerializeField] private Text bombCostText;
        [SerializeField] private Text mageCostText;
        [SerializeField] private Text wallCostText;

        private int _currentLives;
        private int _currentGold;
        private int _currentWave = 1;
        private float _currentMana;
        private bool _isFastForward;

        public int CurrentLives => _currentLives;
        public int CurrentGold => _currentGold;
        public int CurrentWave => _currentWave;
        public float CurrentMana => _currentMana;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            _currentLives = initialLives;
            _currentGold = initialGold;
            _currentWave = 1;
            _currentMana = maxMana;

            SetupButtonListeners();
            UpdateAllDisplays();
        }

        private void Start()
        {
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
            if (archerBtn != null) archerBtn.onClick.AddListener(() => OnSelectTower(TowerType.Archer));
            if (bombBtn != null) bombBtn.onClick.AddListener(() => OnSelectTower(TowerType.Bomb));
            if (mageBtn != null) mageBtn.onClick.AddListener(() => OnSelectTower(TowerType.Mage));
            if (wallBtn != null) wallBtn.onClick.AddListener(() => OnSelectTower(TowerType.Wall));
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
            Color activeColor = new Color(0.2f, 1f, 0.4f, 1f);
            Color normalColor = new Color(1f, 1f, 1f, 0f);

            if (archerHighlight != null) archerHighlight.color = (type == TowerType.Archer) ? activeColor : normalColor;
            if (bombHighlight != null) bombHighlight.color = (type == TowerType.Bomb) ? activeColor : normalColor;
            if (mageHighlight != null) mageHighlight.color = (type == TowerType.Mage) ? activeColor : normalColor;
            if (wallHighlight != null) wallHighlight.color = (type == TowerType.Wall) ? activeColor : normalColor;
        }

        /// <summary>
        /// Cập nhật số sinh mệnh (Trừ khi quái lọt vào thành).
        /// </summary>
        public void SetLives(int lives)
        {
            _currentLives = Mathf.Max(0, lives);
            if (livesText != null) livesText.text = _currentLives.ToString();

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
        /// </summary>
        public void SetGold(int gold)
        {
            _currentGold = Mathf.Max(0, gold);
            if (goldText != null) goldText.text = _currentGold.ToString();

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
        /// Cập nhật hiển thị đợt quái.
        /// </summary>
        public void SetWave(int wave, int total = -1)
        {
            _currentWave = wave;
            if (total > 0) totalWaves = total;
            if (waveText != null) waveText.text = $"WAVE: {_currentWave}/{totalWaves}";
        }

        /// <summary>
        /// Trừ Mana khi kích hoạt kỹ năng Mưa Thiên Thạch.
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

        private void UpdateManaDisplay()
        {
            if (manaSlider != null)
            {
                manaSlider.value = _currentMana / maxMana;
            }
            if (manaText != null)
            {
                manaText.text = $"MANA: {Mathf.FloorToInt(_currentMana)}/{Mathf.FloorToInt(maxMana)}";
            }
        }

        /// <summary>
        /// Tự động làm mờ / vô hiệu hóa nút mua tháp nếu người chơi không đủ vàng.
        /// </summary>
        public void UpdateTowerButtonsAvailability()
        {
            if (BuildManager.Instance == null) return;

            int gold = _currentGold;
            SetButtonState(archerBtn, gold >= BuildManager.Instance.ArcherCost);
            SetButtonState(bombBtn, gold >= BuildManager.Instance.BombCost);
            SetButtonState(mageBtn, gold >= BuildManager.Instance.MageCost);
            SetButtonState(wallBtn, gold >= BuildManager.Instance.WallCost);
        }

        private void SetButtonState(Button btn, bool enabledState)
        {
            if (btn == null) return;
            btn.interactable = enabledState;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = enabledState ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
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
            if (livesText != null) livesText.text = _currentLives.ToString();
            if (goldText != null) goldText.text = _currentGold.ToString();
            if (waveText != null) waveText.text = $"WAVE: {_currentWave}/{totalWaves}";
            if (speedText != null) speedText.text = _isFastForward ? "2X" : "1X";

            if (archerCostText != null) archerCostText.text = "100";
            if (bombCostText != null) bombCostText.text = "125";
            if (mageCostText != null) mageCostText.text = "150";
            if (wallCostText != null) wallCostText.text = "70";

            UpdateManaDisplay();
            UpdateTowerButtonsAvailability();
        }
    }
}
