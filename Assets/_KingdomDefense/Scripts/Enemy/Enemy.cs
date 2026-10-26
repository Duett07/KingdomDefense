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

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _currentHealth = maxHealth;

            if (showHealthBar)
            {
                CreateSimpleHealthBar();
            }
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
            // Luôn hướng thanh máu về phía Camera
            if (_healthBarContainer != null && Camera.main != null)
            {
                _healthBarContainer.rotation = Camera.main.transform.rotation;
            }
        }

        /// <summary>
        /// Tạo thanh máu 3D nổi nhỏ gọn trên đầu quái vật (Quad billboard không phụ thuộc Canvas).
        /// </summary>
        private void CreateSimpleHealthBar()
        {
            GameObject container = new GameObject("HealthBar");
            container.transform.SetParent(transform);
            container.transform.localPosition = healthBarOffset;
            _healthBarContainer = container.transform;

            // Khung nền đen/xám
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Background";
            bg.transform.SetParent(_healthBarContainer);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(0.65f, 0.10f, 1f);
            Destroy(bg.GetComponent<Collider>());
            SetQuadColor(bg, new Color(0.1f, 0.1f, 0.1f, 0.85f));

            // Thanh máu xanh lá/vàng/đỏ
            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fill.name = "Fill";
            fill.transform.SetParent(_healthBarContainer);
            fill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            fill.transform.localScale = new Vector3(0.62f, 0.08f, 1f);
            Destroy(fill.GetComponent<Collider>());
            SetQuadColor(fill, new Color(0.2f, 0.85f, 0.25f, 1f));
            _healthBarFill = fill.transform;
        }

        private void SetQuadColor(GameObject quad, Color color)
        {
            Renderer r = quad.GetComponent<Renderer>();
            if (r != null)
            {
                Shader unlit = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                Material mat = new Material(unlit);
                mat.color = color;
                r.sharedMaterial = mat;
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
            _healthBarFill.localScale = new Vector3(0.62f * ratio, 0.08f, 1f);

            // Căn lề trái khi co ngắn lại
            float offsetX = -0.31f * (1f - ratio);
            _healthBarFill.localPosition = new Vector3(offsetX, 0f, -0.01f);

            // Đổi màu vàng -> đỏ khi máu thấp
            Renderer r = _healthBarFill.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                if (ratio > 0.5f)
                    r.sharedMaterial.color = Color.Lerp(Color.yellow, new Color(0.2f, 0.85f, 0.25f), (ratio - 0.5f) * 2f);
                else
                    r.sharedMaterial.color = Color.Lerp(Color.red, Color.yellow, ratio * 2f);
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

            if (_healthBarContainer != null)
            {
                _healthBarContainer.gameObject.SetActive(showHealthBar);
            }
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
