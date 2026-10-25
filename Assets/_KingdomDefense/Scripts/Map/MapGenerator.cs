using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KingdomDefense.Build;
using KingdomDefense.Waypoints;
using KingdomDefense.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Map
{
    /// <summary>
    /// Trình tạo bản đồ và giao diện chuẩn phong cách KINGDOM RUSH.
    /// Bao gồm:
    /// - Địa hình thung lũng cỏ rêu ô-liu, đường mòn cát vàng uốn lượn mềm mại
    /// - Rừng thông mùa thu vàng cam bao bọc 2 bên chiến trường
    /// - Cối xay gió trung cổ, cây khô cổ thụ, mỏm đá nhiều tầng
    /// - 12 ô cắm tháp chuẩn Kingdom Rush: Gò đất cát tròn cắm biển gỗ hình khiên
    /// - Hệ thống UI HUD Canvas: Trái tim đỏ (Máu), Đồng vàng, Đầu lâu (Wave), Nút 1x/2x, Nút Cài đặt
    /// </summary>
    [ExecuteInEditMode]
    public class MapGenerator : MonoBehaviour
    {
        [Header("--- BẢNG MÀU KINGDOM RUSH CHUẨN ---")]
        [SerializeField] private Color grassColor = new Color(0.49f, 0.58f, 0.32f);       // Cỏ rêu ô-liu (#7D9452)
        [SerializeField] private Color roadColor = new Color(0.86f, 0.83f, 0.60f);        // Đường đất cát vàng (#DCD499)
        [SerializeField] private Color roadEdgeColor = new Color(0.38f, 0.44f, 0.25f);    // Viền mép đường đậm (#617040)
        [SerializeField] private Color pineFoliageColor = new Color(0.78f, 0.56f, 0.22f); // Lá thông vàng thu (#C78F38)
        [SerializeField] private Color pineTrunkColor = new Color(0.32f, 0.22f, 0.15f);   // Thân cây nâu trầm
        [SerializeField] private Color deadTreeColor = new Color(0.35f, 0.32f, 0.28f);    // Cây khô xám tro
        [SerializeField] private Color rockColor = new Color(0.68f, 0.72f, 0.76f);        // Đá xám núi
        [SerializeField] private Color windmillColor = new Color(0.88f, 0.85f, 0.76f);    // Tường cối xay gió
        [SerializeField] private Color windmillBladeColor = new Color(0.85f, 0.60f, 0.18f);// Cánh cối xay vàng cam

        [Header("--- THÔNG SỐ BẢN ĐỒ ---")]
        [SerializeField] private float mapWidth = 42f;
        [SerializeField] private float mapDepth = 38f;
        [SerializeField] private float roadWidth = 2.2f;

        [Header("--- KAYKIT 3D MEDIEVAL PREFABS (TỰ NẠP NẾU TRỐNG) ---")]
        [SerializeField] private Material kayKitMaterial;
        [SerializeField] private GameObject treeLargePrefab;
        [SerializeField] private GameObject treeSmallPrefab;
        [SerializeField] private GameObject treeSinglePrefab;
        [SerializeField] private GameObject windmillPrefab;
        [SerializeField] private GameObject castlePrefab;
        [SerializeField] private GameObject mountainPrefab;
        [SerializeField] private GameObject mountainBPrefab;
        [SerializeField] private GameObject mountainCPrefab;
        [SerializeField] private GameObject rockPrefab;
        [SerializeField] private GameObject rockBPrefab;
        [SerializeField] private GameObject targetPrefab;
        [SerializeField] private GameObject barrelPrefab;
        [SerializeField] private GameObject cratePrefab;
        [SerializeField] private GameObject tentPrefab;
        [SerializeField] private GameObject fencePrefab;
        [SerializeField] private GameObject wallGatePrefab;
        [SerializeField] private GameObject wallStraightPrefab;
        [SerializeField] private GameObject wallTowerPrefab;
        [SerializeField] private GameObject wallCornerPrefab;
        [SerializeField] private GameObject barracksPrefab;
        [SerializeField] private GameObject homePrefab;
        [SerializeField] private GameObject wellPrefab;
        [SerializeField] private GameObject flagBluePrefab;
        [SerializeField] private GameObject flagRedPrefab;
        [SerializeField] private GameObject waterlilyPrefab;
        [SerializeField] private GameObject waterplantPrefab;

        [Header("--- SPRITES UI HUD (TỰ NẠP NẾU TRỐNG) ---")]
        [SerializeField] private Sprite iconHeart;
        [SerializeField] private Sprite iconCoin;
        [SerializeField] private Sprite iconSkull;
        [SerializeField] private Sprite iconFastForward;
        [SerializeField] private Sprite iconSettings;
        [SerializeField] private Sprite iconPause;
        [SerializeField] private Sprite iconResume;
        [SerializeField] private Sprite iconBadgePanel;

        [Header("--- CONTAINERS ---")]
        [SerializeField] private Transform terrainContainer;
        [SerializeField] private Transform roadContainer;
        [SerializeField] private Transform nodesContainer;
        [SerializeField] private Transform forestContainer;
        [SerializeField] private Transform landmarksContainer;
        [SerializeField] private Transform waypointsContainer;

        // Tọa độ các điểm mốc dẫn đường mòn uốn lượn chữ S
        private readonly List<Vector3> _roadCurvePoints = new List<Vector3>();
        private readonly List<Vector3> _nodePositions = new List<Vector3>();

        private void Awake()
        {
            EnsureWaveSpawner();
            EnsureObjectPool();

            // Nếu sa bàn đã được sinh trong Scene (MapGenerator đã có các GameObject con), giữ nguyên và không làm gì cả
            if (transform.childCount > 0)
            {
                return;
            }

            if (Application.isPlaying)
            {
                GenerateMap();
            }
        }

        private void EnsureWaveSpawner()
        {
            if (Object.FindAnyObjectByType<KingdomDefense.Enemy.WaveSpawner>() == null)
            {
                GameObject spawnerObj = new GameObject("WaveSpawner");
                spawnerObj.transform.SetParent(transform);
                spawnerObj.AddComponent<KingdomDefense.Enemy.WaveSpawner>();
            }
        }

        private void EnsureObjectPool()
        {
            if (Object.FindAnyObjectByType<KingdomDefense.Core.ObjectPool>() == null)
            {
                GameObject poolObj = new GameObject("ObjectPool");
                poolObj.transform.SetParent(transform);
                poolObj.AddComponent<KingdomDefense.Core.ObjectPool>();
            }
        }

        /// <summary>
        /// Tạo toàn bộ Sa bàn & Giao diện chuẩn phong cách Kingdom Rush bằng 3D Assets KayKit.
        /// </summary>
        [ContextMenu("Generate Map")]
        public void GenerateMap()
        {
            ClearMap();
            EnsureObjectPool();
            SetupContainers();
            TryLoadUISprites();
            TryLoadKayKitAssets();
            SetupEnvironmentAndLighting();

            BuildRoadAndNodeCoordinates();
            GenerateTerrainBase();
            GenerateCurvedRoad();
            GenerateKingdomRushNodes();
            GeneratePerimeterMountains();
            GenerateAutumnPineForest();
            GenerateWaterPondAndWindmill();
            GenerateEnclosedFortressAndCamp();
            GenerateRockFormations();
            GenerateMedievalProps();
            GenerateDeadTrees();
            GenerateWaypoints();
            GenerateKingdomRushHUD();

            Debug.Log("[MapGenerator] Đã tạo thành công Bản đồ Thung lũng & Thành trì khép kín bằng bộ 3D KayKit Medieval!");
        }

        /// <summary>
        /// Xóa toàn bộ các thành phần cũ để làm mới.
        /// </summary>
        [ContextMenu("Clear Map")]
        public void ClearMap()
        {
            DestroyContainerChildren(terrainContainer);
            DestroyContainerChildren(roadContainer);
            DestroyContainerChildren(nodesContainer);
            DestroyContainerChildren(forestContainer);
            DestroyContainerChildren(landmarksContainer);
            DestroyContainerChildren(waypointsContainer);

            // Xóa Canvas HUD cũ nếu có
            Canvas oldCanvas = Object.FindAnyObjectByType<Canvas>();
            if (oldCanvas != null && oldCanvas.name.Contains("KingdomRush"))
            {
                DestroyImmediateHelper(oldCanvas.gameObject);
            }

            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in transform) children.Add(child.gameObject);
            foreach (GameObject child in children) DestroyImmediateHelper(child);
        }

        private void SetupContainers()
        {
            terrainContainer = CreateContainer("--- 01_Terrain_Base ---");
            roadContainer = CreateContainer("--- 02_KingdomRush_Road ---");
            nodesContainer = CreateContainer("--- 03_Tower_Signposts ---");
            forestContainer = CreateContainer("--- 04_Autumn_Forest ---");
            landmarksContainer = CreateContainer("--- 05_Landmarks_Props ---");
            waypointsContainer = CreateContainer("--- 06_Waypoints ---");
        }

        private Transform CreateContainer(string name)
        {
            Transform existing = transform.Find(name);
            if (existing != null) return existing;
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            return obj.transform;
        }

        private void DestroyContainerChildren(Transform container)
        {
            if (container == null) return;
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in container) toDestroy.Add(child.gameObject);
            foreach (GameObject obj in toDestroy) DestroyImmediateHelper(obj);
        }

        private void DestroyImmediateHelper(Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) { DestroyImmediate(obj); return; }
#endif
            Destroy(obj);
        }

        /// <summary>
        /// Tinh chỉnh góc máy và ánh sáng trong trẻo chuẩn hoạt hình Isometric.
        /// </summary>
        private void SetupEnvironmentAndLighting()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.22f, 0.28f, 0.24f); // Nền xanh ô liu dịu mắt
                cam.transform.position = new Vector3(1.35f, 26f, -14.0f);
                cam.transform.rotation = Quaternion.Euler(60f, 0f, 0f); // Top-down góc 60 độ bao quát thung lũng & thành trì
                cam.fieldOfView = 60f;
            }

            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.color = new Color(1.0f, 0.96f, 0.88f); // Ánh nắng vàng ấm tươi sáng
                sun.intensity = 1.25f;
                sun.shadows = LightShadows.Soft;
                sun.shadowNormalBias = 0.2f;
                sun.shadowBias = 0.05f;
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private void TryLoadUISprites()
        {
#if UNITY_EDITOR
            string p = "Assets/_KingdomDefense/Sprites/UI/";
            if (iconHeart == null) iconHeart = AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_heart.png");
            if (iconCoin == null) iconCoin = AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_coin.png");
            if (iconSkull == null) iconSkull = AssetDatabase.LoadAssetAtPath<Sprite>(p + "icon_skull.png");
            if (iconFastForward == null) iconFastForward = AssetDatabase.LoadAssetAtPath<Sprite>(p + "btn_fast_forward.png");
            if (iconSettings == null) iconSettings = AssetDatabase.LoadAssetAtPath<Sprite>(p + "btn_settings.png");
            if (iconPause == null) iconPause = AssetDatabase.LoadAssetAtPath<Sprite>(p + "btn_pause.png");
            if (iconResume == null) iconResume = AssetDatabase.LoadAssetAtPath<Sprite>(p + "btn_resume.png");
            if (iconBadgePanel == null) iconBadgePanel = AssetDatabase.LoadAssetAtPath<Sprite>(p + "ui_badge_panel.png");
#endif
        }

        private void TryLoadKayKitAssets()
        {
#if UNITY_EDITOR
            string m = "Assets/_KingdomDefense/Materials/M_KayKit_Medieval.mat";
            string baseP = "Assets/_KingdomDefense/Models/KayKit/";

            if (kayKitMaterial == null) kayKitMaterial = AssetDatabase.LoadAssetAtPath<Material>(m);

            if (treeLargePrefab == null) treeLargePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/trees_A_large.fbx");
            if (treeSmallPrefab == null) treeSmallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/trees_B_small.fbx");
            if (treeSinglePrefab == null) treeSinglePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/tree_single_A.fbx");

            if (windmillPrefab == null) windmillPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/red/building_windmill_red.fbx");
            if (castlePrefab == null) castlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/blue/building_castle_blue.fbx");

            if (mountainPrefab == null) mountainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/mountain_A_grass_trees.fbx");
            if (mountainBPrefab == null) mountainBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/mountain_B_grass_trees.fbx");
            if (mountainCPrefab == null) mountainCPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/mountain_C_grass_trees.fbx");

            if (rockPrefab == null) rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/rock_single_A.fbx");
            if (rockBPrefab == null) rockBPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/rock_single_B.fbx");

            if (targetPrefab == null) targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/props/target.fbx");
            if (barrelPrefab == null) barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/props/barrel.fbx");
            if (cratePrefab == null) cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/props/crate_A_big.fbx");
            if (tentPrefab == null) tentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/props/tent.fbx");
            if (fencePrefab == null) fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/neutral/fence_wood_straight.fbx");
            if (wallGatePrefab == null) wallGatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/neutral/wall_straight_gate.fbx");
            if (wallStraightPrefab == null) wallStraightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/neutral/wall_straight.fbx");
            if (wallTowerPrefab == null) wallTowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/blue/building_tower_A_blue.fbx");
            if (wallCornerPrefab == null) wallCornerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/neutral/wall_corner_A_outside.fbx");

            if (barracksPrefab == null) barracksPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/blue/building_barracks_blue.fbx");
            if (homePrefab == null) homePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/blue/building_home_A_blue.fbx");
            if (wellPrefab == null) wellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/blue/building_well_blue.fbx");
            if (flagBluePrefab == null) flagBluePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/props/flag_blue.fbx");
            if (flagRedPrefab == null) flagRedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/props/flag_red.fbx");
            if (waterlilyPrefab == null) waterlilyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/waterlily_A.fbx");
            if (waterplantPrefab == null) waterplantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "decoration/nature/waterplant_A.fbx");
#endif
        }

        private GameObject InstantiateModel(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent, float scale = 1f)
        {
            if (prefab == null) return null;
            GameObject instance;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = pos;
                instance.transform.rotation = rot;
                instance.transform.SetParent(parent);
            }
            else
            {
                instance = Instantiate(prefab, pos, rot, parent);
            }
#else
            instance = Instantiate(prefab, pos, rot, parent);
#endif
            instance.transform.localScale = Vector3.one * scale;
            ApplyKayKitMaterial(instance);
            return instance;
        }

        private void ApplyKayKitMaterial(GameObject go)
        {
            if (go == null) return;

            // Đảm bảo Material luôn hợp lệ và có Texture, triệt tiêu hoàn toàn lỗi màu hồng
            if (kayKitMaterial == null || kayKitMaterial.shader == null || kayKitMaterial.shader.name.Contains("Error"))
            {
                Shader stdShader = Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse") ?? Shader.Find("Unlit/Texture");
                kayKitMaterial = new Material(stdShader);
                kayKitMaterial.name = "M_KayKit_Runtime";
#if UNITY_EDITOR
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_KingdomDefense/Models/KayKit/Textures/hexagons_medieval.png");
                if (tex != null)
                {
                    kayKitMaterial.mainTexture = tex;
                }
#endif
            }

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.sharedMaterial = kayKitMaterial;
            }
        }

        /// <summary>
        /// Khởi tạo tọa độ đường mòn uốn lượn chữ S dẫn từ Cổng Bắc qua Cổng Thành vào Lâu Đài Hoàng Gia.
        /// </summary>
        private void BuildRoadAndNodeCoordinates()
        {
            _roadCurvePoints.Clear();
            _nodePositions.Clear();

            // Lộ trình uốn lượn thung lũng chữ S mềm mại: từ Doanh trại quái vật (Cổng Bắc) đến Lâu đài hoàng gia (Cổng Nam)
            _roadCurvePoints.Add(new Vector3(-1.0f, 0.02f, 9.5f));   // P00: Doanh trại quái vật Cổng Bắc
            _roadCurvePoints.Add(new Vector3(-1.0f, 0.02f, 7.2f));   // P01: Xuất phát
            _roadCurvePoints.Add(new Vector3(1.5f, 0.02f, 5.2f));    // P02: Cua phải
            _roadCurvePoints.Add(new Vector3(4.5f, 0.02f, 3.2f));    // P03: Sườn đồi
            _roadCurvePoints.Add(new Vector3(5.2f, 0.02f, 0.5f));    // P04: Cua phía Đông
            _roadCurvePoints.Add(new Vector3(2.2f, 0.02f, -1.5f));   // P05: Lòng thung lũng
            _roadCurvePoints.Add(new Vector3(-1.2f, 0.02f, -2.8f));  // P06: Rừng thông
            _roadCurvePoints.Add(new Vector3(-3.2f, 0.02f, -4.6f));  // P07: Khúc cua phía Tây
            _roadCurvePoints.Add(new Vector3(-1.2f, 0.02f, -6.5f));  // P08: Hướng về cổng thành
            _roadCurvePoints.Add(new Vector3(1.35f, 0.02f, -7.5f));  // P09: Xuyên qua Cổng Tường Thành (wall_straight_gate)
            _roadCurvePoints.Add(new Vector3(1.35f, 0.02f, -10.5f)); // P10: Điểm đích - Trước thềm Lâu Đài Hoàng Gia!

            // 12 vị trí ô cắm tháp (Node) đặt dọc 2 bên các khúc cua chiến lược chuẩn sa bàn Kingdom Rush
            _nodePositions.Add(new Vector3(-3.2f, 0.03f, 7.2f));
            _nodePositions.Add(new Vector3(1.9f, 0.03f, 7.3f));
            _nodePositions.Add(new Vector3(-1.3f, 0.03f, 4.7f));
            _nodePositions.Add(new Vector3(4.8f, 0.03f, 5.3f));
            _nodePositions.Add(new Vector3(7.0f, 0.03f, 3.0f));
            _nodePositions.Add(new Vector3(7.3f, 0.03f, 0.5f));
            _nodePositions.Add(new Vector3(4.8f, 0.03f, -2.5f));
            _nodePositions.Add(new Vector3(-0.2f, 0.03f, -0.3f));
            _nodePositions.Add(new Vector3(-3.4f, 0.03f, -1.8f));
            _nodePositions.Add(new Vector3(-5.4f, 0.03f, -4.6f));
            _nodePositions.Add(new Vector3(-3.7f, 0.03f, -7.0f));
            _nodePositions.Add(new Vector3(3.5f, 0.03f, -5.8f));
        }

        /// <summary>
        /// Tạo mặt cỏ rêu thung lũng phẳng phiu và chân đế sa bàn rộng lớn.
        /// </summary>
        private void GenerateTerrainBase()
        {
            float groundCenterZ = -4.0f;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "KingdomRush_Grass_Plateau";
            ground.transform.SetParent(terrainContainer);
            ground.transform.position = new Vector3(0f, -0.5f, groundCenterZ);
            ground.transform.localScale = new Vector3(mapWidth, 1f, mapDepth);
            SetMaterial(ground, grassColor, 0.05f);

            // Chân đế đá sâu bên dưới
            GameObject baseCliff = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseCliff.name = "KingdomRush_Cliff_Base";
            baseCliff.transform.SetParent(terrainContainer);
            baseCliff.transform.position = new Vector3(0f, -1.6f, groundCenterZ);
            baseCliff.transform.localScale = new Vector3(mapWidth + 1.2f, 1.4f, mapDepth + 1.2f);
            SetMaterial(baseCliff, new Color(0.22f, 0.20f, 0.18f), 0.05f);
        }

        /// <summary>
        /// Tạo đường mòn uốn lượn mượt mà chuẩn Ribbon Mesh bằng Catmull-Rom spline:
        /// - Lớp gờ đất mộc sẫm màu sâu bên dưới tạo chiều sâu tự nhiên.
        /// - Mặt đường đất cát vàng nện chặt uốn cong mềm mại, không có góc cạnh thô ráp.
        /// - Nắp tròn bo mượt tại Cổng Bắc và Thềm Lâu Đài.
        /// - Điểm xuyết các phiến đá cuội tự nhiên dọc 2 bên lề đường.
        /// </summary>
        private void GenerateCurvedRoad()
        {
            if (_roadCurvePoints == null || _roadCurvePoints.Count < 2) return;

            // 1. Tính toán chuỗi điểm spline Catmull-Rom mượt mà
            List<Vector3> splinePoints = GenerateCatmullRomSplinePoints(_roadCurvePoints, 8);

            // 2. Lớp gờ đất mộc sẫm màu / móng đường uốn lượn (rộng hơn và nằm dưới)
            CreateRoadRibbonMesh("Road_Shoulder_Base", splinePoints, roadWidth + 0.55f, 0.012f, roadEdgeColor, 0.03f);

            // 3. Lòng đường đất cát vàng tươi nện chặt (nằm trên lớp gờ đất)
            CreateRoadRibbonMesh("Road_Surface_Main", splinePoints, roadWidth, 0.022f, roadColor, 0.05f);

            // 4. Bịt tròn 2 đầu đường (Cổng Bắc và Thềm Lâu Đài)
            CreateRoadEndCaps(splinePoints, roadWidth);

            // 5. Điểm xuyết các hòn đá cuội tự nhiên dọc 2 bên lề đường
            DecorateRoadEdges(splinePoints, roadWidth);
        }

        private List<Vector3> GenerateCatmullRomSplinePoints(List<Vector3> controlPoints, int subdivisions)
        {
            List<Vector3> result = new List<Vector3>();
            int count = controlPoints.Count;
            if (count < 2) return result;

            for (int i = 0; i < count - 1; i++)
            {
                Vector3 p0 = i > 0 ? controlPoints[i - 1] : 2f * controlPoints[0] - controlPoints[1];
                Vector3 p1 = controlPoints[i];
                Vector3 p2 = controlPoints[i + 1];
                Vector3 p3 = (i + 2 < count) ? controlPoints[i + 2] : 2f * controlPoints[count - 1] - controlPoints[count - 2];

                int steps = (i == count - 2) ? subdivisions + 1 : subdivisions;
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)subdivisions;
                    result.Add(GetCatmullRomPoint(t, p0, p1, p2, p3));
                }
            }
            return result;
        }

        private Vector3 GetCatmullRomPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            float x = 0.5f * ((2f * p1.x) + (-p0.x + p2.x) * t + (2f * p0.x - 5f * p1.x + 4f * p2.x - p3.x) * t2 + (-p0.x + 3f * p1.x - 3f * p2.x + p3.x) * t3);
            float y = 0.5f * ((2f * p1.y) + (-p0.y + p2.y) * t + (2f * p0.y - 5f * p1.y + 4f * p2.y - p3.y) * t2 + (-p0.y + 3f * p1.y - 3f * p2.y + p3.y) * t3);
            float z = 0.5f * ((2f * p1.z) + (-p0.z + p2.z) * t + (2f * p0.z - 5f * p1.z + 4f * p2.z - p3.z) * t2 + (-p0.z + 3f * p1.z - 3f * p2.z + p3.z) * t3);
            return new Vector3(x, y, z);
        }

        private GameObject CreateRoadRibbonMesh(string name, List<Vector3> points, float width, float yOffset, Color color, float glossiness)
        {
            int count = points.Count;
            if (count < 2) return null;

            Vector3[] vertices = new Vector3[count * 2];
            Vector3[] normals = new Vector3[count * 2];
            Vector2[] uvs = new Vector2[count * 2];
            int[] triangles = new int[(count - 1) * 6];

            float halfWidth = width * 0.5f;
            float accumulatedDist = 0f;

            for (int i = 0; i < count; i++)
            {
                Vector3 curr = points[i];
                if (i > 0)
                {
                    accumulatedDist += Vector3.Distance(curr, points[i - 1]);
                }

                Vector3 forward;
                if (i == 0)
                {
                    forward = (points[1] - curr).normalized;
                }
                else if (i == count - 1)
                {
                    forward = (curr - points[i - 1]).normalized;
                }
                else
                {
                    forward = (points[i + 1] - points[i - 1]).normalized;
                }
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
                else forward.Normalize();

                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                Vector3 leftPos = curr - right * halfWidth + new Vector3(0f, yOffset, 0f);
                Vector3 rightPos = curr + right * halfWidth + new Vector3(0f, yOffset, 0f);

                int vLeft = i * 2;
                int vRight = i * 2 + 1;

                vertices[vLeft] = leftPos;
                vertices[vRight] = rightPos;

                normals[vLeft] = Vector3.up;
                normals[vRight] = Vector3.up;

                uvs[vLeft] = new Vector2(0f, accumulatedDist * 0.4f);
                uvs[vRight] = new Vector2(1f, accumulatedDist * 0.4f);

                if (i < count - 1)
                {
                    int tIndex = i * 6;
                    int nLeft = (i + 1) * 2;
                    int nRight = (i + 1) * 2 + 1;

                    triangles[tIndex + 0] = vLeft;
                    triangles[tIndex + 1] = nLeft;
                    triangles[tIndex + 2] = vRight;

                    triangles[tIndex + 3] = vRight;
                    triangles[tIndex + 4] = nLeft;
                    triangles[tIndex + 5] = nRight;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = name + "_Mesh";
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            GameObject ribbonObj = new GameObject(name);
            ribbonObj.transform.SetParent(roadContainer);
            ribbonObj.transform.localPosition = Vector3.zero;

            MeshFilter mf = ribbonObj.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = ribbonObj.AddComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            mat.color = color;
            mat.SetFloat("_Glossiness", glossiness);
            mat.SetFloat("_Metallic", 0f);
#if UNITY_EDITOR
            if (!Application.isPlaying) mr.sharedMaterial = mat;
            else mr.material = mat;
#else
            mr.material = mat;
#endif

            return ribbonObj;
        }

        private void CreateRoadEndCaps(List<Vector3> points, float width)
        {
            if (points == null || points.Count < 2) return;

            // Nắp tròn đầu đường (Cổng Bắc)
            CreateCapDisc("Road_Start_Shoulder_Cap", points[0], width + 0.55f, 0.012f, roadEdgeColor, 0.03f);
            CreateCapDisc("Road_Start_Surface_Cap", points[0], width, 0.022f, roadColor, 0.05f);

            // Nắp tròn cuối đường (Thềm Lâu Đài)
            CreateCapDisc("Road_End_Shoulder_Cap", points[points.Count - 1], width + 0.55f, 0.012f, roadEdgeColor, 0.03f);
            CreateCapDisc("Road_End_Surface_Cap", points[points.Count - 1], width, 0.022f, roadColor, 0.05f);
        }

        private void CreateCapDisc(string name, Vector3 pos, float diameter, float yOffset, Color color, float glossiness)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.transform.SetParent(roadContainer);
            disc.transform.position = pos + new Vector3(0f, yOffset, 0f);
            disc.transform.localScale = new Vector3(diameter, 0.01f, diameter);
            SetMaterial(disc, color, glossiness);
            DestroyImmediateHelper(disc.GetComponent<Collider>());
        }

        private void DecorateRoadEdges(List<Vector3> points, float width)
        {
            float halfWidth = width * 0.5f;
            // Đặt các phiến đá cuội tự nhiên ở các khúc cua chiến lược
            int[] pebbleIndices = new int[] { 10, 22, 34, 46, 58, 68 };
            for (int i = 0; i < pebbleIndices.Length; i++)
            {
                int idx = pebbleIndices[i];
                if (idx <= 0 || idx >= points.Count - 1) continue;

                Vector3 curr = points[idx];
                Vector3 forward = (points[idx + 1] - points[idx - 1]).normalized;
                forward.y = 0f;
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                // Xen kẽ bên trái và bên phải khúc cua
                float side = (i % 2 == 0) ? 1f : -1f;
                Vector3 pebblePos = curr + right * (side * (halfWidth + 0.38f));
                pebblePos.y = 0.02f;

                GameObject rockModel = (i % 2 == 0) ? (rockPrefab ?? rockBPrefab) : (rockBPrefab ?? rockPrefab);
                if (rockModel != null)
                {
                    float scale = 0.28f + (i % 3) * 0.06f;
                    Quaternion rot = Quaternion.Euler(0f, (i * 73f) % 360f, 0f);
                    InstantiateModel(rockModel, pebblePos, rot, roadContainer, scale);
                }
                else
                {
                    GameObject pebble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pebble.name = $"Road_Pebble_{i}";
                    pebble.transform.SetParent(roadContainer);
                    pebble.transform.position = pebblePos;
                    pebble.transform.localScale = new Vector3(0.25f, 0.12f, 0.28f);
                    pebble.transform.rotation = Quaternion.Euler(0f, i * 45f, 0f);
                    SetMaterial(pebble, new Color(0.52f, 0.50f, 0.48f), 0.1f);
                    DestroyImmediateHelper(pebble.GetComponent<Collider>());
                }
            }
        }

        /// <summary>
        /// Tạo 12 ô cắm tháp chuẩn Hoàng Gia Kingdom Rush:
        /// - Chân đế đất sẫm (Soil_Rim) bo tự nhiên vào nền cỏ
        /// - Bệ đá hoa cương chắt chiu (Granite_Platform) với 8 viên đá then (Keystone) bao quanh
        /// - Quảng trường lát đá mộc (Foundation_Plaza) là bề mặt đổi màu highlight khi rê chuột
        /// - Huy hiệu trung tâm bằng đồng cổ (Center_Marker) tự động ẩn khi xây tháp xong.
        /// </summary>
        private void GenerateKingdomRushNodes()
        {
            int index = 1;
            foreach (Vector3 pos in _nodePositions)
            {
                GameObject nodeObj = new GameObject($"TowerNode_{index:D2}");
                nodeObj.transform.position = pos;
                nodeObj.transform.SetParent(nodesContainer);

                // 1. Viền đất sẫm ngoài cùng hòa vào cỏ
                GameObject soilRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                soilRim.name = "Soil_Rim";
                soilRim.transform.SetParent(nodeObj.transform);
                soilRim.transform.position = pos + new Vector3(0f, 0.015f, 0f);
                soilRim.transform.localScale = new Vector3(2.35f, 0.02f, 2.35f);
                SetMaterial(soilRim, new Color(0.30f, 0.26f, 0.18f), 0.03f);
                DestroyImmediateHelper(soilRim.GetComponent<Collider>());

                // 2. Thềm đá hoa cương nâng cao
                GameObject granitePlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                granitePlatform.name = "Granite_Platform";
                granitePlatform.transform.SetParent(nodeObj.transform);
                granitePlatform.transform.position = pos + new Vector3(0f, 0.035f, 0f);
                granitePlatform.transform.localScale = new Vector3(2.10f, 0.035f, 2.10f);
                SetMaterial(granitePlatform, new Color(0.48f, 0.46f, 0.43f), 0.05f);
                DestroyImmediateHelper(granitePlatform.GetComponent<Collider>());

                // 3. 8 khối đá then (Keystones) viền xung quanh gờ đá
                for (int k = 0; k < 8; k++)
                {
                    float angleRad = k * Mathf.PI / 4f;
                    float angleDeg = k * 45f;
                    Vector3 stoneOffset = new Vector3(Mathf.Cos(angleRad) * 0.98f, 0.045f, Mathf.Sin(angleRad) * 0.98f);

                    GameObject keystone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    keystone.name = $"Keystone_{k}";
                    keystone.transform.SetParent(nodeObj.transform);
                    keystone.transform.position = pos + stoneOffset;
                    keystone.transform.localScale = new Vector3(0.22f, 0.045f, 0.22f);
                    keystone.transform.rotation = Quaternion.Euler(0f, -angleDeg + 90f, 0f);
                    SetMaterial(keystone, new Color(0.58f, 0.56f, 0.52f), 0.05f);
                    DestroyImmediateHelper(keystone.GetComponent<Collider>());
                }

                // 4. Quảng trường đá mộc flagstone (Bề mặt Node dùng cho Highlight rê chuột)
                GameObject plaza = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plaza.name = "Foundation_Plaza";
                plaza.transform.SetParent(nodeObj.transform);
                plaza.transform.position = pos + new Vector3(0f, 0.055f, 0f);
                plaza.transform.localScale = new Vector3(1.75f, 0.025f, 1.75f);
                SetMaterial(plaza, new Color(0.70f, 0.67f, 0.62f), 0.05f);
                DestroyImmediateHelper(plaza.GetComponent<Collider>());

                // 5. Huy hiệu trung tâm bằng đồng (Center_Marker) tự động ẩn khi tháp được xây
                GameObject centerMarker = new GameObject("Center_Marker");
                centerMarker.transform.SetParent(nodeObj.transform);
                centerMarker.transform.position = pos + new Vector3(0f, 0.07f, 0f);

                // 5a. Đĩa huy hiệu đồng cổ
                GameObject medallion = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                medallion.name = "Medallion_Disc";
                medallion.transform.SetParent(centerMarker.transform);
                medallion.transform.position = pos + new Vector3(0f, 0.07f, 0f);
                medallion.transform.localScale = new Vector3(0.75f, 0.012f, 0.75f);
                SetMaterial(medallion, new Color(0.78f, 0.64f, 0.28f), 0.25f);
                DestroyImmediateHelper(medallion.GetComponent<Collider>());

                // 5b. Thanh rèn chữ thập thợ xây hoàng gia
                GameObject barX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barX.name = "Cross_Bar_X";
                barX.transform.SetParent(centerMarker.transform);
                barX.transform.position = pos + new Vector3(0f, 0.078f, 0f);
                barX.transform.localScale = new Vector3(0.10f, 0.015f, 0.55f);
                SetMaterial(barX, new Color(0.38f, 0.32f, 0.22f), 0.15f);
                DestroyImmediateHelper(barX.GetComponent<Collider>());

                GameObject barZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barZ.name = "Cross_Bar_Z";
                barZ.transform.SetParent(centerMarker.transform);
                barZ.transform.position = pos + new Vector3(0f, 0.078f, 0f);
                barZ.transform.localScale = new Vector3(0.55f, 0.015f, 0.10f);
                SetMaterial(barZ, new Color(0.38f, 0.32f, 0.22f), 0.15f);
                DestroyImmediateHelper(barZ.GetComponent<Collider>());

                // 5c. Tán đinh vàng ở tâm
                GameObject rivet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rivet.name = "Center_Rivet";
                rivet.transform.SetParent(centerMarker.transform);
                rivet.transform.position = pos + new Vector3(0f, 0.088f, 0f);
                rivet.transform.localScale = new Vector3(0.16f, 0.035f, 0.16f);
                SetMaterial(rivet, new Color(0.95f, 0.82f, 0.35f), 0.35f);
                DestroyImmediateHelper(rivet.GetComponent<Collider>());

                // Gắn BoxCollider để nhận click chuột dễ dàng
                BoxCollider col = nodeObj.AddComponent<BoxCollider>();
                col.size = new Vector3(2.2f, 0.8f, 2.2f);
                col.center = new Vector3(0f, 0.35f, 0f);

                // Gắn script Node
                nodeObj.AddComponent<Node>();

                index++;
            }
        }

        /// <summary>
        /// Tạo các rặng núi đá hùng vĩ bao bọc viền sa bàn, hình thành một thung lũng lòng chảo tự nhiên.
        /// </summary>
        private void GeneratePerimeterMountains()
        {
            GameObject mA = mountainPrefab != null ? mountainPrefab : mountainBPrefab;
            GameObject mB = mountainBPrefab != null ? mountainBPrefab : mA;
            GameObject mC = mountainCPrefab != null ? mountainCPrefab : mB;

            if (mA == null && mB == null && mC == null) return;

            // 1. Rặng núi phía Tây (Cánh tả)
            InstantiateModel(mA, new Vector3(-13.5f, 0f, 7.0f), Quaternion.Euler(0f, 40f, 0f), landmarksContainer, 1.85f);
            InstantiateModel(mC, new Vector3(-14.0f, 0f, 0.5f), Quaternion.Euler(0f, -25f, 0f), landmarksContainer, 1.95f);
            InstantiateModel(mB, new Vector3(-13.5f, 0f, -6.5f), Quaternion.Euler(0f, 50f, 0f), landmarksContainer, 1.85f);
            InstantiateModel(mA, new Vector3(-13.5f, 0f, -13.5f), Quaternion.Euler(0f, 15f, 0f), landmarksContainer, 1.85f);

            // 2. Rặng núi phía Đông (Cánh hữu)
            InstantiateModel(mB, new Vector3(13.5f, 0f, 7.0f), Quaternion.Euler(0f, -45f, 0f), landmarksContainer, 1.85f);
            InstantiateModel(mA, new Vector3(14.0f, 0f, 0.5f), Quaternion.Euler(0f, 30f, 0f), landmarksContainer, 1.95f);
            InstantiateModel(mC, new Vector3(13.5f, 0f, -6.5f), Quaternion.Euler(0f, -40f, 0f), landmarksContainer, 1.85f);
            InstantiateModel(mB, new Vector3(13.5f, 0f, -13.5f), Quaternion.Euler(0f, 60f, 0f), landmarksContainer, 1.85f);

            // 3. Rặng núi che chắn phía Bắc (Sau lều trại địch)
            InstantiateModel(mC, new Vector3(-6.5f, 0f, 11.5f), Quaternion.Euler(0f, 110f, 0f), landmarksContainer, 1.75f);
            InstantiateModel(mA, new Vector3(5.5f, 0f, 11.5f), Quaternion.Euler(0f, -90f, 0f), landmarksContainer, 1.75f);

            // 4. Rặng núi che chắn phía Nam (Sau thành trì hoàng gia)
            InstantiateModel(mA, new Vector3(-2.5f, 0f, -18.2f), Quaternion.Euler(0f, 180f, 0f), landmarksContainer, 1.75f);
            InstantiateModel(mB, new Vector3(5.5f, 0f, -18.2f), Quaternion.Euler(0f, 180f, 0f), landmarksContainer, 1.75f);
        }

        /// <summary>
        /// Tạo rừng thông mùa thu vàng cam dày đặc bao quanh thung lũng (Ưu tiên dùng 3D KayKit Trees).
        /// </summary>
        private void GenerateAutumnPineForest()
        {
            List<GameObject> treePool = new List<GameObject>();
            if (treeLargePrefab != null) treePool.Add(treeLargePrefab);
            if (treeSmallPrefab != null) treePool.Add(treeSmallPrefab);
            if (treeSinglePrefab != null) treePool.Add(treeSinglePrefab);

            // Trồng rừng dày ở cánh tả (Left flank)
            for (float x = -11.5f; x <= -6f; x += 1.45f)
            {
                for (float z = -6.5f; z <= 7.5f; z += 1.5f)
                {
                    float jitterX = x + Random.Range(-0.35f, 0.35f);
                    float jitterZ = z + Random.Range(-0.35f, 0.35f);
                    Vector3 pos = new Vector3(jitterX, 0f, jitterZ);
                    if (IsTooCloseToRoad(pos) || IsInsideFortress(pos)) continue;

                    if (treePool.Count > 0)
                    {
                        GameObject prefab = treePool[Random.Range(0, treePool.Count)];
                        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        float s = Random.Range(1.05f, 1.4f);
                        InstantiateModel(prefab, pos, rot, forestContainer, s);
                    }
                    else
                    {
                        CreatePineTree(pos, forestContainer, Random.Range(0.85f, 1.25f));
                    }
                }
            }

            // Trồng rừng dày ở cánh hữu (Right flank)
            for (float x = 6.5f; x <= 11.5f; x += 1.45f)
            {
                for (float z = -6.5f; z <= 7.5f; z += 1.5f)
                {
                    float jitterX = x + Random.Range(-0.35f, 0.35f);
                    float jitterZ = z + Random.Range(-0.35f, 0.35f);
                    Vector3 pos = new Vector3(jitterX, 0f, jitterZ);
                    if (IsTooCloseToRoad(pos) || IsInsideFortress(pos) || IsNearWaterPond(pos)) continue;

                    if (treePool.Count > 0)
                    {
                        GameObject prefab = treePool[Random.Range(0, treePool.Count)];
                        Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        float s = Random.Range(1.05f, 1.4f);
                        InstantiateModel(prefab, pos, rot, forestContainer, s);
                    }
                    else
                    {
                        CreatePineTree(pos, forestContainer, Random.Range(0.85f, 1.25f));
                    }
                }
            }
        }

        private void CreatePineTree(Vector3 pos, Transform parent, float scale)
        {
            GameObject tree = new GameObject("Autumn_Pine_Tree");
            tree.transform.position = pos;
            tree.transform.SetParent(parent);

            // Thân cây gỗ nâu
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform);
            trunk.transform.position = pos + new Vector3(0f, 0.4f * scale, 0f);
            trunk.transform.localScale = new Vector3(0.18f * scale, 0.4f * scale, 0.18f * scale);
            SetMaterial(trunk, pineTrunkColor, 0.05f);

            // 3 Tầng lá hình nón vàng cam mùa thu
            Color colTier1 = pineFoliageColor;
            Color colTier2 = new Color(pineFoliageColor.r * 1.08f, pineFoliageColor.g * 1.05f, pineFoliageColor.b * 0.9f);
            Color colTier3 = new Color(pineFoliageColor.r * 1.15f, pineFoliageColor.g * 1.12f, pineFoliageColor.b * 0.85f);

            CreateConeFoliage(tree.transform, pos + new Vector3(0f, 0.8f * scale, 0f), 0.95f * scale, 0.65f * scale, colTier1);
            CreateConeFoliage(tree.transform, pos + new Vector3(0f, 1.25f * scale, 0f), 0.75f * scale, 0.55f * scale, colTier2);
            CreateConeFoliage(tree.transform, pos + new Vector3(0f, 1.65f * scale, 0f), 0.55f * scale, 0.50f * scale, colTier3);
        }

        private void CreateConeFoliage(Transform parent, Vector3 pos, float radius, float height, Color color)
        {
            GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.transform.SetParent(parent);
            cone.transform.position = pos;
            cone.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            SetMaterial(cone, color, 0.05f);
        }

        /// <summary>
        /// Tạo hồ nước nhỏ trong xanh với hoa súng, cây thủy sinh và cối xay gió trung cổ.
        /// </summary>
        private void GenerateWaterPondAndWindmill()
        {
            // 1. Cối xay gió trung cổ nhìn ra thung lũng
            if (windmillPrefab != null)
            {
                InstantiateModel(windmillPrefab, new Vector3(8.5f, 0f, 1.2f), Quaternion.Euler(0f, -50f, 0f), landmarksContainer, 1.25f);
            }
            else
            {
                CreateWindmill(new Vector3(8.5f, 0f, 1.2f), landmarksContainer);
            }

            // 2. Hồ nước phẳng lặng
            Vector3 pondPos = new Vector3(8.2f, 0f, -2.5f);
            GameObject pond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pond.name = "Water_Pond";
            pond.transform.SetParent(landmarksContainer);
            pond.transform.position = pondPos + new Vector3(0f, 0.012f, 0f);
            pond.transform.localScale = new Vector3(4.5f, 0.015f, 3.2f);
            SetMaterial(pond, new Color(0.25f, 0.60f, 0.72f), 0.9f);

            // Viền bờ hồ sẫm màu
            GameObject pondRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pondRim.name = "Water_Pond_Rim";
            pondRim.transform.SetParent(landmarksContainer);
            pondRim.transform.position = pondPos + new Vector3(0f, 0.008f, 0f);
            pondRim.transform.localScale = new Vector3(4.9f, 0.01f, 3.6f);
            SetMaterial(pondRim, new Color(0.40f, 0.44f, 0.28f), 0.05f);

            // Hoa súng & cây thủy sinh trên mặt hồ
            if (waterlilyPrefab != null)
            {
                InstantiateModel(waterlilyPrefab, pondPos + new Vector3(-0.6f, 0.02f, 0.4f), Quaternion.Euler(0f, 40f, 0f), landmarksContainer, 1.1f);
                InstantiateModel(waterlilyPrefab, pondPos + new Vector3(0.7f, 0.02f, -0.3f), Quaternion.Euler(0f, -80f, 0f), landmarksContainer, 0.95f);
            }
            if (waterplantPrefab != null)
            {
                InstantiateModel(waterplantPrefab, pondPos + new Vector3(-1.8f, 0f, -0.8f), Quaternion.identity, landmarksContainer, 1.1f);
                InstantiateModel(waterplantPrefab, pondPos + new Vector3(1.6f, 0f, 0.9f), Quaternion.identity, landmarksContainer, 1.0f);
            }
        }

        private void CreateWindmill(Vector3 pos, Transform parent)
        {
            GameObject mill = new GameObject("Medieval_Windmill");
            mill.transform.position = pos;
            mill.transform.SetParent(parent);

            // Tháp đá tròn
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.transform.SetParent(mill.transform);
            tower.transform.position = pos + new Vector3(0f, 0.75f, 0f);
            tower.transform.localScale = new Vector3(1.2f, 0.75f, 1.2f);
            SetMaterial(tower, windmillColor, 0.1f);

            // Mái tháp ngói đỏ gạch
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            roof.transform.SetParent(mill.transform);
            roof.transform.position = pos + new Vector3(0f, 1.6f, 0f);
            roof.transform.localScale = new Vector3(1.35f, 0.25f, 1.35f);
            SetMaterial(roof, new Color(0.78f, 0.35f, 0.20f), 0.1f);

            // 4 Cánh quạt vàng cam
            GameObject rotor = new GameObject("Windmill_Rotor");
            rotor.transform.SetParent(mill.transform);
            rotor.transform.position = pos + new Vector3(0f, 1.35f, -0.65f);

            for (int i = 0; i < 4; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.transform.SetParent(rotor.transform);
                blade.transform.localPosition = Vector3.zero;
                blade.transform.localScale = new Vector3(0.18f, 1.2f, 0.02f);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
                SetMaterial(blade, windmillBladeColor, 0.05f);
            }
        }

        /// <summary>
        /// Tạo THÀNH TRÌ KIÊN CỐ KHÉP KÍN (Enclosed Citadel & Fortress) ở phía Nam
        /// và Doanh trại quái vật ở Cổng Bắc.
        /// </summary>
        private void GenerateEnclosedFortressAndCamp()
        {
            // ========================================================
            // A. THÀNH TRÌ KHÉP KÍN PHÍA NAM (SOUTH CITADEL)
            // ========================================================
            float gateX = 1.35f;
            float frontZ = -7.5f;
            float backZ = -16.3f;
            float leftX = -5.25f;
            float rightX = 7.95f;
            float wallScale = 1.35f;

            // 1. CỔNG THÀNH VÒM ĐÁ (Mặt Bắc - đón đường mòn)
            Vector3 gatePos = new Vector3(gateX, 0f, frontZ);
            if (wallGatePrefab != null)
            {
                InstantiateModel(wallGatePrefab, gatePos, Quaternion.identity, landmarksContainer, wallScale);
            }

            // 2. TƯỜNG MẶT TRƯỚC (Front Wall - Z = -7.5f)
            if (wallStraightPrefab != null)
            {
                // Cánh trái
                InstantiateModel(wallStraightPrefab, new Vector3(-0.85f, 0f, frontZ), Quaternion.identity, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(-3.05f, 0f, frontZ), Quaternion.identity, landmarksContainer, wallScale);

                // Cánh phải
                InstantiateModel(wallStraightPrefab, new Vector3(3.55f, 0f, frontZ), Quaternion.identity, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(5.75f, 0f, frontZ), Quaternion.identity, landmarksContainer, wallScale);
            }

            // 3. BỐN THÁP CANH GÓC (4 Corner Fortress Towers)
            GameObject towerToUse = wallTowerPrefab != null ? wallTowerPrefab : wallCornerPrefab;
            if (towerToUse != null)
            {
                // Góc Tây Bắc
                InstantiateModel(towerToUse, new Vector3(leftX, 0f, frontZ), Quaternion.identity, landmarksContainer, wallScale);
                // Góc Đông Bắc
                InstantiateModel(towerToUse, new Vector3(rightX, 0f, frontZ), Quaternion.identity, landmarksContainer, wallScale);
                // Góc Tây Nam
                InstantiateModel(towerToUse, new Vector3(leftX, 0f, backZ), Quaternion.identity, landmarksContainer, wallScale);
                // Góc Đông Nam
                InstantiateModel(towerToUse, new Vector3(rightX, 0f, backZ), Quaternion.identity, landmarksContainer, wallScale);
            }

            // 4. TƯỜNG CÁNH TẢ (West Wall - X = -5.25f, xoay 90 độ, khép kín 100% nối liền 2 tháp góc Tây Bắc và Tây Nam)
            if (wallStraightPrefab != null)
            {
                Quaternion rotSide = Quaternion.Euler(0f, 90f, 0f);
                InstantiateModel(wallStraightPrefab, new Vector3(leftX, 0f, -8.6f), rotSide, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(leftX, 0f, -10.8f), rotSide, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(leftX, 0f, -13.0f), rotSide, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(leftX, 0f, -15.2f), rotSide, landmarksContainer, wallScale);
            }

            // 5. TƯỜNG CÁNH HỮU (East Wall - X = 7.95f, xoay 90 độ, khép kín 100% nối liền 2 tháp góc Đông Bắc và Đông Nam)
            if (wallStraightPrefab != null)
            {
                Quaternion rotSide = Quaternion.Euler(0f, 90f, 0f);
                InstantiateModel(wallStraightPrefab, new Vector3(rightX, 0f, -8.6f), rotSide, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(rightX, 0f, -10.8f), rotSide, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(rightX, 0f, -13.0f), rotSide, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(rightX, 0f, -15.2f), rotSide, landmarksContainer, wallScale);
            }

            // 6. TƯỜNG HẬU PHÍA SAU (South Wall - Z = -16.3f, xoay 180 độ khép kín 100%)
            if (wallStraightPrefab != null)
            {
                Quaternion rotBack = Quaternion.Euler(0f, 180f, 0f);
                InstantiateModel(wallStraightPrefab, new Vector3(-3.05f, 0f, backZ), rotBack, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(-0.85f, 0f, backZ), rotBack, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(1.35f, 0f, backZ), rotBack, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(3.55f, 0f, backZ), rotBack, landmarksContainer, wallScale);
                InstantiateModel(wallStraightPrefab, new Vector3(5.75f, 0f, backZ), rotBack, landmarksContainer, wallScale);
            }

            // 7. LÂU ĐÀI HOÀNG GIA (Nằm sừng sững uy nghiêm ở trung tâm sân thành)
            if (castlePrefab != null)
            {
                InstantiateModel(castlePrefab, new Vector3(gateX, 0f, -12.5f), Quaternion.Euler(0f, 180f, 0f), landmarksContainer, 1.5f);
            }

            // 8. DOANH TRẠI & CÔNG TRÌNH PHỤ TRỢ BÊN TRONG THÀNH
            if (barracksPrefab != null)
            {
                InstantiateModel(barracksPrefab, new Vector3(-2.2f, 0f, -11.8f), Quaternion.Euler(0f, 90f, 0f), landmarksContainer, 1.15f);
            }
            if (homePrefab != null)
            {
                InstantiateModel(homePrefab, new Vector3(4.8f, 0f, -11.8f), Quaternion.Euler(0f, -90f, 0f), landmarksContainer, 1.15f);
            }
            if (wellPrefab != null)
            {
                InstantiateModel(wellPrefab, new Vector3(3.2f, 0f, -9.2f), Quaternion.identity, landmarksContainer, 1.1f);
            }

            // 9. CỜ VƯƠNG QUỐC BLUE (Cắm 2 bên Cổng thành)
            if (flagBluePrefab != null)
            {
                InstantiateModel(flagBluePrefab, new Vector3(0.0f, 0f, -6.8f), Quaternion.identity, landmarksContainer, 1.2f);
                InstantiateModel(flagBluePrefab, new Vector3(2.7f, 0f, -6.8f), Quaternion.identity, landmarksContainer, 1.2f);
            }

            // 10. THÙNG GỖ, RƯƠNG TRANG BỊ TRONG SÂN THÀNH
            if (cratePrefab != null)
            {
                InstantiateModel(cratePrefab, new Vector3(-0.5f, 0f, -9.2f), Quaternion.Euler(0f, 15f, 0f), landmarksContainer, 1.1f);
                InstantiateModel(cratePrefab, new Vector3(4.5f, 0f, -13.5f), Quaternion.Euler(0f, -30f, 0f), landmarksContainer, 1.1f);
            }
            if (barrelPrefab != null)
            {
                InstantiateModel(barrelPrefab, new Vector3(-0.8f, 0f, -9.0f), Quaternion.identity, landmarksContainer, 1.1f);
                InstantiateModel(barrelPrefab, new Vector3(4.8f, 0f, -9.0f), Quaternion.identity, landmarksContainer, 1.0f);
            }

            // ========================================================
            // B. DOANH TRẠI QUÁI VẬT TẠI CỔNG BẮC (NORTH MONSTER CAMP)
            // ========================================================
            if (tentPrefab != null)
            {
                InstantiateModel(tentPrefab, new Vector3(-1.0f, 0f, 10.2f), Quaternion.Euler(0f, 0f, 0f), landmarksContainer, 1.35f);
                InstantiateModel(tentPrefab, new Vector3(-3.2f, 0f, 10.0f), Quaternion.Euler(0f, 25f, 0f), landmarksContainer, 1.15f);
            }
            if (flagRedPrefab != null)
            {
                InstantiateModel(flagRedPrefab, new Vector3(0.5f, 0f, 10.0f), Quaternion.identity, landmarksContainer, 1.25f);
            }
            if (cratePrefab != null)
            {
                InstantiateModel(cratePrefab, new Vector3(0.2f, 0f, 8.8f), Quaternion.Euler(0f, 20f, 0f), landmarksContainer, 1.05f);
            }
        }

        /// <summary>
        /// Tạo các cụm mỏm đá rải rác chân núi và ven đường (Dùng KayKit Rocks).
        /// </summary>
        private void GenerateRockFormations()
        {
            Vector3[] rockSites = new Vector3[]
            {
                new Vector3(4.8f, 0f, 7.5f),
                new Vector3(-5.5f, 0f, -0.5f),
                new Vector3(6.5f, 0f, -6.2f),
                new Vector3(-2.2f, 0f, -5.5f),
                new Vector3(9.5f, 0f, 5.0f),
                new Vector3(-9.0f, 0f, -2.5f)
            };

            foreach (Vector3 p in rockSites)
            {
                if (rockPrefab != null || rockBPrefab != null)
                {
                    GameObject rFab = (rockBPrefab != null && Random.value > 0.5f) ? rockBPrefab : rockPrefab;
                    if (rFab != null)
                    {
                        InstantiateModel(rFab, p, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), landmarksContainer, Random.Range(1.0f, 1.4f));
                        continue;
                    }
                }

                // Fallback nếu không có model KayKit
                GameObject rockCluster = new GameObject("Rock_Formation");
                rockCluster.transform.position = p;
                rockCluster.transform.SetParent(landmarksContainer);

                for (int i = 0; i < 3; i++)
                {
                    GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stone.transform.SetParent(rockCluster.transform);
                    stone.transform.position = p + new Vector3((i - 1) * 0.4f, (i + 1) * 0.3f, 0f);
                    stone.transform.localScale = new Vector3(0.85f - i * 0.15f, 0.6f, 0.85f - i * 0.15f);
                    stone.transform.rotation = Quaternion.Euler(Random.Range(-10, 10), Random.Range(0, 360), Random.Range(-10, 10));
                    SetMaterial(stone, rockColor, 0.1f);
                }
            }
        }

        /// <summary>
        /// Tạo các đạo cụ trang trí trung cổ (Props: Bia bắn cung, Thùng sồi, Rương kho báu, Hàng rào).
        /// </summary>
        private void GenerateMedievalProps()
        {
            // Bia tập bắn cung ven đường chiến lược
            if (targetPrefab != null)
            {
                InstantiateModel(targetPrefab, new Vector3(-2.2f, 0f, 5.2f), Quaternion.Euler(0f, 45f, 0f), landmarksContainer, 1.1f);
                InstantiateModel(targetPrefab, new Vector3(3.5f, 0f, 1.8f), Quaternion.Euler(0f, -60f, 0f), landmarksContainer, 1.1f);
            }

            // Hàng rào gỗ chắn khúc cua nguy hiểm
            if (fencePrefab != null)
            {
                InstantiateModel(fencePrefab, new Vector3(6.5f, 0f, 1.5f), Quaternion.Euler(0f, 75f, 0f), landmarksContainer, 1.1f);
                InstantiateModel(fencePrefab, new Vector3(-4.8f, 0f, -3.2f), Quaternion.Euler(0f, -45f, 0f), landmarksContainer, 1.1f);
            }
        }

        /// <summary>
        /// Tạo cây khô cổ thụ rải rác bên sườn đồi.
        /// </summary>
        private void GenerateDeadTrees()
        {
            Vector3[] sites = new Vector3[]
            {
                new Vector3(-4.5f, 0f, 5.5f),
                new Vector3(0.2f, 0f, 1.8f),
                new Vector3(-4.2f, 0f, 1.2f)
            };

            foreach (Vector3 p in sites)
            {
                GameObject tree = new GameObject("Dead_Tree");
                tree.transform.position = p;
                tree.transform.SetParent(landmarksContainer);

                // Thân cây khô
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.transform.SetParent(tree.transform);
                trunk.transform.position = p + new Vector3(0f, 0.65f, 0f);
                trunk.transform.localScale = new Vector3(0.25f, 0.65f, 0.25f);
                trunk.transform.rotation = Quaternion.Euler(Random.Range(-8, 8), 0f, Random.Range(-8, 8));
                SetMaterial(trunk, deadTreeColor, 0.05f);

                // Cành cây khẳng khiu
                for (int c = 0; c < 2; c++)
                {
                    GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    branch.transform.SetParent(tree.transform);
                    branch.transform.position = p + new Vector3(c == 0 ? -0.3f : 0.3f, 1.1f, 0f);
                    branch.transform.localScale = new Vector3(0.5f, 0.1f, 0.1f);
                    branch.transform.rotation = Quaternion.Euler(0f, c * 60f, c == 0 ? 30f : -30f);
                    SetMaterial(branch, deadTreeColor, 0.05f);
                }
            }
        }

        private bool IsTooCloseToRoad(Vector3 pos)
        {
            foreach (Vector3 p in _roadCurvePoints)
            {
                if (Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(p.x, 0, p.z)) < roadWidth + 0.8f)
                    return true;
            }
            foreach (Vector3 n in _nodePositions)
            {
                if (Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(n.x, 0, n.z)) < 1.3f)
                    return true;
            }
            return false;
        }

        private bool IsInsideFortress(Vector3 pos)
        {
            return (pos.x >= -6.5f && pos.x <= 9.0f && pos.z <= -6.0f && pos.z >= -17.5f);
        }

        private bool IsNearWaterPond(Vector3 pos)
        {
            return Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(8.2f, 0, -2.5f)) < 3.0f;
        }

        private void GenerateWaypoints()
        {
            Waypoints.Waypoints waypointsScript = waypointsContainer.GetComponent<Waypoints.Waypoints>();
            if (waypointsScript == null) waypointsScript = waypointsContainer.gameObject.AddComponent<Waypoints.Waypoints>();

            for (int i = 0; i < _roadCurvePoints.Count; i++)
            {
                GameObject wp = new GameObject($"Waypoint_{i:D2}");
                wp.transform.position = _roadCurvePoints[i] + new Vector3(0f, 0.25f, 0f);
                wp.transform.SetParent(waypointsContainer);
            }

            waypointsScript.LoadPoints();
        }

        /// <summary>
        /// Tạo Canvas UI HUD chuẩn Kingdom Rush:
        /// Trái tim ❤️ (Máu), Đồng vàng 🪙 (Tiền), Đầu lâu 💀 (Wave), Nút 1X/2X, Nút Cài đặt.
        /// </summary>
        /// <summary>
        /// Tạo Canvas UI HUD chuẩn Kingdom Rush:
        /// - Top-Left: Máu ❤️ (Lives), Vàng 🪙 (Gold), Wave 💀
        /// - Top-Center: Thanh Năng Lượng Mana Slider (100 MP)
        /// - Top-Right: Nút 1X/2X, Nút Cài đặt
        /// - Bottom-Dock: 4 Nút chọn mua Tháp Cung [1], Tháp Pháo [2], Tháp Phép [3], Tường Thành [4]
        /// </summary>
        private void GenerateKingdomRushHUD()
        {
            // 0. Đảm bảo BuildManager tồn tại trong Scene
            BuildManager bm = Object.FindAnyObjectByType<BuildManager>();
            if (bm == null)
            {
                GameObject bmObj = new GameObject("BuildManager");
                bm = bmObj.AddComponent<BuildManager>();
                bm.TryLoadDefaultAssets();
            }

            // 0.1. Đảm bảo EventSystem tồn tại để bắt sự kiện click chuột cho toàn bộ UI
            UnityEngine.EventSystems.EventSystem es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }

            // Kiểm tra Canvas cũ
            GameObject canvasObj = GameObject.Find("Canvas_KingdomRushHUD");
            if (canvasObj != null) DestroyImmediateHelper(canvasObj);

            canvasObj = new GameObject("Canvas_KingdomRushHUD");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            GameHUD hud = canvasObj.AddComponent<GameHUD>();

            // 1. TOP-LEFT PANEL (Lives, Gold, Wave)
            GameObject topLeftPanel = new GameObject("TopLeft_StatusPanel");
            topLeftPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform tlRect = topLeftPanel.AddComponent<RectTransform>();
            tlRect.anchorMin = new Vector2(0, 1);
            tlRect.anchorMax = new Vector2(0, 1);
            tlRect.pivot = new Vector2(0, 1);
            tlRect.anchoredPosition = new Vector2(25, -25);
            tlRect.sizeDelta = new Vector2(510, 75);

            Image bg = topLeftPanel.AddComponent<Image>();
            if (iconBadgePanel != null) { bg.sprite = iconBadgePanel; bg.type = Image.Type.Sliced; }
            else bg.color = new Color(0.12f, 0.14f, 0.18f, 0.88f);

            CreateBadgeItem(topLeftPanel.transform, new Vector2(18, 0), iconHeart, "20", new Color(1f, 0.95f, 0.95f), new Vector2(110, 50), out Text livesText);
            CreateBadgeItem(topLeftPanel.transform, new Vector2(150, 0), iconCoin, "350", new Color(1f, 0.88f, 0.25f), new Vector2(125, 50), out Text goldText);
            CreateBadgeItem(topLeftPanel.transform, new Vector2(295, 0), iconSkull, "WAVE: 1/10", Color.white, new Vector2(190, 50), out Text waveText);

            // 2. TOP-CENTER PANEL (Mana Energy Slider)
            CreateManaBar(canvasObj.transform, out Slider manaSlider, out Text manaText);

            // 3. TOP-RIGHT PANEL (Fast Forward x1/x2, Pause)
            GameObject topRightPanel = new GameObject("TopRight_ControlPanel");
            topRightPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform trRect = topRightPanel.AddComponent<RectTransform>();
            trRect.anchorMin = new Vector2(1, 1);
            trRect.anchorMax = new Vector2(1, 1);
            trRect.pivot = new Vector2(1, 1);
            trRect.anchoredPosition = new Vector2(-25, -25);
            trRect.sizeDelta = new Vector2(165, 75);

            CreateIconButton(topRightPanel.transform, new Vector2(-38, 0), iconFastForward, "1X", out Button speedBtn, out Text speedText, "Btn_Speed");
            if (speedBtn != null) speedBtn.onClick.AddListener(hud.ToggleFastForward);

            CreateIconButton(topRightPanel.transform, new Vector2(38, 0), iconPause, "", out Button pauseBtn, out Text pauseText, "Btn_Pause");
            if (pauseBtn != null) pauseBtn.onClick.AddListener(hud.TogglePause);

            // 4. BOTTOM-DOCK (Building Toolbar: 5 nút Cung, Pháo, Phép, Băng, Tường Thành)
            GameObject bottomDock = new GameObject("BottomCenter_BuildDock");
            bottomDock.transform.SetParent(canvasObj.transform, false);
            RectTransform bdRect = bottomDock.AddComponent<RectTransform>();
            bdRect.anchorMin = new Vector2(0.5f, 0f);
            bdRect.anchorMax = new Vector2(0.5f, 0f);
            bdRect.pivot = new Vector2(0.5f, 0f);
            bdRect.anchoredPosition = new Vector2(0f, 20f);
            bdRect.sizeDelta = new Vector2(920, 125);

            Image bdBg = bottomDock.AddComponent<Image>();
            if (iconBadgePanel != null) { bdBg.sprite = iconBadgePanel; bdBg.type = Image.Type.Sliced; }
            else bdBg.color = new Color(0.10f, 0.12f, 0.16f, 0.92f);

            // Nút 1: Tháp Cung [1] (100 Vàng)
            CreateTowerCardButton(bottomDock.transform, new Vector2(-340, 0), "🏹 CUNG [1]", "100", out Button archerBtn, out Image archerHl, out Text archerCost);
            // Nút 2: Tháp Pháo [2] (125 Vàng)
            CreateTowerCardButton(bottomDock.transform, new Vector2(-170, 0), "💣 PHÁO [2]", "125", out Button bombBtn, out Image bombHl, out Text bombCost);
            // Nút 3: Tháp Phép [3] (150 Vàng)
            CreateTowerCardButton(bottomDock.transform, new Vector2(0, 0), "⚡ PHÉP [3]", "150", out Button mageBtn, out Image mageHl, out Text mageCost);
            // Nút 4: Tháp Băng [4] (175 Vàng)
            CreateTowerCardButton(bottomDock.transform, new Vector2(170, 0), "❄️ BĂNG [4]", "175", out Button frostBtn, out Image frostHl, out Text frostCost);
            // Nút 5: Tường Thành [5] (70 Vàng)
            CreateTowerCardButton(bottomDock.transform, new Vector2(340, 0), "🛡️ TƯỜNG [5]", "70", out Button wallBtn, out Image wallHl, out Text wallCost);

            // Liên kết tham chiếu vào GameHUD
            SetPrivateField(hud, "livesText", livesText);
            SetPrivateField(hud, "goldText", goldText);
            SetPrivateField(hud, "waveText", waveText);
            SetPrivateField(hud, "speedBtn", speedBtn);
            SetPrivateField(hud, "speedText", speedText);
            SetPrivateField(hud, "pauseBtn", pauseBtn);
            SetPrivateField(hud, "pauseText", pauseText);
            SetPrivateField(hud, "iconPause", iconPause);
            SetPrivateField(hud, "iconResume", iconResume);
            SetPrivateField(hud, "manaSlider", manaSlider);
            SetPrivateField(hud, "manaText", manaText);

            SetPrivateField(hud, "archerBtn", archerBtn);
            SetPrivateField(hud, "bombBtn", bombBtn);
            SetPrivateField(hud, "mageBtn", mageBtn);
            SetPrivateField(hud, "frostBtn", frostBtn);
            SetPrivateField(hud, "wallBtn", wallBtn);

            SetPrivateField(hud, "archerHighlight", archerHl);
            SetPrivateField(hud, "bombHighlight", bombHl);
            SetPrivateField(hud, "mageHighlight", mageHl);
            SetPrivateField(hud, "frostHighlight", frostHl);
            SetPrivateField(hud, "wallHighlight", wallHl);

            SetPrivateField(hud, "archerCostText", archerCost);
            SetPrivateField(hud, "bombCostText", bombCost);
            SetPrivateField(hud, "mageCostText", mageCost);
            SetPrivateField(hud, "frostCostText", frostCost);
            SetPrivateField(hud, "wallCostText", wallCost);
        }

        private void CreateManaBar(Transform parent, out Slider outSlider, out Text outText)
        {
            GameObject manaObj = new GameObject("TopCenter_ManaBar");
            manaObj.transform.SetParent(parent, false);
            RectTransform mr = manaObj.AddComponent<RectTransform>();
            mr.anchorMin = new Vector2(0.5f, 1f);
            mr.anchorMax = new Vector2(0.5f, 1f);
            mr.pivot = new Vector2(0.5f, 1f);
            mr.anchoredPosition = new Vector2(0f, -25f);
            mr.sizeDelta = new Vector2(360, 48);

            // Nền thanh Mana
            Image bg = manaObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.10f, 0.14f, 0.90f);

            // Slider Component
            outSlider = manaObj.AddComponent<Slider>();
            outSlider.minValue = 0f;
            outSlider.maxValue = 1f;
            outSlider.value = 1f;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(manaObj.transform, false);
            RectTransform far = fillArea.AddComponent<RectTransform>();
            far.anchorMin = Vector2.zero;
            far.anchorMax = Vector2.one;
            far.sizeDelta = new Vector2(-12, -12);

            // Fill Bar
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fr = fill.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.65f, 1f, 1f); // Xanh lam ma thuật neon
            outSlider.fillRect = fr;

            // Text nhãn Mana
            GameObject textObj = new GameObject("ManaText");
            textObj.transform.SetParent(manaObj.transform, false);
            RectTransform tr = textObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.sizeDelta = Vector2.zero;

            outText = textObj.AddComponent<Text>();
            outText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (outText.font == null) outText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            outText.text = "MANA: 100/100 [Space: Thiên Thạch]";
            outText.fontSize = 18;
            outText.fontStyle = FontStyle.Bold;
            outText.color = Color.white;
            outText.alignment = TextAnchor.MiddleCenter;

            Outline o = textObj.AddComponent<Outline>();
            o.effectColor = Color.black;
            o.effectDistance = new Vector2(1, -1);
        }

        private void CreateTowerCardButton(Transform parent, Vector2 pos, string towerName, string costStr, out Button outButton, out Image outHighlight, out Text outCostText)
        {
            GameObject cardObj = new GameObject($"Card_{towerName}");
            cardObj.transform.SetParent(parent, false);
            RectTransform cr = cardObj.AddComponent<RectTransform>();
            cr.anchoredPosition = pos;
            cr.sizeDelta = new Vector2(155, 95);

            // Nền thẻ sẫm màu sang trọng
            Image bg = cardObj.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.17f, 0.23f, 0.95f);
            bg.raycastTarget = true; // Chỉ duy nhất nền thẻ nhận tia click chuột!

            outButton = cardObj.AddComponent<Button>();
            outButton.targetGraphic = bg;

            ColorBlock cb = outButton.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.35f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.65f);
            outButton.colors = cb;

            // Viền Highlight khi được chọn
            GameObject hlObj = new GameObject("HighlightBorder");
            hlObj.transform.SetParent(cardObj.transform, false);
            RectTransform hr = hlObj.AddComponent<RectTransform>();
            hr.anchorMin = Vector2.zero;
            hr.anchorMax = Vector2.one;
            hr.sizeDelta = new Vector2(8, 8);
            outHighlight = hlObj.AddComponent<Image>();
            outHighlight.color = new Color(1f, 1f, 1f, 0f); // Mặc định ẩn
            outHighlight.raycastTarget = false; // Tuyệt đối không chặn chuột!

            // Tên tháp & Hotkey
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform tr = titleObj.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(0f, 0.48f);
            tr.anchorMax = new Vector2(1f, 1f);
            tr.offsetMin = new Vector2(5, 0);
            tr.offsetMax = new Vector2(-5, -5);

            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleText.font == null) titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.text = towerName;
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.96f, 0.85f); // Trắng ngà sắc nét
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.raycastTarget = false; // Không chặn tia click chuột!

            Outline to = titleObj.AddComponent<Outline>();
            to.effectColor = new Color(0f, 0f, 0f, 0.9f);
            to.effectDistance = new Vector2(1.5f, -1.5f);

            // Giá vàng (Cost)
            GameObject costObj = new GameObject("Cost");
            costObj.transform.SetParent(cardObj.transform, false);
            RectTransform cor = costObj.AddComponent<RectTransform>();
            cor.anchorMin = new Vector2(0f, 0f);
            cor.anchorMax = new Vector2(1f, 0.48f);
            cor.offsetMin = new Vector2(5, 5);
            cor.offsetMax = new Vector2(-5, 0);

            outCostText = costObj.AddComponent<Text>();
            outCostText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (outCostText.font == null) outCostText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            outCostText.text = $"{costStr} 🪙";
            outCostText.fontSize = 20;
            outCostText.fontStyle = FontStyle.Bold;
            outCostText.color = new Color(1f, 0.88f, 0.25f); // Vàng kim rực rỡ
            outCostText.alignment = TextAnchor.MiddleCenter;
            outCostText.raycastTarget = false; // Không chặn tia click chuột!

            Outline co = costObj.AddComponent<Outline>();
            co.effectColor = new Color(0f, 0f, 0f, 0.9f);
            co.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void CreateBadgeItem(Transform parent, Vector2 pos, Sprite iconSprite, string defaultText, Color textColor, Vector2 sizeDelta, out Text outText)
        {
            GameObject item = new GameObject("BadgeItem");
            item.transform.SetParent(parent, false);
            RectTransform r = item.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = sizeDelta;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(item.transform, false);
            RectTransform iconR = iconObj.AddComponent<RectTransform>();
            iconR.anchorMin = new Vector2(0f, 0.5f);
            iconR.anchorMax = new Vector2(0f, 0.5f);
            iconR.pivot = new Vector2(0f, 0.5f);
            iconR.sizeDelta = new Vector2(38, 38);
            iconR.anchoredPosition = Vector2.zero;
            Image img = iconObj.AddComponent<Image>();
            if (iconSprite != null) img.sprite = iconSprite;
            else img.color = Color.white;

            // Text
            GameObject textObj = new GameObject("ValueText");
            textObj.transform.SetParent(item.transform, false);
            RectTransform textR = textObj.AddComponent<RectTransform>();
            textR.anchorMin = new Vector2(0f, 0.5f);
            textR.anchorMax = new Vector2(0f, 0.5f);
            textR.pivot = new Vector2(0f, 0.5f);
            textR.anchoredPosition = new Vector2(44, 0);
            textR.sizeDelta = new Vector2(sizeDelta.x - 44, 38);
            outText = textObj.AddComponent<Text>();
            outText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (outText.font == null) outText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            outText.text = defaultText;
            outText.fontSize = 22;
            outText.fontStyle = FontStyle.Bold;
            outText.color = textColor;
            outText.alignment = TextAnchor.MiddleLeft;

            // Đổ bóng chữ đen hoạt hình
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            outline.effectDistance = new Vector2(2, -2);
        }

        private void CreateIconButton(Transform parent, Vector2 pos, Sprite sprite, string label, out Button outButton, out Text outText, string objName = "Btn_Action")
        {
            GameObject btnObj = new GameObject(objName);
            btnObj.transform.SetParent(parent, false);
            RectTransform r = btnObj.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = new Vector2(62, 52);

            Image img = btnObj.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            else img.color = new Color(0.12f, 0.15f, 0.20f, 0.92f);

            outButton = btnObj.AddComponent<Button>();

            ColorBlock cb = outButton.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.3f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.85f, 1f);
            outButton.colors = cb;

            if (!string.IsNullOrEmpty(label))
            {
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(btnObj.transform, false);
                RectTransform lr = labelObj.AddComponent<RectTransform>();
                lr.anchorMin = Vector2.zero;
                lr.anchorMax = Vector2.one;
                lr.sizeDelta = Vector2.zero;
                outText = labelObj.AddComponent<Text>();
                outText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (outText.font == null) outText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                outText.text = label;
                outText.fontSize = 20;
                outText.fontStyle = FontStyle.Bold;
                outText.color = Color.white;
                outText.alignment = TextAnchor.MiddleCenter;

                Outline o = labelObj.AddComponent<Outline>();
                o.effectColor = Color.black;
                o.effectDistance = new Vector2(1, -1);
            }
            else
            {
                outText = null;
            }
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
        }

        private void SetMaterial(GameObject obj, Color color, float glossiness)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Material m = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                m.color = color;
                m.SetFloat("_Glossiness", glossiness);
                m.SetFloat("_Metallic", 0f);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    r.sharedMaterial = m;
                    return;
                }
#endif
                r.material = m;
            }
        }
    }
}
