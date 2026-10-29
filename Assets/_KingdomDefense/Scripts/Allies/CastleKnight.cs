using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.Core;
using KingdomDefense.Enemy;
using KingdomDefense.Audio;

namespace KingdomDefense.Allies
{
    /// <summary>
    /// Hiệp sĩ Hoàng gia xuất trận từ Cổng Thành Nam (Castle Knight Defender):
    /// - Kế thừa IDamageable, sở hữu 250 HP, 30% Giáp vật lý (Armor = 0.30f).
    /// - Hành quân chốt chặn các khúc cua thung lũng, đón đầu và đấu kiếm cận chiến với quái vật đi bộ.
    /// - Giữ chân quái vật (IsBlocked = true), câu giờ chống Boss Orcus và bảo vệ tường thành.
    /// - Tự động hiển thị thanh máu 3D trên đầu và giải phóng quái khi hy sinh.
    /// </summary>
    public class CastleKnight : MonoBehaviour, IDamageable
    {
        [Header("--- CHỈ SỐ CHIẾN BINH HOÀNG GIA ---")]
        [SerializeField] private float maxHealth = 250f;
        [SerializeField] private float armor = 0.30f;
        [SerializeField] private float attackDamage = 20f;
        [SerializeField] private float attackRate = 1.0f;
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float engageDistance = 2.2f;

        [Header("--- VỊ TRÍ ĐÓNG QUÂN (RALLY POINT) ---")]
        [SerializeField] private Vector3 rallyPoint = new Vector3(1.35f, 0f, -4.5f);

        public static readonly List<CastleKnight> ActiveKnights = new List<CastleKnight>();

        private float _currentHealth;
        private bool _isDead = false;
        private Enemy.Enemy _currentTargetEnemy;
        private float _lastAttackTime = 0f;
        private float _walkCycle = 0f;

        // Thanh máu 3D Billboard tối ưu hiệu năng
        private Transform _healthBarContainer;
        private Transform _healthBarFill;

        public event Action<CastleKnight> OnKnightDied;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;
        public float Armor => armor;
        public float AttackDamage => attackDamage;
        public Enemy.Enemy TargetEnemy => _currentTargetEnemy;

        public void SetRallyPoint(Vector3 point)
        {
            rallyPoint = point;
        }

        public void ConfigureStats(float hp, float arm, float dmg, float atkRate = 1.0f)
        {
            maxHealth = hp;
            _currentHealth = hp;
            armor = Mathf.Clamp(arm, 0f, 0.85f);
            attackDamage = dmg;
            attackRate = Mathf.Max(0.2f, atkRate);
            UpdateHealthBarVisual();
        }

        private void OnEnable()
        {
            if (!ActiveKnights.Contains(this))
            {
                ActiveKnights.Add(this);
            }
        }

        private void Awake()
        {
            _currentHealth = maxHealth;
            CreateHealthBar();
        }

        private void Start()
        {
            if (!ActiveKnights.Contains(this))
            {
                ActiveKnights.Add(this);
            }
            UpdateHealthBarVisual();
        }

        private void Update()
        {
            if (_isDead) return;

            // 1. Quét tìm tất cả quái vật đi bộ đang ở trong phạm vi chốt chặn (2.2m)
            var engagedEnemies = GetEngagedGroundEnemies(engageDistance);

            // 2. Nếu có quái vật tiếp cận chốt chặn: DỪNG LẠI và chiến đấu! TUYỆT ĐỐI không đi qua quái vật!
            if (engagedEnemies.Count > 0)
            {
                DuelEnemies(engagedEnemies);
            }
            else
            {
                // 3. Chỉ khi KHÔNG còn quái vật nào trong phạm vi: Tiếp tục hành quân về phía Cổng Thành Quái
                ReleaseTarget();
                MoveToRallyPoint();
            }
        }

        private List<Enemy.Enemy> GetEngagedGroundEnemies(float radius)
        {
            List<Enemy.Enemy> list = new List<Enemy.Enemy>();
            var enemies = Enemy.Enemy.ActiveEnemies;
            if (enemies == null) return list;

            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e != null && !e.IsDead && e.gameObject.activeInHierarchy && !e.IsFlying)
                {
                    float dist = Vector3.Distance(transform.position, e.transform.position);
                    if (dist <= radius)
                    {
                        list.Add(e);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Giao chiến cận chiến và giữ chân toàn bộ quái vật trong phạm vi:
        /// - Hiệp sĩ chém quái vật gần nhất.
        /// - Toàn bộ quái vật bị chặn đứng không thể tiến bước (IsBlocked = true).
        /// - Các quái vật đánh trả lại hiệp sĩ theo nhịp đánh.
        /// </summary>
        private void DuelEnemies(List<Enemy.Enemy> enemies)
        {
            if (enemies == null || enemies.Count == 0) return;

            // 1. Chọn quái vật gần nhất làm mục tiêu chính
            Enemy.Enemy primaryTarget = enemies[0];
            float closestDist = Vector3.Distance(transform.position, primaryTarget.transform.position);
            for (int i = 1; i < enemies.Count; i++)
            {
                float d = Vector3.Distance(transform.position, enemies[i].transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    primaryTarget = enemies[i];
                }
            }

            _currentTargetEnemy = primaryTarget;

            // Khóa chặt trạng thái IsBlocked của tất cả quái vật đối đầu
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Movement != null && !enemies[i].Movement.IsBlocked)
                {
                    enemies[i].Movement.IsBlocked = true;
                }
            }

            // 2. Xoay mặt đối diện quái vật
            Vector3 lookDir = primaryTarget.transform.position - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized), 15f * Time.deltaTime);
            }

            // 3. Hiệp sĩ vung kiếm chém quái
            if (Time.time >= _lastAttackTime + attackRate)
            {
                _lastAttackTime = Time.time;
                primaryTarget.TakeDamage(attackDamage, DamageType.Physical);
                StartCoroutine(AttackLungeAnimation());
            }

            // 4. Các quái vật vây đánh hiệp sĩ đánh trả lại
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e != null && !e.IsDead && Time.time >= e.LastAttackTime + e.AttackRate)
                {
                    e.RecordAttackTime(Time.time);
                    TakeDamage(e.AttackDamage, DamageType.Physical);
                    if (_isDead) break;
                }
            }
        }

        private IEnumerator AttackLungeAnimation()
        {
            Vector3 originalPos = transform.position;
            Vector3 forwardLunge = originalPos + transform.forward * 0.18f;

            float elapsed = 0f;
            float dur = 0.12f;
            while (elapsed < dur && !_isDead)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(originalPos, forwardLunge, elapsed / dur);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < dur && !_isDead)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(forwardLunge, originalPos, elapsed / dur);
                yield return null;
            }
        }

        [Header("--- HÀNH QUÂN NGƯỢC ĐƯỜNG MÒN ĐẾN THÀNH QUÁI ---")]
        [SerializeField] private bool marchToEnemyCamp = true;
        private int _currentWaypointIndex = -1;
        private bool _hasReachedEnemyCamp = false;

        public bool HasReachedEnemyCamp => _hasReachedEnemyCamp;
        public int CurrentWaypointIndex => _currentWaypointIndex;

        /// <summary>
        /// Hành quân ngược toàn bộ chiều dài con đường mòn từ Cổng Thành Nam đến Thành Quái Cổng Bắc.
        /// Khi đến đích, đứng chốt chặn cố định tại cửa Thành Quái để khóa chặt đầu ra quái vật.
        /// </summary>
        private void MoveToRallyPoint()
        {
            if (marchToEnemyCamp && Waypoints.Waypoints.Points != null && Waypoints.Waypoints.Points.Length > 0)
            {
                // Khởi tạo điểm xuất phát nếu chưa có
                if (_currentWaypointIndex < 0)
                {
                    _currentWaypointIndex = Waypoints.Waypoints.Points.Length - 1;
                }

                if (_hasReachedEnemyCamp || _currentWaypointIndex < 0)
                {
                    // Đã đến Cổng Thành Quái: Đứng chốt chặn kiên cố quay mặt về hướng Bắc
                    Vector3 guardDir = Vector3.forward;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(guardDir), 8f * Time.deltaTime);
                    return;
                }

                Vector3 targetPos = Waypoints.Waypoints.Points[_currentWaypointIndex].position;
                Vector3 toTarget = targetPos - transform.position;
                toTarget.y = 0f;

                if (toTarget.magnitude <= 0.45f)
                {
                    _currentWaypointIndex--;
                    if (_currentWaypointIndex < 0)
                    {
                        _hasReachedEnemyCamp = true;
                    }
                    return;
                }

                // Xoay mặt và bước đi ngược chiều quái vật
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget.normalized), 12f * Time.deltaTime);
                transform.position += toTarget.normalized * (moveSpeed * Time.deltaTime);

                // Hiệu ứng bước chân sinh động
                _walkCycle += Time.deltaTime * moveSpeed * 8f;
                float rollAngle = Mathf.Sin(_walkCycle) * 3.5f;
                transform.localRotation = Quaternion.Euler(0f, transform.localRotation.eulerAngles.y, rollAngle);
                return;
            }

            // Fallback nếu không có hệ thống waypoints
            Vector3 toRally = rallyPoint - transform.position;
            toRally.y = 0f;

            if (toRally.sqrMagnitude > 0.04f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toRally.normalized), 10f * Time.deltaTime);
                transform.position += toRally.normalized * (moveSpeed * Time.deltaTime);
                _walkCycle += Time.deltaTime * moveSpeed * 8f;
                float rollAngle = Mathf.Sin(_walkCycle) * 3.5f;
                transform.localRotation = Quaternion.Euler(0f, transform.localRotation.eulerAngles.y, rollAngle);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.forward), 8f * Time.deltaTime);
            }
        }

        public void TakeDamage(float amount, DamageType damageType = DamageType.Physical)
        {
            if (_isDead) return;

            float finalDamage = amount;
            if (damageType == DamageType.Physical)
            {
                finalDamage = amount * (1f - armor);
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - finalDamage);
            UpdateHealthBarVisual();

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            ActiveKnights.Remove(this);
            ReleaseTarget();

            OnKnightDied?.Invoke(this);

            StartCoroutine(DeathRoutine());
        }

        private void ReleaseTarget()
        {
            if (_currentTargetEnemy != null)
            {
                if (_currentTargetEnemy.Movement != null)
                {
                    _currentTargetEnemy.Movement.IsBlocked = false;
                }
                _currentTargetEnemy = null;
            }

            var enemies = GetEngagedGroundEnemies(engageDistance);
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].Movement != null)
                {
                    enemies[i].Movement.IsBlocked = false;
                }
            }
        }

        private IEnumerator DeathRoutine()
        {
            if (_healthBarContainer != null) _healthBarContainer.gameObject.SetActive(false);

            Vector3 startScale = transform.localScale;
            float dur = 0.25f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / dur);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            ActiveKnights.Remove(this);
            ReleaseTarget();
        }

        private void OnDisable()
        {
            ActiveKnights.Remove(this);
            ReleaseTarget();
        }

        #region --- THANH MÁU 3D BILLBOARD TRÊN ĐẦU ---

        private void CreateHealthBar()
        {
            if (_healthBarContainer != null) return;

            GameObject container = new GameObject("KnightHealthBar");
            container.transform.SetParent(transform, false);
            _healthBarContainer = container.transform;

            // Khung nền đen xám (dùng Cube mỏng vững chắc, không bị lỗi culling hay mất texture)
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Background";
            bg.transform.SetParent(_healthBarContainer, false);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(1.30f, 0.18f, 0.02f);
            DestroyImmediateHelper(bg.GetComponent<Collider>());
            SetSolidColor(bg, new Color(0.08f, 0.08f, 0.08f, 1.0f));

            // Thanh máu xanh lá/vàng/đỏ
            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(_healthBarContainer, false);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.015f);
            fill.transform.localScale = new Vector3(1.24f, 0.13f, 0.025f);
            DestroyImmediateHelper(fill.GetComponent<Collider>());
            SetSolidColor(fill, new Color(0.20f, 0.88f, 0.25f, 1.0f));
            _healthBarFill = fill.transform;

            // Khởi tạo ngay scale và offset chuẩn xác
            Vector3 pScale = transform.localScale;
            float invX = pScale.x > 0.001f ? (1f / pScale.x) : 1f;
            float invY = pScale.y > 0.001f ? (1f / pScale.y) : 1f;
            float invZ = pScale.z > 0.001f ? (1f / pScale.z) : 1f;
            _healthBarContainer.localScale = new Vector3(invX, invY, invZ);
            _healthBarContainer.localPosition = new Vector3(0f, 1.45f * invY, 0f);

            UpdateHealthBarVisual();
        }

        private void DestroyImmediateHelper(UnityEngine.Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEngine.Object.DestroyImmediate(obj);
            else UnityEngine.Object.Destroy(obj);
#else
            UnityEngine.Object.Destroy(obj);
#endif
        }

        private void SetSolidColor(GameObject go, Color color)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Mobile/Diffuse") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material mat = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
                mat.mainTexture = Texture2D.whiteTexture;
                mat.color = color;
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                r.material = mat;
            }
        }

        private void LateUpdate()
        {
            // Luôn hướng thanh máu về phía Camera và giữ kích thước hiển thị cố định trong không gian thế giới
            if (_healthBarContainer != null)
            {
                if (Camera.main != null)
                {
                    _healthBarContainer.rotation = Camera.main.transform.rotation;
                }

                Vector3 pScale = transform.localScale;
                float invX = pScale.x > 0.001f ? (1f / pScale.x) : 1f;
                float invY = pScale.y > 0.001f ? (1f / pScale.y) : 1f;
                float invZ = pScale.z > 0.001f ? (1f / pScale.z) : 1f;
                _healthBarContainer.localScale = new Vector3(invX, invY, invZ);
                _healthBarContainer.localPosition = new Vector3(0f, 1.45f * invY, 0f);
            }
        }

        private void UpdateHealthBarVisual()
        {
            if (_healthBarFill == null) return;

            float ratio = Mathf.Clamp01(_currentHealth / maxHealth);
            float fullWidth = 1.24f;
            float currentWidth = fullWidth * ratio;

            _healthBarFill.localScale = new Vector3(currentWidth, 0.13f, 0.025f);
            _healthBarFill.localPosition = new Vector3(-0.5f * (fullWidth - currentWidth), 0f, -0.015f);

            Renderer r = _healthBarFill.GetComponent<Renderer>();
            if (r != null && r.material != null)
            {
                if (ratio > 0.5f)
                    r.material.color = Color.Lerp(new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.88f, 0.25f), (ratio - 0.5f) * 2f);
                else
                    r.material.color = Color.Lerp(new Color(0.95f, 0.15f, 0.15f), new Color(1f, 0.85f, 0.1f), ratio * 2f);
            }
        }

        #endregion
    }
}
