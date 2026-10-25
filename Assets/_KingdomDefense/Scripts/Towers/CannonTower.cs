using System.Collections;
using UnityEngine;
using KingdomDefense.Enemy;
using KingdomDefense.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Towers
{
    /// <summary>
    /// Tháp Đại Bác / Tháp Bắn Đá Công Thành (Cannon / Catapult Tower).
    /// Bắn đạn pháo cầu vồng parabol tầm xa, ngắm đón đầu đường đi của quái vật,
    /// phát nổ diện rộng (Splash Damage AoE 2.5m) phá vỡ đội hình quái vật đông đúc.
    /// Thông số chuẩn theo tài liệu thiết kế Kingdom Defense (Bảng 2.1).
    /// </summary>
    public class CannonTower : Tower
    {
        [Header("--- THIẾT LẬP ĐẠI BÁC / MÁY BẮN ĐÁ ---")]
        [Tooltip("Bán kính sát thương nổ lan (AoE)")]
        [SerializeField] private float explosionRadius = 2.5f;

        [Tooltip("Độ cao vồng parabol tối thiểu khi bắn")]
        [SerializeField] private float minArcHeight = 1.6f;

        [Tooltip("Prefab quả đạn pháo (CannonBall)")]
        [SerializeField] private GameObject cannonBallPrefab;

        private Transform _catapultArm;
        private Vector3 _originalArmLocalPos;
        private Quaternion _originalArmLocalRot;
        private bool _isRecoiling = false;

        private void Awake()
        {
            towerDisplayName = "Tháp Đại Bác";
            baseCost = 125;
            upgradeCostLv2 = 110;
            upgradeCostLv3 = 180;
            totalInvestedGold = baseCost;

            turnSpeed = 8.5f;
            canTargetFlying = false; // Pháo mặt đất không thể nhắm bắn quái bay Dark Bat
            ApplyLevelStats();
            EnsureReferences();
        }

        public override void ApplyLevelStats()
        {
            switch (level)
            {
                case 1:
                    damage = 65f;
                    range = 4.5f;
                    fireRate = 0.6f;
                    explosionRadius = 2.5f;
                    break;
                case 2:
                    damage = 120f;
                    range = 5.0f;
                    fireRate = 0.7f;
                    explosionRadius = 3.0f;
                    break;
                case 3:
                default:
                    damage = 220f;
                    range = 5.8f;
                    fireRate = 0.9f;
                    explosionRadius = 3.8f;
                    break;
            }
        }

        protected override void EnsureReferences()
        {
            // 1. Tự động xây dựng cơ chế nòng pháo / máy bắn đá trên đỉnh tháp nếu chưa có
            Transform topMount = transform.Find("Catapult_Top_Mount");
            if (topMount == null)
            {
                GameObject mountObj = new GameObject("Catapult_Top_Mount");
                mountObj.transform.SetParent(transform, false);
                mountObj.transform.localPosition = new Vector3(0f, 2.35f, 0f);
                topMount = mountObj.transform;

                // Thân giá đỡ gỗ sồi
                GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frame.name = "Frame_Base";
                frame.transform.SetParent(topMount, false);
                frame.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                frame.transform.localScale = new Vector3(0.55f, 0.16f, 0.55f);
                Destroy(frame.GetComponent<Collider>());
                ApplyColor(frame, new Color(0.35f, 0.25f, 0.16f)); // Gỗ sồi nâu sẫm

                // Trục quay sắt kim loại
                GameObject axle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                axle.name = "Steel_Axle";
                axle.transform.SetParent(topMount, false);
                axle.transform.localPosition = new Vector3(0f, 0.20f, -0.08f);
                axle.transform.localScale = new Vector3(0.12f, 0.32f, 0.12f);
                axle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Destroy(axle.GetComponent<Collider>());
                ApplyColor(axle, new Color(0.22f, 0.22f, 0.25f)); // Sắt xám

                // Cần bắn đá (Catapult Arm)
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "Catapult_Arm";
                arm.transform.SetParent(topMount, false);
                arm.transform.localPosition = new Vector3(0f, 0.22f, 0.32f);
                arm.transform.localScale = new Vector3(0.18f, 0.14f, 0.85f);
                arm.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
                Destroy(arm.GetComponent<Collider>());
                ApplyColor(arm, new Color(0.42f, 0.30f, 0.18f));

                // Máng nạp đạn bằng sắt ở đầu cần
                GameObject bucket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bucket.name = "Ammo_Bucket";
                bucket.transform.SetParent(arm.transform, false);
                bucket.transform.localPosition = new Vector3(0f, 0.12f, 0.42f);
                bucket.transform.localScale = new Vector3(1.8f, 0.45f, 1.8f);
                Destroy(bucket.GetComponent<Collider>());
                ApplyColor(bucket, new Color(0.20f, 0.20f, 0.22f));

                // Điểm phóng đạn (FirePoint) đặt tại lòng máng nạp đạn
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(bucket.transform, false);
                fp.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                firePoint = fp.transform;

                _catapultArm = arm.transform;
                _originalArmLocalPos = arm.transform.localPosition;
                _originalArmLocalRot = arm.transform.localRotation;
            }
            else
            {
                Transform arm = topMount.Find("Catapult_Arm");
                if (arm != null)
                {
                    _catapultArm = arm;
                    _originalArmLocalPos = arm.localPosition;
                    _originalArmLocalRot = arm.localRotation;
                }
            }

            // Gán phần xoay ngắm mục tiêu là cụm Catapult trên đỉnh tháp
            partToRotate = topMount;

            // 2. Tự nạp đạn pháo CannonBall
            if (cannonBallPrefab == null)
            {
#if UNITY_EDITOR
                cannonBallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_KingdomDefense/Prefabs/CannonBall.prefab");
#endif
                if (cannonBallPrefab == null)
                {
                    cannonBallPrefab = CreateFallbackCannonBallPrefab();
                }
            }

            projectilePrefab = cannonBallPrefab;
        }

        private void ApplyColor(GameObject obj, Color color)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                Material mat = new Material(s);
                mat.color = color;
                mat.SetFloat("_Glossiness", 0.1f);
                r.sharedMaterial = mat;
            }
        }

        private GameObject CreateFallbackCannonBallPrefab()
        {
            GameObject ballObj = new GameObject("CannonBall_Template");
            ballObj.AddComponent<CannonBall>();
            ballObj.SetActive(false);
            return ballObj;
        }

        /// <summary>
        /// Bắn đón đầu vị trí mục tiêu theo vận tốc di chuyển (Lead Targeting).
        /// </summary>
        protected override void Shoot()
        {
            if (target == null) return;

            Vector3 spawnPos = (firePoint != null) ? firePoint.position : (transform.position + Vector3.up * 2.6f);

            // 1. Tính toán vị trí đón đầu (Lead Target Calculation)
            Vector3 predictedTarget = PredictLeadPosition(spawnPos);

            // 2. Xuất đạn pháo từ ObjectPool
            GameObject ballObj = null;
            if (ObjectPool.Instance != null && projectilePrefab != null)
            {
                ballObj = ObjectPool.Instance.Spawn(projectilePrefab, spawnPos, Quaternion.identity);
            }
            else if (projectilePrefab != null)
            {
                ballObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            }

            if (ballObj != null)
            {
                CannonBall cannonBall = ballObj.GetComponent<CannonBall>();
                if (cannonBall == null) cannonBall = ballObj.AddComponent<CannonBall>();

                cannonBall.Launch(spawnPos, predictedTarget, damage, explosionRadius, minArcHeight);
            }

            // 3. Hiệu ứng giật cần bắn (Recoil Kickback)
            if (_catapultArm != null && !_isRecoiling)
            {
                StartCoroutine(RecoilRoutine());
            }

            // 4. Bụi khói bắn ra tại nòng
            SpawnMuzzleBlast(spawnPos);
        }

        /// <summary>
        /// Dự đoán vị trí quái vật sẽ chạy tới dựa trên tốc độ và hướng đi của nó.
        /// </summary>
        private Vector3 PredictLeadPosition(Vector3 spawnPos)
        {
            Vector3 currentPos = target.position;
            EnemyMovement em = target.GetComponent<EnemyMovement>();

            if (em != null)
            {
                float speed = em.Speed;
                Vector3 forward = em.transform.forward;
                float dist = Vector3.Distance(new Vector3(spawnPos.x, 0f, spawnPos.z), new Vector3(currentPos.x, 0f, currentPos.z));

                // Thời gian đạn pháo bay tới đích ước lượng (tốc độ đạn pháo ~14 m/s)
                float flightTime = Mathf.Clamp(dist / 14f, 0.45f, 1.35f);

                // Điểm đón đầu quái sẽ tới
                Vector3 leadPos = currentPos + forward * (speed * flightTime);
                leadPos.y = 0.02f; // Rơi sát mặt cỏ
                return leadPos;
            }

            return new Vector3(currentPos.x, 0.02f, currentPos.z);
        }

        private IEnumerator RecoilRoutine()
        {
            _isRecoiling = true;

            // Bật giật mạnh lên trên
            Quaternion kickRot = _originalArmLocalRot * Quaternion.Euler(-25f, 0f, 0f);
            float elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                _catapultArm.localRotation = Quaternion.Slerp(_originalArmLocalRot, kickRot, elapsed / 0.08f);
                yield return null;
            }

            // Hạ cần chầm chậm về vị trí nạp đạn
            elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                _catapultArm.localRotation = Quaternion.Slerp(kickRot, _originalArmLocalRot, elapsed / 0.35f);
                yield return null;
            }

            _catapultArm.localRotation = _originalArmLocalRot;
            _isRecoiling = false;
        }

        private void SpawnMuzzleBlast(Vector3 pos)
        {
            GameObject blast = new GameObject("Muzzle_Blast_Puff");
            blast.transform.position = pos;

            for (int i = 0; i < 4; i++)
            {
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                p.transform.SetParent(blast.transform, false);
                p.transform.localPosition = Random.insideUnitSphere * 0.25f;
                p.transform.localScale = Vector3.one * Random.Range(0.2f, 0.38f);
                Destroy(p.GetComponent<Collider>());

                Renderer r = p.GetComponent<Renderer>();
                if (r != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material mat = new Material(s);
                    mat.color = new Color(0.75f, 0.72f, 0.65f, 0.75f);
                    r.sharedMaterial = mat;
                }
            }

            Destroy(blast, 0.25f);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Vẽ bán kính nổ lan của đạn pháo màu đỏ cam
            Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
