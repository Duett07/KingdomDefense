using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.Waypoints;
using KingdomDefense.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Enemy
{
    public enum SwordsmanType
    {
        ScoutSwordsman,   // Kiếm sĩ Trinh sát nhẹ (Chạy nhanh, máu vừa)
        KnightSwordsman,  // Hiệp sĩ Thiết giáp (Cầm Kiếm + Khiên, cân bằng)
        Berserker,        // Đại kiếm sĩ Cuồng nộ (Cầm Đại kiếm 2 tay, sát thương cao, máu trâu)
        SwordCaptain      // Đội trưởng Kiếm binh (Chỉ huy thiết giáp, máu lớn)
    }

    /// <summary>
    /// Quản lý xuất hiện các đợt quái là các Chiến Sĩ Cầm Kiếm (Swordsmen Warriors).
    /// Kích thước tỉ lệ chuẩn 0.5x thu nhỏ vừa vặn với kích thước thành trì và đường mòn.
    /// Tự động trang bị Kiếm (Sword) và Khiên (Shield) vào tay nhân vật và áp dụng Texture chuẩn.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        public static WaveSpawner Instance { get; private set; }

        [Header("--- MÔ HÌNH CHIẾN SĨ 3D (TỰ NẠP NẾU TRỐNG) ---")]
        [SerializeField] private GameObject knightPrefab;
        [SerializeField] private GameObject barbarianPrefab;
        [SerializeField] private GameObject roguePrefab;
        [SerializeField] private GameObject swordPrefab;
        [SerializeField] private GameObject greatswordPrefab;
        [SerializeField] private GameObject shieldPrefab;

        [Header("--- MATERIALS & TEXTURES ---")]
        [SerializeField] private Material knightMaterial;
        [SerializeField] private Material barbarianMaterial;
        [SerializeField] private Texture2D knightTexture;
        [SerializeField] private Texture2D barbarianTexture;

        [Header("--- THIẾT LẬP ĐỢT TẤN CÔNG (WAVES) ---")]
        [SerializeField] private int totalWaves = 10;
        [SerializeField] private float timeBetweenWaves = 8f;
        [SerializeField] private float timeBetweenSpawns = 1.0f;
        [SerializeField] private float initialDelay = 1.5f;

        private float _waveCountdown;
        private int _currentWaveIndex = 0;
        private bool _isSpawningWave = false;

        public int CurrentWaveIndex => _currentWaveIndex;
        public float WaveCountdown => _waveCountdown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && Object.FindAnyObjectByType<WaveSpawner>() == null)
            {
                if (Object.FindAnyObjectByType<Waypoints.Waypoints>() != null)
                {
                    GameObject spawnerObj = new GameObject("WaveSpawner");
                    spawnerObj.AddComponent<WaveSpawner>();
                    Debug.Log("[WaveSpawner] Tự động khởi tạo WaveSpawner trong Scene bản đồ!");
                }
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            _waveCountdown = initialDelay;
            EnsureAssetsLoaded();
        }

        private void Start()
        {
            EnsureAssetsLoaded();

            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.SetWave(1, totalWaves);
            }

            Debug.Log($"[WaveSpawner] Đã sẵn sàng xuất quân! Đợt 1 sẽ tiến công sau {_waveCountdown:F1}s.");
        }

        private void Update()
        {
            if (_isSpawningWave || _currentWaveIndex >= totalWaves) return;

            if (_waveCountdown <= 0f)
            {
                StartCoroutine(SpawnWaveRoutine());
                _waveCountdown = timeBetweenWaves;
            }
            else
            {
                _waveCountdown -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Nạp các Model FBX nhân vật Hiệp sĩ, Chiến binh và Vũ khí nếu chưa được gán.
        /// </summary>
        public void EnsureAssetsLoaded()
        {
#if UNITY_EDITOR
            string charPath = "Assets/_KingdomDefense/Models/Characters/Adventures/";
            string weaponPath = "Assets/_KingdomDefense/Models/Characters/Weapons/";
            string matPath = "Assets/_KingdomDefense/Materials/";

            if (knightPrefab == null)
                knightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(charPath + "Knight.fbx");
            if (barbarianPrefab == null)
                barbarianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(charPath + "Barbarian.fbx");
            if (roguePrefab == null)
                roguePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(charPath + "Rogue.fbx");

            if (swordPrefab == null)
                swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath + "sword_1handed.fbx");
            if (greatswordPrefab == null)
                greatswordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath + "sword_2handed.fbx");
            if (shieldPrefab == null)
                shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath + "shield_round_color.fbx");

            if (knightMaterial == null)
                knightMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath + "M_Knight.mat");
            if (barbarianMaterial == null)
                barbarianMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath + "M_Barbarian.mat");

            if (knightTexture == null)
                knightTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(charPath + "knight_texture.png");
            if (barbarianTexture == null)
                barbarianTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(charPath + "barbarian_texture.png");
#endif

            // Đảm bảo Materials luôn có Texture hợp lệ
            if (knightMaterial == null || knightMaterial.mainTexture == null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse");
                knightMaterial = new Material(s) { name = "M_Knight_Runtime" };
                if (knightTexture != null) knightMaterial.mainTexture = knightTexture;
            }

            if (barbarianMaterial == null || barbarianMaterial.mainTexture == null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse");
                barbarianMaterial = new Material(s) { name = "M_Barbarian_Runtime" };
                if (barbarianTexture != null) barbarianMaterial.mainTexture = barbarianTexture;
            }
        }

        private IEnumerator SpawnWaveRoutine()
        {
            _isSpawningWave = true;
            _currentWaveIndex++;

            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.SetWave(_currentWaveIndex, totalWaves);
            }

            List<SwordsmanType> waveTroops = BuildWaveComposition(_currentWaveIndex);
            Debug.Log($"[WaveSpawner] === BẮT ĐẦU WAVE {_currentWaveIndex}/{totalWaves} ({waveTroops.Count} Chiến sĩ cầm kiếm) ===");

            foreach (SwordsmanType type in waveTroops)
            {
                SpawnSwordsman(type);
                yield return new WaitForSeconds(timeBetweenSpawns);
            }

            _isSpawningWave = false;
        }

        private List<SwordsmanType> BuildWaveComposition(int wave)
        {
            List<SwordsmanType> list = new List<SwordsmanType>();

            if (wave == 1)
            {
                // Wave 1: 5 Chiến sĩ Kiếm & Khiên
                for (int i = 0; i < 5; i++) list.Add(SwordsmanType.KnightSwordsman);
            }
            else if (wave == 2)
            {
                // Wave 2: 4 Kiếm sĩ cơ bản + 2 Kiếm sĩ Trinh sát chạy nhanh
                for (int i = 0; i < 4; i++) list.Add(SwordsmanType.KnightSwordsman);
                for (int i = 0; i < 2; i++) list.Add(SwordsmanType.ScoutSwordsman);
            }
            else if (wave == 3)
            {
                // Wave 3: Xuất hiện Đại kiếm sĩ Cuồng nộ (Berserker)
                for (int i = 0; i < 4; i++) list.Add(SwordsmanType.KnightSwordsman);
                for (int i = 0; i < 2; i++) list.Add(SwordsmanType.Berserker);
                for (int i = 0; i < 2; i++) list.Add(SwordsmanType.ScoutSwordsman);
            }
            else
            {
                // Wave 4+: Binh đoàn hỗn hợp tăng dần
                int knights = 3 + wave;
                int berserkers = 1 + wave / 2;
                int scouts = 2 + wave / 3;
                int captains = wave >= 5 ? 1 + (wave - 4) / 2 : 0;

                for (int i = 0; i < knights; i++) list.Add(SwordsmanType.KnightSwordsman);
                for (int i = 0; i < berserkers; i++) list.Add(SwordsmanType.Berserker);
                for (int i = 0; i < scouts; i++) list.Add(SwordsmanType.ScoutSwordsman);
                for (int i = 0; i < captains; i++) list.Add(SwordsmanType.SwordCaptain);
            }

            return list;
        }

        /// <summary>
        /// Khởi tạo một chiến sĩ cầm kiếm 3D với kích thước tỉ lệ phù hợp hoàn hảo với thành trì (Scale ~0.5).
        /// </summary>
        public GameObject SpawnSwordsman(SwordsmanType type)
        {
            Vector3 spawnPos = Vector3.zero;
            if (Waypoints.Waypoints.Points != null && Waypoints.Waypoints.Points.Length > 0)
            {
                // Thêm độ lệch nhẹ (jitter) sang 2 bên lòng đường để các chiến sĩ bước đi thành hàng ngũ sinh động
                Vector3 basePos = Waypoints.Waypoints.Points[0].position;
                float sideJitter = Random.Range(-0.4f, 0.4f);
                spawnPos = basePos + new Vector3(sideJitter, 0f, 0f);
            }

            GameObject modelPrefab = GetPrefabForType(type);
            Material targetMat = (type == SwordsmanType.Berserker) ? barbarianMaterial : knightMaterial;

            GameObject soldierObj;
            if (modelPrefab != null)
            {
                soldierObj = Instantiate(modelPrefab, spawnPos, Quaternion.identity, transform);
            }
            else
            {
                soldierObj = CreateFallbackSoldier();
                soldierObj.transform.position = spawnPos;
                soldierObj.transform.SetParent(transform);
            }

            soldierObj.name = $"Soldier_{type}_W{_currentWaveIndex}_{Time.frameCount}";

            // Kích thước chuẩn tỉ lệ thành trì: ~0.50f (vừa vặn cổng thành và lòng đường mòn)
            float baseScale = GetScaleForType(type);
            soldierObj.transform.localScale = Vector3.one * baseScale;

            // Áp dụng Material chính thức
            ApplyMaterialTo(soldierObj, targetMat);

            // Trang bị Kiếm & Khiên cầm trên tay
            bool isTwoHanded = (type == SwordsmanType.Berserker || type == SwordsmanType.SwordCaptain);
            EquipSwordAndShield(soldierObj, isTwoHanded, targetMat);

            // Logic di chuyển
            EnemyMovement movement = soldierObj.GetComponent<EnemyMovement>();
            if (movement == null) movement = soldierObj.AddComponent<EnemyMovement>();

            // Chỉ số máu và thưởng vàng
            Enemy enemy = soldierObj.GetComponent<Enemy>();
            if (enemy == null) enemy = soldierObj.AddComponent<Enemy>();
            SetupSoldierStats(enemy, movement, type);

            // Kích hoạt hiển thị
            soldierObj.SetActive(true);

            Debug.Log($"[WaveSpawner] Đã xuất quân '{soldierObj.name}' ({type}) kích thước tỉ lệ {baseScale:F2}x!");
            return soldierObj;
        }

        private float GetScaleForType(SwordsmanType type)
        {
            switch (type)
            {
                case SwordsmanType.ScoutSwordsman: return 0.46f;
                case SwordsmanType.KnightSwordsman: return 0.50f;
                case SwordsmanType.Berserker: return 0.54f;
                case SwordsmanType.SwordCaptain: return 0.58f;
                default: return 0.50f;
            }
        }

        private GameObject GetPrefabForType(SwordsmanType type)
        {
            switch (type)
            {
                case SwordsmanType.Berserker:
                    return barbarianPrefab != null ? barbarianPrefab : knightPrefab;
                case SwordsmanType.ScoutSwordsman:
                    return roguePrefab != null ? roguePrefab : knightPrefab;
                default:
                    return knightPrefab;
            }
        }

        private void SetupSoldierStats(Enemy enemy, EnemyMovement movement, SwordsmanType type)
        {
            switch (type)
            {
                case SwordsmanType.ScoutSwordsman:
                    movement.Speed = 3.8f;
                    enemy.ConfigureStats("Kiếm sĩ Tiên phong", 80f, 10);
                    break;
                case SwordsmanType.KnightSwordsman:
                    movement.Speed = 2.8f;
                    enemy.ConfigureStats("Chiến sĩ Thiết giáp", 150f, 18);
                    break;
                case SwordsmanType.Berserker:
                    movement.Speed = 2.4f;
                    enemy.ConfigureStats("Đại kiếm sĩ Cuồng nộ", 240f, 30);
                    break;
                case SwordsmanType.SwordCaptain:
                    movement.Speed = 2.1f;
                    enemy.ConfigureStats("Đội trưởng Kiếm binh", 350f, 45);
                    break;
            }
        }

        /// <summary>
        /// Gắn kiếm và khiên trực tiếp vào tay chiến sĩ.
        /// </summary>
        private void EquipSwordAndShield(GameObject character, bool isTwoHanded, Material characterMat)
        {
            // Tìm xương cánh tay phải để cầm kiếm
            Transform rightArm = FindDeepChild(character.transform, "lowerarm.r")
                              ?? FindDeepChild(character.transform, "Knight_ArmRight")
                              ?? FindDeepChild(character.transform, "hand.r");

            GameObject weaponPrefab = isTwoHanded && greatswordPrefab != null ? greatswordPrefab : swordPrefab;
            if (weaponPrefab != null)
            {
                Transform parent = rightArm != null ? rightArm : character.transform;
                GameObject sword = Instantiate(weaponPrefab, parent);
                sword.name = "Equipped_Sword";

                if (rightArm != null)
                {
                    sword.transform.localPosition = new Vector3(0.05f, -0.22f, 0.18f);
                    sword.transform.localRotation = Quaternion.Euler(75f, 15f, -20f);
                    sword.transform.localScale = Vector3.one * 0.95f;
                }
                else
                {
                    sword.transform.localPosition = new Vector3(0.25f, 0.45f, 0.25f);
                    sword.transform.localRotation = Quaternion.Euler(60f, 20f, 0f);
                    sword.transform.localScale = Vector3.one * 0.95f;
                }

                ApplyMaterialTo(sword, characterMat);
            }

            // Nếu là hiệp sĩ 1 tay kiếm, gắn thêm Khiên bảo vệ vào tay trái
            if (!isTwoHanded && shieldPrefab != null)
            {
                Transform leftArm = FindDeepChild(character.transform, "lowerarm.l")
                                 ?? FindDeepChild(character.transform, "Knight_ArmLeft")
                                 ?? FindDeepChild(character.transform, "hand.l");

                Transform parent = leftArm != null ? leftArm : character.transform;
                GameObject shield = Instantiate(shieldPrefab, parent);
                shield.name = "Equipped_Shield";

                if (leftArm != null)
                {
                    shield.transform.localPosition = new Vector3(-0.06f, -0.12f, 0.08f);
                    shield.transform.localRotation = Quaternion.Euler(-15f, 90f, 0f);
                    shield.transform.localScale = Vector3.one * 0.95f;
                }
                else
                {
                    shield.transform.localPosition = new Vector3(-0.25f, 0.45f, 0.15f);
                    shield.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    shield.transform.localScale = Vector3.one * 0.95f;
                }

                ApplyMaterialTo(shield, characterMat);
            }
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return child;
                Transform found = FindDeepChild(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private void ApplyMaterialTo(GameObject go, Material mat)
        {
            if (mat == null) return;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.transform.parent != null && r.transform.parent.name == "HealthBar") continue;
                r.sharedMaterial = mat;
            }
        }

        private GameObject CreateFallbackSoldier()
        {
            GameObject go = new GameObject("Soldier_Fallback");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(go.transform);
            body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            return go;
        }
    }
}
