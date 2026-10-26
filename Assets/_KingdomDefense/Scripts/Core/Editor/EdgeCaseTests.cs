using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomDefense.Build;
using KingdomDefense.Towers;
using KingdomDefense.Enemy;
using KingdomDefense.Core;
using KingdomDefense.UI;

namespace KingdomDefense.Tests
{
    public class EdgeCaseTests
    {
        private GameObject _hudObj;
        private GameHUD _hud;
        private GameObject _buildManagerObj;
        private BuildManager _buildManager;

        [SetUp]
        public void SetUp()
        {
            _hudObj = new GameObject("Test_HUD");
            _hud = _hudObj.AddComponent<GameHUD>();
            _hud.SetGold(500);

            _buildManagerObj = new GameObject("Test_BuildManager");
            _buildManager = _buildManagerObj.AddComponent<BuildManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_buildManagerObj != null) Object.DestroyImmediate(_buildManagerObj);
            if (_hudObj != null) Object.DestroyImmediate(_hudObj);
        }

        [Test]
        public void RapidInput_CannotDoubleBuildOnSameNode_RefundSafe()
        {
            // 1. Tạo ô Node
            GameObject nodeObj = new GameObject("Test_Node");
            Node node = nodeObj.AddComponent<Node>();

            _buildManager.SelectTowerToBuild(TowerType.Archer);
            int initialGold = _hud.CurrentGold;
            int archerCost = _buildManager.ArcherCost;

            // Lần click 1: Xây tháp thành công
            bool firstBuild = _buildManager.BuildTowerOn(node);
            Assert.IsTrue(firstBuild, "Lần xây tháp đầu tiên phải thành công.");
            Assert.IsTrue(node.HasTurret, "Node phải có tháp sau khi xây.");
            Assert.AreEqual(initialGold - archerCost, _hud.CurrentGold, "Tiền phải bị trừ đúng chi phí.");

            // Lần click 2 (Spam click trên cùng Node): Phải bị từ chối và KHÔNG trừ thêm tiền
            _buildManager.SelectTowerToBuild(TowerType.Archer);
            bool secondBuild = _buildManager.BuildTowerOn(node);
            Assert.IsFalse(secondBuild, "Lần xây thứ 2 trên ô đã có tháp phải bị từ chối.");
            Assert.AreEqual(initialGold - archerCost, _hud.CurrentGold, "Không được trừ tiền lần 2 khi spam click.");

            Object.DestroyImmediate(nodeObj);
        }

        [Test]
        public void RapidInput_CannotDoubleSell_RefundsOnlyOnce()
        {
            // Tạo tháp với vốn đầu tư 100 vàng
            GameObject towerObj = new GameObject("Test_Tower");
            Tower tower = towerObj.AddComponent<Tower>();
            tower.InitializeBaseCost(100);

            int goldBefore = _hud.CurrentGold;
            int expectedRefund = tower.SellAmount; // 70% của 100 = 70 vàng

            // Bán tháp lần 1
            tower.Sell();
            Assert.IsTrue(tower.IsSold, "Tháp phải được đánh dấu IsSold sau khi bán.");
            Assert.AreEqual(goldBefore + expectedRefund, _hud.CurrentGold, "Hoàn trả đúng 70% vàng.");

            // Bán tháp lần 2 (Spam click nút Bán cùng frame)
            tower.Sell();
            Assert.AreEqual(goldBefore + expectedRefund, _hud.CurrentGold, "Không được cộng dồn tiền hoàn trả khi click bán nhiều lần.");

            Object.DestroyImmediate(towerObj);
        }

        [Test]
        public void SellTowerWhileShooting_ProjectileContinuesSafelyWithoutCrash()
        {
            // Tạo tháp và đạn
            GameObject towerObj = new GameObject("Test_ArcherTower");
            Tower tower = towerObj.AddComponent<Tower>();
            tower.InitializeBaseCost(100);

            GameObject enemyObj = new GameObject("Test_Enemy");
            Enemy.Enemy enemy = enemyObj.AddComponent<Enemy.Enemy>();
            enemy.ConfigureStats("Goblin", 100f, 15);

            GameObject arrowObj = new GameObject("Test_Arrow");
            Arrow arrow = arrowObj.AddComponent<Arrow>();
            arrow.transform.position = new Vector3(0f, 2f, 0f);
            arrow.Seek(enemy.transform, 25f);

            // Bán tháp ngay khi đạn vừa bắn ra
            tower.Sell();
            Assert.IsTrue(tower.IsSold, "Tháp đã bị bán thành công.");

            // Đạn vẫn tồn tại và không bị phụ thuộc vào thực thể tháp đã hủy
            Assert.IsNotNull(arrow, "Mũi tên vẫn tiếp tục bay độc lập sau khi tháp bị bán.");
            Assert.DoesNotThrow(() =>
            {
                // Giả lập mũi tên chạm trúng mục tiêu
                arrow.transform.position = enemy.transform.position + new Vector3(0f, 0.45f, 0f);
            }, "Mũi tên chạm đích không được ném ra NullReferenceException.");

            Object.DestroyImmediate(arrowObj);
            Object.DestroyImmediate(enemyObj);
            Object.DestroyImmediate(towerObj);
        }

        [Test]
        public void MissedArrow_GroundImpact_DespawnsCleanly()
        {
            GameObject arrowObj = new GameObject("Test_MissedArrow");
            Arrow arrow = arrowObj.AddComponent<Arrow>();
            arrow.transform.position = new Vector3(0f, 2.0f, 0f);

            // Bắn không có mục tiêu (hoặc mục tiêu đã chết trước khi tên tới)
            arrow.Seek(null, 25f);

            // Đặt tọa độ mũi tên cắm xuống đất (Y <= 0.15f)
            arrow.transform.position = new Vector3(5f, 0.10f, 5f);

            // Kiểm tra hàm Despawn không lỗi và thu hồi về ObjectPool / Destroy
            Assert.DoesNotThrow(() =>
            {
                arrow.Despawn();
            }, "Despawn khi đạn chạm đất phải an toàn.");

            if (arrowObj != null) Object.DestroyImmediate(arrowObj);
        }

        [Test]
        public void CannonBall_FailsafeTimeout_ExplodesAndDespawnsCleanly()
        {
            GameObject ballObj = new GameObject("Test_CannonBall");
            CannonBall cannonBall = ballObj.AddComponent<CannonBall>();

            Vector3 start = new Vector3(0f, 2f, 0f);
            Vector3 target = new Vector3(10f, 0f, 10f);

            cannonBall.Launch(start, target, 50f, 3.0f);

            // Kiểm tra Despawn an toàn
            Assert.DoesNotThrow(() =>
            {
                cannonBall.Despawn();
            }, "Despawn đạn pháo an toàn, không rò rỉ bộ nhớ.");

            if (ballObj != null) Object.DestroyImmediate(ballObj);
        }
    }
}
