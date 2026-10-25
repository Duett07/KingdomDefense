#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KingdomDefense.Enemy.Editor
{
    /// <summary>
    /// Công cụ Editor tự động tạo và lưu trữ file Asset ScriptableObject WaveConfig_10Waves
    /// vào thư mục Assets/_KingdomDefense/Configs/ chuẩn theo Bảng 2.3.
    /// </summary>
    public static class WaveConfigBuilder
    {
        private const string CONFIG_DIR = "Assets/_KingdomDefense/Configs";
        private const string CONFIG_PATH = CONFIG_DIR + "/WaveConfig_10Waves.asset";

        [InitializeOnLoadMethod]
        private static void AutoEnsureWaveConfigOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(CONFIG_PATH))
                {
                    GenerateWaveConfigFile(false);
                }
            };
        }

        [MenuItem("Kingdom Defense/Generate 10-Wave Config Asset", priority = 21)]
        public static void GenerateWaveConfigManual()
        {
            GenerateWaveConfigFile(true);
        }

        public static WaveConfig GenerateWaveConfigFile(bool pingAsset)
        {
            if (!Directory.Exists(CONFIG_DIR))
            {
                Directory.CreateDirectory(CONFIG_DIR);
                AssetDatabase.Refresh();
            }

            WaveConfig config = AssetDatabase.LoadAssetAtPath<WaveConfig>(CONFIG_PATH);

            if (config == null)
            {
                config = WaveConfig.CreateDefault10WavesConfig();
                AssetDatabase.CreateAsset(config, CONFIG_PATH);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=#4CAF50>[WaveConfigBuilder] Đã tạo thành công file Asset cấu hình 10 Waves: {CONFIG_PATH}</color>");
            }
            else
            {
                // Cập nhật lại dữ liệu 10 wave chuẩn
                WaveConfig newDefault = WaveConfig.CreateDefault10WavesConfig();
                config.campaignTitle = newDefault.campaignTitle;
                config.campaignDescription = newDefault.campaignDescription;
                config.Waves.Clear();
                config.Waves.AddRange(newDefault.Waves);

                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=#2196F3>[WaveConfigBuilder] Đã làm mới dữ liệu cấu hình 10 Waves: {CONFIG_PATH}</color>");
            }

            if (pingAsset)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }

            return config;
        }
    }
}
#endif
