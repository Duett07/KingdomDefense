using UnityEngine;
using KingdomDefense.Enemy;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Towers
{
    /// <summary>
    /// Lớp cơ sở (Base Class) cho tất cả các loại Tháp phòng thủ trong Kingdom Defense.
    /// Thực thi giải thuật quét và chọn mục tiêu ưu tiên quái gần cổng thành nhất (Đoạn mã 4.1),
    /// tự động xoay nòng/mâm pháo bằng Quaternion.Lerp và bắn đạn/mũi tên theo chu kỳ fireRate.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [Header("--- THIẾT LẬP TẤN CÔNG ---")]
        [Tooltip("Bán kính tầm bắn của tháp")]
        [SerializeField] protected float range = 6.5f;

        [Tooltip("Tốc độ bắn (số phát mỗi giây)")]
        [SerializeField] protected float fireRate = 1.2f;

        [Tooltip("Sát thương mỗi phát bắn")]
        [SerializeField] protected float damage = 35f;

        [Header("--- BỘ PHẬN & HIỂN THỊ ---")]
        [Tooltip("Bộ phận mâm xoay / đỉnh tháp hướng về mục tiêu")]
        [SerializeField] protected Transform partToRotate;

        [Tooltip("Vị trí xuất phát của mũi tên / viên đạn")]
        [SerializeField] protected Transform firePoint;

        [Tooltip("Tốc độ xoay ngắm bắn")]
        [SerializeField] protected float turnSpeed = 10f;

        [Header("--- ĐẠN & MŨI TÊN ---")]
        [SerializeField] protected GameObject projectilePrefab;

        protected Transform target;
        protected float fireCountdown = 0f;

        public float Range => range;
        public float Damage => damage;
        public Transform Target => target;

        protected virtual void Start()
        {
            // Thiết lập chu kỳ dò tìm mục tiêu 4 lần mỗi giây để tối ưu hiệu năng
            InvokeRepeating(nameof(UpdateTarget), 0f, 0.25f);
            EnsureReferences();
        }

        protected virtual void Update()
        {
            if (target == null) return;

            // Xoay mâm pháo hướng về mục tiêu bằng Quaternion.Lerp
            LockOnTarget();

            // Đếm ngược và khai hỏa
            if (fireCountdown <= 0f)
            {
                Shoot();
                fireCountdown = 1f / fireRate;
            }

            fireCountdown -= Time.deltaTime;
        }

        /// <summary>
        /// Giải thuật quét và chọn mục tiêu của tháp (Đoạn mã 4.1 trong tài liệu thiết kế).
        /// Ưu tiên quái vật đã đi xa nhất trên đường mòn (gần Cổng thành hoàng gia nhất).
        /// </summary>
        protected virtual void UpdateTarget()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float shortestDistanceToExit = Mathf.Infinity;
            Transform nearestEnemy = null;

            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;

                float distanceToTower = Vector3.Distance(transform.position, enemy.transform.position);
                if (distanceToTower <= range)
                {
                    EnemyMovement em = enemy.GetComponent<EnemyMovement>();
                    float distToGoal = (em != null) ? em.GetDistanceToGoal() : distanceToTower;

                    if (distToGoal < shortestDistanceToExit)
                    {
                        shortestDistanceToExit = distToGoal;
                        nearestEnemy = enemy.transform;
                    }
                }
            }

            target = nearestEnemy;
        }

        /// <summary>
        /// Xoay mâm pháo / người bắn hướng về mục tiêu bằng Quaternion.Lerp mượt mà.
        /// </summary>
        protected virtual void LockOnTarget()
        {
            if (partToRotate == null || target == null) return;

            Vector3 dir = target.position - partToRotate.position;
            dir.y = 0f; // Khóa trục Y để xoay phẳng theo phương ngang

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(dir);
                Vector3 rotation = Quaternion.Lerp(partToRotate.rotation, lookRotation, Time.deltaTime * turnSpeed).eulerAngles;
                partToRotate.rotation = Quaternion.Euler(0f, rotation.y, 0f);
            }
        }

        /// <summary>
        /// Bắn đạn / mũi tên bám mục tiêu.
        /// </summary>
        protected virtual void Shoot()
        {
            if (projectilePrefab == null || target == null) return;

            Vector3 spawnPosition = (firePoint != null) ? firePoint.position : (transform.position + Vector3.up * 2.2f);
            GameObject projObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

            // Gán vật liệu chuẩn KayKit cho mũi tên
            Material mat = null;
#if UNITY_EDITOR
            mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_KingdomDefense/Materials/M_KayKit_Medieval.mat");
#endif
            if (mat != null)
            {
                foreach (var r in projObj.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = mat;
                }
            }

            Arrow arrow = projObj.GetComponent<Arrow>();
            if (arrow == null) arrow = projObj.AddComponent<Arrow>();

            arrow.Seek(target, damage);
        }

        protected virtual void EnsureReferences()
        {
            // Tự tìm kiếm hoặc khởi tạo firePoint ở đỉnh tháp
            if (firePoint == null)
            {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform);
                fp.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                firePoint = fp.transform;
            }

            // Nếu chưa gán partToRotate, lấy phần thân tháp hoặc chính transform
            if (partToRotate == null)
            {
                partToRotate = transform;
            }

            // Tự nạp Prefab mũi tên nếu trống
            if (projectilePrefab == null)
            {
#if UNITY_EDITOR
                projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/arrow.fbx");
#endif
                if (projectilePrefab == null)
                {
                    projectilePrefab = CreateFallbackArrowPrefab();
                }
            }
        }

        private GameObject CreateFallbackArrowPrefab()
        {
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arrow.name = "Arrow_Template";
            arrow.transform.localScale = new Vector3(0.08f, 0.4f, 0.08f);
            Destroy(arrow.GetComponent<Collider>());

            Renderer r = arrow.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                Material mat = new Material(s);
                mat.color = new Color(0.85f, 0.7f, 0.2f);
                r.sharedMaterial = mat;
            }

            arrow.AddComponent<Arrow>();
            arrow.SetActive(false);
            return arrow;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
