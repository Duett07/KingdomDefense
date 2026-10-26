using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace KingdomDefense.Editor
{
    /// <summary>
    /// Hệ thống đóng gói xuất bản đa nền tảng (Multi-Platform Build Pipeline):
    /// - Standalone Windows (x86_64): Tạo file KingdomDefense.exe độc lập kèm thư mục dữ liệu.
    /// - Android Mobile (APK): Xuất file KingdomDefense.apk cài đặt trực tiếp trên smartphone/tablet.
    /// - Tự động thiết lập cấu hình: Màn hình ngang (Landscape), Package ID, Scene nạp và kiến trúc ARM64.
    /// - Tích hợp Menu Editor và hỗ trợ chạy tự động qua Command Line / CI.
    /// </summary>
    public static class KingdomDefenseBuildPipeline
    {
        private const string SCENE_PATH = "Assets/_KingdomDefense/Scenes/Battlefield.unity";
        private const string COMPANY_NAME = "MTA";
        private const string PRODUCT_NAME = "KingdomDefense";
        private const string ANDROID_PACKAGE_NAME = "com.mta.kingdomdefense";

        private static string[] GetBuildScenes()
        {
            // Đảm bảo Scene Battlefield được đăng ký
            if (!File.Exists(SCENE_PATH))
            {
                Debug.LogError($"[BuildPipeline] Không tìm thấy Scene tại đường dẫn: {SCENE_PATH}");
                return new string[0];
            }

            return new string[] { SCENE_PATH };
        }

        [MenuItem("Kingdom Defense/Build/1. Build Windows (x86_64)", priority = 10)]
        public static bool BuildWindows()
        {
            Debug.Log("<color=#F1C40F>[BuildPipeline] BẮT ĐẦU ĐÓNG GÓI BẢN WINDOWS X86_64...</color>");

            string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Windows_x86_64");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string exePath = Path.Combine(outputFolder, $"{PRODUCT_NAME}.exe");

            ConfigureCommonPlayerSettings();

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetBuildScenes(),
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            return HandleBuildResult(report, "Standalone Windows (x86_64)", outputFolder);
        }

        [MenuItem("Kingdom Defense/Build/2. Build Android (APK Mobile)", priority = 11)]
        public static bool BuildAndroid()
        {
            Debug.Log("<color=#3498DB>[BuildPipeline] BẮT ĐẦU ĐÓNG GÓI BẢN MOBILE ANDROID (APK)...</color>");

            string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Android");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string apkPath = Path.Combine(outputFolder, $"{PRODUCT_NAME}.apk");

            ConfigureCommonPlayerSettings();
            ConfigureAndroidPlayerSettings();

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = GetBuildScenes(),
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            return HandleBuildResult(report, "Mobile Android (APK)", outputFolder);
        }

        [MenuItem("Kingdom Defense/Build/3. Build All Platforms (Windows + Android)", priority = 12)]
        public static void BuildAllPlatforms()
        {
            Debug.Log("<color=#9B59B6>[BuildPipeline] BẮT ĐẦU ĐÓNG GÓI TOÀN BỘ NỀN TẢNG (WINDOWS + MOBILE ANDROID)...</color>");

            bool winSuccess = BuildWindows();
            bool androidSuccess = BuildAndroid();

            if (winSuccess && androidSuccess)
            {
                Debug.Log("<color=#2ECC71><b>[BuildPipeline] HOÀN TẤT ĐÓNG GÓI THÀNH CÔNG 100% CẢ 2 BẢN WINDOWS & ANDROID!</b></color>");
            }
            else
            {
                Debug.LogWarning($"[BuildPipeline] Kết quả: Windows={winSuccess}, Android={androidSuccess}");
            }
        }

        [MenuItem("Kingdom Defense/Build/4. Mở thư mục Builds", priority = 20)]
        public static void OpenBuildsFolder()
        {
            string buildsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
            if (!Directory.Exists(buildsFolder))
            {
                Directory.CreateDirectory(buildsFolder);
            }
            EditorUtility.RevealInFinder(buildsFolder);
        }

        private static void ConfigureCommonPlayerSettings()
        {
            PlayerSettings.companyName = COMPANY_NAME;
            PlayerSettings.productName = PRODUCT_NAME;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;

            // Đăng ký Scene vào EditorBuildSettings
            string[] scenePaths = GetBuildScenes();
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }
            EditorBuildSettings.scenes = buildScenes;
        }

        private static void ConfigureAndroidPlayerSettings()
        {
            // Cấu hình Package Name chuẩn
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ANDROID_PACKAGE_NAME);

            // Màn hình ngang (Landscape) cho game Tower Defense chuẩn phong cách Kingdom Rush
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Xuất trực tiếp file .apk cài đặt (tắt xuất .aab AppBundle)
            EditorUserBuildSettings.buildAppBundle = false;

            // Hỗ trợ kiến trúc thiết bị phổ biến hiện nay (ARM64 và ARMv7)
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

            // Thiết lập phiên bản Android tối thiểu: Android 8.0 (API 26+)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            Debug.Log("[BuildPipeline] Đã cấu hình tối ưu thiết lập Android Player: Landscape, APK, ARM64 + ARMv7.");
        }

        private static bool HandleBuildResult(BuildReport report, string platformName, string outputFolder)
        {
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                float sizeMB = summary.totalSize / (1024f * 1024f);
                Debug.Log($"<color=#2ECC71><b>[BuildPipeline] ĐÓNG GÓI THÀNH CÔNG BẢN {platformName.ToUpper()}!</b></color>\n" +
                          $"• Kích thước: {sizeMB:F2} MB\n" +
                          $"• Thời gian build: {summary.totalTime.TotalSeconds:F1} giây\n" +
                          $"• Đường dẫn: {outputFolder}");

#if UNITY_EDITOR
                EditorUtility.RevealInFinder(summary.outputPath);
#endif
                return true;
            }
            else
            {
                Debug.LogError($"<color=#E74C3C>[BuildPipeline] ĐÓNG GÓI THẤT BẠI CHO {platformName.ToUpper()}!</color>\n" +
                               $"• Lỗi: {summary.totalErrors}\n" +
                               $"• Cảnh báo: {summary.totalWarnings}");
                return false;
            }
        }
    }
}
