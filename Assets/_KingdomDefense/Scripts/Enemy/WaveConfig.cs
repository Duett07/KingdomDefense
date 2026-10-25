using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomDefense.Enemy
{
    /// <summary>
    /// Chế độ thứ tự xuất hiện của các nhóm quái trong cùng một Wave.
    /// </summary>
    public enum SpawnOrderMode
    {
        Sequential,   // Tuần tự: Ra hết nhóm này rồi mới đến nhóm kế tiếp
        Interleaved   // Xen kẽ: Phân bổ đều các chủng loại quái theo tỷ lệ (vd: 3 Goblin xen 1 Orc)
    }

    /// <summary>
    /// Định nghĩa đơn vị quái được sinh ra kèm nhịp trễ sau khi sinh.
    /// </summary>
    public struct SpawnItem
    {
        public EnemyType type;
        public float delayAfterSpawn;

        public SpawnItem(EnemyType type, float delay)
        {
            this.type = type;
            this.delayAfterSpawn = delay;
        }
    }

    /// <summary>
    /// Định nghĩa một nhóm quái cùng loại trong đợt tấn công.
    /// </summary>
    [Serializable]
    public class WaveGroup
    {
        [Tooltip("Chủng loại quái vật xuất hiện")]
        public EnemyType enemyType = EnemyType.GoblinRunner;

        [Tooltip("Số lượng quái trong nhóm này")]
        [Min(1)]
        public int count = 10;

        [Tooltip("Nếu > 0, ghi đè nhịp ra quái riêng cho nhóm này. Nếu <= 0, dùng spawnRate chung của Wave.")]
        public float spawnIntervalOverride = 0f;

        public WaveGroup() { }

        public WaveGroup(EnemyType type, int count, float intervalOverride = 0f)
        {
            this.enemyType = type;
            this.count = count;
            this.spawnIntervalOverride = intervalOverride;
        }
    }

    /// <summary>
    /// Cấu hình chi tiết cho một Wave chiến đấu.
    /// </summary>
    [Serializable]
    public class WaveDefinition
    {
        [Header("--- THÔNG TIN LÀN SÓNG ---")]
        [Tooltip("Số thứ tự đợt tấn công (1 -> 10)")]
        public int waveNumber = 1;

        [Tooltip("Tên gọi hiển thị của đợt tấn công")]
        public string waveTitle = "Wave 1";

        [Tooltip("Mô tả chiến thuật và độ khó (theo Bảng 2.3)")]
        [TextArea(2, 4)]
        public string tacticsDescription = "";

        [Header("--- THỜI GIAN & NHỊP ĐỘ ---")]
        [Tooltip("Thời gian đếm ngược chuẩn bị trước khi Wave này bắt đầu (giây)")]
        [Min(1f)]
        public float timeBeforeWave = 8.0f;

        [Tooltip("Khoảng cách mặc định giữa mỗi lần xuất quái trong Wave này (giây)")]
        [Min(0.1f)]
        public float spawnRate = 1.0f;

        [Tooltip("Chế độ ra quái: Tuần tự hay Xen kẽ đa binh chủng")]
        public SpawnOrderMode spawnMode = SpawnOrderMode.Sequential;

        [Header("--- DANH SÁCH NHÓM QUÁI ---")]
        public List<WaveGroup> groups = new List<WaveGroup>();

        /// <summary>
        /// Tổng số lượng quái vật sẽ xuất hiện trong Wave này.
        /// </summary>
        public int TotalEnemyCount
        {
            get
            {
                if (groups == null) return 0;
                int sum = 0;
                foreach (var g in groups)
                {
                    if (g != null) sum += g.count;
                }
                return sum;
            }
        }

        /// <summary>
        /// Tạo danh sách tuần tự các SpawnItem (EnemyType + delay) theo chế độ Sequential hoặc Interleaved.
        /// </summary>
        public List<SpawnItem> GenerateSpawnSequence()
        {
            List<SpawnItem> sequence = new List<SpawnItem>();
            if (groups == null || groups.Count == 0) return sequence;

            if (spawnMode == SpawnOrderMode.Sequential)
            {
                foreach (var grp in groups)
                {
                    if (grp == null || grp.count <= 0) continue;
                    float interval = grp.spawnIntervalOverride > 0f ? grp.spawnIntervalOverride : spawnRate;
                    for (int i = 0; i < grp.count; i++)
                    {
                        sequence.Add(new SpawnItem(grp.enemyType, interval));
                    }
                }
            }
            else // Interleaved (Phân bổ xen kẽ theo thuật toán tích lũy Bresenham)
            {
                int validGroupCount = groups.Count;
                int[] remaining = new int[validGroupCount];
                int total = 0;

                for (int i = 0; i < validGroupCount; i++)
                {
                    remaining[i] = groups[i] != null ? groups[i].count : 0;
                    total += remaining[i];
                }

                if (total <= 0) return sequence;

                float[] accumulator = new float[validGroupCount];

                for (int step = 0; step < total; step++)
                {
                    int bestIdx = -1;
                    float maxAccum = -1f;

                    for (int i = 0; i < validGroupCount; i++)
                    {
                        if (remaining[i] > 0)
                        {
                            accumulator[i] += groups[i].count;
                            if (accumulator[i] > maxAccum)
                            {
                                maxAccum = accumulator[i];
                                bestIdx = i;
                            }
                        }
                    }

                    if (bestIdx >= 0)
                    {
                        accumulator[bestIdx] -= total;
                        remaining[bestIdx]--;

                        var grp = groups[bestIdx];
                        float interval = grp.spawnIntervalOverride > 0f ? grp.spawnIntervalOverride : spawnRate;
                        sequence.Add(new SpawnItem(grp.enemyType, interval));
                    }
                }
            }

            return sequence;
        }
    }

    /// <summary>
    /// ScriptableObject chứa toàn bộ cấu trúc dữ liệu 10 Waves của màn chơi.
    /// Cho phép Game Designer tùy biến nhịp độ, độ khó trực quan trong Unity Inspector.
    /// Chuẩn hóa theo Bảng 2.3 trong Tài liệu Phân tích Thiết kế Kingdom Defense.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveConfig_10Waves", menuName = "Kingdom Defense/Wave Configuration", order = 1)]
    public class WaveConfig : ScriptableObject
    {
        [Header("--- THÔNG TIN CẤU HÌNH ---")]
        public string campaignTitle = "Chiến Dịch 10 Làn Sóng - Kingdom Defense";
        [TextArea(2, 3)]
        public string campaignDescription = "Cấu hình chuẩn 10 đợt tấn công từ Goblin sơ cấp đến Trùm cuối Boss Orcus.";

        [Header("--- DANH SÁCH CÁC WAVES ---")]
        [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

        public List<WaveDefinition> Waves => waves;
        public int TotalWaves => waves != null ? waves.Count : 0;

        /// <summary>
        /// Lấy cấu hình Wave an toàn (1-indexed: waveNumber từ 1 -> TotalWaves).
        /// </summary>
        public WaveDefinition GetWave(int waveNumber)
        {
            if (waves == null || waves.Count == 0) return null;
            int index = Mathf.Clamp(waveNumber - 1, 0, waves.Count - 1);
            return waves[index];
        }

        /// <summary>
        /// Tạo cấu hình chuẩn 10 Waves theo đúng thông số Bảng 2.3 trong Tài liệu thiết kế.
        /// </summary>
        public static WaveConfig CreateDefault10WavesConfig()
        {
            WaveConfig config = CreateInstance<WaveConfig>();
            config.name = "WaveConfig_10Waves";
            config.campaignTitle = "Chiến Dịch 10 Làn Sóng Chuẩn (Bảng 2.3)";
            config.campaignDescription = "Cấu hình 10 đợt quái tự động tạo chuẩn thiết kế: Goblin, Orc, Stone Golem, Dark Bat, Boss Orcus.";

            config.waves = new List<WaveDefinition>
            {
                // Wave 1: 10 Goblin Runner, 1.5s/con
                new WaveDefinition
                {
                    waveNumber = 1,
                    waveTitle = "Wave 1: Thử Thách Tiên Phong",
                    tacticsDescription = "Dễ. Làm quen với thao tác xây Tháp Cung tên.",
                    timeBeforeWave = 5.0f,
                    spawnRate = 1.5f,
                    spawnMode = SpawnOrderMode.Sequential,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.GoblinRunner, 10)
                    }
                },

                // Wave 2: 12 Goblin, 4 Orc, 1.2s/con
                new WaveDefinition
                {
                    waveNumber = 2,
                    waveTitle = "Wave 2: Thiết Binh Tiến Bước",
                    tacticsDescription = "Bắt đầu xuất hiện lính giáp nhẹ, cần nâng cấp tháp.",
                    timeBeforeWave = 8.0f,
                    spawnRate = 1.2f,
                    spawnMode = SpawnOrderMode.Interleaved,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.GoblinRunner, 12),
                        new WaveGroup(EnemyType.OrcWarrior, 4)
                    }
                },

                // Wave 3: 15 Orc Warrior, 1.0s/con
                new WaveDefinition
                {
                    waveNumber = 3,
                    waveTitle = "Wave 3: Đột Kích Bầy Đông",
                    tacticsDescription = "Cần xây Tháp Đại Bác để giải phóng sát thương diện rộng.",
                    timeBeforeWave = 8.0f,
                    spawnRate = 1.0f,
                    spawnMode = SpawnOrderMode.Sequential,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.OrcWarrior, 15)
                    }
                },

                // Wave 4: 12 Dark Bat, 0.8s/con
                new WaveDefinition
                {
                    waveNumber = 4,
                    waveTitle = "Wave 4: Bóng Đêm Không Phận (Quái Bay)",
                    tacticsDescription = "Quái bay lách qua đường mòn, bắt buộc có Tháp Cung đón đầu.",
                    timeBeforeWave = 8.0f,
                    spawnRate = 0.8f,
                    spawnMode = SpawnOrderMode.Sequential,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.DarkBat, 12)
                    }
                },

                // Wave 5: 15 Goblin, 8 Orc, 6 Dơi, 0.7s/con
                new WaveDefinition
                {
                    waveNumber = 5,
                    waveTitle = "Wave 5: Hỗn Hợp Binh Chủng",
                    tacticsDescription = "Nhịp độ dồn dập, thử thách khả năng kiểm soát hỏa lực.",
                    timeBeforeWave = 8.0f,
                    spawnRate = 0.7f,
                    spawnMode = SpawnOrderMode.Interleaved,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.GoblinRunner, 15),
                        new WaveGroup(EnemyType.OrcWarrior, 8),
                        new WaveGroup(EnemyType.DarkBat, 6)
                    }
                },

                // Wave 6: 3 Stone Golem + 10 Orc, 1.8s/con
                new WaveDefinition
                {
                    waveNumber = 6,
                    waveTitle = "Wave 6: Cự Nham Trầm Mặc",
                    tacticsDescription = "Golem kháng vật lý cao, buộc phải xây Tháp Pháp Sư.",
                    timeBeforeWave = 8.0f,
                    spawnRate = 1.8f,
                    spawnMode = SpawnOrderMode.Interleaved,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.StoneGolem, 3),
                        new WaveGroup(EnemyType.OrcWarrior, 10)
                    }
                },

                // Wave 7: 25 Dark Bat, 0.5s/con
                new WaveDefinition
                {
                    waveNumber = 7,
                    waveTitle = "Wave 7: Bão Dơi Quỷ (Air Swarm)",
                    tacticsDescription = "Áp lực không phận cực lớn, cần Tháp Băng hỗ trợ.",
                    timeBeforeWave = 8.0f,
                    spawnRate = 0.5f,
                    spawnMode = SpawnOrderMode.Sequential,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.DarkBat, 25)
                    }
                },

                // Wave 8: 30 Goblin, 15 Orc, 5 Golem, 0.4s/con
                new WaveDefinition
                {
                    waveNumber = 8,
                    waveTitle = "Wave 8: Biển Quái Vật (Massive Horde)",
                    tacticsDescription = "Quái tràn ngập bản đồ, cần phối hợp Tháp Băng + Đại Bác.",
                    timeBeforeWave = 8.0f,
                    spawnRate = 0.4f,
                    spawnMode = SpawnOrderMode.Interleaved,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.GoblinRunner, 30),
                        new WaveGroup(EnemyType.OrcWarrior, 15),
                        new WaveGroup(EnemyType.StoneGolem, 5)
                    }
                },

                // Wave 9: 8 Golem + 20 Orc, 1.0s/con
                new WaveDefinition
                {
                    waveNumber = 9,
                    waveTitle = "Wave 9: Đội Cận Vệ Hoàng Gia Đen",
                    tacticsDescription = "Cực kỳ trâu bò, người chơi phải kích hoạt Mưa Thiên Thạch.",
                    timeBeforeWave = 8.0f,
                    spawnRate = 1.0f,
                    spawnMode = SpawnOrderMode.Interleaved,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.StoneGolem, 8),
                        new WaveGroup(EnemyType.OrcWarrior, 20)
                    }
                },

                // Wave 10: 1 Boss Orcus + 15 Orc + 6 Golem + 10 Dơi, 1.2s/con
                new WaveDefinition
                {
                    waveNumber = 10,
                    waveTitle = "Wave 10: BOSS WAVE - Orcus the Destroyer",
                    tacticsDescription = "Trùm cuối có 3800 HP và skill choáng tháp. Đòi hỏi tổng lực phòng thủ.",
                    timeBeforeWave = 10.0f,
                    spawnRate = 1.2f,
                    spawnMode = SpawnOrderMode.Sequential,
                    groups = new List<WaveGroup>
                    {
                        new WaveGroup(EnemyType.OrcWarrior, 15),
                        new WaveGroup(EnemyType.StoneGolem, 6),
                        new WaveGroup(EnemyType.DarkBat, 10),
                        new WaveGroup(EnemyType.BossOrcus, 1, 3.0f) // Trùm cuối xuất trận oai nghiêm sau 3s delay
                    }
                }
            };

            return config;
        }
    }
}
