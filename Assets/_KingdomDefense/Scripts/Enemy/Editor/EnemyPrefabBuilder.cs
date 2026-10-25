#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace KingdomDefense.Enemy.Editor
{
    /// <summary>
    /// Công cụ Editor tự động tạo và lưu trữ 5 Prefab quái vật chính thức vào thư mục Prefabs/Enemies/:
    /// 1. GoblinRunner.prefab
    /// 2. OrcWarrior.prefab
    /// 3. StoneGolem.prefab
    /// 4. DarkBat.prefab
    /// 5. BossOrcus.prefab
    /// </summary>
    public static class EnemyPrefabBuilder
    {
        private const string PrefabDir = "Assets/_KingdomDefense/Prefabs/Enemies";

        [InitializeOnLoadMethod]
        private static void CheckAndGeneratePrefabsOnLoad()
        {
            // Tự động kiểm tra nếu chưa có Prefab thì tự sinh
            if (!Directory.Exists(PrefabDir) || Directory.GetFiles(PrefabDir, "*.prefab").Length < 5)
            {
                EditorApplication.delayCall += () =>
                {
                    GenerateAllEnemyPrefabs(silent: true);
                };
            }
        }

        [MenuItem("Kingdom Defense/Build 5 Enemy Prefabs")]
        public static void GenerateAllEnemyPrefabsMenu()
        {
            GenerateAllEnemyPrefabs(silent: false);
        }

        public static void GenerateAllEnemyPrefabs(bool silent = false)
        {
            if (!AssetDatabase.IsValidFolder("Assets/_KingdomDefense/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_KingdomDefense", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabDir))
            {
                AssetDatabase.CreateFolder("Assets/_KingdomDefense/Prefabs", "Enemies");
            }

            EnemyType[] types = (EnemyType[])System.Enum.GetValues(typeof(EnemyType));
            int createdCount = 0;

            foreach (EnemyType type in types)
            {
                string prefabPath = $"{PrefabDir}/{type}.prefab";

                // Lắp ráp đối tượng tạm
                GameObject enemyObj = EnemyFactory.BuildEnemyFromAssets(type, Vector3.zero, Quaternion.identity);

                // Lưu thành Prefab Asset
                PrefabUtility.SaveAsPrefabAsset(enemyObj, prefabPath);
                Object.DestroyImmediate(enemyObj);
                createdCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
            {
                EditorUtility.DisplayDialog("Kingdom Defense", $"Đã khởi tạo thành công {createdCount} Prefab Quái vật tại:\n{PrefabDir}", "OK");
            }
            Debug.Log($"[EnemyPrefabBuilder] Đã hoàn tất đóng gói {createdCount} Prefab Quái vật vào {PrefabDir}!");
        }
    }
}
#endif
