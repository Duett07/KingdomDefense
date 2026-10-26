using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using KingdomDefense.UI;
using KingdomDefense.Enemy;
using KingdomDefense.Audio;

namespace KingdomDefense.Core
{
    /// <summary>
    /// Trạng thái tổng thể của trận đánh Kingdom Defense.
    /// </summary>
    public enum GameState
    {
        Playing,
        Paused,
        Victory,
        Defeat
    }

    /// <summary>
    /// Quản lý vòng lặp trò chơi cốt lõi (Core Game Loop):
    /// - Quản lý trạng thái: Đang chơi (Playing), Tạm dừng (Paused), Chiến thắng (Victory), Thất bại (Defeat).
    /// - Điều kiện Thất bại: Lives <= 0 ➔ Kích hoạt Game Over Modal.
    /// - Điều kiện Chiến thắng: Diệt sạch quái Wave 10 và Lives > 0 ➔ Kích hoạt Victory Modal.
    /// - Hệ thống đánh giá Sao:
    ///   + 3 Sao: Giữ nguyên vẹn 20/20 Lives.
    ///   + 2 Sao: Còn 10 - 19 Lives.
    ///   + 1 Sao: Còn dưới 10 Lives.
    /// - Hiệu ứng chuyển cảnh Slow-motion, hoạt ảnh bung nở Sao vàng, và cơ chế Chơi lại (Restart) sạch sẽ.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("--- THIẾT LẬP ĐIỀU KIỆN SAO ---")]
        [Tooltip("Số sinh mệnh tối đa để đạt 3 Sao")]
        [SerializeField] private int maxLivesForThreeStars = 20;
        [Tooltip("Ngưỡng sinh mệnh tối thiểu để đạt 2 Sao")]
        [SerializeField] private int minLivesForTwoStars = 10;

        [Header("--- GIAO DIỆN MODAL KẾT THÚC (OPTIONAL) ---")]
        [SerializeField] private GameObject victoryModalPrefab;
        [SerializeField] private GameObject defeatModalPrefab;

        private GameState _currentState = GameState.Playing;
        private int _starsEarned = 0;
        private bool _isEndingSequenceActive = false;

        // UI Runtime Modal References
        private GameObject _activeModalObject;
        private Coroutine _endGameRoutine;

        public GameState CurrentState => _currentState;
        public int StarsEarned => _starsEarned;
        public bool IsGameOver => _currentState == GameState.Victory || _currentState == GameState.Defeat;

        // Sự kiện thông báo trạng thái toàn cục
        public event System.Action<GameState> OnGameStateChanged;
        public event System.Action<int> OnVictory;
        public event System.Action OnDefeat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && Object.FindAnyObjectByType<GameManager>() == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
                Debug.Log("[GameManager] Tự động khởi tạo GameManager trong Scene!");
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _currentState = GameState.Playing;
            _starsEarned = 0;
            _isEndingSequenceActive = false;
        }

        private void Start()
        {
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.OnLivesChanged += HandleLivesChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.OnLivesChanged -= HandleLivesChanged;
            }
        }

        private void Update()
        {
            if (_currentState != GameState.Playing || _isEndingSequenceActive) return;

            // Kiểm tra điều kiện Thất bại: Lives <= 0
            if (GameHUD.Instance != null && GameHUD.Instance.CurrentLives <= 0)
            {
                TriggerDefeat();
                return;
            }

            // Kiểm tra điều kiện Chiến thắng:
            // 1. WaveSpawner đã hoàn tất xuất toàn bộ quái Wave 10
            // 2. Không còn quái vật nào sống sót trên bản đồ (Enemy.ActiveEnemies.Count == 0)
            // 3. Vương quốc còn sinh mệnh (Lives > 0)
            if (WaveSpawner.Instance != null && WaveSpawner.Instance.AllWavesSpawned)
            {
                int activeEnemies = Enemy.Enemy.ActiveEnemies != null ? Enemy.Enemy.ActiveEnemies.Count : 0;
                if (activeEnemies == 0)
                {
                    int currentLives = GameHUD.Instance != null ? GameHUD.Instance.CurrentLives : 0;
                    if (currentLives > 0)
                    {
                        TriggerVictory();
                    }
                }
            }
        }

        /// <summary>
        /// Xử lý khi lượng sinh mệnh của thành trì thay đổi.
        /// </summary>
        private void HandleLivesChanged(int newLives)
        {
            if (_currentState != GameState.Playing || _isEndingSequenceActive) return;

            if (newLives <= 0)
            {
                TriggerDefeat();
            }
        }

        /// <summary>
        /// Tính toán số sao đạt được dựa trên số sinh mệnh còn lại của Vương quốc:
        /// - 3 Sao (20 Lives): Thủ thành tuyệt đối, không quái nào chạm tới thành.
        /// - 2 Sao (10 - 19 Lives): Kiên cường phòng thủ, bảo toàn lực lượng.
        /// - 1 Sao (< 10 Lives): Chiến thắng trong gian nan.
        /// </summary>
        public int CalculateStars(int remainingLives)
        {
            if (remainingLives >= maxLivesForThreeStars) return 3;
            if (remainingLives >= minLivesForTwoStars) return 2;
            return 1;
        }

        /// <summary>
        /// Kích hoạt trạng thái Chiến thắng (Victory).
        /// </summary>
        public void TriggerVictory()
        {
            if (_isEndingSequenceActive || _currentState != GameState.Playing) return;

            int remainingLives = GameHUD.Instance != null ? GameHUD.Instance.CurrentLives : 20;
            _starsEarned = CalculateStars(remainingLives);
            _currentState = GameState.Victory;
            _isEndingSequenceActive = true;

            Debug.Log($"[GameManager] === CHIẾN THẮNG VANG DỘI! === Sinh mệnh: {remainingLives}, Đạt: {_starsEarned} Sao!");
            OnGameStateChanged?.Invoke(_currentState);
            OnVictory?.Invoke(_starsEarned);
            AudioManager.Instance?.PlayVictory();

            _endGameRoutine = StartCoroutine(EndGameSequenceRoutine(true, _starsEarned, remainingLives));
        }

        /// <summary>
        /// Kích hoạt trạng thái Thất bại (Defeat / Game Over).
        /// </summary>
        public void TriggerDefeat()
        {
            if (_isEndingSequenceActive || _currentState != GameState.Playing) return;

            _starsEarned = 0;
            _currentState = GameState.Defeat;
            _isEndingSequenceActive = true;

            Debug.LogWarning("[GameManager] === VƯƠNG QUỐC ĐÃ THẤT THỦ! === Game Over.");
            OnGameStateChanged?.Invoke(_currentState);
            OnDefeat?.Invoke();
            AudioManager.Instance?.PlayDefeat();

            _endGameRoutine = StartCoroutine(EndGameSequenceRoutine(false, 0, 0));
        }

        /// <summary>
        /// Coroutine làm chậm thời gian (Slow-motion 0.2s) rồi dừng Time.timeScale = 0,
        /// sau đó hiển thị Modal Giao diện Hoàng gia và kích hoạt hiệu ứng Bung nở Sao.
        /// </summary>
        private IEnumerator EndGameSequenceRoutine(bool isVictory, int stars, int remainingLives)
        {
            // Bước 1: Slow-motion giảm tốc độ game mượt mà
            float slowDuration = 0.25f;
            float elapsed = 0f;
            float initialTimeScale = Time.timeScale > 0.01f ? Time.timeScale : 1f;

            while (elapsed < slowDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(initialTimeScale, 0.05f, elapsed / slowDuration);
                yield return null;
            }

            Time.timeScale = 0f;

            // Bước 2: Hiển thị Modal Giao diện tương ứng
            DisplayEndGameModal(isVictory, stars, remainingLives);
        }

        /// <summary>
        /// Hiển thị bảng Modal Victory hoặc Defeat. Tự động sinh giao diện chuẩn Hoàng gia nếu chưa gắn Prefab.
        /// </summary>
        private void DisplayEndGameModal(bool isVictory, int stars, int remainingLives)
        {
            if (_activeModalObject != null)
            {
                Destroy(_activeModalObject);
            }

            // Trường hợp biên 1: Nếu người chơi đang mở Pause Menu -> Đóng ngay lập tức để ưu tiên bảng Thắng/Thua
            if (KingdomDefense.UI.GameHUD.Instance != null && KingdomDefense.UI.GameHUD.Instance.IsPaused)
            {
                KingdomDefense.UI.GameHUD.Instance.ClosePauseModal();
            }

            Canvas targetCanvas = Object.FindAnyObjectByType<Canvas>();
            if (targetCanvas == null)
            {
                GameObject canvasObj = new GameObject("EndGameCanvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 999;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            EnsureEventSystem();

            // Sinh bảng Modal giao diện Hoàng gia sắc nét
            _activeModalObject = CreateProceduralModal(targetCanvas.transform, isVictory, stars, remainingLives);
        }

        /// <summary>
        /// Tạo giao diện Modal kết thúc trận đánh chuẩn phong cách Kingdom Rush kinh điển:
        /// - Màu chủ đạo: Vàng Hoàng Gia (Royal Gold #F59E0B), Đỏ Thẫm (Crimson #DC2626), Đá Than Sẫm (Slate #0F172A).
        /// - Tuyệt đối không dùng màu Tím/Violet (Tuân thủ Purple Ban).
        /// </summary>
        private GameObject CreateProceduralModal(Transform parent, bool isVictory, int stars, int remainingLives)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Lớp phủ đen làm mờ nền màn hình (Backdrop Overlay)
            GameObject overlay = new GameObject(isVictory ? "VictoryModal_Overlay" : "DefeatModal_Overlay");
            overlay.transform.SetParent(parent, false);
            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0.04f, 0.06f, 0.09f, 0.88f); // Đen than mờ sang trọng

            // 2. Hộp thoại chính (Dialog Card Panel)
            GameObject panel = new GameObject("ModalPanel");
            panel.transform.SetParent(overlay.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(560f, 440f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.10f, 0.13f, 0.18f, 0.98f); // Đá phiến hoàng gia sẫm

            Outline panelOutline = panel.AddComponent<Outline>();
            panelOutline.effectColor = isVictory 
                ? new Color(0.96f, 0.72f, 0.15f, 1f)  // Viền vàng rực rỡ
                : new Color(0.85f, 0.20f, 0.20f, 1f); // Viền đỏ cảnh báo
            panelOutline.effectDistance = new Vector2(3f, -3f);

            // 3. Tiêu đề Banner (Header Title)
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 155f);
            titleRect.sizeDelta = new Vector2(520f, 55f);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = isVictory ? "CHIẾN THẮNG KHẢI HOÀN!" : "VƯƠNG QUỐC THẤT THỦ!";
            titleText.color = isVictory ? new Color(1f, 0.84f, 0.15f, 1f) : new Color(0.98f, 0.32f, 0.32f, 1f);

            // 4. Phụ đề mô tả (Subtitle)
            GameObject subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(panel.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0f, 115f);
            subRect.sizeDelta = new Vector2(500f, 35f);

            Text subText = subObj.AddComponent<Text>();
            subText.font = defaultFont;
            subText.fontSize = 15;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.text = isVictory 
                ? "Chúa tể Orcus và quân đoàn bóng đêm đã bị tiêu diệt hoàn toàn!" 
                : "Thành lũy đã sụp đổ! Hãy củng cố lại chiến thuật phòng ngự!";
            subText.color = new Color(0.82f, 0.86f, 0.92f, 0.9f);

            // 5. Khu vực Đánh giá Sao (Stars Display Container - Cho Chiến thắng)
            if (isVictory)
            {
                CreateStarsDisplay(panel.transform, stars, defaultFont);
            }

            // 6. Khu vực Thống kê Chiến trường (Battle Stats Recap)
            GameObject statsObj = new GameObject("StatsBox");
            statsObj.transform.SetParent(panel.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchoredPosition = new Vector2(0f, isVictory ? -25f : 15f);
            statsRect.sizeDelta = new Vector2(480f, 90f);

            Image statsBg = statsObj.AddComponent<Image>();
            statsBg.color = new Color(0.06f, 0.08f, 0.12f, 0.75f);

            int gold = GameHUD.Instance != null ? GameHUD.Instance.CurrentGold : 0;
            int wave = WaveSpawner.Instance != null ? WaveSpawner.Instance.CurrentWaveIndex : 10;
            int totalWave = WaveSpawner.Instance != null ? WaveSpawner.Instance.TotalWaves : 10;

            GameObject statsLabelObj = new GameObject("StatsText");
            statsLabelObj.transform.SetParent(statsObj.transform, false);
            RectTransform statsLabelRect = statsLabelObj.AddComponent<RectTransform>();
            statsLabelRect.anchorMin = Vector2.zero;
            statsLabelRect.anchorMax = Vector2.one;
            statsLabelRect.offsetMin = new Vector2(15f, 5f);
            statsLabelRect.offsetMax = new Vector2(-15f, -5f);

            Text statsText = statsLabelObj.AddComponent<Text>();
            statsText.font = defaultFont;
            statsText.fontSize = 16;
            statsText.lineSpacing = 1.3f;
            statsText.alignment = TextAnchor.MiddleCenter;
            statsText.color = Color.white;
            statsText.text = $"• Sinh mệnh bảo toàn: {remainingLives}/20 HP\n" +
                             $"• Ngân sách tích lũy: {gold} Vàng  |  Đợt quái: {wave}/{totalWave}";

            // 7. Hàng Nút Hành Động (Action Buttons: Chơi Lại / Về Menu)
            CreateActionButtons(panel.transform, isVictory, defaultFont);

            // Kích hoạt hiệu ứng bung nở hộp thoại (Punch Scale Tween) mượt mà chạy trên Time.unscaledDeltaTime
            StartCoroutine(AnimateModalPopRoutine(panel.transform));

            return overlay;
        }

        /// <summary>
        /// Tạo hiển thị 3 Ngôi sao vàng hoàng gia với hiệu ứng bung nở.
        /// </summary>
        private void CreateStarsDisplay(Transform panel, int stars, Font font)
        {
            GameObject starsRow = new GameObject("StarsRow");
            starsRow.transform.SetParent(panel, false);
            RectTransform rowRect = starsRow.AddComponent<RectTransform>();
            rowRect.anchoredPosition = new Vector2(0f, 50f);
            rowRect.sizeDelta = new Vector2(300f, 65f);

            List<Text> starTexts = new List<Text>();

            for (int i = 0; i < 3; i++)
            {
                GameObject starObj = new GameObject($"Star_{i + 1}");
                starObj.transform.SetParent(starsRow.transform, false);
                RectTransform starRect = starObj.AddComponent<RectTransform>();
                starRect.anchoredPosition = new Vector2((i - 1) * 75f, 0f);
                starRect.sizeDelta = new Vector2(65f, 65f);

                Text starTxt = starObj.AddComponent<Text>();
                starTxt.font = font;
                starTxt.fontSize = 50;
                starTxt.alignment = TextAnchor.MiddleCenter;
                starTxt.text = "★";

                bool isEarned = (i < stars);
                starTxt.color = isEarned ? new Color(1f, 0.85f, 0.20f, 1f) : new Color(0.25f, 0.28f, 0.35f, 0.6f);

                Outline stOutline = starObj.AddComponent<Outline>();
                stOutline.effectColor = isEarned ? new Color(0.7f, 0.45f, 0.05f, 1f) : Color.black;
                stOutline.effectDistance = new Vector2(2f, -2f);

                starTexts.Add(starTxt);
            }

            // Dòng chữ đánh giá thứ hạng
            GameObject rankObj = new GameObject("StarRankText");
            rankObj.transform.SetParent(panel, false);
            RectTransform rankRect = rankObj.AddComponent<RectTransform>();
            rankRect.anchoredPosition = new Vector2(0f, 10f);
            rankRect.sizeDelta = new Vector2(480f, 30f);

            Text rankText = rankObj.AddComponent<Text>();
            rankText.font = font;
            rankText.fontSize = 15;
            rankText.fontStyle = FontStyle.Bold;
            rankText.alignment = TextAnchor.MiddleCenter;

            if (stars == 3)
            {
                rankText.text = "ĐÁNH GIÁ: 3 / 3 SAO (CHIẾN THẮNG HOÀN HẢO - THÀNH NGUYÊN VẸN)";
                rankText.color = new Color(0.2f, 0.95f, 0.45f, 1f); // Xanh ngọc lục bảo danh dự
            }
            else if (stars == 2)
            {
                rankText.text = "ĐÁNH GIÁ: 2 / 3 SAO (CHIẾN THẮNG KIÊN CƯỜNG)";
                rankText.color = new Color(1f, 0.82f, 0.25f, 1f); // Vàng sáng
            }
            else
            {
                rankText.text = "ĐÁNH GIÁ: 1 / 3 SAO (CHIẾN THẮNG TRONG GIAN KHỔ)";
                rankText.color = new Color(0.95f, 0.65f, 0.25f, 1f); // Cam vàng
            }

            // Kích hoạt coroutine hiệu ứng bung nở ngôi sao theo thời gian thực (Unscaled Time)
            StartCoroutine(AnimateStarsPopRoutine(starsRow.transform, starTexts, stars));
        }

        /// <summary>
        /// Hoạt ảnh bung nở từng ngôi sao vàng (Pop Animation 1.4x -> 1.0x).
        /// </summary>
        private IEnumerator AnimateStarsPopRoutine(Transform starsRow, List<Text> starTexts, int stars)
        {
            for (int i = 0; i < starTexts.Count; i++)
            {
                starTexts[i].transform.localScale = Vector3.zero;
            }

            yield return new WaitForSecondsRealtime(0.15f);

            for (int i = 0; i < starTexts.Count; i++)
            {
                Transform sTransform = starTexts[i].transform;
                float duration = 0.20f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    // Hiệu ứng nảy overshoot
                    float scale = Mathf.Sin(t * Mathf.PI * 0.7f) * 1.35f;
                    sTransform.localScale = Vector3.one * scale;
                    yield return null;
                }

                sTransform.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(0.12f);
            }
        }

        /// <summary>
        /// Hiệu ứng bung nở nảy nhẹ (Punch Scale Tween) cho Dialog Modal Panel (0.85x -> 1.05x -> 1.0x).
        /// Hoạt động mượt mà trên Time.unscaledDeltaTime bất chấp Time.timeScale = 0.
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
        /// Tạo các nút Chơi Lại (Restart) và Về Menu.
        /// </summary>
        private void CreateActionButtons(Transform panel, bool isVictory, Font font)
        {
            // 1. Nút CHƠI LẠI (Restart Level)
            GameObject restartBtnObj = new GameObject("RestartButton");
            restartBtnObj.transform.SetParent(panel, false);
            RectTransform restartRect = restartBtnObj.AddComponent<RectTransform>();
            restartRect.anchoredPosition = new Vector2(-110f, -150f);
            restartRect.sizeDelta = new Vector2(200f, 48f);

            Image restartImg = restartBtnObj.AddComponent<Image>();
            restartImg.color = isVictory 
                ? new Color(0.12f, 0.58f, 0.32f, 1f)  // Xanh lục bảo thắng trận
                : new Color(0.85f, 0.28f, 0.20f, 1f); // Đỏ thẫm quyết chiến

            Button restartBtn = restartBtnObj.AddComponent<Button>();
            restartBtn.targetGraphic = restartImg;
            restartBtn.onClick.AddListener(RestartLevel);

            GameObject restartTextObj = new GameObject("Label");
            restartTextObj.transform.SetParent(restartBtnObj.transform, false);
            RectTransform rTextRect = restartTextObj.AddComponent<RectTransform>();
            rTextRect.anchorMin = Vector2.zero;
            rTextRect.anchorMax = Vector2.one;
            rTextRect.sizeDelta = Vector2.zero;

            Text rText = restartTextObj.AddComponent<Text>();
            rText.font = font;
            rText.fontSize = 18;
            rText.fontStyle = FontStyle.Bold;
            rText.alignment = TextAnchor.MiddleCenter;
            rText.color = Color.white;
            rText.text = isVictory ? "CHƠI LẠI TRẬN" : "TÁI CHIẾN (RETRY)";

            // 2. Nút VỀ MENU / THOÁT
            GameObject menuBtnObj = new GameObject("MenuButton");
            menuBtnObj.transform.SetParent(panel, false);
            RectTransform menuRect = menuBtnObj.AddComponent<RectTransform>();
            menuRect.anchoredPosition = new Vector2(110f, -150f);
            menuRect.sizeDelta = new Vector2(200f, 48f);

            Image menuImg = menuBtnObj.AddComponent<Image>();
            menuImg.color = new Color(0.22f, 0.26f, 0.34f, 1f); // Xám đá phiến sang trọng

            Button menuBtn = menuBtnObj.AddComponent<Button>();
            menuBtn.targetGraphic = menuImg;
            menuBtn.onClick.AddListener(RestartLevel); // Tải lại màn chơi

            GameObject menuTextObj = new GameObject("Label");
            menuTextObj.transform.SetParent(menuBtnObj.transform, false);
            RectTransform mTextRect = menuTextObj.AddComponent<RectTransform>();
            mTextRect.anchorMin = Vector2.zero;
            mTextRect.anchorMax = Vector2.one;
            mTextRect.sizeDelta = Vector2.zero;

            Text mText = menuTextObj.AddComponent<Text>();
            mText.font = font;
            mText.fontSize = 18;
            mText.fontStyle = FontStyle.Bold;
            mText.alignment = TextAnchor.MiddleCenter;
            mText.color = new Color(0.9f, 0.92f, 0.95f, 1f);
            mText.text = "THOÁT RA MENU";
        }

        /// <summary>
        /// Khởi động lại màn chơi hiện tại một cách an toàn:
        /// - Đặt lại Time.timeScale = 1f
        /// - Dọn sạch danh sách quái tĩnh Enemy.ActiveEnemies
        /// - Tải lại Scene sạch sẽ qua SceneManager
        /// </summary>
        public void RestartLevel()
        {
            Debug.Log("[GameManager] Đang tải lại màn chơi (Restart Level)...");

            Time.timeScale = 1f;

            if (Enemy.Enemy.ActiveEnemies != null)
            {
                Enemy.Enemy.ActiveEnemies.Clear();
            }

            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex);
        }

        /// <summary>
        /// Đảm bảo EventSystem luôn tồn tại để các nút nhận được click chuột.
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
                Debug.Log("[GameManager] Tự động tạo EventSystem cho Modal UI!");
            }
        }
    }
}
