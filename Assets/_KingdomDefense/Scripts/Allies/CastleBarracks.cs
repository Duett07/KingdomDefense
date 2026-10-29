using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Allies
{
    /// <summary>
    /// Doanh trại Cổng Nam / Thành Hoàng Gia (Castle Barracks):
    /// - Quản lý đội hình 3 Hiệp sĩ Hoàng gia (Castle Knight Squad).
    /// - Xuất quân ra các chốt chặn hiểm trở đón đầu quái vật đi bộ.
    /// - Khi một hiệp sĩ hy sinh, tự động đếm ngược 25 giây và xuất quân bổ sung.
    /// - Tự động tải và lắp ghép mô hình 3D Knight.fbx, Kiếm và Khiên hiệp sĩ chuẩn phong cách Kingdom Rush.
    /// </summary>
    public class CastleBarracks : MonoBehaviour
    {
        public static CastleBarracks Instance { get; private set; }

        [Header("--- THIẾT LẬP ĐỘI HÌNH HIỆP SĨ ---")]
        [SerializeField] private int squadSize = 3;
        [SerializeField] private float respawnCooldown = 25f;
        [SerializeField] private Vector3 rallyPoint = new Vector3(1.35f, 0f, -5.5f);
        [SerializeField] private Vector3 spawnExitPoint = new Vector3(1.35f, 0f, -7.5f);

        public float RespawnCooldown => respawnCooldown;

        [Header("--- TÀI NGUYÊN 3D HIỆP SĨ ---")]
        [SerializeField] private GameObject knightPrefab;
        [SerializeField] private GameObject swordPrefab;
        [SerializeField] private GameObject shieldPrefab;
        [SerializeField] private Material knightMaterial;

        private readonly List<CastleKnight> _activeKnights = new List<CastleKnight>();
        private readonly Vector3[] _formationOffsets = new Vector3[]
        {
            new Vector3(0f, 0f, 0.5f),    // Tiên phong (Mũi giáo)
            new Vector3(-0.85f, 0f, -0.4f),// Cánh tả
            new Vector3(0.85f, 0f, -0.4f)  // Cánh hữu
        };

        public int SquadSize => squadSize;
        public int AliveKnightCount => _activeKnights.Count;
        public IReadOnlyList<CastleKnight> ActiveKnights => _activeKnights;
        public Vector3 RallyPoint => rallyPoint;

        public void SetRallyPoint(Vector3 newRallyPoint)
        {
            rallyPoint = newRallyPoint;
            for (int i = 0; i < _activeKnights.Count; i++)
            {
                if (_activeKnights[i] != null)
                {
                    Vector3 offset = (i < _formationOffsets.Length) ? _formationOffsets[i] : Vector3.zero;
                    _activeKnights[i].SetRallyPoint(rallyPoint + offset);
                }
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            LoadDefaultAssets();
        }

        private void Start()
        {
            // Không tự động xuất lính: Lính chỉ được triệu hồi khi người chơi bấm nút trên Bottom Dock
        }

        /// <summary>
        /// Người chơi chủ động triệu hồi 1 Hiệp sĩ Hoàng gia xuất trận từ Cổng Thành Nam qua nút trên Bottom Dock.
        /// </summary>
        public CastleKnight SpawnPlayerKnight()
        {
            CastleKnight knight = SpawnSingleKnight(_activeKnights.Count);
            if (knight != null)
            {
                Debug.Log($"<color=#3498DB>[CastleBarracks]</color> Đã xuất 1 Hiệp sĩ Hoàng gia '{knight.name}' hành quân ngược đường mòn về phía Thành Quái!");
            }
            return knight;
        }

        private void LoadDefaultAssets()
        {
            // 1. Ưu tiên nạp từ Resources để tương thích 100% trên thiết bị Mobile và bản build độc lập
            if (knightPrefab == null)
                knightPrefab = Resources.Load<GameObject>("Allies/Knight") ?? Resources.Load<GameObject>("Knight");

            if (knightMaterial == null)
                knightMaterial = Resources.Load<Material>("Materials/M_Knight") ?? Resources.Load<Material>("M_Knight");

            if (swordPrefab == null)
                swordPrefab = Resources.Load<GameObject>("Weapons/sword_1handed") ?? Resources.Load<GameObject>("sword_1handed");

            if (shieldPrefab == null)
                shieldPrefab = Resources.Load<GameObject>("Weapons/shield_badge_color") ?? Resources.Load<GameObject>("shield_badge_color");

#if UNITY_EDITOR
            if (knightPrefab == null)
                knightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Models/Characters/Adventures/Knight.fbx");

            if (knightMaterial == null)
                knightMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_KingdomDefense/Materials/M_Knight.mat");

            if (swordPrefab == null)
                swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/sword_1handed.fbx");

            if (shieldPrefab == null)
                shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/shield_badge_color.fbx");
#endif
        }

        /// <summary>
        /// Xuất trọn vẹn đội hình hiệp sĩ ra cửa thành.
        /// </summary>
        public void SpawnSquad()
        {
            for (int i = _activeKnights.Count; i < squadSize; i++)
            {
                SpawnSingleKnight(i);
            }
        }

        /// <summary>
        /// Khởi tạo một Hiệp sĩ hoàng gia, trang bị kiếm khiên và chỉ định vị trí đóng quân trong đội hình.
        /// </summary>
        public CastleKnight SpawnSingleKnight(int squadSlotIndex)
        {
            Vector3 spawnPos = spawnExitPoint + new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f));
            GameObject knightObj;

            if (knightPrefab != null)
            {
                knightObj = Instantiate(knightPrefab, spawnPos, Quaternion.identity, transform);
            }
            else
            {
                knightObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                knightObj.name = "Knight_Fallback";
                knightObj.transform.position = spawnPos;
                knightObj.transform.SetParent(transform);
            }

            knightObj.name = $"CastleKnight_{squadSlotIndex + 1}_{Time.frameCount}";
            knightObj.transform.localScale = Vector3.one * 0.55f;

            // Áp dụng Material
            if (knightMaterial != null)
            {
                Renderer[] rends = knightObj.GetComponentsInChildren<Renderer>();
                for (int r = 0; r < rends.Length; r++)
                {
                    rends[r].sharedMaterial = knightMaterial;
                }
            }

            // Trang bị Kiếm và Khiên
            EquipWeapons(knightObj);

            // Gắn component CastleKnight điều khiển hành quân & đối kháng
            CastleKnight knight = knightObj.GetComponent<CastleKnight>();
            if (knight == null) knight = knightObj.AddComponent<CastleKnight>();

            Vector3 offset = (squadSlotIndex < _formationOffsets.Length) ? _formationOffsets[squadSlotIndex] : Vector3.zero;
            knight.SetRallyPoint(rallyPoint + offset);

            knight.OnKnightDied += HandleKnightDied;
            _activeKnights.Add(knight);

            return knight;
        }

        private void EquipWeapons(GameObject character)
        {
            // Cầm kiếm tay phải
            if (swordPrefab != null)
            {
                Transform rightArm = FindDeepChild(character.transform, "lowerarm.r")
                                  ?? FindDeepChild(character.transform, "Knight_ArmRight")
                                  ?? FindDeepChild(character.transform, "hand.r");

                Transform parent = rightArm != null ? rightArm : character.transform;
                GameObject sword = Instantiate(swordPrefab, parent);
                sword.name = "Knight_Sword";

                if (rightArm != null)
                {
                    sword.transform.localPosition = new Vector3(0.05f, -0.22f, 0.18f);
                    sword.transform.localRotation = Quaternion.Euler(75f, 15f, -20f);
                    sword.transform.localScale = Vector3.one * 0.95f;
                }
                else
                {
                    sword.transform.localPosition = new Vector3(0.25f, 0.45f, 0.25f);
                    sword.transform.localRotation = Quaternion.Euler(60f, 20f, 0f);
                    sword.transform.localScale = Vector3.one * 0.95f;
                }

                if (knightMaterial != null)
                {
                    Renderer[] rends = sword.GetComponentsInChildren<Renderer>();
                    for (int r = 0; r < rends.Length; r++) rends[r].sharedMaterial = knightMaterial;
                }
            }

            // Cầm khiên tay trái
            if (shieldPrefab != null)
            {
                Transform leftArm = FindDeepChild(character.transform, "lowerarm.l")
                                 ?? FindDeepChild(character.transform, "Knight_ArmLeft")
                                 ?? FindDeepChild(character.transform, "hand.l");

                Transform parent = leftArm != null ? leftArm : character.transform;
                GameObject shield = Instantiate(shieldPrefab, parent);
                shield.name = "Knight_Shield";

                if (leftArm != null)
                {
                    shield.transform.localPosition = new Vector3(-0.06f, -0.12f, 0.08f);
                    shield.transform.localRotation = Quaternion.Euler(-15f, 90f, 0f);
                    shield.transform.localScale = Vector3.one * 0.95f;
                }
                else
                {
                    shield.transform.localPosition = new Vector3(-0.25f, 0.45f, 0.15f);
                    shield.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    shield.transform.localScale = Vector3.one * 0.95f;
                }

                if (knightMaterial != null)
                {
                    Renderer[] rends = shield.GetComponentsInChildren<Renderer>();
                    for (int r = 0; r < rends.Length; r++) rends[r].sharedMaterial = knightMaterial;
                }
            }
        }

        private void HandleKnightDied(CastleKnight knight)
        {
            if (knight != null)
            {
                knight.OnKnightDied -= HandleKnightDied;
                _activeKnights.Remove(knight);
            }
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return child;

                Transform result = FindDeepChild(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rallyPoint, 1.0f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(spawnExitPoint, rallyPoint);
        }
    }
}
