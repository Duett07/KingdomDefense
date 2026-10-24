using System.Collections.Generic;
using UnityEngine;

namespace KingdomDefense.Core
{
    /// <summary>
    /// Giao diện cho các đối tượng trong Object Pool cần reset trạng thái khi tái sử dụng.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }

    /// <summary>
    /// Gắn lên mọi GameObject được quản lý bởi ObjectPool để truy xuất nguồn gốc Pool và hỗ trợ Despawn nhanh.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        [SerializeField] private string poolTag;
        public string PoolTag => poolTag;

        public void SetPoolTag(string tag)
        {
            poolTag = tag;
        }

        public void Despawn()
        {
            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.ReturnToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Hệ thống Quản lý Tái sử dụng Đối tượng (Generic Multi-Prefab Object Pool).
    /// Đáp ứng tiêu chuẩn Đoạn mã 4.2 trong tài liệu thiết kế Kingdom Defense:
    /// - Cấp phát trước 50 viên đạn và tái sử dụng bằng SetActive(true/false).
    /// - Hỗ trợ đa dạng nhiều loại đạn/hiệu ứng (Tên bắn, Đạn pháo, Tia sét, Băng, VFX nổ).
    /// - Cơ chế Auto-Expand tự động nở rộng khi vượt quá kích thước ban đầu, triệt tiêu sụt giảm FPS.
    /// - Gom nhóm ngăn nắp theo từng Container con trong Hierarchy.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [System.Serializable]
        public class PoolConfig
        {
            [Tooltip("Tên định danh của Pool (VD: Arrow, CannonBall, MageBolt)")]
            public string tag;
            [Tooltip("Prefab gốc của đối tượng cần cấp phát trước")]
            public GameObject prefab;
            [Tooltip("Số lượng cấp phát sẵn ban đầu (Mặc định: 50 viên)")]
            public int initialSize = 50;
            [Tooltip("Tự động tạo thêm khi nhu cầu bắn vượt quá dung lượng hiện tại")]
            public bool autoExpand = true;
        }

        [Header("--- CẤU HÌNH CÁC BỘ ĐỆM POOL ---")]
        [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();

        private readonly Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, PoolConfig> _configDictionary = new Dictionary<string, PoolConfig>();
        private readonly Dictionary<GameObject, string> _prefabToTagMap = new Dictionary<GameObject, string>();
        private readonly Dictionary<string, Transform> _poolContainers = new Dictionary<string, Transform>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializePools();
        }

        private void InitializePools()
        {
            // Tự nạp cấu hình mặc định cho đạn mũi tên và đạn pháo nếu danh sách trống
            EnsureDefaultProjectilesPoolConfig();

            foreach (PoolConfig config in poolConfigs)
            {
                if (config == null || config.prefab == null || string.IsNullOrEmpty(config.tag))
                {
                    continue;
                }

                RegisterAndPrewarmPool(config);
            }
        }

        private void EnsureDefaultProjectilesPoolConfig()
        {
            if (poolConfigs == null) poolConfigs = new List<PoolConfig>();

            // 1. Nạp sẵn 50 viên đạn Mũi tên (Arrow)
            bool hasArrowConfig = false;
            foreach (var cfg in poolConfigs)
            {
                if (cfg != null && (cfg.tag == "Arrow" || (cfg.prefab != null && cfg.prefab.name.Contains("Arrow"))))
                {
                    hasArrowConfig = true;
                    break;
                }
            }

            if (!hasArrowConfig)
            {
                GameObject arrowPrefab = null;
#if UNITY_EDITOR
                arrowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Prefabs/Arrow.prefab") ??
                              UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/arrow.fbx");
#endif
                if (arrowPrefab != null)
                {
                    poolConfigs.Add(new PoolConfig
                    {
                        tag = "Arrow",
                        prefab = arrowPrefab,
                        initialSize = 50,
                        autoExpand = true
                    });
                }
            }

            // 2. Nạp sẵn 25 quả Đạn Pháo (CannonBall)
            bool hasCannonConfig = false;
            foreach (var cfg in poolConfigs)
            {
                if (cfg != null && (cfg.tag == "CannonBall" || (cfg.prefab != null && cfg.prefab.name.Contains("CannonBall"))))
                {
                    hasCannonConfig = true;
                    break;
                }
            }

            if (!hasCannonConfig)
            {
                GameObject cannonPrefab = null;
#if UNITY_EDITOR
                cannonPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Prefabs/CannonBall.prefab");
#endif
                if (cannonPrefab == null)
                {
                    cannonPrefab = new GameObject("CannonBall_Default");
                    cannonPrefab.AddComponent<KingdomDefense.Towers.CannonBall>();
                    cannonPrefab.SetActive(false);
                }

                poolConfigs.Add(new PoolConfig
                {
                    tag = "CannonBall",
                    prefab = cannonPrefab,
                    initialSize = 25,
                    autoExpand = true
                });
            }
        }

        /// <summary>
        /// Đăng ký và cấp phát trước một Pool mới.
        /// </summary>
        public void RegisterAndPrewarmPool(PoolConfig config)
        {
            if (_poolDictionary.ContainsKey(config.tag)) return;

            _configDictionary[config.tag] = config;
            _poolDictionary[config.tag] = new Queue<GameObject>();

            if (config.prefab != null)
            {
                _prefabToTagMap[config.prefab] = config.tag;
            }

            // Tạo Container gom nhóm trong Hierarchy để giữ không gian sạch sẽ
            GameObject containerObj = new GameObject($"--- Pool_{config.tag} ---");
            containerObj.transform.SetParent(transform);
            _poolContainers[config.tag] = containerObj.transform;

            // Cấp phát trước theo initialSize (chuẩn 50 đối tượng)
            for (int i = 0; i < config.initialSize; i++)
            {
                GameObject obj = CreateNewPoolInstance(config);
                _poolDictionary[config.tag].Enqueue(obj);
            }
        }

        private GameObject CreateNewPoolInstance(PoolConfig config)
        {
            GameObject obj = Instantiate(config.prefab, _poolContainers[config.tag]);
            obj.name = $"{config.tag}_Pooled";
            obj.SetActive(false);

            PooledObject po = obj.GetComponent<PooledObject>();
            if (po == null) po = obj.AddComponent<PooledObject>();
            po.SetPoolTag(config.tag);

            return obj;
        }

        /// <summary>
        /// Xuất một đối tượng từ Pool thông qua tham chiếu Prefab.
        /// Tự động đăng ký Pool mới nếu Prefab chưa được cấu hình trước.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!_prefabToTagMap.TryGetValue(prefab, out string tag))
            {
                tag = prefab.name;
                PoolConfig newConfig = new PoolConfig
                {
                    tag = tag,
                    prefab = prefab,
                    initialSize = 25,
                    autoExpand = true
                };
                RegisterAndPrewarmPool(newConfig);
            }

            return Spawn(tag, position, rotation);
        }

        /// <summary>
        /// Xuất một đối tượng từ Pool thông qua Tag.
        /// </summary>
        public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.TryGetValue(tag, out Queue<GameObject> poolQueue))
            {
                Debug.LogWarning($"[ObjectPool] Không tìm thấy Pool nào có tag '{tag}'!");
                return null;
            }

            GameObject obj = null;

            // Lấy đối tượng khả dụng trong queue
            while (poolQueue.Count > 0 && obj == null)
            {
                obj = poolQueue.Dequeue();
            }

            // Nếu hết đạn trong queue, kiểm tra cơ chế Auto-Expand
            if (obj == null)
            {
                if (_configDictionary.TryGetValue(tag, out PoolConfig config) && config.autoExpand)
                {
                    obj = CreateNewPoolInstance(config);
                }
                else
                {
                    Debug.LogWarning($"[ObjectPool] Pool '{tag}' đã cạn và không bật Auto-Expand!");
                    return null;
                }
            }

            // Đặt vị trí, góc xoay và kích hoạt
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            // Kích hoạt thông báo IPoolable nếu có
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawnFromPool();
            }

            return obj;
        }

        /// <summary>
        /// Hỗ trợ Generic để lấy trực tiếp Component (VD: Spawn<Arrow>(prefab, pos, rot)).
        /// </summary>
        public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            GameObject obj = Spawn(prefab, position, rotation);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        /// <summary>
        /// Thu hồi đối tượng và trả về Pool để tái sử dụng.
        /// </summary>
        public void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;

            string tag = null;
            PooledObject po = obj.GetComponent<PooledObject>();
            if (po != null && !string.IsNullOrEmpty(po.PoolTag))
            {
                tag = po.PoolTag;
            }

            if (string.IsNullOrEmpty(tag) || !_poolDictionary.TryGetValue(tag, out Queue<GameObject> poolQueue))
            {
                // Nếu đối tượng không thuộc bất kỳ Pool nào, fallback về Destroy
                Destroy(obj);
                return;
            }

            // Gọi hàm thu hồi IPoolable trước khi tắt
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnReturnToPool();
            }

            obj.SetActive(false);

            // Đảm bảo đối tượng nằm gọn trong Container của Pool
            if (_poolContainers.TryGetValue(tag, out Transform container) && container != null)
            {
                obj.transform.SetParent(container);
            }

            poolQueue.Enqueue(obj);
        }

        /// <summary>
        /// Lấy số lượng đối tượng đang sẵn sàng trong Pool.
        /// </summary>
        public int GetAvailableCount(string tag)
        {
            return _poolDictionary.TryGetValue(tag, out var queue) ? queue.Count : 0;
        }
    }
}
