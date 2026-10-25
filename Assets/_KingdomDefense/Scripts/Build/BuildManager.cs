using UnityEngine;
using KingdomDefense.UI;
using KingdomDefense.Towers;
using KingdomDefense.Skills;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KingdomDefense.Build
{
    public enum TowerType
    {
        None,
        Archer,     // 100 Gold
        Bomb,       // 125 Gold
        Mage,       // 150 Gold
        Frost,      // 175 Gold (Tháp Băng Phong làm chậm 50%)
        Wall        // 70 Gold (Tường thành / Rào chắn cản đường)
    }

    /// <summary>
    /// Quản lý cơ chế Xây dựng (Building System) của Kingdom Defense.
    /// Cho phép chọn tháp từ UI hoặc phím tắt [1..5],
    /// kiểm tra lượng vàng và đặt tháp / tường thành lên các ô Node.
    /// </summary>
    public class BuildManager : MonoBehaviour
    {
        public static BuildManager Instance { get; private set; }

        [Header("--- THIẾT LẬP CHI PHÍ VÀNG ---")]
        [SerializeField] private int archerCost = 100;
        [SerializeField] private int bombCost = 125;
        [SerializeField] private int mageCost = 150;
        [SerializeField] private int frostCost = 175;
        [SerializeField] private int wallCost = 70;

        [Header("--- 3D PREFABS CÔNG TRÌNH ---")]
        [SerializeField] private GameObject archerTowerPrefab;
        [SerializeField] private GameObject bombTowerPrefab;
        [SerializeField] private GameObject mageTowerPrefab;
        [SerializeField] private GameObject frostTowerPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private Material kayKitMaterial;

        private TowerType _selectedTowerType = TowerType.None;

        public TowerType SelectedTowerType => _selectedTowerType;
        public int ArcherCost => archerCost;
        public int BombCost => bombCost;
        public int MageCost => mageCost;
        public int FrostCost => frostCost;
        public int WallCost => wallCost;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            TryLoadDefaultAssets();
        }

        private void Update()
        {
            // Chặn phím tắt chọn công trình khi game đang tạm dừng (Pause)
            if ((GameHUD.Instance != null && GameHUD.Instance.IsPaused) || Time.timeScale <= 0.0001f)
            {
                return;
            }

            // Hỗ trợ phím tắt số [1], [2], [3], [4], [5] để chọn công trình nhanh
            bool key1 = false, key2 = false, key3 = false, key4 = false, key5 = false, keyEsc = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                key1 = Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame;
                key2 = Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame;
                key3 = Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame;
                key4 = Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame;
                key5 = Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame;
                keyEsc = Keyboard.current.escapeKey.wasPressedThisFrame;
            }
#endif

            // Fallback sang Legacy Input nếu New Input System không kích hoạt
            if (!key1 && !key2 && !key3 && !key4 && !key5 && !keyEsc)
            {
                try
                {
                    key1 = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
                    key2 = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
                    key3 = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
                    key4 = Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
                    key5 = Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);
                    keyEsc = Input.GetKeyDown(KeyCode.Escape);
                }
                catch (System.InvalidOperationException) { }
            }

            bool rightClick = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                rightClick = Mouse.current.rightButton.wasPressedThisFrame;
            }
#endif
            if (!rightClick)
            {
                try
                {
                    rightClick = Input.GetMouseButtonDown(1);
                }
                catch (System.InvalidOperationException) { }
            }

            if (key1) SelectTowerToBuild(TowerType.Archer);
            else if (key2) SelectTowerToBuild(TowerType.Bomb);
            else if (key3) SelectTowerToBuild(TowerType.Mage);
            else if (key4) SelectTowerToBuild(TowerType.Frost);
            else if (key5) SelectTowerToBuild(TowerType.Wall);
            else if (keyEsc || rightClick)
            {
                // Nếu Thiên Thạch đang ngắm thì nhường ưu tiên cho MeteorShowerSkill hủy trước
                if (MeteorShowerSkill.Instance == null || !MeteorShowerSkill.Instance.IsAiming)
                {
                    SelectTowerToBuild(TowerType.None);
                }
            }
        }

        /// <summary>
        /// Nạp tự động các model 3D KayKit nếu chưa được gán trên Inspector.
        /// </summary>
        public void TryLoadDefaultAssets()
        {
#if UNITY_EDITOR
            string baseP = "Assets/_KingdomDefense/Models/KayKit/";
            if (archerTowerPrefab == null)
                archerTowerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Prefabs/ArcherTower.prefab") ??
                                   UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/yellow/building_tower_A_yellow.fbx");
            if (bombTowerPrefab == null)
                bombTowerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Prefabs/CannonTower.prefab") ??
                                  UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/yellow/building_tower_catapult_yellow.fbx");
            if (mageTowerPrefab == null)
                mageTowerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/blue/building_tower_B_blue.fbx");
            if (frostTowerPrefab == null)
                frostTowerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/blue/building_tower_A_blue.fbx");
            if (wallPrefab == null)
                wallPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(baseP + "buildings/neutral/wall_straight.fbx");
            if (kayKitMaterial == null)
                kayKitMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_KingdomDefense/Materials/M_KayKit_Medieval.mat");
#endif
        }

        /// <summary>
        /// Chọn loại công trình để xây dựng.
        /// - Nhấn lần 1: Chọn tháp (nếu đủ vàng).
        /// - Nhấn lần 2 hoặc click chuột phải: Hủy chọn (Toggle).
        /// </summary>
        public void SelectTowerToBuild(TowerType type)
        {
            if (type != TowerType.None && !HasEnoughMoney(type))
            {
                Debug.LogWarning($"[BuildManager] Không đủ vàng để chọn {type} (Cần {GetCost(type)} Vàng)!");
                return;
            }

            if (type != TowerType.None)
            {
                // Nếu đang ngắm Thiên Thạch, hủy ngắm trước khi chọn tháp
                if (MeteorShowerSkill.Instance != null && MeteorShowerSkill.Instance.IsAiming)
                {
                    MeteorShowerSkill.Instance.SetAiming(false);
                }
            }

            if (_selectedTowerType == type)
            {
                _selectedTowerType = TowerType.None; // Bấm lại lần nữa để hủy chọn
            }
            else
            {
                _selectedTowerType = type;
            }

            Debug.Log($"[BuildManager] Đang chọn công trình: {_selectedTowerType}");

            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.UpdateSelectedTowerUI(_selectedTowerType);
            }
        }

        /// <summary>
        /// Kiểm tra xem người chơi có đủ vàng để xây loại công trình đang chọn không.
        /// </summary>
        public bool HasEnoughMoney(TowerType type)
        {
            int cost = GetCost(type);
            int currentGold = GameHUD.Instance != null ? GameHUD.Instance.CurrentGold : 999;
            return currentGold >= cost;
        }

        public int GetCost(TowerType type)
        {
            return type switch
            {
                TowerType.Archer => archerCost,
                TowerType.Bomb => bombCost,
                TowerType.Mage => mageCost,
                TowerType.Frost => frostCost,
                TowerType.Wall => wallCost,
                _ => 0
            };
        }

        /// <summary>
        /// Thực hiện xây dựng công trình lên ô Node.
        /// </summary>
        public bool BuildTowerOn(Node node)
        {
            if (_selectedTowerType == TowerType.None)
            {
                Debug.LogWarning("[BuildManager] Chưa chọn công trình nào từ UI! Hãy click chọn Tháp hoặc Tường thành trước.");
                return false;
            }

            // Ghi nhận loại công trình trước khi trừ vàng, tránh bị SpendGold kích hoạt UpdateTowerButtonsAvailability làm reset _selectedTowerType
            TowerType towerTypeToBuild = _selectedTowerType;
            int cost = GetCost(towerTypeToBuild);

            if (!HasEnoughMoney(towerTypeToBuild))
            {
                Debug.LogWarning($"[BuildManager] Không đủ vàng! Cần {cost} vàng.");
                return false;
            }

            if (GameHUD.Instance != null && !GameHUD.Instance.SpendGold(cost))
            {
                Debug.LogWarning($"[BuildManager] Không đủ vàng! Cần {cost} vàng.");
                return false;
            }

            GameObject prefab = towerTypeToBuild switch
            {
                TowerType.Archer => archerTowerPrefab,
                TowerType.Bomb => bombTowerPrefab,
                TowerType.Mage => mageTowerPrefab,
                TowerType.Frost => frostTowerPrefab,
                TowerType.Wall => wallPrefab,
                _ => null
            };

            Vector3 buildPos = node.GetBuildPosition();
            GameObject towerInstance;

#if UNITY_EDITOR
            if (!Application.isPlaying && prefab != null)
            {
                towerInstance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                towerInstance.transform.position = buildPos;
                towerInstance.transform.rotation = Quaternion.identity;
                towerInstance.transform.SetParent(node.transform);
            }
            else if (prefab != null)
            {
                towerInstance = Instantiate(prefab, buildPos, Quaternion.identity, node.transform);
            }
            else
            {
                towerInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                towerInstance.transform.position = buildPos + new Vector3(0, 0.5f, 0);
                towerInstance.transform.SetParent(node.transform);
            }
#else
            if (prefab != null)
            {
                towerInstance = Instantiate(prefab, buildPos, Quaternion.identity, node.transform);
            }
            else
            {
                towerInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                towerInstance.transform.position = buildPos + new Vector3(0, 0.5f, 0);
                towerInstance.transform.SetParent(node.transform);
            }
#endif

            // Gán vật liệu chuẩn KayKit cho phần kiến trúc cơ sở trước khi gắn chi tiết nhân vật/ngọc
            if (kayKitMaterial != null)
            {
                foreach (Renderer r in towerInstance.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = kayKitMaterial;
                }
            }

            // Đảm bảo gắn script chuyên biệt cho từng loại Tháp theo towerTypeToBuild
            if (towerTypeToBuild == TowerType.Archer)
            {
                ArcherTower archerTower = towerInstance.GetComponent<ArcherTower>();
                if (archerTower == null)
                {
                    archerTower = towerInstance.AddComponent<ArcherTower>();
                }
            }
            else if (towerTypeToBuild == TowerType.Bomb)
            {
                CannonTower cannonTower = towerInstance.GetComponent<CannonTower>();
                if (cannonTower == null)
                {
                    cannonTower = towerInstance.AddComponent<CannonTower>();
                }
            }
            else if (towerTypeToBuild == TowerType.Mage)
            {
                MageTower mageTower = towerInstance.GetComponent<MageTower>();
                if (mageTower == null)
                {
                    mageTower = towerInstance.AddComponent<MageTower>();
                }
            }
            else if (towerTypeToBuild == TowerType.Frost)
            {
                FrostTower frostTower = towerInstance.GetComponent<FrostTower>();
                if (frostTower == null)
                {
                    frostTower = towerInstance.AddComponent<FrostTower>();
                }
            }

            node.SetTurret(towerInstance);

            Tower builtTower = towerInstance.GetComponent<Tower>();
            if (builtTower != null)
            {
                builtTower.InitializeBaseCost(cost);
            }

            Debug.Log($"[BuildManager] Đã xây thành công {towerTypeToBuild} tại {buildPos}! (-{cost} Vàng)");

            // Bỏ chọn sau khi xây xong
            SelectTowerToBuild(TowerType.None);
            return true;
        }
    }
}
