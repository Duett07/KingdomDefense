using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Enemy
{
    /// <summary>
    /// Xưởng lắp ráp và khởi tạo 5 chủng quái vật chính thức (Enemy Factory).
    /// Hỗ trợ nạp Prefab từ thư mục Assets/_KingdomDefense/Prefabs/Enemies/ hoặc tự động lắp ghép
    /// mô hình 3D, trang bị vũ khí, vật liệu và chỉ số chiến đấu chuẩn Bảng 2.1.
    /// </summary>
    public static class EnemyFactory
    {
        private const string PrefabBasePath = "Assets/_KingdomDefense/Prefabs/Enemies/";

        /// <summary>
        /// Tạo hoặc sinh mới đối tượng quái vật theo loại quy định.
        /// </summary>
        public static GameObject CreateEnemy(EnemyType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject prefab = LoadEnemyPrefab(type);
            GameObject enemyObj;

            if (prefab != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    enemyObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                    enemyObj.transform.position = position;
                    enemyObj.transform.rotation = rotation;
                }
                else
                {
                    enemyObj = Object.Instantiate(prefab, position, rotation, parent);
                }
#else
                enemyObj = Object.Instantiate(prefab, position, rotation, parent);
#endif
            }
            else
            {
                enemyObj = BuildEnemyFromAssets(type, position, rotation, parent);
            }

            enemyObj.tag = "Enemy";
            return enemyObj;
        }

        public static GameObject LoadEnemyPrefab(EnemyType type)
        {
            // 1. Ưu tiên nạp từ Resources/Enemies (Hoạt động trên mọi nền tảng: Android, iOS, Windows, WebGL)
            GameObject resPrefab = Resources.Load<GameObject>($"Enemies/{type}") ??
                                   Resources.Load<GameObject>($"Prefabs/Enemies/{type}");
            if (resPrefab != null) return resPrefab;

#if UNITY_EDITOR
            string path = $"{PrefabBasePath}{type}.prefab";
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            return null;
#endif
        }

        /// <summary>
        /// Tự động lắp ráp cấu trúc 3D, vũ khí và thành phần script cho từng loại quái vật.
        /// </summary>
        public static GameObject BuildEnemyFromAssets(EnemyType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject root = new GameObject($"Enemy_{type}");
            root.transform.position = position;
            root.transform.rotation = rotation;
            if (parent != null) root.transform.SetParent(parent);
            root.tag = "Enemy";

            switch (type)
            {
                case EnemyType.GoblinRunner:
                    BuildGoblinRunner(root);
                    break;
                case EnemyType.OrcWarrior:
                    BuildOrcWarrior(root);
                    break;
                case EnemyType.StoneGolem:
                    BuildStoneGolem(root);
                    break;
                case EnemyType.DarkBat:
                    BuildDarkBat(root);
                    break;
                case EnemyType.BossOrcus:
                    BuildBossOrcus(root);
                    break;
            }

            // Gắn thành phần PooledObject để hỗ trợ ObjectPool
            PooledObject po = root.GetComponent<PooledObject>();
            if (po == null) po = root.AddComponent<PooledObject>();
            po.SetPoolTag(type.ToString());

            return root;
        }

        #region --- XÂY DỰNG CHI TIẾT TỪNG LOẠI QUÁI ---

        // 1. GOBLIN RUNNER: Rất nhanh (4.5 m/s), máu giấy (80 HP), 0% Giáp
        private static void BuildGoblinRunner(GameObject root)
        {
            root.transform.localScale = Vector3.one * 0.42f;

            GameObject modelPrefab = LoadAsset<GameObject>("Assets/_KingdomDefense/Models/Characters/Adventures/Rogue.fbx");
            Material rogueMat = LoadAsset<Material>("Assets/_KingdomDefense/Materials/M_Rogue.mat");
            GameObject daggerPrefab = LoadAsset<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/dagger.fbx");

            GameObject visual = InstantiateModel(modelPrefab, root.transform);
            ApplyMaterial(visual, rogueMat);

            if (daggerPrefab != null)
            {
                GameObject dagger = Object.Instantiate(daggerPrefab, visual.transform);
                dagger.name = "Equipped_Dagger";
                dagger.transform.localPosition = new Vector3(0.22f, 0.45f, 0.28f);
                dagger.transform.localRotation = Quaternion.Euler(60f, 0f, -20f);
                dagger.transform.localScale = Vector3.one * 0.85f;
                ApplyMaterial(dagger, rogueMat);
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.height = 0.9f;
            col.radius = 0.35f;

            EnemyMovement mv = root.AddComponent<EnemyMovement>();
            mv.Speed = 4.5f;

            Enemy enemy = root.AddComponent<Enemy>();
            enemy.ConfigureStats("Goblin Runner", 80f, 0f, 0f, 15, false, false, 0f);
        }

        // 2. ORC WARRIOR: Máu vừa (240 HP), 2.6 m/s, 25% Giáp Vật Lý
        private static void BuildOrcWarrior(GameObject root)
        {
            root.transform.localScale = Vector3.one * 0.52f;

            GameObject modelPrefab = LoadAsset<GameObject>("Assets/_KingdomDefense/Models/Characters/Adventures/Barbarian.fbx");
            Material barMat = LoadAsset<Material>("Assets/_KingdomDefense/Materials/M_Barbarian.mat");
            GameObject axePrefab = LoadAsset<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/axe_1handed.fbx");
            GameObject shieldPrefab = LoadAsset<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/shield_spikes.fbx");

            GameObject visual = InstantiateModel(modelPrefab, root.transform);
            ApplyMaterial(visual, barMat);

            if (axePrefab != null)
            {
                GameObject axe = Object.Instantiate(axePrefab, visual.transform);
                axe.name = "Equipped_Axe";
                axe.transform.localPosition = new Vector3(0.24f, 0.48f, 0.26f);
                axe.transform.localRotation = Quaternion.Euler(70f, 15f, -10f);
                axe.transform.localScale = Vector3.one * 0.9f;
                ApplyMaterial(axe, barMat);
            }

            if (shieldPrefab != null)
            {
                GameObject shield = Object.Instantiate(shieldPrefab, visual.transform);
                shield.name = "Equipped_Shield";
                shield.transform.localPosition = new Vector3(-0.25f, 0.48f, 0.15f);
                shield.transform.localRotation = Quaternion.Euler(0f, -80f, 0f);
                shield.transform.localScale = Vector3.one * 0.85f;
                ApplyMaterial(shield, barMat);
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.55f, 0f);
            col.height = 1.1f;
            col.radius = 0.45f;

            EnemyMovement mv = root.AddComponent<EnemyMovement>();
            mv.Speed = 2.6f;

            Enemy enemy = root.AddComponent<Enemy>();
            enemy.ConfigureStats("Orc Warrior", 220f, 0.25f, 0f, 25, false, false, 0f);
        }

        // 3. STONE GOLEM: Máu cực lớn (950 HP), 1.2 m/s, 60% Giáp, Kháng nổ AoE 50%
        private static void BuildStoneGolem(GameObject root)
        {
            root.transform.localScale = Vector3.one * 0.78f;

            GameObject modelPrefab = LoadAsset<GameObject>("Assets/_KingdomDefense/Models/Characters/Adventures/Knight.fbx");
            Material stoneMat = LoadAsset<Material>("Assets/_KingdomDefense/Materials/M_KayKit_Medieval.mat");

            GameObject visual = InstantiateModel(modelPrefab, root.transform);
            ApplyMaterial(visual, stoneMat);

            // Gắn thêm 2 phiến đá khổng lồ trên vai tạo giáp đá kiên cố
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject rockArmor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rockArmor.name = $"Rock_Shoulder_{i}";
                rockArmor.transform.SetParent(visual.transform, false);
                rockArmor.transform.localPosition = new Vector3(i * 0.26f, 0.62f, 0f);
                rockArmor.transform.localScale = new Vector3(0.24f, 0.22f, 0.24f);
                rockArmor.transform.localRotation = Quaternion.Euler(15f, i * 20f, 0f);
                Object.Destroy(rockArmor.GetComponent<Collider>());
                ApplyMaterial(rockArmor, stoneMat);
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.height = 1.5f;
            col.radius = 0.65f;

            EnemyMovement mv = root.AddComponent<EnemyMovement>();
            mv.Speed = 1.2f;

            Enemy enemy = root.AddComponent<Enemy>();
            enemy.ConfigureStats("Stone Golem", 850f, 0.60f, 0f, 60, true, false, 0f);
        }

        // 4. DARK BAT: Bay ở độ cao 2.8m, 3.8 m/s, 120 HP, bỏ qua tháp pháo mặt đất
        private static void BuildDarkBat(GameObject root)
        {
            root.transform.localScale = Vector3.one * 0.48f;

            GameObject visual = new GameObject("Visual_Bat");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;

            Material batMat = LoadAsset<Material>("Assets/_KingdomDefense/Materials/M_Skeleton.mat");
            if (batMat == null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                batMat = new Material(s);
                batMat.color = new Color(0.12f, 0.12f, 0.15f); // Hắc ám
            }

            // Thân dơi
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Bat_Body";
            body.transform.SetParent(visual.transform, false);
            body.transform.localScale = new Vector3(0.35f, 0.45f, 0.55f);
            Object.Destroy(body.GetComponent<Collider>());
            ApplyMaterial(body, batMat);

            // Hai cánh dơi sải rộng
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wing.name = $"Bat_Wing_{i}";
                wing.transform.SetParent(body.transform, false);
                wing.transform.localPosition = new Vector3(i * 0.65f, 0.1f, -0.05f);
                wing.transform.localRotation = Quaternion.Euler(15f, i * -25f, i * -15f);
                wing.transform.localScale = new Vector3(0.85f, 0.05f, 0.65f);
                Object.Destroy(wing.GetComponent<Collider>());
                ApplyMaterial(wing, batMat);
            }

            // Mắt đỏ ma quái
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = $"Red_Eye_{i}";
                eye.transform.SetParent(body.transform, false);
                eye.transform.localPosition = new Vector3(i * 0.12f, 0.12f, 0.25f);
                eye.transform.localScale = Vector3.one * 0.08f;
                Object.Destroy(eye.GetComponent<Collider>());

                Renderer r = eye.GetComponent<Renderer>();
                if (r != null)
                {
                    Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                    Material mEye = new Material(s) { color = Color.red };
                    r.sharedMaterial = mEye;
                }
            }

            SphereCollider col = root.AddComponent<SphereCollider>();
            col.center = Vector3.zero;
            col.radius = 0.55f;

            EnemyMovement mv = root.AddComponent<EnemyMovement>();
            mv.Speed = 3.8f;

            Enemy enemy = root.AddComponent<Enemy>();
            enemy.ConfigureStats("Dark Bat", 110f, 0f, 0f, 35, false, true, 2.8f);
        }

        // 5. BOSS ORCUS: Trùm cuối 3800 HP, 40% Giáp, 30% Kháng Phép, kỹ năng Dậm Đất 6.0m
        private static void BuildBossOrcus(GameObject root)
        {
            root.transform.localScale = Vector3.one * 1.05f;

            GameObject modelPrefab = LoadAsset<GameObject>("Assets/_KingdomDefense/Models/Characters/Adventures/Knight.fbx");
            Material bossMat = LoadAsset<Material>("Assets/_KingdomDefense/Materials/M_BossOrcus.mat") ??
                               LoadAsset<Material>("Assets/_KingdomDefense/Materials/M_Knight.mat");
            GameObject swordPrefab = LoadAsset<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/sword_2handed.fbx") ??
                                     LoadAsset<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/sword_1handed.fbx");

            GameObject visual = InstantiateModel(modelPrefab, root.transform);
            ApplyMaterial(visual, bossMat);

            if (swordPrefab != null)
            {
                GameObject sword = Object.Instantiate(swordPrefab, visual.transform);
                sword.name = "Equipped_Demonic_Greatsword";
                sword.transform.localPosition = new Vector3(0.28f, 0.55f, 0.35f);
                sword.transform.localRotation = Quaternion.Euler(75f, 20f, -15f);
                sword.transform.localScale = Vector3.one * 1.35f;
                ApplyMaterial(sword, bossMat);
            }

            // Vương miện sừng quỷ hắc ám
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                horn.name = $"Demon_Horn_{i}";
                horn.transform.SetParent(visual.transform, false);
                horn.transform.localPosition = new Vector3(i * 0.16f, 0.88f, 0.05f);
                horn.transform.localRotation = Quaternion.Euler(30f, 0f, i * -25f);
                horn.transform.localScale = new Vector3(0.06f, 0.15f, 0.06f);
#if UNITY_EDITOR
                if (!Application.isPlaying) Object.DestroyImmediate(horn.GetComponent<Collider>());
                else Object.Destroy(horn.GetComponent<Collider>());
#else
                Object.Destroy(horn.GetComponent<Collider>());
#endif
                ApplyMaterial(horn, bossMat);
            }

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.height = 1.9f;
            col.radius = 0.75f;

            EnemyMovement mv = root.AddComponent<EnemyMovement>();
            mv.Speed = 1.0f;

            BossOrcus boss = root.AddComponent<BossOrcus>();
            boss.ConfigureStats("Ma Vương Orcus", 3600f, 0.35f, 0.25f, 250, true, false, 0f);
        }

        #endregion

        #region --- HELPER METHODS ---

        private static GameObject InstantiateModel(GameObject prefab, Transform parent)
        {
            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab, parent);
                instance.name = "Visual_Character";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                return instance;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fallback.name = "Visual_Fallback";
            fallback.transform.SetParent(parent, false);
            fallback.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            fallback.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
#if UNITY_EDITOR
            if (!Application.isPlaying) Object.DestroyImmediate(fallback.GetComponent<Collider>());
            else Object.Destroy(fallback.GetComponent<Collider>());
#else
            Object.Destroy(fallback.GetComponent<Collider>());
#endif
            return fallback;
        }

        private static void ApplyMaterial(GameObject obj, Material mat)
        {
            if (obj == null || mat == null) return;
            foreach (Renderer r in obj.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = mat;
            }
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path)) return null;

            // 1. Thử nạp từ Resources theo tên tệp không phần mở rộng
            string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(path);
            T res = Resources.Load<T>($"Materials/{fileNameWithoutExt}") ??
                    Resources.Load<T>($"Enemies/{fileNameWithoutExt}") ??
                    Resources.Load<T>($"Allies/{fileNameWithoutExt}") ??
                    Resources.Load<T>($"Weapons/{fileNameWithoutExt}") ??
                    Resources.Load<T>($"Prefabs/{fileNameWithoutExt}") ??
                    Resources.Load<T>(fileNameWithoutExt);

            if (res != null) return res;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }

        #endregion
    }
}
