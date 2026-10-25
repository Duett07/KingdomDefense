using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.Core;

namespace KingdomDefense.Towers
{
    /// <summary>
    /// Đạn pháo đại bác / đá công thành (CannonBall).
    /// Bay theo quỹ đạo cầu vồng parabol mượt mà hướng tới điểm đón đầu,
    /// phát nổ diện rộng (AoE Splash Damage) trong bán kính 2.5m,
    /// sát thương giảm dần theo cự ly và tạo hiệu ứng cháy nổ khói lửa hoành tráng.
    /// Tương thích hoàn toàn với ObjectPool (IPoolable).
    /// </summary>
    public class CannonBall : MonoBehaviour, IPoolable
    {
        [Header("--- THIẾT LẬP ĐẠN PHÁO ---")]
        [Tooltip("Sát thương tại tâm vụ nổ")]
        [SerializeField] private float damage = 65f;

        [Tooltip("Bán kính nổ sát thương lan (m)")]
        [SerializeField] private float explosionRadius = 2.5f;

        [Tooltip("Tốc độ bay của đạn pháo (m/s)")]
        [SerializeField] private float speed = 14f;

        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _totalDuration;
        private float _elapsedTime;
        private float _arcHeight;
        private bool _isFlying = false;

        private TrailRenderer _trail;
        private Transform _visualBall;

        public void OnSpawnFromPool()
        {
            _elapsedTime = 0f;
            _isFlying = false;

            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        public void OnReturnToPool()
        {
            _isFlying = false;
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

        private void Awake()
        {
            EnsureVisualAndTrail();
        }

        private void EnsureVisualAndTrail()
        {
            // 1. Tạo hoặc gán vệt khói lửa đường đạn (Smoke & Fire Trail)
            _trail = GetComponent<TrailRenderer>();
            if (_trail == null)
            {
                _trail = gameObject.AddComponent<TrailRenderer>();
                _trail.time = 0.22f;
                _trail.startWidth = 0.16f;
                _trail.endWidth = 0.02f;

                Shader trailShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (trailShader != null)
                {
                    _trail.material = new Material(trailShader);
                    _trail.startColor = new Color(1.0f, 0.55f, 0.1f, 0.9f); // Cam lửa bốc cháy
                    _trail.endColor = new Color(0.25f, 0.22f, 0.20f, 0.0f); // Tàn khói đen xám
                }
            }

            // 2. Tìm hoặc tạo hình khối quả pháo sắt/đá tròn đen
            Transform existingVisual = transform.Find("Visual_Ball");
            if (existingVisual != null)
            {
                _visualBall = existingVisual;
            }
            else
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Visual_Ball";
                sphere.transform.SetParent(transform, false);
                sphere.transform.localScale = Vector3.one * 0.38f;
                Destroy(sphere.GetComponent<Collider>());

                Renderer r = sphere.GetComponent<Renderer>();
                if (r != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material mat = new Material(s);
                    mat.color = new Color(0.22f, 0.21f, 0.20f); // Sắt đúc xám đen cổ điển
                    mat.SetFloat("_Metallic", 0.45f);
                    mat.SetFloat("_Glossiness", 0.35f);
                    r.sharedMaterial = mat;
                }
                _visualBall = sphere.transform;
            }
        }

        /// <summary>
        /// Phóng đạn pháo theo quỹ đạo cầu vồng parabol tới điểm đích đã tính toán.
        /// </summary>
        public void Launch(Vector3 start, Vector3 target, float ballDamage, float radius, float arcHeightOverride = 1.6f)
        {
            _startPos = start;
            _targetPos = target;
            damage = ballDamage;
            explosionRadius = radius;

            float horizontalDist = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(target.x, target.z));
            _totalDuration = Mathf.Clamp(horizontalDist / speed, 0.45f, 1.35f);
            _arcHeight = Mathf.Max(arcHeightOverride, horizontalDist * 0.42f);
            _elapsedTime = 0f;
            _isFlying = true;

            transform.position = start;

            if (_trail != null)
            {
                _trail.Clear();
            }
        }

        private void Update()
        {
            if (!_isFlying) return;

            _elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsedTime / _totalDuration);

            // Tọa độ nội suy tuyến tính mặt phẳng
            Vector3 flatPos = Vector3.Lerp(_startPos, _targetPos, t);

            // Độ cao vồng parabol: Đạt đỉnh cực đại tại t = 0.5
            float arcY = 4f * _arcHeight * t * (1f - t);
            Vector3 currentPos = new Vector3(flatPos.x, flatPos.y + arcY, flatPos.z);

            // Xoay hướng đạn theo tiếp tuyến vector vận tốc tức thời
            Vector3 dir = currentPos - transform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            }
            transform.position = currentPos;

            // Xoay tròn quả pháo quanh trục
            if (_visualBall != null)
            {
                _visualBall.Rotate(Vector3.right * (450f * Time.deltaTime), Space.Self);
            }

            // Chạm đích và phát nổ
            if (t >= 1f)
            {
                _isFlying = false;
                Explode();
            }
        }

        /// <summary>
        /// Kích hoạt vụ nổ sát thương diện rộng và hiệu ứng khói lửa.
        /// </summary>
        private void Explode()
        {
            Vector3 explosionCenter = transform.position;

            // 1. Quét toàn bộ mục tiêu trong bán kính nổ
            Collider[] hits = Physics.OverlapSphere(explosionCenter, explosionRadius);
            HashSet<Enemy.Enemy> hitEnemies = new HashSet<Enemy.Enemy>();

            foreach (Collider hit in hits)
            {
                if (hit == null) continue;

                Enemy.Enemy enemy = hit.GetComponent<Enemy.Enemy>() ?? hit.GetComponentInParent<Enemy.Enemy>();
                if (enemy != null && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);

                    // Sát thương giảm dần theo khoảng cách: 100% tại tâm, 50% ở mép ngoài
                    float dist = Vector3.Distance(explosionCenter, enemy.transform.position);
                    float ratio = Mathf.Clamp01(1f - (dist / explosionRadius) * 0.5f);
                    float actualDamage = damage * ratio;

                    enemy.TakeDamage(actualDamage, DamageType.AoEExplosion);

                    // Đẩy văng nhẹ (Knockback)
                    Vector3 knockDir = enemy.transform.position - explosionCenter;
                    knockDir.y = 0f;
                    if (knockDir.sqrMagnitude > 0.001f)
                    {
                        enemy.transform.position += knockDir.normalized * 0.32f;
                    }
                }
            }

            // 2. Tạo hiệu ứng nổ khói lửa hoành tráng
            SpawnExplosionFX(explosionCenter);

            // 3. Trả về ObjectPool
            Despawn();
        }

        /// <summary>
        /// Tạo hiệu ứng nổ bùn đất, lửa cam và khói đen cuộn trào.
        /// </summary>
        private void SpawnExplosionFX(Vector3 pos)
        {
            GameObject fxContainer = new GameObject("Cannon_Explosion_FX");
            fxContainer.transform.position = pos;

            // a. Quả cầu lửa chớp sáng (Fireball Flash)
            GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireball.transform.SetParent(fxContainer.transform);
            fireball.transform.localPosition = Vector3.up * 0.2f;
            fireball.transform.localScale = Vector3.one * 1.5f;
            Destroy(fireball.GetComponent<Collider>());

            Renderer rFire = fireball.GetComponent<Renderer>();
            if (rFire != null)
            {
                Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material mat = new Material(s);
                mat.color = new Color(1.0f, 0.65f, 0.15f, 0.95f);
                rFire.sharedMaterial = mat;
            }

            // b. 6 Mảnh đất đá văng tung tóe (Debris Chunks)
            for (int i = 0; i < 6; i++)
            {
                GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.transform.SetParent(fxContainer.transform);
                float angle = i * Mathf.PI * 2f / 6f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Random.Range(0.4f, 0.8f), Mathf.Sin(angle));
                chunk.transform.localPosition = dir * Random.Range(0.4f, 0.9f);
                chunk.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);
                chunk.transform.rotation = Random.rotation;
                Destroy(chunk.GetComponent<Collider>());

                Renderer rChunk = chunk.GetComponent<Renderer>();
                if (rChunk != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material mat = new Material(s);
                    mat.color = new Color(0.28f, 0.24f, 0.18f); // Đất nâu sẫm
                    rChunk.sharedMaterial = mat;
                }
            }

            // c. Vết xém đen trên mặt đất (Scorch Mark)
            GameObject scorch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scorch.name = "Scorch_Mark";
            scorch.transform.SetParent(fxContainer.transform);
            scorch.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            scorch.transform.localScale = new Vector3(2.2f, 0.005f, 2.2f);
            Destroy(scorch.GetComponent<Collider>());

            Renderer rScorch = scorch.GetComponent<Renderer>();
            if (rScorch != null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                Material mat = new Material(s);
                mat.color = new Color(0.18f, 0.16f, 0.14f); // Vết cháy đen
                rScorch.sharedMaterial = mat;
            }

            Destroy(fxContainer, 0.45f);
        }
    }
}
