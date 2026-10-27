using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomDefense.Allies;
using KingdomDefense.Core;
using KingdomDefense.Enemy;

namespace KingdomDefense.Tests
{
    public class CastleKnightTests
    {
        private GameObject _knightObj;
        private CastleKnight _knight;
        private GameObject _enemyObj;
        private Enemy.Enemy _enemy;
        private EnemyMovement _movement;

        [SetUp]
        public void Setup()
        {
            _knightObj = new GameObject("TestKnight");
            _knight = _knightObj.AddComponent<CastleKnight>();

            _enemyObj = new GameObject("TestEnemy");
            _movement = _enemyObj.AddComponent<EnemyMovement>();
            _enemy = _enemyObj.AddComponent<Enemy.Enemy>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_knightObj != null) Object.DestroyImmediate(_knightObj);
            if (_enemyObj != null) Object.DestroyImmediate(_enemyObj);
            CastleKnight.ActiveKnights.Clear();
        }

        [Test]
        public void CastleKnight_Initialization_SetsStatsAndHealthCorrectly()
        {
            Assert.AreEqual(250f, _knight.MaxHealth, "Máu tối đa của Hiệp sĩ hoàng gia phải là 250 HP.");
            Assert.AreEqual(250f, _knight.CurrentHealth, "Máu khởi đầu phải đầy bình 250 HP.");
            Assert.AreEqual(0.30f, _knight.Armor, 0.001f, "Giáp vật lý của Hiệp sĩ phải là 30%.");
            Assert.AreEqual(20f, _knight.AttackDamage, "Sát thương chém cận chiến của Hiệp sĩ phải là 20 HP.");
            Assert.IsFalse(_knight.IsDead, "Hiệp sĩ vừa xuất trận không được ở trạng thái chết.");
        }

        [Test]
        public void CastleKnight_TakeDamage_AppliesArmorReduction()
        {
            // Nhận 100 sát thương vật lý -> Giáp 30% giảm còn 70 sát thương nhận vào
            _knight.TakeDamage(100f, DamageType.Physical);

            float expectedHp = 250f - (100f * (1f - 0.30f));
            Assert.AreEqual(expectedHp, _knight.CurrentHealth, 0.01f, "Giáp 30% phải giảm trừ 30 sát thương từ đòn 100 vật lý.");
        }

        [Test]
        public void CastleKnight_InterceptsGroundEnemy_BlocksMovement()
        {
            // Đặt quái vật trong tầm chặn đầu cận chiến (1.0m)
            _knightObj.transform.position = Vector3.zero;
            _enemyObj.transform.position = new Vector3(0f, 0f, 1.0f);

            // Giả lập Update chu kỳ chiến thuật
            var updateMethod = typeof(CastleKnight).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(updateMethod, "Hàm Update của CastleKnight phải tồn tại.");
            updateMethod.Invoke(_knight, null);

            Assert.IsTrue(_movement.IsBlocked, "Quái vật đi bộ khi tiếp cận Hiệp sĩ phải bị chặn đứng (IsBlocked = true).");
            Assert.AreEqual(_enemy, _knight.TargetEnemy, "Hiệp sĩ phải khóa mục tiêu quái vật đi bộ.");
        }

        [Test]
        public void CastleKnight_DoesNotInterceptFlyingEnemy()
        {
            // Thiết lập quái bay (DarkBat)
            _enemy.SetFlying(true, 2.8f);
            _knightObj.transform.position = Vector3.zero;
            _enemyObj.transform.position = new Vector3(0f, 0f, 1.0f);

            var updateMethod = typeof(CastleKnight).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(_knight, null);

            Assert.IsFalse(_movement.IsBlocked, "Quái bay trên không không được bị chặn bởi Hiệp sĩ cận chiến.");
            Assert.IsNull(_knight.TargetEnemy, "Hiệp sĩ không được khóa mục tiêu quái bay.");
        }

        [Test]
        public void CastleKnight_OnDeath_ReleasesBlockedEnemy()
        {
            _knightObj.transform.position = Vector3.zero;
            _enemyObj.transform.position = new Vector3(0f, 0f, 1.0f);

            var updateMethod = typeof(CastleKnight).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(_knight, null);
            Assert.IsTrue(_movement.IsBlocked);

            // Hiệp sĩ nhận sát thương chí mạng và hy sinh
            _knight.TakeDamage(1000f, DamageType.Pure);

            Assert.IsTrue(_knight.IsDead, "Hiệp sĩ phải ở trạng thái đã hy sinh.");
            Assert.IsFalse(_movement.IsBlocked, "Khi hiệp sĩ hy sinh, quái vật phải được giải phóng tiếp tục tiến về cổng thành.");
        }

        [Test]
        public void EnemyMovement_CheckKnightBlockade_DetectsAndStopsEnemy()
        {
            _knightObj.transform.position = Vector3.zero;
            _enemyObj.transform.position = new Vector3(0f, 0f, 1.8f);

            if (!CastleKnight.ActiveKnights.Contains(_knight))
            {
                CastleKnight.ActiveKnights.Add(_knight);
            }

            CastleKnight detectedKnight = _movement.CheckKnightBlockade();
            Assert.IsNotNull(detectedKnight, "CheckKnightBlockade() phải phát hiện hiệp sĩ trong bán kính 2.2m.");
            Assert.AreEqual(_knight, detectedKnight);

            // Giả lập Update quái vật đi bộ
            var updateEnemyMethod = typeof(EnemyMovement).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateEnemyMethod.Invoke(_movement, null);

            Assert.IsTrue(_movement.IsBlocked, "Quái vật đi bộ khi gặp hiệp sĩ phải tự động đặt IsBlocked = true và dừng lại.");
        }

        [Test]
        public void HealthBars_CreatedForBothKnightAndEnemy_WithValidMeshesAndMaterials()
        {
            // Kiểm tra thanh máu của Hiệp sĩ
            Transform knightBar = _knightObj.transform.Find("KnightHealthBar");
            Assert.IsNotNull(knightBar, "Hiệp sĩ phải có container 'KnightHealthBar'.");
            Assert.IsNotNull(knightBar.Find("Background"), "Thanh máu hiệp sĩ phải có Background cube.");
            Assert.IsNotNull(knightBar.Find("Fill"), "Thanh máu hiệp sĩ phải có Fill cube.");

            // Kiểm tra thanh máu của Quái vật
            _enemy.EnsureHealthBarExists();
            Transform enemyBar = _enemyObj.transform.Find("HealthBar");
            Assert.IsNotNull(enemyBar, "Quái vật phải có container 'HealthBar'.");
            Assert.IsNotNull(enemyBar.Find("Background"), "Thanh máu quái vật phải có Background cube.");
            Assert.IsNotNull(enemyBar.Find("Fill"), "Thanh máu quái vật phải có Fill cube.");
        }

        [Test]
        public void MutualBlockade_GroundEnemyAndKnight_StopEachOtherUntilOneIsEliminated()
        {
            _knightObj.transform.position = Vector3.zero;
            _enemyObj.transform.position = new Vector3(0f, 0f, 1.5f);

            if (!CastleKnight.ActiveKnights.Contains(_knight))
            {
                CastleKnight.ActiveKnights.Add(_knight);
            }

            // Cả 2 cùng cập nhật
            var knightUpdate = typeof(CastleKnight).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var enemyUpdate = typeof(EnemyMovement).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            knightUpdate.Invoke(_knight, null);
            enemyUpdate.Invoke(_movement, null);

            // 1. Cả 2 bên đều bị chặn: quái vật không thể đi qua lính, lính không đi qua quái vật
            Assert.IsTrue(_movement.IsBlocked, "Quái vật bị chặn lại không thể đi qua hiệp sĩ.");
            Assert.AreEqual(_enemy, _knight.TargetEnemy, "Hiệp sĩ khóa chặt mục tiêu quái vật đối đầu và dừng hành quân.");

            // 2. Tiêu diệt quái vật: Quái vật chết -> Hiệp sĩ giải phóng mục tiêu
            _enemy.TakeDamage(1000f, DamageType.Pure);
            Assert.IsTrue(_enemy.IsDead, "Quái vật phải bị tiêu diệt.");

            knightUpdate.Invoke(_knight, null);
            Assert.IsNull(_knight.TargetEnemy, "Sau khi quái vật bị tiêu diệt, hiệp sĩ không còn bị khóa mục tiêu và tiếp tục tiến quân.");
        }

        [Test]
        public void MapRoadConfiguration_TowerNodes_AreSafeDistanceBufferFromSpawn()
        {
            // Điểm xuất quái Cổng Bắc
            Vector3 spawnPoint = new Vector3(-1.5f, 0.02f, 13.5f);

            // 12 vị trí ô cắm tháp mới (đặt trên thảm cỏ, cách mép đường > 0.4m, cách spawn >= 7.5m)
            Vector3[] nodePositions = new Vector3[]
            {
                new Vector3(-2.00f, 0.03f, 5.90f),
                new Vector3(5.50f, 0.03f, 10.70f),
                new Vector3(8.60f, 0.03f, 8.30f),
                new Vector3(10.60f, 0.03f, 4.80f),
                new Vector3(2.80f, 0.03f, 3.80f),
                new Vector3(10.10f, 0.03f, 1.00f),
                new Vector3(7.80f, 0.03f, -2.00f),
                new Vector3(-0.75f, 0.03f, 2.30f),
                new Vector3(-4.50f, 0.03f, 2.05f),
                new Vector3(-7.25f, 0.03f, -0.45f),
                new Vector3(-6.75f, 0.03f, -4.20f),
                new Vector3(4.60f, 0.03f, -4.00f)
            };

            for (int i = 0; i < nodePositions.Length; i++)
            {
                float dist = Vector3.Distance(spawnPoint, nodePositions[i]);
                Assert.Greater(dist, 7.45f, $"Ô cắm tháp thứ {i + 1} ({nodePositions[i]}) phải cách điểm xuất quái tối thiểu 7.5m (Thực tế: {dist:F2}m).");
                Assert.Greater(nodePositions[i].z, -7.0f, $"Ô cắm tháp thứ {i + 1} không được đè vào tường thành phía Nam tại Z = -7.5m.");
            }
        }

        [Test]
        public void TowerNodes_DoNotOverlapOrTouch_MinimumPairwiseDistance()
        {
            // 12 vị trí ô cắm tháp
            Vector3[] nodePositions = new Vector3[]
            {
                new Vector3(-2.00f, 0.03f, 5.90f),
                new Vector3(5.50f, 0.03f, 10.70f),
                new Vector3(8.60f, 0.03f, 8.30f),
                new Vector3(10.60f, 0.03f, 4.80f),
                new Vector3(2.80f, 0.03f, 3.80f),
                new Vector3(10.10f, 0.03f, 1.00f),
                new Vector3(7.80f, 0.03f, -2.00f),
                new Vector3(-0.75f, 0.03f, 2.30f),
                new Vector3(-4.50f, 0.03f, 2.05f),
                new Vector3(-7.25f, 0.03f, -0.45f),
                new Vector3(-6.75f, 0.03f, -4.20f),
                new Vector3(4.60f, 0.03f, -4.00f)
            };

            // Đường kính chân đế mỗi tháp là 2.1m (bán kính 1.05m).
            // Khoảng cách giữa 2 ô bất kỳ phải >= 3.70m để đảm bảo khoảng hở cỏ tự nhiên giữa 2 chân đế >= 1.6m (hoàn toàn không dính nhau).
            float minSeparation = 3.70f;

            for (int i = 0; i < nodePositions.Length; i++)
            {
                for (int j = i + 1; j < nodePositions.Length; j++)
                {
                    float dist = Vector2.Distance(new Vector2(nodePositions[i].x, nodePositions[i].z), new Vector2(nodePositions[j].x, nodePositions[j].z));
                    Assert.GreaterOrEqual(dist, minSeparation, $"Ô tháp N{i + 1} ({nodePositions[i]}) và N{j + 1} ({nodePositions[j]}) quá gần nhau ({dist:F2}m < {minSeparation:F2}m). Các ô tháp không được dính vào nhau!");
                }
            }
        }
    }
}
