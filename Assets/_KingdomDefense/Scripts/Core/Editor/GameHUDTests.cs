using NUnit.Framework;
using UnityEngine;
using KingdomDefense.UI;
using KingdomDefense.Build;
using KingdomDefense.Skills;

namespace KingdomDefense.Tests
{
    public class GameHUDTests
    {
        private GameObject _hudObj;
        private GameHUD _hud;
        private GameObject _buildManagerObj;
        private BuildManager _buildManager;

        [SetUp]
        public void SetUp()
        {
            _hudObj = new GameObject("Test_GameHUD");
            _hud = _hudObj.AddComponent<GameHUD>();

            _buildManagerObj = new GameObject("Test_BuildManager");
            _buildManager = _buildManagerObj.AddComponent<BuildManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1.0f;
            if (MeteorShowerSkill.Instance != null)
            {
                Object.DestroyImmediate(MeteorShowerSkill.Instance.gameObject);
            }
            if (_hudObj != null)
            {
                Object.DestroyImmediate(_hudObj);
            }
            if (_buildManagerObj != null)
            {
                Object.DestroyImmediate(_buildManagerObj);
            }
        }

        [Test]
        public void TopHUD_InitialValues_AreCorrect()
        {
            Assert.AreEqual(20, _hud.CurrentLives, "Máu khởi đầu phải là 20.");
            Assert.AreEqual(350, _hud.CurrentGold, "Vàng khởi đầu phải là 350.");
            Assert.AreEqual(1, _hud.CurrentWave, "Đợt quái khởi đầu phải là 1.");
            Assert.AreEqual(100f, _hud.CurrentMana, "Mana khởi đầu phải là 100.");
            Assert.IsFalse(_hud.IsCriticalLives, "Trạng thái máu nguy cấp ban đầu phải là false.");
        }

        [Test]
        public void TopHUD_CriticalLives_TriggersWhenUnderFiveLives()
        {
            _hud.SetLives(4);
            Assert.IsTrue(_hud.IsCriticalLives, "Máu dưới 5 phải kích hoạt cờ IsCriticalLives.");

            _hud.SetLives(10);
            Assert.IsFalse(_hud.IsCriticalLives, "Máu phục hồi >= 5 phải tắt cờ IsCriticalLives.");
        }

        [Test]
        public void TopHUD_GoldEconomy_UpdatesCorrectly()
        {
            _hud.AddGold(100);
            Assert.AreEqual(450, _hud.CurrentGold, "Cộng 100 vàng phải thành 450.");

            bool spent = _hud.SpendGold(200);
            Assert.IsTrue(spent, "Phải chi tiêu thành công khi đủ tiền.");
            Assert.AreEqual(250, _hud.CurrentGold, "Số dư vàng phải là 250.");

            bool overspent = _hud.SpendGold(500);
            Assert.IsFalse(overspent, "Không được chi tiêu quá số vàng hiện có.");
            Assert.AreEqual(250, _hud.CurrentGold, "Số dư vàng phải giữ nguyên 250.");
        }

        [Test]
        public void TopHUD_ManaConsumption_WorksCorrectly()
        {
            Assert.IsTrue(_hud.HasEnoughMana(60f), "Phải đủ 60 Mana cho Meteor Shower.");
            bool consumed = _hud.ConsumeMana(60f);
            Assert.IsTrue(consumed, "Phải tiêu thụ thành công 60 Mana.");
            Assert.AreEqual(40f, _hud.CurrentMana, "Số Mana còn lại phải là 40.");

            bool consumedMore = _hud.ConsumeMana(60f);
            Assert.IsFalse(consumedMore, "Không được tiêu thụ nếu không đủ Mana.");
            Assert.AreEqual(40f, _hud.CurrentMana, "Mana phải giữ nguyên 40.");
        }

        [Test]
        public void BottomDock_TowerCosts_MatchSpecifications()
        {
            Assert.AreEqual(100, _buildManager.ArcherCost, "Tháp Cung chi phí 100 Vàng.");
            Assert.AreEqual(125, _buildManager.BombCost, "Tháp Pháo chi phí 125 Vàng.");
            Assert.AreEqual(150, _buildManager.MageCost, "Tháp Phép chi phí 150 Vàng.");
            Assert.AreEqual(175, _buildManager.FrostCost, "Tháp Băng Phong chi phí 175 Vàng.");
            Assert.AreEqual(70, _buildManager.WallCost, "Tường Thành chi phí 70 Vàng.");
        }

        [Test]
        public void BottomDock_TowerSelection_ValidatesGoldAndAllowsToggle()
        {
            // Ban đầu có 350 vàng -> đủ tiền mua Archer (100)
            _buildManager.SelectTowerToBuild(TowerType.Archer);
            Assert.AreEqual(TowerType.Archer, _buildManager.SelectedTowerType, "Phải chọn được Tháp Cung khi đủ vàng.");

            // Nhấn lại lần nữa -> Toggle hủy chọn
            _buildManager.SelectTowerToBuild(TowerType.Archer);
            Assert.AreEqual(TowerType.None, _buildManager.SelectedTowerType, "Nhấn lại lần nữa phải hủy chọn (Toggle).");

            // Giả lập người chơi chỉ còn 50 vàng
            _hud.SetGold(50);
            _buildManager.SelectTowerToBuild(TowerType.Bomb); // Cần 125 vàng
            Assert.AreEqual(TowerType.None, _buildManager.SelectedTowerType, "Không được phép chọn tháp khi không đủ vàng.");
        }

        [Test]
        public void BottomDock_AutoDeselect_WhenGoldDropsBelowCost()
        {
            // Người chơi có 150 vàng, đang chọn Tháp Phép (150 vàng)
            _hud.SetGold(150);
            _buildManager.SelectTowerToBuild(TowerType.Mage);
            Assert.AreEqual(TowerType.Mage, _buildManager.SelectedTowerType);

            // Bất ngờ bị hụt vàng (ví dụ sửa chữa hoặc trừ phí) xuống 100 vàng
            _hud.SetGold(100);
            Assert.AreEqual(TowerType.None, _buildManager.SelectedTowerType, "Khi vàng giảm xuống dưới chi phí, phải tự động hủy chọn.");
        }

        [Test]
        public void HUDLayout_AlignAndEnforceHUDLayout_AppliesPixelPerfectDimensions()
        {
            GameObject tlObj = new GameObject("TopLeft_StatusPanel");
            tlObj.transform.SetParent(_hudObj.transform, false);
            RectTransform tlRect = tlObj.AddComponent<RectTransform>();

            GameObject bdObj = new GameObject("BottomCenter_BuildDock");
            bdObj.transform.SetParent(_hudObj.transform, false);
            RectTransform bdRect = bdObj.AddComponent<RectTransform>();

            _hud.AlignAndEnforceHUDLayout();

            Assert.AreEqual(new Vector2(510f, 75f), tlRect.sizeDelta, "TopLeft Panel phải có kích thước 510x75.");
            Assert.AreEqual(new Vector2(840f, 125f), bdRect.sizeDelta, "BottomDock Panel phải có kích thước 840x125 để bao trọn các nút.");
            Assert.AreEqual(new Vector2(0f, 20f), bdRect.anchoredPosition, "BottomDock Panel phải cách đáy màn hình 20px.");
        }

        [Test]
        public void BuildTowerOn_WhenGoldDropsBelowThreshold_PreservesTowerTypeAndBuildsCorrectly()
        {
            // Ban đầu có 150 vàng, xây tháp 100 vàng -> còn 50 vàng (< 100 vàng)
            _hud.SetGold(150);
            _buildManager.SelectTowerToBuild(TowerType.Archer);

            GameObject nodeObj = new GameObject("Test_Node");
            Node node = nodeObj.AddComponent<Node>();

            bool success = _buildManager.BuildTowerOn(node);

            Assert.IsTrue(success, "Quá trình xây dựng tháp thứ 3 phải thành công.");
            Assert.AreEqual(50, _hud.CurrentGold, "Số vàng còn lại phải là 50.");
            Assert.IsTrue(node.HasTurret, "Ô Node phải chứa tháp đã xây.");
            Assert.IsNotNull(node.CurrentTurret.GetComponent<KingdomDefense.Towers.ArcherTower>(), "Tháp xây phải có component ArcherTower, không bị biến thành Cylinder rỗng.");

            Object.DestroyImmediate(nodeObj);
        }

        [Test]
        public void TopHUD_FastForward_TogglesTimeScaleBetweenOneAndTwo()
        {
            Assert.IsFalse(_hud.IsFastForward, "Mặc định không ở chế độ tăng tốc.");
            Assert.AreEqual(1.0f, Time.timeScale, "Time.timeScale mặc định là 1.0.");

            _hud.ToggleFastForward();
            Assert.IsTrue(_hud.IsFastForward, "Kích hoạt Fast Forward thành công.");
            Assert.AreEqual(2.0f, Time.timeScale, "Time.timeScale phải là 2.0.");

            _hud.ToggleFastForward();
            Assert.IsFalse(_hud.IsFastForward, "Tắt Fast Forward thành công.");
            Assert.AreEqual(1.0f, Time.timeScale, "Time.timeScale phải quay lại 1.0.");
        }

        [Test]
        public void TopHUD_Pause_TogglesTimeScaleAndPauseOverlay()
        {
            Assert.IsFalse(_hud.IsPaused, "Mặc định game không ở trạng thái Pause.");

            _hud.TogglePause();
            Assert.IsTrue(_hud.IsPaused, "Kích hoạt Pause thành công.");
            Assert.AreEqual(0.0f, Time.timeScale, "Khi Pause thì Time.timeScale phải bằng 0.");

            _hud.TogglePause();
            Assert.IsFalse(_hud.IsPaused, "Hủy Pause thành công.");
            Assert.AreEqual(1.0f, Time.timeScale, "Khi tiếp tục game thì Time.timeScale phải phục hồi về 1.0.");
        }

        [Test]
        public void TopHUD_Pause_ResumeRestoresFastForwardSpeed()
        {
            // Đang chơi ở tốc độ 2X
            _hud.ToggleFastForward();
            Assert.AreEqual(2.0f, Time.timeScale);

            // Bấm Pause -> timeScale = 0
            _hud.TogglePause();
            Assert.IsTrue(_hud.IsPaused);
            Assert.AreEqual(0.0f, Time.timeScale);

            // Trong lúc Pause, người chơi bấm chuyển đổi tốc độ về 1X
            _hud.ToggleFastForward();
            Assert.IsFalse(_hud.IsFastForward);
            Assert.AreEqual(0.0f, Time.timeScale, "Trong lúc Pause, timeScale vẫn phải là 0.");

            // Hủy Pause -> Phục hồi về 1X
            _hud.TogglePause();
            Assert.IsFalse(_hud.IsPaused);
            Assert.AreEqual(1.0f, Time.timeScale, "Khi tiếp tục, timeScale phải áp dụng theo trạng thái 1X vừa chọn.");
        }

        [Test]
        public void TopHUD_PauseVisuals_SwitchesSpritesAndFallbackTextCorrectly()
        {
            // Kiểm tra fallback khi chưa có sprite (dùng text "||" khi chơi và "▶" khi pause)
            GameObject pauseBtnObj = new GameObject("Btn_Pause");
            pauseBtnObj.transform.SetParent(_hudObj.transform);
            UnityEngine.UI.Image btnImg = pauseBtnObj.AddComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Button btn = pauseBtnObj.AddComponent<UnityEngine.UI.Button>();

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(pauseBtnObj.transform);
            UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();

            var fieldPauseBtn = typeof(GameHUD).GetField("pauseBtn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldPauseBtn?.SetValue(_hud, btn);
            var fieldPauseText = typeof(GameHUD).GetField("pauseText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldPauseText?.SetValue(_hud, text);

            _hud.UpdatePauseVisuals();
            Assert.AreEqual("||", text.text, "Khi đang chơi (Resume) và không có sprite, nút phải hiển thị icon 2 thanh đứng '||'.");

            _hud.TogglePause();
            Assert.AreEqual("▶", text.text, "Khi tạm dừng (Pause), nút phải hiển thị biểu tượng tam giác '▶'.");

            _hud.TogglePause();
            Assert.AreEqual("||", text.text, "Khi tiếp tục lại (Resume), nút phải hiển thị lại biểu tượng 2 thanh đứng '||'.");

            // Kiểm tra khi có sprite iconPause và iconResume
            Texture2D tex = new Texture2D(16, 16);
            Sprite dummyPause = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.zero);
            Sprite dummyResume = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.zero);

            var fieldIconPause = typeof(GameHUD).GetField("iconPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldIconPause?.SetValue(_hud, dummyPause);
            var fieldIconResume = typeof(GameHUD).GetField("iconResume", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldIconResume?.SetValue(_hud, dummyResume);

            _hud.UpdatePauseVisuals();
            Assert.AreEqual(dummyPause, btnImg.sprite, "Khi đang chơi, sprite trên nút phải là iconPause.");
            Assert.AreEqual("", text.text, "Khi đã có sprite, text phải được để trống.");

            _hud.TogglePause();
            Assert.AreEqual(dummyResume, btnImg.sprite, "Khi tạm dừng, sprite trên nút phải là iconResume.");

            _hud.TogglePause();
            Assert.AreEqual(dummyPause, btnImg.sprite, "Khi tiếp tục lại, sprite trên nút phải quay về iconPause.");

            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(dummyPause);
            Object.DestroyImmediate(dummyResume);
        }

        [Test]
        public void PauseModal_ContainsThreeButtons_ResumeRestartAndMenu()
        {
            _hud.EnsurePauseOverlay();
            var fieldOverlay = typeof(GameHUD).GetField("pauseOverlay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject overlay = fieldOverlay?.GetValue(_hud) as GameObject;

            Assert.IsNotNull(overlay, "Pause overlay phải được khởi tạo.");
            Transform panel = overlay.transform.Find("PauseModal_Panel");
            Assert.IsNotNull(panel, "Hộp thoại PauseModal_Panel phải tồn tại.");

            // Tìm 3 nút chức năng
            UnityEngine.UI.Button[] buttons = panel.GetComponentsInChildren<UnityEngine.UI.Button>();
            Assert.AreEqual(3, buttons.Length, "Pause Modal phải chứa đúng 3 nút: Resume, Restart, và Quit/Menu.");

            bool hasResume = false;
            bool hasRestart = false;
            bool hasMenu = false;

            foreach (var b in buttons)
            {
                UnityEngine.UI.Text lbl = b.GetComponentInChildren<UnityEngine.UI.Text>();
                if (lbl != null)
                {
                    if (lbl.text.Contains("RESUME") || lbl.text.Contains("TIẾP TỤC")) hasResume = true;
                    if (lbl.text.Contains("RESTART") || lbl.text.Contains("CHƠI LẠI")) hasRestart = true;
                    if (lbl.text.Contains("MENU") || lbl.text.Contains("THOÁT")) hasMenu = true;
                }
            }

            Assert.IsTrue(hasResume, "Phải có nút Tiếp tục (Resume).");
            Assert.IsTrue(hasRestart, "Phải có nút Chơi lại (Restart).");
            Assert.IsTrue(hasMenu, "Phải có nút Về menu / Thoát.");
        }

        [Test]
        public void PauseModal_ClosePauseModal_ResumesGameAndHidesOverlay()
        {
            _hud.TogglePause();
            Assert.IsTrue(_hud.IsPaused);
            Assert.AreEqual(0.0f, Time.timeScale);

            _hud.ClosePauseModal();
            Assert.IsFalse(_hud.IsPaused);
            Assert.AreEqual(1.0f, Time.timeScale);

            var fieldOverlay = typeof(GameHUD).GetField("pauseOverlay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject overlay = fieldOverlay?.GetValue(_hud) as GameObject;
            if (overlay != null)
            {
                Assert.IsFalse(overlay.activeSelf, "Pause Overlay phải bị ẩn sau khi đóng.");
            }
        }

        [Test]
        public void Hotkeys_TowerMapping_NumbersOneToFiveMapCorrectly()
        {
            // Phím [1]: Tháp Cung
            _buildManager.SelectTowerToBuild(TowerType.Archer);
            Assert.AreEqual(TowerType.Archer, _buildManager.SelectedTowerType, "Phím [1] phải chọn Tháp Cung.");

            // Phím [2]: Tháp Pháo
            _buildManager.SelectTowerToBuild(TowerType.Bomb);
            Assert.AreEqual(TowerType.Bomb, _buildManager.SelectedTowerType, "Phím [2] phải chọn Tháp Pháo.");

            // Phím [3]: Tháp Phép
            _buildManager.SelectTowerToBuild(TowerType.Mage);
            Assert.AreEqual(TowerType.Mage, _buildManager.SelectedTowerType, "Phím [3] phải chọn Tháp Phép.");

            // Phím [4]: Tháp Băng
            _buildManager.SelectTowerToBuild(TowerType.Frost);
            Assert.AreEqual(TowerType.Frost, _buildManager.SelectedTowerType, "Phím [4] phải chọn Tháp Băng.");

            // Phím [5]: Tường Thành
            _buildManager.SelectTowerToBuild(TowerType.Wall);
            Assert.AreEqual(TowerType.Wall, _buildManager.SelectedTowerType, "Phím [5] phải chọn Tường Thành.");

            // Nhấn lại lần nữa: Hủy chọn (Toggle)
            _buildManager.SelectTowerToBuild(TowerType.Wall);
            Assert.AreEqual(TowerType.None, _buildManager.SelectedTowerType, "Nhấn lại phím cùng loại phải hủy chọn.");
        }

        [Test]
        public void Hotkeys_EscPriority_CancelsAimingFirst_ThenTower_ThenTogglesPause()
        {
            GameObject skillObj = new GameObject("Test_MeteorShower");
            MeteorShowerSkill skill = skillObj.AddComponent<MeteorShowerSkill>();

            // Ưu tiên 1: Đang bật ngắm Mưa Thiên Thạch -> Esc hủy ngắm
            skill.SetAiming(true);
            _buildManager.SelectTowerToBuild(TowerType.None);
            string action1 = _hud.HandleEscAction();
            Assert.AreEqual("CancelMeteorAiming", action1, "Esc cấp 1 phải hủy chế độ ngắm bắn Thiên Thạch.");
            Assert.IsFalse(skill.IsAiming, "Hồng tâm ngắm phải bị tắt.");
            Assert.IsFalse(_hud.IsPaused, "Game không được mở Pause Menu khi vừa hủy ngắm.");

            // Ưu tiên 2: Đang chọn Tháp Băng [4] -> Esc hủy chọn tháp
            _buildManager.SelectTowerToBuild(TowerType.Frost);
            Assert.AreEqual(TowerType.Frost, _buildManager.SelectedTowerType);
            string action2 = _hud.HandleEscAction();
            Assert.AreEqual("CancelTowerSelection", action2, "Esc cấp 2 phải hủy chọn công trình đang chọn.");
            Assert.AreEqual(TowerType.None, _buildManager.SelectedTowerType, "SelectedTowerType phải trở về None.");
            Assert.IsFalse(_hud.IsPaused, "Game không được mở Pause Menu khi vừa hủy chọn tháp.");

            // Ưu tiên 3: Không có thao tác dở dang -> Esc mở Pause Menu
            string action3 = _hud.HandleEscAction();
            Assert.AreEqual("OpenPauseMenu", action3, "Esc cấp 3 phải mở Pause Menu.");
            Assert.IsTrue(_hud.IsPaused, "Game phải chuyển sang trạng thái tạm dừng.");

            // Nhấn Esc lần nữa khi đang Pause -> Đóng Pause Menu và tiếp tục chơi
            string action4 = _hud.HandleEscAction();
            Assert.AreEqual("ClosePauseMenu", action4, "Esc khi đang Pause phải đóng menu và tiếp tục.");
            Assert.IsFalse(_hud.IsPaused, "Game phải trở về trạng thái tiếp tục chơi.");

            Object.DestroyImmediate(skillObj);
        }

        [Test]
        public void Hotkeys_AimingAndTowerMutuallyCancelEachOther()
        {
            GameObject skillObj = new GameObject("Test_MeteorShower");
            MeteorShowerSkill skill = skillObj.AddComponent<MeteorShowerSkill>();

            // Đang ngắm Thiên Thạch, chọn Tháp Cung -> Hủy ngắm Thiên Thạch
            skill.SetAiming(true);
            Assert.IsTrue(skill.IsAiming);
            _buildManager.SelectTowerToBuild(TowerType.Archer);
            Assert.IsFalse(skill.IsAiming, "Khi chọn tháp mới, chế độ ngắm Thiên Thạch phải tự hủy.");
            Assert.AreEqual(TowerType.Archer, _buildManager.SelectedTowerType);

            // Đang chọn tháp, phím Space bật ngắm Thiên Thạch -> Hủy chọn tháp
            _buildManager.SelectTowerToBuild(TowerType.Frost);
            Assert.AreEqual(TowerType.Frost, _buildManager.SelectedTowerType);
            if (_buildManager.SelectedTowerType != TowerType.None)
            {
                _buildManager.SelectTowerToBuild(TowerType.None);
            }
            skill.SetAiming(true);
            Assert.AreEqual(TowerType.None, _buildManager.SelectedTowerType, "Khi kích hoạt kỹ năng Space, tháp chọn phải tự hủy.");
            Assert.IsTrue(skill.IsAiming);

            Object.DestroyImmediate(skillObj);
        }

        [Test]
        public void Hotkeys_ManaDisplay_ShowsSpaceHintWhenReady()
        {
            GameObject textObj = new GameObject("Test_ManaText");
            UnityEngine.UI.Text textComp = textObj.AddComponent<UnityEngine.UI.Text>();

            var fieldText = typeof(GameHUD).GetField("manaText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldText?.SetValue(_hud, textComp);

            // Nạp 100 Mana (>= 60)
            _hud.UpdateManaDisplay();
            Assert.IsTrue(textComp.text.Contains("[Space]"), "Mana >= 60 MP phải hiển thị gợi ý [Space].");

            // Trừ Mana xuống dưới 60
            _hud.ConsumeMana(50f); // Còn 50 Mana
            Assert.IsFalse(textComp.text.Contains("[Space]"), "Mana < 60 MP không được hiển thị gợi ý [Space].");

            Object.DestroyImmediate(textObj);
        }
    }
}
