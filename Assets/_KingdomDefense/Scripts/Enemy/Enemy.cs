using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.UI;

namespace KingdomDefense.Enemy
{
    /// <summary>
    /// Quản lý chỉ số, lượng máu, nhận sát thương và phần thưởng vàng của quái vật.
    /// Cung cấp thanh máu nổi trực quan trên đầu (World Space Health Bar).
    /// </summary>
    [RequireComponent(typeof(EnemyMovement))]
    public class Enemy : MonoBehaviour
    {
        public static readonly List<Enemy> ActiveEnemies = new List<Enemy>();

        [Header("--- THIẾT LẬP CHỈ SỐ QUÁI ---")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private int goldReward = 15;
        [SerializeField] private string enemyName = "Goblin Raider";

        [Header("--- THANH MÁU TRỰC QUAN (HEALTH BAR) ---")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.05f, 0f);

        private float _currentHealth;
        private bool _isDead = false;
        private EnemyMovement _movement;

        // Thành phần thanh máu thủ công gọn nhẹ không cần Canvas phức tạp
        private Transform _healthBarContainer;
        private Transform _healthBarFill;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => _currentHealth;
        public int GoldReward => goldReward;
        public bool IsDead => _isDead;

        public void ConfigureStats(string name, float hp, int gold)
        {
            enemyName = name;
            maxHealth = hp;
            _currentHealth = hp;
            goldReward = gold;
            UpdateHealthBar();
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

        [Header("--- HIỆU ỨNG BĂNG GIÁ (FROST SLOW) ---")]
        private float _slowTimer = 0f;
        private bool _isSlowed = false;
        private GameObject _frostSlowIndicator;

        public bool IsSlowed => _isSlowed;

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
        /// Tạo thanh máu 3D nổi nhỏ gọn trên đầu quái vật.
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

            // Thanh máu xanh lá/đỏ
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
        /// Nhận sát thương từ Tháp phòng thủ hoặc Kỹ năng Thiên Thạch.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;
            UpdateHealthBar();

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Nhận sát thương phép thuật từ Tháp Pháp Sư (Mage Tower):
        /// Bỏ qua 100% Giáp vật lý, trừ trực tiếp vào Máu.
        /// </summary>
        public void TakeMagicDamage(float amount)
        {
            // Sát thương phép bỏ qua hoàn toàn giáp vật lý
            TakeDamage(amount);
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
        /// Tiêu diệt quái vật khi hết máu: thưởng vàng và hủy GameObject.
        /// </summary>
        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log($"[Enemy] '{gameObject.name}' bị tiêu diệt! Thưởng +{goldReward} Vàng.");

            // Cộng thưởng vàng qua GameHUD
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.AddGold(goldReward);
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Gọi khi quái vật chạm Cổng Thành Hoàng Gia.
        /// </summary>
        public void OnReachGoal()
        {
            if (_isDead) return;
            _isDead = true;
            Destroy(gameObject);
        }
    }
}
