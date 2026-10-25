using NUnit.Framework;
using UnityEngine;
using KingdomDefense.UI;

namespace KingdomDefense.Tests
{
    public class GameHUDTests
    {
        private GameObject _hudObj;
        private GameHUD _hud;

        [SetUp]
        public void SetUp()
        {
            _hudObj = new GameObject("Test_GameHUD");
            _hud = _hudObj.AddComponent<GameHUD>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_hudObj != null)
            {
                Object.DestroyImmediate(_hudObj);
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
    }
}
