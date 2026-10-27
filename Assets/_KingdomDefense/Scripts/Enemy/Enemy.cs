using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.UI;
using KingdomDefense.Core;
using KingdomDefense.Audio;

namespace KingdomDefense.Enemy
{
    /// <summary>
    /// Quản lý chỉ số, lượng máu, nhận sát thương (Giáp vật lý vs Kháng phép),
    /// phần thưởng vàng và vòng đời Object Pool của quái vật trong Kingdom Defense.
    /// Kế thừa giao diện IDamageable (nhận sát thương phân loại) và IPoolable (tái sinh mượt mà).
    /// </summary>
    [RequireComponent(typeof(EnemyMovement))]
    public class Enemy : MonoBehaviour, IDamageable, IPoolable
    {
        public static readonly List<Enemy> ActiveEnemies = new List<Enemy>();

        [Header("--- THIẾT LẬP CHỈ SỐ QUÁI ---")]
        [SerializeField] private string enemyName = "Goblin Runner";
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private int goldReward = 15;

        [Header("--- THIẾT LẬP PHÒNG NGỰ (ARMOR & RESIST) ---")]
        [Tooltip("Tỷ lệ giảm trừ sát thương vật lý (0.25 = 25% Giáp - Orc, 0.60 = 60% Giáp - Golem)")]
        [Range(0f, 0.9f)]
        [SerializeField] private float armor = 0f;

        [Tooltip("Tỷ lệ giảm trừ sát thương phép thuật (0.30 = 30% Kháng phép - Boss Orcus)")]
        [Range(0f, 0.9f)]
        [SerializeField] private float magicResistance = 0f;

        [Tooltip("Kháng sát thương nổ diện rộng AoE (giảm 50% splash damage - dùng cho Stone Golem)")]
        [SerializeField] private bool isAoEResistant = false;

        [Header("--- THANH MÁU TRỰC QUAN (HEALTH BAR) ---")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.05f, 0f);

        private float _currentHealth;
        private bool _isDead = false;
        private EnemyMovement _movement;

        // Thành phần thanh máu thủ công 3D Billboard tối ưu hiệu năng (Zero Canvas Overhead)
        private Transform _healthBarContainer;
        private Transform _healthBarFill;

        [Header("--- HIỆU ỨNG BĂNG GIÁ (FROST SLOW) ---")]
        private float _slowTimer = 0f;
        private bool _isSlowed = false;
        private GameObject _frostSlowIndicator;

        [Header("--- THUỘC TÍNH QUÁI BAY (FLYING) ---")]
        [Tooltip("Có phải quái vật bay trên không (Dark Bat) không")]
        [SerializeField] private bool isFlying = false;
        [Tooltip("Độ cao bay tính từ mặt đất")]
        [SerializeField] private float flightHeight = 2.8f;

        [Header("--- THIẾT LẬP CẬN CHIẾN KHI GIAO TRANH VỚI LÍNH ---")]
        [Tooltip("Sát thương cận chiến quái gây ra cho lính chặn đường")]
        [SerializeField] private float attackDamage = 15f;
        [Tooltip("Tốc độ đánh (giây giữa mỗi đòn đánh)")]
        [SerializeField] private float attackRate = 1.0f;

        private float _lastAttackTime = 0f;
        public float LastAttackTime => _lastAttackTime;
        public void RecordAttackTime(float time) => _lastAttackTime = time;

        public string EnemyName => enemyName;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => _currentHealth;
        public float Armor => armor;
        public float MagicResistance => magicResistance;
        public bool IsAoEResistant => isAoEResistant;
        public bool IsFlying => isFlying;
        public float FlightHeight => flightHeight;
        public int GoldReward => goldReward;
        public bool IsDead => _isDead;
        public bool IsSlowed => _isSlowed;
        public EnemyMovement Movement => _movement != null ? _movement : (_movement = GetComponent<EnemyMovement>());
        public float AttackDamage => attackDamage;
        public float AttackRate => attackRate;

        public void SetMeleeStats(float damage, float rate = 1.0f)
        {
            attackDamage = damage;
            attackRate = rate;
        }

        public void SetFlying(bool flying, float height = 2.8f)
        {
            isFlying = flying;
            flightHeight = height;
        }

        public void ConfigureStats(string name, float hp, float arm, float mr, int gold, bool aoeResist = false, bool flying = false, float flyHeight = 2.8f)
        {
            enemyName = name;
            maxHealth = hp;
            _currentHealth = hp;
            armor = arm;
            magicResistance = mr;
            goldReward = gold;
            isAoEResistant = aoeResist;
            isFlying = flying;
            flightHeight = flyHeight;
            UpdateHealthBar();
        }

        public void ConfigureStats(string name, float hp, int gold)
        {
            ConfigureStats(name, hp, 0f, 0f, gold, false, false, 2.8f);
        }

        private void OnEnable()
        {
            if (!ActiveEnemies.Contains(this))
            {
                ActiveEnemies.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveEnemies.Remove(this);
        }

        /// <summary>
        /// Tiêu diệt toàn bộ quái vật đang hoạt động trên chiến trường (hỗ trợ kiểm thử cân bằng và Debug).
        /// </summary>
        public static void KillAllActiveEnemies()
        {
            var enemiesToKill = new List<Enemy>(ActiveEnemies);
            for (int i = 0; i < enemiesToKill.Count; i++)
            {
                if (enemiesToKill[i] != null && !enemiesToKill[i].IsDead)
                {
                    enemiesToKill[i].TakeDamage(99999f, DamageType.Pure);
                }
            }
        }

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _currentHealth = maxHealth;
            EnsureHealthBarExists();
        }

        private void Start()
        {
            EnsureHealthBarExists();
            UpdateHealthBar();
        }

        private void Update()
        {
            if (_isSlowed)
            {
                _slowTimer -= Time.deltaTime;
                if (_slowTimer <= 0f)
                {
                    RemoveSlow();
                }
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

                float worldHeight = GetHealthBarWorldHeight();
                _healthBarContainer.localPosition = new Vector3(0f, worldHeight * invY, 0f);
            }
        }

        private float GetHealthBarWorldHeight()
        {
            if (isFlying) return 1.05f;
            if (maxHealth >= 1000f) return 2.80f; // Boss Orcus
            if (maxHealth >= 300f) return 1.95f;  // Stone Golem
            return 1.35f;                         // Quái thường
        }

        /// <summary>
        /// Đảm bảo thanh máu 3D luôn tồn tại và hiển thị trực quan cho mọi quái vật.
        /// </summary>
        public void EnsureHealthBarExists()
        {
            if (!showHealthBar)
            {
                if (_healthBarContainer != null) _healthBarContainer.gameObject.SetActive(false);
                return;
            }

            if (_healthBarContainer == null || _healthBarFill == null)
            {
                CreateSimpleHealthBar();
            }

            if (_healthBarContainer != null)
            {
                _healthBarContainer.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Tạo thanh máu 3D nổi nhỏ gọn trên đầu quái vật (dùng Cube mỏng vững chắc, Unlit solid material không phụ thuộc texture hay culling).
        /// </summary>
        public void CreateSimpleHealthBar()
        {
            if (_healthBarContainer != null) return;

            GameObject container = new GameObject("HealthBar");
            container.transform.SetParent(transform, false);
            _healthBarContainer = container.transform;

            // Khung nền đen xám (Background)
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Background";
            bg.transform.SetParent(_healthBarContainer, false);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(1.30f, 0.18f, 0.02f);
            DestroyImmediateHelper(bg.GetComponent<Collider>());
            SetSolidColor(bg, new Color(0.08f, 0.08f, 0.08f, 1.0f));

            // Thanh máu Fill
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
            _healthBarContainer.localPosition = new Vector3(0f, GetHealthBarWorldHeight() * invY, 0f);

            UpdateHealthBar();
        }

        private void DestroyImmediateHelper(Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) Object.DestroyImmediate(obj);
            else Object.Destroy(obj);
#else
            Object.Destroy(obj);
#endif
        }

        private void SetSolidColor(GameObject go, Color color)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("GUI/Text Shader") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                Material mat = new Material(shader);
                mat.mainTexture = Texture2D.whiteTexture;
                mat.color = color;
                r.material = mat;
            }
        }

        /// <summary>
        /// Thực thi giao diện IDamageable: Nhận sát thương và phân loại xử lý theo cơ chế Giáp / Kháng phép.
        /// - Physical: Bị giảm bởi Giáp vật lý: Actual = Raw * (1 - Armor)
        /// - Magic: Bỏ qua 100% Giáp vật lý: Actual = Raw * (1 - MagicResistance)
        /// - AoEExplosion: Sát thương nổ lan, bị giảm bởi Giáp và giảm thêm 50% nếu quái kháng nổ (Stone Golem)
        /// - Pure: Sát thương chuẩn, bỏ qua toàn bộ phòng ngự
        /// </summary>
        public void TakeDamage(float amount, DamageType damageType = DamageType.Physical)
        {
            if (_isDead) return;

            float actualDamage = amount;

            switch (damageType)
            {
                case DamageType.Physical:
                    actualDamage = amount * (1f - Mathf.Clamp01(armor));
                    break;

                case DamageType.Magic:
                    actualDamage = amount * (1f - Mathf.Clamp01(magicResistance));
                    break;

                case DamageType.AoEExplosion:
                    float basePhysDamage = amount * (1f - Mathf.Clamp01(armor));
                    actualDamage = isAoEResistant ? (basePhysDamage * 0.5f) : basePhysDamage;
                    break;

                case DamageType.Pure:
                    actualDamage = amount;
                    break;
            }

            actualDamage = Mathf.Max(1f, actualDamage);
            _currentHealth -= actualDamage;
            UpdateHealthBar();

            // Hiệu ứng chớp đỏ trực quan khi trúng đòn
            StartCoroutine(FlashDamageRoutine());

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Tương thích ngược: Nhận sát thương vật lý mặc định.
        /// </summary>
        public void TakeDamage(float amount)
        {
            TakeDamage(amount, DamageType.Physical);
        }

        /// <summary>
        /// Tương thích ngược: Nhận sát thương phép thuật từ Tháp Pháp Sư (Mage Tower).
        /// </summary>
        public void TakeMagicDamage(float amount)
        {
            TakeDamage(amount, DamageType.Magic);
        }

        private IEnumerator FlashDamageRoutine()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) yield break;

            foreach (Renderer r in renderers)
            {
                if (r.name == "Fill" || r.name == "Background") continue;
                if (r.material != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = Color.Lerp(r.material.color, Color.red, 0.45f);
                }
            }

            yield return new WaitForSeconds(0.08f);

            foreach (Renderer r in renderers)
            {
                if (r.name == "Fill" || r.name == "Background") continue;
                if (r.material != null && r.material.HasProperty("_Color"))
                {
                    r.material.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Áp dụng hiệu ứng Làm chậm (Slow Effect) từ Tháp Băng Phong (Frost Tower).
        /// Giảm tốc độ chạy theo tỷ lệ slowPercent và duy trì trong duration giây.
        /// </summary>
        public void ApplySlow(float slowPercent, float duration)
        {
            if (_isDead) return;

            // Làm mới thời gian duy trì hiệu ứng băng giá
            _slowTimer = duration;

            if (!_isSlowed)
            {
                _isSlowed = true;
                if (_movement != null)
                {
                    float factor = 1f - Mathf.Clamp01(slowPercent);
                    _movement.SetSpeed(_movement.BaseSpeed * factor);
                }

                ShowFrostSlowIndicator(true);
            }
        }

        /// <summary>
        /// Hóa giải hiệu ứng đóng băng, khôi phục tốc độ di chuyển ban đầu.
        /// </summary>
        public void RemoveSlow()
        {
            _isSlowed = false;
            _slowTimer = 0f;

            if (_movement != null)
            {
                _movement.ResetSpeed();
            }

            ShowFrostSlowIndicator(false);
        }

        /// <summary>
        /// Hiển thị vòng hào quang sương băng lam nhạt dưới chân và tinh thể tuyết trên đầu quái.
        /// </summary>
        private void ShowFrostSlowIndicator(bool active)
        {
            if (active)
            {
                if (_frostSlowIndicator == null)
                {
                    _frostSlowIndicator = new GameObject("Frost_Slow_FX");
                    _frostSlowIndicator.transform.SetParent(transform, false);
                    _frostSlowIndicator.transform.localPosition = new Vector3(0f, 0.05f, 0f);

                    // Vòng tròn băng giá tỏa sáng dưới chân
                    GameObject frostRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    frostRing.name = "Frost_Ground_Ring";
                    frostRing.transform.SetParent(_frostSlowIndicator.transform, false);
                    frostRing.transform.localPosition = Vector3.zero;
                    frostRing.transform.localScale = new Vector3(0.95f, 0.01f, 0.95f);
                    Destroy(frostRing.GetComponent<Collider>());

                    Renderer r = frostRing.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                        Material m = new Material(s);
                        m.color = new Color(0.1f, 0.88f, 1.0f, 0.55f); // Lam băng
                        r.sharedMaterial = m;
                    }

                    // Tinh thể tuyết bay lơ lửng cạnh thanh máu
                    GameObject iceFlake = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    iceFlake.name = "Ice_Flake";
                    iceFlake.transform.SetParent(_frostSlowIndicator.transform, false);
                    iceFlake.transform.localPosition = healthBarOffset + new Vector3(-0.45f, 0.02f, 0f);
                    iceFlake.transform.localScale = Vector3.one * 0.12f;
                    Destroy(iceFlake.GetComponent<Collider>());

                    Renderer rFlake = iceFlake.GetComponent<Renderer>();
                    if (rFlake != null)
                    {
                        Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                        Material m = new Material(s);
                        m.color = new Color(0.7f, 0.95f, 1.0f, 0.9f);
                        rFlake.sharedMaterial = m;
                    }
                }
                _frostSlowIndicator.SetActive(true);
            }
            else
            {
                if (_frostSlowIndicator != null)
                {
                    _frostSlowIndicator.SetActive(false);
                }
            }
        }

        private void UpdateHealthBar()
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

        /// <summary>
        /// Tiêu diệt quái vật khi hết máu: thưởng vàng, nổ hiệu ứng tan biến và hoàn trả về Object Pool.
        /// </summary>
        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log($"[Enemy] '{enemyName}' bị tiêu diệt! Thưởng +{goldReward} Vàng.");

            // Cộng thưởng vàng qua GameHUD
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.AddGold(goldReward);
            }

            // Âm thanh quái gầm rú khi bị tiêu diệt
            AudioManager.Instance?.PlayEnemyDeath();

            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            // Ẩn thanh máu và vòng sương băng
            if (_healthBarContainer != null) _healthBarContainer.gameObject.SetActive(false);
            if (_frostSlowIndicator != null) _frostSlowIndicator.SetActive(false);

            // Sinh hiệu ứng khói bụi và đồng tiền vàng bay
            SpawnDeathVFX();

            // Hiệu ứng co biến mất sinh động
            Vector3 startScale = transform.localScale;
            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            transform.localScale = startScale;

            // Hoàn trả vào ObjectPool nếu có thành phần PooledObject, ngược lại Destroy an toàn
            PooledObject po = GetComponent<PooledObject>();
            if (po != null && ObjectPool.Instance != null)
            {
                po.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Tạo hiệu ứng khói bụi tan biến và đồng vàng bay lên (+Gold).
        /// </summary>
        private void SpawnDeathVFX()
        {
            GameObject deathFX = new GameObject("Enemy_Death_FX");
            deathFX.transform.position = transform.position + Vector3.up * 0.35f;

            // 5 Hạt khói bụi tan rã
            for (int i = 0; i < 5; i++)
            {
                GameObject smoke = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                smoke.transform.SetParent(deathFX.transform, false);
                float angle = i * Mathf.PI * 2f / 5f;
                smoke.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.3f, Random.Range(0.05f, 0.35f), Mathf.Sin(angle) * 0.3f);
                smoke.transform.localScale = Vector3.one * 0.22f;
                Destroy(smoke.GetComponent<Collider>());

                Renderer r = smoke.GetComponent<Renderer>();
                if (r != null)
                {
                    Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                    Material mat = new Material(s);
                    mat.color = new Color(0.85f, 0.85f, 0.85f, 0.8f);
                    r.sharedMaterial = mat;
                }
            }

            // Đồng tiền vàng bay lên (+Gold indicator)
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Gold_Coin_Indicator";
            coin.transform.SetParent(deathFX.transform, false);
            coin.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            coin.transform.localScale = new Vector3(0.24f, 0.02f, 0.24f);
            coin.transform.localRotation = Quaternion.Euler(45f, 30f, 0f);
            Destroy(coin.GetComponent<Collider>());

            Renderer rCoin = coin.GetComponent<Renderer>();
            if (rCoin != null)
            {
                Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material mat = new Material(s);
                mat.color = new Color(1.0f, 0.85f, 0.15f, 0.95f);
                rCoin.sharedMaterial = mat;
            }

            Destroy(deathFX, 0.35f);
        }

        /// <summary>
        /// Khởi tạo lại trạng thái quái vật (sử dụng khi tái sinh từ Object Pool).
        /// </summary>
        public void ResetEnemy()
        {
            _isDead = false;
            _currentHealth = maxHealth;
            _slowTimer = 0f;
            _isSlowed = false;

            if (_frostSlowIndicator != null) _frostSlowIndicator.SetActive(false);

            if (_movement != null)
            {
                _movement.RestartPath();
            }

            EnsureHealthBarExists();
            UpdateHealthBar();
        }

        public void OnSpawnFromPool()
        {
            ResetEnemy();
        }

        public void OnReturnToPool()
        {
            RemoveSlow();
            if (_healthBarContainer != null) _healthBarContainer.gameObject.SetActive(false);
        }

        /// <summary>
        /// Gọi khi quái vật chạm Cổng Thành Hoàng Gia.
        /// </summary>
        public void OnReachGoal()
        {
            if (_isDead) return;
            _isDead = true;

            PooledObject po = GetComponent<PooledObject>();
            if (po != null && ObjectPool.Instance != null)
            {
                po.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
