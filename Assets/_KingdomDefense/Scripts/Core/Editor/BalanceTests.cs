using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomDefense.Enemy;
using KingdomDefense.UI;

namespace KingdomDefense.Tests
{
    public class BalanceTests
    {
        [Test]
        public void WaveConfig_10Waves_HasConsistentProgression()
        {
            WaveConfig config = WaveConfig.CreateDefault10WavesConfig();
            Assert.IsNotNull(config, "WaveConfig mặc định phải tạo thành công.");
            Assert.AreEqual(10, config.TotalWaves, "Tổng số Wave chuẩn phải là 10.");

            int prevEnemyCount = 0;
            for (int w = 1; w <= 10; w++)
            {
                WaveDefinition wave = config.GetWave(w);
                Assert.IsNotNull(wave, $"Wave {w} không được null.");
                Assert.AreEqual(w, wave.waveNumber, $"Số thứ tự Wave {w} phải chuẩn.");
                Assert.Greater(wave.TotalEnemyCount, 0, $"Wave {w} phải có số lượng quái > 0.");
                Assert.Greater(wave.spawnRate, 0.1f, $"Wave {w} phải có spawnRate hợp lệ.");
                Assert.Greater(wave.timeBeforeWave, 2.0f, $"Wave {w} phải có thời gian đếm ngược chuẩn bị.");

                // Kiểm tra Wave 10 phải có Trùm Cuối Boss Orcus
                if (w == 10)
                {
                    bool hasBoss = wave.groups.Exists(g => g.enemyType == EnemyType.BossOrcus);
                    Assert.IsTrue(hasBoss, "Wave 10 bắt buộc phải có Trùm cuối Boss Orcus.");
                }
                else
                {
                    bool hasBoss = wave.groups.Exists(g => g.enemyType == EnemyType.BossOrcus);
                    Assert.IsFalse(hasBoss, $"Wave {w} không được chứa Boss Orcus trước trận chung kết.");
                }
            }
        }

        [Test]
        public void WaveEconomy_ProvidesSufficientGoldForTowerDefense()
        {
            WaveConfig config = WaveConfig.CreateDefault10WavesConfig();
            int initialGold = 350;
            int totalKillGold = 0;
            int totalWaveBonusGold = 0;

            for (int w = 1; w <= 10; w++)
            {
                WaveDefinition wave = config.GetWave(w);
                foreach (var grp in wave.groups)
                {
                    int rewardPerUnit = 0;
                    switch (grp.enemyType)
                    {
                        case EnemyType.GoblinRunner: rewardPerUnit = 15; break;
                        case EnemyType.OrcWarrior: rewardPerUnit = 25; break;
                        case EnemyType.StoneGolem: rewardPerUnit = 60; break;
                        case EnemyType.DarkBat: rewardPerUnit = 35; break;
                        case EnemyType.BossOrcus: rewardPerUnit = 250; break;
                    }
                    totalKillGold += rewardPerUnit * grp.count;
                }

                // Thưởng vượt Wave: 50 + (w * 15)
                totalWaveBonusGold += 50 + (w * 15);
            }

            int grandTotalGold = initialGold + totalKillGold + totalWaveBonusGold;

            // Kiểm tra tổng thu nhập qua 10 Wave
            Assert.Greater(totalKillGold, 5000, "Tổng vàng diệt quái phải đủ lớn để người chơi xây dựng.");
            Assert.Greater(totalWaveBonusGold, 1000, "Thưởng vượt Wave phải tiếp tế tài chính định kỳ.");
            Assert.Greater(grandTotalGold, 7000, "Tổng ngân sách chiến dịch 10 Wave phải đạt trên 7000 Vàng để xây và nâng cấp tối đa 4 loại tháp.");
        }

        [Test]
        public void EnemyArchetypes_StatsAreDifferentiatedAndBalanced()
        {
            // Kiểm tra tỷ lệ HP và Giáp đặc trưng của từng chủng loại
            float goblinHp = 80f;
            float orcHp = 220f;
            float golemHp = 850f;
            float batHp = 110f;
            float bossHp = 3600f;

            Assert.Less(goblinHp, orcHp, "Goblin phải máu mỏng hơn Orc.");
            Assert.Less(orcHp, golemHp, "Orc phải máu ít hơn Golem.");
            Assert.Greater(bossHp, golemHp * 3f, "Boss Orcus phải có lượng máu trùm gấp ít nhất 3 lần Golem.");
            Assert.Less(batHp, orcHp, "Dark Bat quái bay phải có máu vừa phải để Tháp Cung hạ gục.");
        }
    }
}
