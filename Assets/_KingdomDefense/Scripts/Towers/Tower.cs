using UnityEngine;
using KingdomDefense.Enemy;
using KingdomDefense.Build;
using KingdomDefense.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Towers
{
    /// <summary>
    /// Lớp cơ sở (Base Class) cho tất cả các loại Tháp phòng thủ trong Kingdom Defense.
    /// Quản lý cấp độ (Level 1 -> 2 -> 3), nâng cấp, bán hoàn 70% vàng,
    /// quét mục tiêu ưu tiên quái gần cổng thành nhất, và vẽ vòng tròn tầm bắn tương tác.
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [Header("--- THIẾT LẬP CẤP ĐỘ & KINH TẾ ---")]
        [SerializeField] protected string towerDisplayName = "Tháp Phòng Thủ";
        [SerializeField] protected int level = 1;
        [SerializeField] protected int baseCost = 100;
        [SerializeField] protected int upgradeCostLv2 = 70;
        [SerializeField] protected int upgradeCostLv3 = 120;
        [SerializeField] protected int totalInvestedGold = 100;

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

        [Header("--- THIẾT LẬP MỤC TIÊU & TRẠNG THÁI ---")]
        [Tooltip("Tháp có thể bắn quái bay (Dark Bat) không")]
        [SerializeField] protected bool canTargetFlying = true;

        [Header("--- ĐẠN & MŨI TÊN ---")]
        [SerializeField] protected GameObject projectilePrefab;

        protected Transform target;
        protected float fireCountdown = 0f;
        protected float stunDurationRemaining = 0f;
        protected bool isStunned = false;
        protected GameObject stunIndicatorFX;
        private LineRenderer _rangeLine;
        private GameObject _tierVisualBadge;

        public Node ParentNode { get; set; }
        public string TowerDisplayName => towerDisplayName;
        public int Level => level;
        public int MaxLevel => 3;
        public bool CanUpgrade => level < MaxLevel;
        public int TotalInvestedGold => totalInvestedGold;
        public int NextUpgradeCost => level == 1 ? upgradeCostLv2 : (level == 2 ? upgradeCostLv3 : 0);
        public int SellAmount => Mathf.RoundToInt(totalInvestedGold * 0.7f);

        public float Range => range;
        public float Damage => damage;
        public float FireRate => fireRate;
        public Transform Target => target;
        public bool CanTargetFlying => canTargetFlying;
        public bool IsStunned => isStunned;

        protected virtual void Start()
        {
            // Thiết lập chu kỳ dò tìm mục tiêu 4 lần mỗi giây để tối ưu hiệu năng
            InvokeRepeating(nameof(UpdateTarget), 0f, 0.25f);
            EnsureReferences();
        }

        protected virtual void Update()
        {
            // Kiểm tra trạng thái bị choáng (Stun) từ Boss Orcus
            if (isStunned)
            {
                stunDurationRemaining -= Time.deltaTime;
                if (stunDurationRemaining <= 0f)
                {
                    isStunned = false;
                    ShowStunVisual(false);
                }
                else
                {
                    if (stunIndicatorFX != null)
                    {
                        stunIndicatorFX.transform.Rotate(Vector3.up, 180f * Time.deltaTime);
                    }
                    return; // Đang bị choáng: không xoay và không thể bắn
                }
            }

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
        /// Làm choáng tháp trong duration giây (kỹ năng dậm đất của Boss Orcus).
        /// </summary>
        public virtual void Stun(float duration)
        {
            stunDurationRemaining = Mathf.Max(stunDurationRemaining, duration);
            if (!isStunned)
            {
                isStunned = true;
                ShowStunVisual(true);
            }
        }

        protected virtual void ShowStunVisual(bool show)
        {
            if (show)
            {
                if (stunIndicatorFX == null)
                {
                    stunIndicatorFX = new GameObject("Tower_Stun_FX");
                    stunIndicatorFX.transform.SetParent(transform, false);
                    stunIndicatorFX.transform.localPosition = new Vector3(0f, 3.2f, 0f);

                    for (int i = 0; i < 3; i++)
                    {
                        GameObject star = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        star.name = $"Stun_Star_{i}";
                        star.transform.SetParent(stunIndicatorFX.transform, false);
                        float angle = i * (Mathf.PI * 2f / 3f);
                        star.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.45f, 0f, Mathf.Sin(angle) * 0.45f);
                        star.transform.localScale = Vector3.one * 0.12f;
                        Destroy(star.GetComponent<Collider>());

                        Renderer r = star.GetComponent<Renderer>();
                        if (r != null)
                        {
                            Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                            Material mat = new Material(s);
                            mat.color = new Color(1f, 0.9f, 0.2f);
                            r.sharedMaterial = mat;
                        }
                    }
                }
                stunIndicatorFX.SetActive(true);
            }
            else
            {
                if (stunIndicatorFX != null) stunIndicatorFX.SetActive(false);
            }
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

                Enemy.Enemy e = enemy.GetComponent<Enemy.Enemy>();
                if (e != null)
                {
                    if (e.IsDead) continue;
                    if (e.IsFlying && !canTargetFlying) continue; // Bỏ qua quái bay nếu tháp không thể bắn quái bay
                }

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
        /// Bắn đạn / mũi tên bám mục tiêu thông qua ObjectPool để tối ưu hiệu năng và tái sử dụng bộ đệm.
        /// </summary>
        protected virtual void Shoot()
        {
            if (projectilePrefab == null || target == null) return;

            Vector3 spawnPosition = (firePoint != null) ? firePoint.position : (transform.position + Vector3.up * 2.2f);
            GameObject projObj;

            if (KingdomDefense.Core.ObjectPool.Instance != null)
            {
                projObj = KingdomDefense.Core.ObjectPool.Instance.Spawn(projectilePrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                projObj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            }

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

            // Tự nạp Prefab mũi tên nếu trống (ưu tiên Prefab đã có component Arrow)
            if (projectilePrefab == null)
            {
#if UNITY_EDITOR
                projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Prefabs/Arrow.prefab") ??
                                   AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Models/Characters/Weapons/arrow.fbx");
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

        public void InitializeBaseCost(int cost)
        {
            baseCost = cost;
            totalInvestedGold = cost;
        }

        public virtual bool TryUpgrade()
        {
            if (!CanUpgrade) return false;
            int cost = NextUpgradeCost;
            if (GameHUD.Instance != null && !GameHUD.Instance.SpendGold(cost))
            {
                Debug.LogWarning($"[Tower] Không đủ vàng! Cần {cost} vàng để nâng cấp.");
                return false;
            }

            totalInvestedGold += cost;
            level++;

            ApplyLevelStats();
            UpdateVisualsForLevel();
            SpawnUpgradeVFX();

            if (_rangeLine != null && _rangeLine.enabled)
            {
                ShowRangeIndicator(true);
            }

            Debug.Log($"<color=#4CAF50>[Tower] Đã nâng cấp {towerDisplayName} lên CẤP {level}! (Đầu tư lũy kế: {totalInvestedGold} Vàng)</color>");
            return true;
        }

        public virtual void ApplyLevelStats()
        {
            // Các lớp con sẽ override để cập nhật range, damage, fireRate chuẩn theo Bảng 2.2
        }

        public virtual void UpdateVisualsForLevel()
        {
            if (_tierVisualBadge != null) Destroy(_tierVisualBadge);

            _tierVisualBadge = new GameObject("Tier_Visual_Badge");
            _tierVisualBadge.transform.SetParent(transform, false);
            _tierVisualBadge.transform.localPosition = new Vector3(0f, 2.7f, 0f);

            if (level == 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    GameObject star = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    star.transform.SetParent(_tierVisualBadge.transform, false);
                    star.transform.localPosition = new Vector3((i == 0 ? -0.22f : 0.22f), 0f, 0f);
                    star.transform.localScale = new Vector3(0.18f, 0.18f, 0.08f);
                    Destroy(star.GetComponent<Collider>());

                    Renderer r = star.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                        Material m = new Material(s) { color = new Color(0.8f, 0.88f, 0.95f) };
                        r.sharedMaterial = m;
                    }
                }
            }
            else if (level == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    GameObject star = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    star.transform.SetParent(_tierVisualBadge.transform, false);
                    star.transform.localPosition = new Vector3((i - 1) * 0.25f, 0.05f * Mathf.Abs(i - 1), 0f);
                    star.transform.localScale = new Vector3(0.22f, 0.22f, 0.1f);
                    Destroy(star.GetComponent<Collider>());

                    Renderer r = star.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                        Material m = new Material(s) { color = new Color(1.0f, 0.85f, 0.15f) };
                        r.sharedMaterial = m;
                    }
                }

                GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                aura.transform.SetParent(_tierVisualBadge.transform, false);
                aura.transform.localPosition = new Vector3(0f, -0.1f, 0f);
                aura.transform.localScale = new Vector3(1.1f, 0.02f, 1.1f);
                Destroy(aura.GetComponent<Collider>());
                Renderer ar = aura.GetComponent<Renderer>();
                if (ar != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material am = new Material(s) { color = new Color(1f, 0.9f, 0.3f, 0.5f) };
                    ar.sharedMaterial = am;
                }
            }
        }

        public virtual void Sell()
        {
            int refund = SellAmount;
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.AddGold(refund);
            }

            Debug.Log($"<color=#FF9800>[Tower] Đã bán {towerDisplayName} Cấp {level}! (+{refund} Vàng, hoàn trả 70% của {totalInvestedGold} Vàng)</color>");

            SpawnSellVFX();

            if (ParentNode != null)
            {
                ParentNode.ClearTurret();
            }

            Destroy(gameObject);
        }

        public void ShowRangeIndicator(bool show)
        {
            if (!show)
            {
                if (_rangeLine != null) _rangeLine.enabled = false;
                return;
            }

            if (_rangeLine == null)
            {
                GameObject rangeObj = new GameObject("Range_Indicator_Line");
                rangeObj.transform.SetParent(transform, false);
                rangeObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);

                _rangeLine = rangeObj.AddComponent<LineRenderer>();
                _rangeLine.useWorldSpace = false;
                _rangeLine.loop = true;
                _rangeLine.startWidth = 0.08f;
                _rangeLine.endWidth = 0.08f;

                Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                Material m = new Material(s);
                m.color = new Color(0.2f, 0.95f, 0.45f, 0.9f);
                _rangeLine.sharedMaterial = m;
            }

            int segments = 64;
            _rangeLine.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * range, 0.05f, Mathf.Sin(angle) * range);
                _rangeLine.SetPosition(i, pos);
            }

            _rangeLine.enabled = true;
        }

        protected virtual void OnMouseDown()
        {
            if (BuildManager.Instance != null && BuildManager.Instance.SelectedTowerType != TowerType.None)
            {
                return;
            }

            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (TowerUI.Instance != null)
            {
                TowerUI.Instance.SelectTower(this);
            }
        }

        protected void SpawnUpgradeVFX()
        {
            GameObject vfx = new GameObject("VFX_Tower_Upgrade_Burst");
            vfx.transform.position = transform.position + new Vector3(0, 1.2f, 0);

            for (int i = 0; i < 12; i++)
            {
                GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.transform.SetParent(vfx.transform);
                float angle = i * Mathf.PI * 2f / 12f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Random.Range(0.6f, 1.4f), Mathf.Sin(angle)).normalized;
                spark.transform.localPosition = dir * 0.4f;
                spark.transform.localScale = Vector3.one * 0.2f;
                Destroy(spark.GetComponent<Collider>());

                Renderer r = spark.GetComponent<Renderer>();
                if (r != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material m = new Material(s) { color = new Color(1.0f, 0.85f, 0.2f, 0.95f) };
                    r.sharedMaterial = m;
                }
            }

            Destroy(vfx, 0.6f);
        }

        protected void SpawnSellVFX()
        {
            GameObject vfx = new GameObject("VFX_Tower_Sell_Coins");
            vfx.transform.position = transform.position + new Vector3(0, 0.8f, 0);

            for (int i = 0; i < 8; i++)
            {
                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.transform.SetParent(vfx.transform);
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 1.0f, Mathf.Sin(angle)).normalized;
                coin.transform.localPosition = dir * 0.35f;
                coin.transform.localScale = new Vector3(0.2f, 0.04f, 0.2f);
                Destroy(coin.GetComponent<Collider>());

                Renderer r = coin.GetComponent<Renderer>();
                if (r != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material m = new Material(s) { color = new Color(0.95f, 0.75f, 0.1f) };
                    r.sharedMaterial = m;
                }
            }

            Destroy(vfx, 0.5f);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
