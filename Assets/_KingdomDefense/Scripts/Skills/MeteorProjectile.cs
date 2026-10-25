using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.Core;
using KingdomDefense.Cameras;

namespace KingdomDefense.Skills
{
    /// <summary>
    /// Thực thể Thiên Thạch (Meteor Projectile).
    /// Lao chéo từ độ cao 25m với vệt lửa khói cuồn cuộn,
    /// va chạm mặt đất gây nổ diện rộng 4.0m, gây 400 sát thương thuần (Pure Damage)
    /// lên toàn bộ quái vật và kích hoạt rung chấn Camera.
    /// </summary>
    public class MeteorProjectile : MonoBehaviour
    {
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private float _flightDuration = 0.65f;
        private float _elapsedTime = 0f;
        private float _damage = 400f;
        private float _aoeRadius = 4.0f;
        private bool _hasImpacted = false;

        private Transform _meteorMesh;
        private readonly List<Transform> _trailEmbers = new List<Transform>();

        /// <summary>
        /// Tạo và phóng thiên thạch lao xuống tọa độ chỉ định.
        /// </summary>
        public static MeteorProjectile Spawn(Vector3 targetPos, float damage = 400f, float aoeRadius = 4.0f)
        {
            GameObject obj = new GameObject("Meteor_Projectile");
            MeteorProjectile meteor = obj.AddComponent<MeteorProjectile>();
            meteor.Initialize(targetPos, damage, aoeRadius);
            return meteor;
        }

        public void Initialize(Vector3 targetPos, float damage, float aoeRadius)
        {
            _targetPosition = targetPos;
            _damage = damage;
            _aoeRadius = aoeRadius;

            // Lao chéo từ góc tây-bắc ở độ cao 25m
            _startPosition = targetPos + new Vector3(-8f, 25f, -8f);
            transform.position = _startPosition;

            BuildMeteorVisuals();
        }

        private void BuildMeteorVisuals()
        {
            // 1. Quả cầu thiên thạch đá nóng chảy
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Meteor_Core";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * 1.6f;
            Destroy(sphere.GetComponent<Collider>());
            _meteorMesh = sphere.transform;

            Renderer r = sphere.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                Material mat = new Material(s);
                mat.color = new Color(0.95f, 0.35f, 0.05f); // Cam đỏ dung nham rực cháy
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(1.0f, 0.45f, 0.05f) * 2.2f);
                }
                r.sharedMaterial = mat;
            }

            // 2. Vỏ đá nham thạch đen gồ ghề bao quanh
            for (int i = 0; i < 4; i++)
            {
                GameObject crust = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crust.transform.SetParent(_meteorMesh, false);
                crust.transform.localPosition = Random.onUnitSphere * 0.5f;
                crust.transform.localRotation = Random.rotation;
                crust.transform.localScale = Vector3.one * 0.7f;
                Destroy(crust.GetComponent<Collider>());

                Renderer cr = crust.GetComponent<Renderer>();
                if (cr != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material cMat = new Material(s);
                    cMat.color = new Color(0.18f, 0.14f, 0.12f); // Đá núi lửa đen
                    cr.sharedMaterial = cMat;
                }
            }

            // 3. Vệt lửa đuôi kéo dài
            for (int i = 0; i < 6; i++)
            {
                GameObject ember = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ember.transform.SetParent(transform, false);
                ember.transform.localScale = Vector3.one * Random.Range(0.4f, 0.7f);
                Destroy(ember.GetComponent<Collider>());

                Renderer er = ember.GetComponent<Renderer>();
                if (er != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material emMat = new Material(s);
                    emMat.color = new Color(1.0f, 0.6f, 0.1f, 0.85f);
                    er.sharedMaterial = emMat;
                }
                _trailEmbers.Add(ember.transform);
            }
        }

        private void Update()
        {
            if (_hasImpacted) return;

            _elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsedTime / _flightDuration);

            // Đường bay lao dốc gia tốc (Ease-In Cubic)
            float curve = t * t;
            transform.position = Vector3.Lerp(_startPosition, _targetPosition, curve);

            // Xoay tròn thiên thạch tạo hiệu ứng cuộn lửa
            if (_meteorMesh != null)
            {
                _meteorMesh.Rotate(new Vector3(180f, 240f, 90f) * Time.deltaTime);
            }

            // Cập nhật các hạt tàn tích đuôi lửa bay phía sau
            Vector3 flightDir = (_startPosition - transform.position).normalized;
            for (int i = 0; i < _trailEmbers.Count; i++)
            {
                if (_trailEmbers[i] != null)
                {
                    float offset = (i + 1) * 0.7f;
                    Vector3 jitter = Random.insideUnitSphere * 0.35f;
                    _trailEmbers[i].position = transform.position + flightDir * offset + jitter;
                }
            }

            if (t >= 1.0f)
            {
                OnImpact();
            }
        }

        /// <summary>
        /// Kích hoạt khi thiên thạch va chạm mặt đất.
        /// </summary>
        private void OnImpact()
        {
            _hasImpacted = true;

            // 1. Rung chấn màn hình cực mạnh
            CameraController.Shake(0.45f, 0.45f);

            // 2. Gây 400 sát thương diện rộng lên toàn bộ quái vật
            ApplyAoEDamage();

            // 3. Sinh hiệu ứng nổ tung khói lửa đất đá
            SpawnImpactExplosionVFX();

            // 4. Tiêu hủy projectile
            Destroy(gameObject, 0.05f);
        }

        private void ApplyAoEDamage()
        {
            Collider[] hitColliders = Physics.OverlapSphere(_targetPosition, _aoeRadius);
            HashSet<IDamageable> damagedEntities = new HashSet<IDamageable>();

            foreach (Collider hit in hitColliders)
            {
                IDamageable target = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (target != null && !damagedEntities.Contains(target))
                {
                    damagedEntities.Add(target);

                    // Sát thương phân rã nhẹ theo khoảng cách (ở rìa vẫn nhận tối thiểu 65% = 260 sát thương)
                    float dist = Vector3.Distance(hit.transform.position, _targetPosition);
                    float factor = Mathf.Clamp01(1f - (dist / _aoeRadius) * 0.35f);
                    float actualDamage = _damage * factor;

                    target.TakeDamage(actualDamage, DamageType.Pure);
                }
            }

            Debug.Log($"<color=#FF5722>[MeteorShower] Thiên Thạch phát nổ! Gây {_damage} ST diện rộng lên {damagedEntities.Count} mục tiêu trong bán kính {_aoeRadius:F1}m.</color>");
        }

        private void SpawnImpactExplosionVFX()
        {
            GameObject explosion = new GameObject("Meteor_Impact_Explosion");
            explosion.transform.position = _targetPosition;

            // 1. Quả cầu lửa nổ bung phồng to rồi tan biến
            GameObject fireSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireSphere.transform.SetParent(explosion.transform, false);
            fireSphere.transform.localScale = Vector3.one * 1.5f;
            Destroy(fireSphere.GetComponent<Collider>());
            Renderer fr = fireSphere.GetComponent<Renderer>();
            if (fr != null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                Material fMat = new Material(s);
                fMat.color = new Color(1.0f, 0.45f, 0.1f, 0.9f);
                fr.sharedMaterial = fMat;
            }

            // 2. Vệt nứt cháy đen trên mặt đất (Scorched Crater)
            GameObject crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crater.transform.SetParent(explosion.transform, false);
            crater.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            crater.transform.localScale = new Vector3(_aoeRadius * 1.8f, 0.02f, _aoeRadius * 1.8f);
            Destroy(crater.GetComponent<Collider>());
            Renderer cr = crater.GetComponent<Renderer>();
            if (cr != null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                Material cMat = new Material(s);
                cMat.color = new Color(0.12f, 0.08f, 0.06f, 0.85f); // Đất đá cháy khét đen sẫm
                cr.sharedMaterial = cMat;
            }

            // 3. 16 Khối đất đá rực lửa văng tung tóe ra xung quanh
            for (int i = 0; i < 16; i++)
            {
                GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debris.transform.SetParent(explosion.transform, false);
                float angle = i * Mathf.PI * 2f / 16f;
                Vector3 blastDir = new Vector3(Mathf.Cos(angle), Random.Range(0.4f, 1.2f), Mathf.Sin(angle)).normalized;
                debris.transform.localPosition = blastDir * Random.Range(0.8f, 2.2f);
                debris.transform.localScale = Vector3.one * Random.Range(0.25f, 0.5f);
                Destroy(debris.GetComponent<Collider>());

                Renderer dr = debris.GetComponent<Renderer>();
                if (dr != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material dMat = new Material(s);
                    dMat.color = (i % 2 == 0) ? new Color(1.0f, 0.5f, 0.1f) : new Color(0.2f, 0.15f, 0.15f);
                    dr.sharedMaterial = dMat;
                }
            }

            // 4. Xung kích sóng nổ lan rộng
            GameObject shockwave = new GameObject("Shockwave_Ring");
            shockwave.transform.SetParent(explosion.transform, false);
            shockwave.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            LineRenderer lr = shockwave.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.25f;
            lr.endWidth = 0.25f;
            Shader lineShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            Material lineMat = new Material(lineShader);
            lineMat.color = new Color(1.0f, 0.65f, 0.2f, 0.95f);
            lr.sharedMaterial = lineMat;

            int segs = 48;
            lr.positionCount = segs;
            for (int i = 0; i < segs; i++)
            {
                float a = i * Mathf.PI * 2f / segs;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * _aoeRadius, 0f, Mathf.Sin(a) * _aoeRadius));
            }

            // Tự tiêu hủy hiệu ứng sau 2.5s (giữ hố đất cháy thêm một lúc tạo cảm giác chiến trường)
            Destroy(explosion, 2.5f);
        }
    }
}
