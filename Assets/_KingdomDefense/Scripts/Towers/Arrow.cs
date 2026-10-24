using UnityEngine;
using KingdomDefense.Enemy;

namespace KingdomDefense.Towers
{
    /// <summary>
    /// Đạn tên bay đuổi mục tiêu (Homing Arrow) của Tháp Xạ Thủ.
    /// Tự động uốn lượn bám theo quái vật đang di chuyển, có vệt bay ánh sáng (Trail)
    /// và gây sát thương vật lý khi chạm đích qua TakeDamage(damage).
    /// </summary>
    public class Arrow : MonoBehaviour
    {
        [Header("--- THIẾT LẬP MŨI TÊN ---")]
        [Tooltip("Tốc độ bay của mũi tên (m/s)")]
        [SerializeField] private float speed = 22f;

        [Tooltip("Sát thương gây ra khi trúng đích")]
        [SerializeField] private float damage = 30f;

        [Tooltip("Bán kính nổ lan (nếu = 0 là sát thương đơn mục tiêu)")]
        [SerializeField] private float explosionRadius = 0f;

        private Transform _target;
        private Vector3 _lastDirection;
        private bool _hasTarget = false;
        private bool _meshNeedsXRotation = false;

        private void Awake()
        {
            // Kiểm tra và căn chỉnh góc xoay cho model FBX (trục dài nằm dọc Y trong file gốc)
            bool foundChildMesh = false;
            foreach (Transform child in transform)
            {
                if (child.GetComponent<MeshFilter>() != null || child.name.ToLower().Contains("arrow"))
                {
                    child.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    foundChildMesh = true;
                }
            }

            if (!foundChildMesh && GetComponent<MeshFilter>() != null)
            {
                _meshNeedsXRotation = true;
            }

            // Gắn hiệu ứng vệt sáng bay (Golden Trail)
            SetupFlightTrail();
        }

        private void SetupFlightTrail()
        {
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.12f;
                trail.startWidth = 0.07f;
                trail.endWidth = 0.01f;

                Shader trailShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (trailShader != null)
                {
                    trail.material = new Material(trailShader);
                    trail.startColor = new Color(1f, 0.92f, 0.5f, 0.85f); // Ánh vàng rực rỡ
                    trail.endColor = new Color(1f, 0.6f, 0.2f, 0.0f);
                }
            }
        }

        public void Seek(Transform target, float arrowDamage)
        {
            _target = target;
            damage = arrowDamage;
            _hasTarget = (target != null);

            if (_hasTarget)
            {
                _lastDirection = (target.position + Vector3.up * 0.45f - transform.position).normalized;
            }
        }

        private void Update()
        {
            Vector3 dir;

            if (_target != null)
            {
                // Nhắm vào ngực / tâm của quái vật (offset Y = 0.45m vừa vặn chiến sĩ)
                Vector3 targetCenter = _target.position + new Vector3(0f, 0.45f, 0f);
                dir = targetCenter - transform.position;
                _lastDirection = dir.normalized;
            }
            else
            {
                // Nếu quái đã bị hạ gục trước khi tên bay tới, tiếp tục bay thẳng theo hướng cũ
                dir = _lastDirection;
                if (!_hasTarget || dir == Vector3.zero)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            float distanceThisFrame = speed * Time.deltaTime;

            // Kiểm tra va chạm đích
            if (dir.magnitude <= distanceThisFrame || dir.magnitude <= 0.38f)
            {
                HitTarget();
                return;
            }

            // Xoay đầu mũi tên hướng về mục tiêu
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(dir.normalized);
                if (_meshNeedsXRotation)
                {
                    transform.rotation = lookRotation * Quaternion.Euler(90f, 0f, 0f);
                }
                else
                {
                    transform.rotation = lookRotation;
                }
            }

            // Di chuyển tịnh tiến
            transform.position += dir.normalized * distanceThisFrame;
        }

        private void HitTarget()
        {
            CreateImpactEffect();

            if (explosionRadius > 0f)
            {
                Explode();
            }
            else
            {
                DamageEnemy(_target);
            }

            Destroy(gameObject);
        }

        private void DamageEnemy(Transform enemyTransform)
        {
            if (enemyTransform == null) return;

            Enemy.Enemy enemy = enemyTransform.GetComponent<Enemy.Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        private void Explode()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider c in colliders)
            {
                if (c.CompareTag("Enemy"))
                {
                    DamageEnemy(c.transform);
                }
            }
        }

        /// <summary>
        /// Tạo hiệu ứng tia lửa / bụi va chạm khi mũi tên găm trúng mục tiêu.
        /// </summary>
        private void CreateImpactEffect()
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "Arrow_Impact_Spark";
            spark.transform.position = transform.position;
            spark.transform.localScale = Vector3.one * 0.22f;
            Destroy(spark.GetComponent<Collider>());

            Renderer r = spark.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material mat = new Material(s);
                mat.color = new Color(1f, 0.88f, 0.35f); // Vàng cam phát sáng
                r.sharedMaterial = mat;
            }

            Destroy(spark, 0.15f);
        }
    }
}
