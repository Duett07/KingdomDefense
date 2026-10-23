#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace KingdomDefense.EditorTools
{
    [InitializeOnLoad]
    public static class MaterialFixer
    {
        static MaterialFixer()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying)
                {
                    FixKayKitMaterial();
                }
            };
        }

        [MenuItem("Tools/Kingdom Defense/🏰 Tạo Lại Bản Đồ & Thành Trì", false, 1)]
        public static void RegenerateSceneMap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[MaterialFixer] Không thể tạo lại bản đồ khi đang ở chế độ Play mode! Hãy dừng Play mode trước.");
                return;
            }

            KingdomDefense.Map.MapGenerator generator = Object.FindFirstObjectByType<KingdomDefense.Map.MapGenerator>();
            if (generator != null)
            {
                generator.GenerateMap();
                EditorUtility.SetDirty(generator.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(generator.gameObject.scene);
                Debug.Log("[MaterialFixer] Đã tạo lại Bản đồ với Thành trì khép kín và lưu Scene thành công!");
            }
        }

        [MenuItem("Tools/Kingdom Defense/🔧 Sửa Lỗi Material KayKit (Hết Màu Hồng)", false, 10)]
        public static void FixKayKitMaterial()
        {
            string matPath = "Assets/_KingdomDefense/Materials/M_KayKit_Medieval.mat";
            string texPath = "Assets/_KingdomDefense/Models/KayKit/Textures/hexagons_medieval.png";

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                Debug.LogError($"[MaterialFixer] Không tìm thấy Texture tại {texPath}!");
                return;
            }

            Shader targetShader = Shader.Find("Standard");
            if (targetShader == null) targetShader = Shader.Find("Universal Render Pipeline/Lit");
            if (targetShader == null) targetShader = Shader.Find("Mobile/Diffuse");
            if (targetShader == null) targetShader = Shader.Find("Legacy Shaders/Diffuse");
            if (targetShader == null) targetShader = Shader.Find("Unlit/Texture");

            if (targetShader == null)
            {
                Debug.LogError("[MaterialFixer] Không tìm thấy bất kỳ Shader nào hợp lệ!");
                return;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(targetShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = targetShader;
            }

            mat.mainTexture = tex;
            mat.color = Color.white;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MaterialFixer] Đã cập nhật thành công Material {matPath} với Shader '{targetShader.name}' và Texture '{tex.name}'!");
        }
    }
}
#endif
