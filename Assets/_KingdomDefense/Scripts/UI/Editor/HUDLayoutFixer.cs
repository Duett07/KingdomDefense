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
                hud.AlignAndEnforceHUDLayout();
                EditorUtility.SetDirty(hud.gameObject);
                EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
                EditorSceneManager.SaveScene(hud.gameObject.scene);
                Debug.Log("[HUDLayoutFixer] ✅ Đã căn chỉnh pixel-perfect Top HUD & Bottom Dock và lưu Scene thành công!");
            }
            else
            {
                Debug.LogWarning("[HUDLayoutFixer] Không tìm thấy GameHUD trong Scene hiện tại.");
            }
        }
    }
}
