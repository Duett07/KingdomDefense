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
