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
                    if (!System.IO.File.Exists("Assets/_KingdomDefense/Prefabs/ArcherTower.prefab"))
                    {
                        CreateArcherTowerAndArrowPrefabs();
                    }
                    EnsureSceneEventSystemAndHUD();
                }
            };
        }

        [MenuItem("Tools/Kingdom Defense/🎮 Sửa Lỗi UI Dock & EventSystem", false, 2)]
        public static void EnsureSceneEventSystemAndHUD()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
                return;

            bool sceneDirty = false;

            // 1. Đảm bảo EventSystem có trong Scene
            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(esObj.scene);
                sceneDirty = true;
                Debug.Log("[MaterialFixer] Đã tạo EventSystem trong Scene!");
            }

            // 2. Làm mới UI HUD
            var gen = Object.FindAnyObjectByType<KingdomDefense.Map.MapGenerator>();
            if (gen != null)
            {
                gen.GenerateMap();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gen.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(gen.gameObject.scene);
                Debug.Log("[MaterialFixer] Đã làm mới UI Dock sắc nét và lưu Scene thành công!");
            }
            else if (sceneDirty)
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            }
        }

        [MenuItem("Tools/Kingdom Defense/🏰 Tạo Lại Bản Đồ & Thành Trì", false, 1)]
        public static void RegenerateSceneMap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)
            {
                Debug.LogWarning("[MaterialFixer] Không thể tạo lại bản đồ khi đang ở chế độ Play mode! Hãy dừng Play mode trước.");
                return;
            }

            KingdomDefense.Map.MapGenerator generator = Object.FindAnyObjectByType<KingdomDefense.Map.MapGenerator>();
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

        [MenuItem("Tools/Kingdom Defense/🏹 Tạo Prefab ArcherTower & Arrow", false, 5)]
        public static void CreateArcherTowerAndArrowPrefabs()
        {
            string prefabsDir = "Assets/_KingdomDefense/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets/_KingdomDefense", "Prefabs");
            }

            string arrowPrefabPath = prefabsDir + "/Arrow.prefab";
            string archerTowerPrefabPath = prefabsDir + "/ArcherTower.prefab";

            // 1. TẠO PREFAB ARROW
            string arrowFbxPath = "Assets/_KingdomDefense/Models/Characters/Weapons/arrow.fbx";
            GameObject arrowFbx = AssetDatabase.LoadAssetAtPath<GameObject>(arrowFbxPath);
            Material kayKitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_KingdomDefense/Materials/M_KayKit_Medieval.mat");

            GameObject arrowRoot = new GameObject("Arrow");
            if (arrowFbx != null)
            {
                GameObject meshObj = Object.Instantiate(arrowFbx, arrowRoot.transform);
                meshObj.name = "Mesh";
                meshObj.transform.localPosition = Vector3.zero;
                meshObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                if (kayKitMat != null)
                {
                    foreach (var r in meshObj.GetComponentsInChildren<Renderer>())
                        r.sharedMaterial = kayKitMat;
                }
            }
            arrowRoot.AddComponent<KingdomDefense.Towers.Arrow>();
            PrefabUtility.SaveAsPrefabAsset(arrowRoot, arrowPrefabPath);
            Object.DestroyImmediate(arrowRoot);
            Debug.Log($"[MaterialFixer] Đã tạo thành công {arrowPrefabPath}!");

            // 2. TẠO PREFAB ARCHERTOWER
            string towerFbxPath = "Assets/_KingdomDefense/Models/KayKit/buildings/yellow/building_tower_A_yellow.fbx";
            GameObject towerFbx = AssetDatabase.LoadAssetAtPath<GameObject>(towerFbxPath);
            GameObject rogueFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Models/Characters/Adventures/Rogue.fbx");
            GameObject crossbowFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/crossbow_2handed.fbx");
            GameObject quiverFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/quiver.fbx");
            Material rogueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_KingdomDefense/Materials/M_Rogue.mat");

            GameObject towerRoot = new GameObject("ArcherTower");
            if (towerFbx != null)
            {
                GameObject mesh = Object.Instantiate(towerFbx, towerRoot.transform);
                mesh.name = "Tower_Base";
                mesh.transform.localPosition = Vector3.zero;
                if (kayKitMat != null)
                {
                    foreach (var r in mesh.GetComponentsInChildren<Renderer>())
                        r.sharedMaterial = kayKitMat;
                }
            }

            towerRoot.AddComponent<KingdomDefense.Towers.ArcherTower>();

            if (rogueFbx != null)
            {
                GameObject archer = Object.Instantiate(rogueFbx, towerRoot.transform);
                archer.name = "Archer_Figure";
                archer.transform.localPosition = new Vector3(0f, 2.2f, 0f);
                archer.transform.localScale = Vector3.one * 0.48f;
                if (rogueMat != null)
                {
                    foreach (var r in archer.GetComponentsInChildren<Renderer>())
                        r.sharedMaterial = rogueMat;
                }

                if (crossbowFbx != null)
                {
                    GameObject cb = Object.Instantiate(crossbowFbx, archer.transform);
                    cb.name = "Equipped_Crossbow";
                    cb.transform.localPosition = new Vector3(0f, 0.42f, 0.32f);
                    cb.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
                    cb.transform.localScale = Vector3.one * 0.85f;
                    if (kayKitMat != null)
                    {
                        foreach (var r in cb.GetComponentsInChildren<Renderer>())
                            r.sharedMaterial = kayKitMat;
                    }
                }

                if (quiverFbx != null)
                {
                    GameObject qv = Object.Instantiate(quiverFbx, archer.transform);
                    qv.name = "Equipped_Quiver";
                    qv.transform.localPosition = new Vector3(0.08f, 0.48f, -0.16f);
                    qv.transform.localRotation = Quaternion.Euler(15f, 25f, -15f);
                    qv.transform.localScale = Vector3.one * 0.75f;
                    if (kayKitMat != null)
                    {
                        foreach (var r in qv.GetComponentsInChildren<Renderer>())
                            r.sharedMaterial = kayKitMat;
                    }
                }

                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(archer.transform);
                fp.transform.localPosition = new Vector3(0f, 0.42f, 0.45f);
            }

            PrefabUtility.SaveAsPrefabAsset(towerRoot, archerTowerPrefabPath);
            Object.DestroyImmediate(towerRoot);
            Debug.Log($"[MaterialFixer] Đã tạo thành công {archerTowerPrefabPath}!");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
