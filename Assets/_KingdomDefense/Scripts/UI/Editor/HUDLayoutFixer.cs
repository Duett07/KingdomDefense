using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using KingdomDefense.UI;

namespace KingdomDefense.UI.EditorTools
{
    /// <summary>
    /// Công cụ Editor hỗ trợ căn chỉnh Pixel-Perfect cho Top HUD và Bottom Dock trong Unity Editor.
    /// </summary>
    public static class HUDLayoutFixer
    {
        [MenuItem("Tools/Kingdom Defense/📐 Căn Chỉnh UI Top HUD & Bottom Dock (Pixel-Perfect)", false, 3)]
        [MenuItem("Kingdom Defense/Align HUD Layout (Pixel Perfect)", priority = 25)]
        public static void AlignHUD()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[HUDLayoutFixer] Đang ở chế độ Play mode! GameHUD sẽ tự động căn chỉnh khi chạy.");
                GameHUD activeHud = Object.FindAnyObjectByType<GameHUD>();
                if (activeHud != null)
                {
                    activeHud.AlignAndEnforceHUDLayout();
                    Debug.Log("[HUDLayoutFixer] Đã căn chỉnh HUD trực tiếp trong Play mode!");
                }
                return;
            }

            GameHUD hud = Object.FindAnyObjectByType<GameHUD>();
            if (hud == null)
            {
                GameObject canvasObj = GameObject.Find("Canvas_KingdomRushHUD");
                if (canvasObj != null)
                {
                    hud = canvasObj.GetComponent<GameHUD>();
                }
            }

            if (hud != null)
            {
                Undo.RecordObject(hud.gameObject, "Align HUD Layout");

                Sprite pauseSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_KingdomDefense/Sprites/UI/btn_pause.png");
                Sprite resumeSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_KingdomDefense/Sprites/UI/btn_resume.png");
                var fPause = typeof(GameHUD).GetField("iconPause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fPause != null && pauseSp != null) fPause.SetValue(hud, pauseSp);
                var fResume = typeof(GameHUD).GetField("iconResume", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fResume != null && resumeSp != null) fResume.SetValue(hud, resumeSp);

                hud.EnsurePauseOverlay();
                hud.AutoBindHUDReferences();
                hud.AlignAndEnforceHUDLayout();
                hud.UpdatePauseVisuals();
                EditorUtility.SetDirty(hud.gameObject);
                EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
                EditorSceneManager.SaveScene(hud.gameObject.scene);
                Debug.Log("[HUDLayoutFixer] ✅ Đã căn chỉnh pixel-perfect Top HUD, Control Panel, icon Pause/Resume và lưu Scene thành công!");
            }
            else
            {
                Debug.LogWarning("[HUDLayoutFixer] Không tìm thấy GameHUD trong Scene hiện tại.");
            }
        }
    }
}
