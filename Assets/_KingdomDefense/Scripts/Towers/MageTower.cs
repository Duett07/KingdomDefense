using System.Collections;
using UnityEngine;
using KingdomDefense.Enemy;
using KingdomDefense.VFX;
using KingdomDefense.Audio;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Towers
{
    /// <summary>
    /// Tháp Pháp Sư Cổ Đại (Mage Tower).
    /// Bắn ra luồng tia sét ma thuật tức thời (Arcane Lightning Beam) màu xanh lam điện rực rỡ,
    /// gây sát thương phép thuật thuần túy (45 Magic Damage) bỏ qua 100% giáp vật lý của quái vật.
    /// Là khắc tinh số một của các chủng quái trâu bò nhiều giáp (Stone Golem, Orc Warrior).
    /// </summary>
    public class MageTower : Tower
    {
        [Header("--- THIẾT LẬP PHÁP SƯ MA THUẬT ---")]
        [Tooltip("Số đoạn gấp khúc zíc-zắc của tia sét")]
        [SerializeField] private int lightningSegments = 7;

        [Tooltip("Độ lệch nhánh điện ngẫu nhiên")]
        [SerializeField] private float lightningJitter = 0.15f;

        [Tooltip("Màu sắc tia sét ma thuật (Lam điện rực rỡ)")]
        [SerializeField] private Color lightningColor = new Color(0.05f, 0.92f, 1.0f, 0.95f);

        private LineRenderer _lightningLine;
        private Transform _staffCrystal;
        private Vector3 _originalCrystalScale;
        private Transform _mageFigure;
        private bool _isFiringBeam = false;

        private void Awake()
        {
            towerDisplayName = "Tháp Pháp Sư";
            baseCost = 150;
            upgradeCostLv2 = 150;
            upgradeCostLv3 = 240;
            totalInvestedGold = baseCost;

            turnSpeed = 14f;
            ApplyLevelStats();
            EnsureReferences();
        }

        public override void ApplyLevelStats()
        {
            switch (level)
            {
                case 1:
                    damage = 45f;
                    range = 5.0f;
                    fireRate = 1.0f;
                    break;
                case 2:
                    damage = 90f;
                    range = 5.5f;
                    fireRate = 1.2f;
                    break;
                case 3:
                default:
                    damage = 170f;
                    range = 6.5f;
                    fireRate = 1.5f;
                    break;
            }
        }

        protected override void EnsureReferences()
        {
            // 1. Tự nạp và dựng mô hình Pháp Sư 3D trên đỉnh tháp nếu chưa có
            Transform existingMage = transform.Find("Figure_Mage");
            if (existingMage == null)
            {
                GameObject magePrefab = null;
                GameObject staffPrefab = null;
                GameObject spellbookPrefab = null;
                Material mageMat = null;
                Material kayKitMat = null;

#if UNITY_EDITOR
                string baseChar = "Assets/_KingdomDefense/Models/Characters/";
                magePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseChar + "Adventures/Mage.fbx");
                staffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseChar + "Weapons/staff.fbx");
                spellbookPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(baseChar + "Weapons/spellbook_open.fbx");
                mageMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_KingdomDefense/Materials/M_Mage.mat");
                kayKitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_KingdomDefense/Materials/M_KayKit_Medieval.mat");
#endif

                if (magePrefab != null)
                {
                    GameObject mageObj = Instantiate(magePrefab, transform);
                    mageObj.name = "Figure_Mage";
                    mageObj.transform.localPosition = new Vector3(0f, 2.38f, 0f);
                    mageObj.transform.localScale = Vector3.one * 0.42f;

                    if (mageMat != null)
                    {
                        foreach (Renderer r in mageObj.GetComponentsInChildren<Renderer>())
                        {
                            r.sharedMaterial = mageMat;
                        }
                    }

                    // Trang bị Gậy trượng ma thuật (Staff) bên tay phải
                    if (staffPrefab != null)
                    {
                        GameObject staff = Instantiate(staffPrefab, mageObj.transform);
                        staff.name = "Equipped_Staff";
                        staff.transform.localPosition = new Vector3(0.24f, 0.45f, 0.28f);
                        staff.transform.localRotation = Quaternion.Euler(15f, 0f, -10f);
                        staff.transform.localScale = Vector3.one * 0.85f;

                        if (kayKitMat != null)
                        {
                            foreach (Renderer r in staff.GetComponentsInChildren<Renderer>())
                            {
                                r.sharedMaterial = kayKitMat;
                            }
                        }

                        // Gắn Viên ngọc ma thuật phát sáng xanh lam trên đầu trượng
                        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        crystal.name = "Staff_Arcane_Crystal";
                        crystal.transform.SetParent(staff.transform, false);
                        crystal.transform.localPosition = new Vector3(0f, 0.68f, 0f);
                        crystal.transform.localScale = Vector3.one * 0.22f;
                        Destroy(crystal.GetComponent<Collider>());

                        Renderer rC = crystal.GetComponent<Renderer>();
                        if (rC != null)
                        {
                            Shader s = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                            Material cMat = new Material(s);
                            cMat.color = new Color(0.1f, 0.95f, 1.0f); // Lam điện chớp sáng
                            rC.sharedMaterial = cMat;
                        }

                        _staffCrystal = crystal.transform;
                        _originalCrystalScale = crystal.transform.localScale;

                        // Điểm xuất phát tia sét (FirePoint) đặt tại tâm viên ngọc
                        GameObject fp = new GameObject("FirePoint");
                        fp.transform.SetParent(crystal.transform, false);
                        fp.transform.localPosition = Vector3.zero;
                        firePoint = fp.transform;
                    }

                    // Trang bị Sách phép thuật (Spellbook) mở ra bên tay trái
                    if (spellbookPrefab != null)
                    {
                        GameObject book = Instantiate(spellbookPrefab, mageObj.transform);
                        book.name = "Equipped_Spellbook";
                        book.transform.localPosition = new Vector3(-0.25f, 0.48f, 0.22f);
                        book.transform.localRotation = Quaternion.Euler(20f, -30f, 15f);
                        book.transform.localScale = Vector3.one * 0.75f;

                        if (kayKitMat != null)
                        {
                            foreach (Renderer r in book.GetComponentsInChildren<Renderer>())
                            {
                                r.sharedMaterial = kayKitMat;
                            }
                        }
                    }

                    _mageFigure = mageObj.transform;
                    partToRotate = mageObj.transform;
                }
            }
            else
            {
                _mageFigure = existingMage;
                partToRotate = existingMage;

                Transform crystal = existingMage.Find("Equipped_Staff/Staff_Arcane_Crystal");
                if (crystal != null)
                {
                    _staffCrystal = crystal;
                    _originalCrystalScale = crystal.localScale;
                }
            }

            // Fallback nếu không có figure
            if (partToRotate == null) partToRotate = transform;
            if (firePoint == null)
            {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform, false);
                fp.transform.localPosition = new Vector3(0f, 2.7f, 0f);
                firePoint = fp.transform;
            }

            // 2. Khởi tạo LineRenderer chuyên biệt cho Tia sét ma thuật (Arcane Lightning)
            SetupLightningLine();
        }

        private void SetupLightningLine()
        {
            _lightningLine = GetComponent<LineRenderer>();
            if (_lightningLine == null)
            {
                _lightningLine = gameObject.AddComponent<LineRenderer>();
                _lightningLine.startWidth = 0.14f;
                _lightningLine.endWidth = 0.05f;
                _lightningLine.positionCount = lightningSegments;
                _lightningLine.enabled = false;

                Shader unlit = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                if (unlit != null)
                {
                    Material lineMat = new Material(unlit);
                    lineMat.color = lightningColor;
                    _lightningLine.material = lineMat;
                }
            }
        }

        /// <summary>
        /// Phóng tia sét ma thuật xuyên giáp tức thời vào mục tiêu.
        /// </summary>
        protected override void Shoot()
        {
            if (target == null) return;

            Vector3 startPos = (firePoint != null) ? firePoint.position : (transform.position + Vector3.up * 2.7f);
            Vector3 targetPos = target.position + new Vector3(0f, 0.5f, 0f); // Ngắm trúng ngực quái

            // 1. Gây sát thương Phép thuật thuần túy (Magic Damage) bỏ qua giáp vật lý
            Enemy.Enemy enemy = target.GetComponent<Enemy.Enemy>() ?? target.GetComponentInParent<Enemy.Enemy>();
            if (enemy != null)
            {
                enemy.TakeMagicDamage(damage);
            }

            // 2. Kích hoạt hiệu ứng giật tia sét điện từ sống động
            if (!_isFiringBeam)
            {
                StartCoroutine(LightningStrikeRoutine(startPos, target));
            }

            // 3. Hiệu ứng viên ngọc trên đầu gậy bừng sáng & giải phóng tia lửa điện
            if (_staffCrystal != null)
            {
                StartCoroutine(PulseCrystalRoutine());
            }

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnLightningSparks(firePoint != null ? firePoint.position : startPos);
            }

            // 4. Tạo hiệu ứng điện quang giải phóng tại mục tiêu (Target Discharge Spark) & âm thanh sét
            SpawnTargetDischargeFX(targetPos);
            AudioManager.Instance?.PlayMagicZap();
        }

        /// <summary>
        /// Tạo tia sét zíc-zắc chớp nháy nhiều lần trong 0.18 giây nối giữa đầu trượng và quái vật.
        /// </summary>
        private IEnumerator LightningStrikeRoutine(Vector3 startPos, Transform targetTransform)
        {
            _isFiringBeam = true;
            if (_lightningLine == null) SetupLightningLine();

            _lightningLine.enabled = true;
            float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                Vector3 currentStart = (firePoint != null) ? firePoint.position : startPos;
                Vector3 currentEnd = (targetTransform != null) ? (targetTransform.position + Vector3.up * 0.5f) : (startPos + transform.forward * range);

                DrawZigZagLightning(currentStart, currentEnd);
                yield return null;
            }

            _lightningLine.enabled = false;
            _isFiringBeam = false;
        }

        private void DrawZigZagLightning(Vector3 start, Vector3 end)
        {
            if (_lightningLine == null) return;

            Vector3 dir = end - start;
            Vector3 right = Vector3.Cross(dir.normalized, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, dir.normalized).normalized;

            _lightningLine.positionCount = lightningSegments;
            _lightningLine.SetPosition(0, start);
            _lightningLine.SetPosition(lightningSegments - 1, end);

            for (int i = 1; i < lightningSegments - 1; i++)
            {
                float t = i / (float)(lightningSegments - 1);
                Vector3 basePos = Vector3.Lerp(start, end, t);

                // Thêm độ lệch điện trường zíc-zắc ngẫu nhiên
                float jitterX = Random.Range(-lightningJitter, lightningJitter);
                float jitterY = Random.Range(-lightningJitter * 0.6f, lightningJitter * 0.6f);
                Vector3 offset = (right * jitterX) + (up * jitterY);

                _lightningLine.SetPosition(i, basePos + offset);
            }
        }

        private IEnumerator PulseCrystalRoutine()
        {
            if (_staffCrystal == null) yield break;

            Vector3 flaredScale = _originalCrystalScale * 1.55f;
            float t = 0f;

            while (t < 0.08f)
            {
                t += Time.deltaTime;
                _staffCrystal.localScale = Vector3.Lerp(_originalCrystalScale, flaredScale, t / 0.08f);
                yield return null;
            }

            t = 0f;
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                _staffCrystal.localScale = Vector3.Lerp(flaredScale, _originalCrystalScale, t / 0.18f);
                yield return null;
            }

            _staffCrystal.localScale = _originalCrystalScale;
        }

        /// <summary>
        /// Tạo hiệu ứng tia lửa điện phát nổ giải phóng tại mục tiêu khi trúng sét.
        /// </summary>
        private void SpawnTargetDischargeFX(Vector3 pos)
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnElectricImpact(pos);
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Vẽ bán kính ma thuật xanh lam rực rỡ
            Gizmos.color = new Color(0.05f, 0.92f, 1.0f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
