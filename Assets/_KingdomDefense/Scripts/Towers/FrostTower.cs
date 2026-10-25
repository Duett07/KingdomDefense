using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.Enemy;
using KingdomDefense.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Towers
{
    /// <summary>
    /// Tháp Băng Phong Cổ Đại (Frost Tower).
    /// Tỏa ra Vòng xoáy Bão Tuyết (Blizzard Aura) 360 độ liên tục bán kính 4.5m xung quanh tháp:
    /// - Làm chậm 50% tốc độ di chuyển của mọi quái vật trong vùng ảnh hưởng (duy trì 2.5s sau khi thoát).
    /// - Gây 10 sát thương Băng giá mỗi giây (Frost Damage) theo từng nhịp sóng băng.
    /// - Trên đỉnh tháp ngự một Khối Đại Tinh Thể Băng Tinh (Ice Crystal Core) xoay lơ lửng,
    ///   với 3 mảnh thiên thạch băng quay quanh quỹ đạo và vòng tròn rune băng giá dưới mặt đất.
    /// </summary>
    public class FrostTower : Tower
    {
        [Header("--- THIẾT LẬP BĂNG PHONG & LÀM CHẬM ---")]
        [Tooltip("Tỷ lệ làm chậm tốc độ (0.5 = giảm 50% tốc độ)")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float slowPercentage = 0.50f;

        [Tooltip("Thời gian duy trì hiệu ứng làm chậm sau khi quái thoát khỏi vòng băng (giây)")]
        [SerializeField] private float slowDuration = 2.5f;

        [Tooltip("Màu sắc ánh sáng băng tuyết (Cyan băng giá rực rỡ, tuân thủ Purple Ban)")]
        [SerializeField] private Color frostColor = new Color(0.0f, 0.9f, 1.0f, 0.85f);

        private Transform _crystalCore;
        private Vector3 _baseCrystalPos;
        private readonly List<Transform> _orbitingShards = new List<Transform>();
        private Transform _groundBlizzardRing;
        private Material _crystalMaterial;

        private void Awake()
        {
            towerDisplayName = "Tháp Băng Phong";
            baseCost = 175;
            upgradeCostLv2 = 120;
            upgradeCostLv3 = 200;
            totalInvestedGold = baseCost;

            turnSpeed = 0f; // Tháp hào quang 360 độ không cần xoay nòng đơn lẻ
            ApplyLevelStats();
            EnsureReferences();
        }

        public override void ApplyLevelStats()
        {
            switch (level)
            {
                case 1:
                    damage = 10f;
                    range = 4.0f;
                    fireRate = 1.0f;
                    slowPercentage = 0.40f;
                    break;
                case 2:
                    damage = 18f;
                    range = 4.8f;
                    fireRate = 1.0f;
                    slowPercentage = 0.50f;
                    break;
                case 3:
                default:
                    damage = 30f;
                    range = 5.5f;
                    fireRate = 1.0f;
                    slowPercentage = 0.65f;
                    break;
            }
        }

        protected override void EnsureReferences()
        {
            // 1. Tạo Khối Tinh Thể Băng Cổ Đại lơ lửng trên đỉnh tháp nếu chưa có
            Transform existingCore = transform.Find("Ice_Crystal_Structure");
            if (existingCore == null)
            {
                CreateFrostCrystalStructure();
            }
            else
            {
                _crystalCore = existingCore.Find("Crystal_Core");
                if (_crystalCore != null)
                {
                    _baseCrystalPos = _crystalCore.localPosition;
                }
            }

            // 2. Tạo Vòng Hào Quang Bão Tuyết Mặt Đất (Ground Blizzard Aura Ring)
            Transform existingRing = transform.Find("Ground_Blizzard_Ring");
            if (existingRing == null)
            {
                CreateGroundBlizzardRing();
            }
            else
            {
                _groundBlizzardRing = existingRing;
            }

            if (partToRotate == null) partToRotate = transform;
            if (firePoint == null && _crystalCore != null) firePoint = _crystalCore;
        }

        /// <summary>
        /// Dựng kiến trúc đài tế và Tinh thể Pha lê Băng Tuyết đa giác lơ lửng.
        /// </summary>
        private void CreateFrostCrystalStructure()
        {
            GameObject structRoot = new GameObject("Ice_Crystal_Structure");
            structRoot.transform.SetParent(transform, false);
            structRoot.transform.localPosition = new Vector3(0f, 2.35f, 0f);

            // Bệ đá tế thờ ngọc băng (Altar Pedestal)
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name = "Altar_Pedestal";
            pedestal.transform.SetParent(structRoot.transform, false);
            pedestal.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            pedestal.transform.localScale = new Vector3(0.55f, 0.12f, 0.55f);
            Destroy(pedestal.GetComponent<Collider>());

            // Pha lê chính (Diamond / Octahedron Style Core)
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Crystal_Core";
            core.transform.SetParent(structRoot.transform, false);
            core.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            core.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            core.transform.localScale = new Vector3(0.38f, 0.72f, 0.38f);
            Destroy(core.GetComponent<Collider>());

            Shader unlitShader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            _crystalMaterial = new Material(unlitShader);
            _crystalMaterial.color = frostColor;

            Renderer coreR = core.GetComponent<Renderer>();
            if (coreR != null) coreR.sharedMaterial = _crystalMaterial;

            _crystalCore = core.transform;
            _baseCrystalPos = core.transform.localPosition;

            // 3 Mảnh thiên thạch băng nhỏ quay quanh tinh thể chính (Orbiting Shards)
            for (int i = 0; i < 3; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"Orbiting_Shard_{i}";
                shard.transform.SetParent(structRoot.transform, false);
                float angle = i * (Mathf.PI * 2f / 3f);
                shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.55f, 0.55f, Mathf.Sin(angle) * 0.55f);
                shard.transform.localRotation = Quaternion.Euler(30f, 60f * i, 45f);
                shard.transform.localScale = Vector3.one * 0.15f;
                Destroy(shard.GetComponent<Collider>());

                Renderer sR = shard.GetComponent<Renderer>();
                if (sR != null) sR.sharedMaterial = _crystalMaterial;

                _orbitingShards.Add(shard.transform);
            }
        }

        /// <summary>
        /// Tạo vòng tròn Rune Bão Tuyết trên mặt đất hiển thị trực quan tầm ảnh hưởng 4.5m.
        /// </summary>
        private void CreateGroundBlizzardRing()
        {
            GameObject ringObj = new GameObject("Ground_Blizzard_Ring");
            ringObj.transform.SetParent(transform, false);
            ringObj.transform.localPosition = new Vector3(0f, 0.035f, 0f);

            // Vòng tròn hào quang băng
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Blizzard_Disc";
            disc.transform.SetParent(ringObj.transform, false);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localScale = new Vector3(range * 2f, 0.005f, range * 2f);
            Destroy(disc.GetComponent<Collider>());

            Renderer rDisc = disc.GetComponent<Renderer>();
            if (rDisc != null)
            {
                Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                Material mat = new Material(s);
                mat.color = new Color(0.0f, 0.85f, 1.0f, 0.22f); // Lam băng bán trong suốt
                rDisc.sharedMaterial = mat;
            }

            // 6 Điểm dấu Rune Tinh thể Tuyết xung quanh viền
            for (int i = 0; i < 6; i++)
            {
                GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rune.name = $"Rune_Snowflake_{i}";
                rune.transform.SetParent(ringObj.transform, false);
                float angle = i * (Mathf.PI * 2f / 6f);
                float radius = range * 0.94f;
                rune.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.01f, Mathf.Sin(angle) * radius);
                rune.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 45f, 0f);
                rune.transform.localScale = new Vector3(0.35f, 0.015f, 0.35f);
                Destroy(rune.GetComponent<Collider>());

                Renderer rRune = rune.GetComponent<Renderer>();
                if (rRune != null)
                {
                    Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                    Material mat = new Material(s);
                    mat.color = new Color(0.7f, 0.95f, 1.0f, 0.75f);
                    rRune.sharedMaterial = mat;
                }
            }

            _groundBlizzardRing = ringObj.transform;
        }

        protected override void Update()
        {
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
                    return; // Bị choáng từ Boss: ngừng phát xung và làm chậm
                }
            }

            // 1. Cập nhật hoạt ảnh 3D Tinh Thể Băng & Vòng Tuyết
            AnimateFrostVisuals();

            // 2. Cơ chế Aura 360 độ: Quét liên tục mọi quái trong bán kính 4.5m và áp dụng Làm Chậm 50%
            ApplyBlizzardSlowAura();

            // 3. Chu kỳ phát xung năng lượng băng (Frost Damage Pulse)
            if (fireCountdown <= 0f)
            {
                Shoot();
                fireCountdown = 1f / fireRate;
            }

            fireCountdown -= Time.deltaTime;
        }

        private void AnimateFrostVisuals()
        {
            // Pha lê chính xoay và nhấp nhô lơ lửng
            if (_crystalCore != null)
            {
                _crystalCore.Rotate(Vector3.up, 35f * Time.deltaTime, Space.World);
                float bob = Mathf.Sin(Time.time * 2.2f) * 0.07f;
                _crystalCore.localPosition = _baseCrystalPos + new Vector3(0f, bob, 0f);
            }

            // Các mảnh băng quay quanh quỹ đạo
            if (_crystalCore != null && _orbitingShards.Count > 0)
            {
                float orbitSpeed = 65f * Time.deltaTime;
                Vector3 center = _crystalCore.position;
                foreach (Transform shard in _orbitingShards)
                {
                    if (shard != null)
                    {
                        shard.RotateAround(center, Vector3.up, orbitSpeed);
                        shard.Rotate(Vector3.forward, 45f * Time.deltaTime);
                    }
                }
            }

            // Vòng tròn Rune dưới đất xoay chậm ngược chiều
            if (_groundBlizzardRing != null)
            {
                _groundBlizzardRing.Rotate(Vector3.up, -18f * Time.deltaTime);
            }
        }

        /// <summary>
        /// Duyệt quét toàn bộ quái vật đang hoạt động.
        /// Mọi quái bước vào vùng bán kính 4.5m lập tức bị làm chậm 50% trong 2.5 giây.
        /// </summary>
        private void ApplyBlizzardSlowAura()
        {
            Vector3 towerPos = transform.position;
            float sqrRange = range * range;

            for (int i = 0; i < Enemy.Enemy.ActiveEnemies.Count; i++)
            {
                Enemy.Enemy enemy = Enemy.Enemy.ActiveEnemies[i];
                if (enemy == null || enemy.IsDead) continue;

                Vector3 enemyPos = enemy.transform.position;
                float distSqr = (enemyPos.x - towerPos.x) * (enemyPos.x - towerPos.x) +
                                (enemyPos.z - towerPos.z) * (enemyPos.z - towerPos.z);

                if (distSqr <= sqrRange)
                {
                    enemy.ApplySlow(slowPercentage, slowDuration);
                }
            }
        }

        /// <summary>
        /// Phát xung năng lượng Bão Tuyết (Frost Nova Pulse):
        /// Gây sát thương băng cho toàn bộ quái vật trong phạm vi và kích hoạt hiệu ứng sóng băng mở rộng.
        /// </summary>
        protected override void Shoot()
        {
            Vector3 towerPos = transform.position;
            float sqrRange = range * range;
            bool hitAny = false;

            for (int i = 0; i < Enemy.Enemy.ActiveEnemies.Count; i++)
            {
                Enemy.Enemy enemy = Enemy.Enemy.ActiveEnemies[i];
                if (enemy == null || enemy.IsDead) continue;

                Vector3 enemyPos = enemy.transform.position;
                float distSqr = (enemyPos.x - towerPos.x) * (enemyPos.x - towerPos.x) +
                                (enemyPos.z - towerPos.z) * (enemyPos.z - towerPos.z);

                if (distSqr <= sqrRange)
                {
                    enemy.TakeDamage(damage, DamageType.Magic);
                    hitAny = true;
                }
            }

            // Hiệu ứng sóng băng giá tỏa ra
            SpawnBlizzardPulseFX();

            if (hitAny && _crystalCore != null)
            {
                StartCoroutine(FlareCrystalRoutine());
            }
        }

        /// <summary>
        /// Tạo làn sóng sương tuyết mở rộng lan dần từ tâm tháp ra mép bán kính 4.5m.
        /// </summary>
        private void SpawnBlizzardPulseFX()
        {
            GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pulse.name = "Frost_Nova_Wave";
            pulse.transform.position = transform.position + new Vector3(0f, 0.05f, 0f);
            pulse.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
            Destroy(pulse.GetComponent<Collider>());

            Renderer r = pulse.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material mat = new Material(s);
                mat.color = new Color(0.5f, 0.95f, 1.0f, 0.65f);
                r.sharedMaterial = mat;
            }

            StartCoroutine(ExpandPulseRoutine(pulse, range * 2f));
        }

        private IEnumerator ExpandPulseRoutine(GameObject pulseObj, float targetDiameter)
        {
            float elapsed = 0f;
            float duration = 0.35f;
            Vector3 initialScale = new Vector3(0.5f, 0.01f, 0.5f);
            Vector3 targetScale = new Vector3(targetDiameter, 0.005f, targetDiameter);

            while (elapsed < duration)
            {
                if (pulseObj == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                pulseObj.transform.localScale = Vector3.Lerp(initialScale, targetScale, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (pulseObj != null) Destroy(pulseObj);
        }

        private IEnumerator FlareCrystalRoutine()
        {
            if (_crystalMaterial == null) yield break;

            Color original = frostColor;
            Color flared = Color.white; // Chớp trắng băng tuyết

            _crystalMaterial.color = flared;
            yield return new WaitForSeconds(0.08f);
            _crystalMaterial.color = original;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Vẽ bán kính bão tuyết màu Cyan băng
            Gizmos.color = new Color(0.0f, 0.9f, 1.0f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
