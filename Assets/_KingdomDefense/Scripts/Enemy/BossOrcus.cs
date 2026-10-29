using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.Towers;
using KingdomDefense.Cameras;

namespace KingdomDefense.Enemy
{
    /// <summary>
    /// Trùm Cuối: Ma Vương Orcus the Destroyer (Boss Orcus).
    /// Sở hữu lượng máu khổng lồ (3800 HP), 40% Giáp vật lý, 30% Kháng phép thuật,
    /// và kỹ năng đặc trưng "Dậm Đất Chấn Động" (Stun Quake):
    /// Cứ mỗi 8 giây, Boss dậm mạnh xuống đất tạo làn sóng chấn động bán kính 6.0m,
    /// làm tê liệt/choáng (Stun) toàn bộ Tháp phòng thủ xung quanh trong 3.0 giây.
    /// </summary>
    public class BossOrcus : Enemy
    {
        [Header("--- KỸ NĂNG MA VƯƠNG: DẬM ĐẤT CHẤN ĐỘNG (STUN QUAKE) ---")]
        [Tooltip("Bán kính chấn động làm choáng tháp (m)")]
        [SerializeField] private float stunRadius = 6.0f;

        [Tooltip("Thời gian làm choáng các tháp (giây)")]
        [SerializeField] private float stunDuration = 3.0f;

        [Tooltip("Khoảng cách giữa các lần tung chiêu dậm đất (giây)")]
        [SerializeField] private float slamInterval = 8.0f;

        private float _slamCountdown;
        private bool _isPerformingSlam = false;

        private void Awake()
        {
            // Cấu hình thông số chuẩn Boss Orcus theo Bảng 2.1 (Cân bằng lại 3600 HP, 35% giáp, 25% kháng phép)
            ConfigureStats("Ma Vương Orcus", 3600f, 0.35f, 0.25f, 250, true);
            _slamCountdown = 4.0f; // Đợt dậm đất đầu tiên sau 4 giây xuất trận
        }

        private void Start()
        {
            EnsureBossMaterialsValid();
        }

        public override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();
            EnsureBossMaterialsValid();
        }

        /// <summary>
        /// Đảm bảo tất cả bộ phận (thân hình, sừng, vũ khí) của Boss Orcus đều có Material chuẩn,
        /// tuyệt đối triệt tiêu hiện tượng khối trắng (missing material/white block).
        /// </summary>
        public void EnsureBossMaterialsValid()
        {
            Material bossMat = Resources.Load<Material>("Materials/M_BossOrcus") ??
                               Resources.Load<Material>("Materials/M_Knight");
#if UNITY_EDITOR
            if (bossMat == null)
            {
                bossMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_KingdomDefense/Materials/M_BossOrcus.mat") ??
                          UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/_KingdomDefense/Materials/M_Knight.mat");
            }
#endif

            if (bossMat != null)
            {
                Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in allRenderers)
                {
                    if (r == null) continue;
                    string rName = r.gameObject.name;
                    if (rName == "Fill" || rName == "Background" || rName == "Orcus_Quake_Ring" || rName == "Frost_Ground_Ring") continue;

                    if (r.sharedMaterial == null || r.sharedMaterial.name.Contains("Default"))
                    {
                        r.sharedMaterial = bossMat;
                    }
                }
            }
        }

        private void Update()
        {
            if (IsDead) return;

            _slamCountdown -= Time.deltaTime;
            if (_slamCountdown <= 0f && !_isPerformingSlam)
            {
                StartCoroutine(PerformStunSlamRoutine());
                _slamCountdown = slamInterval;
            }
        }

        /// <summary>
        /// Kích hoạt chuỗi hoạt ảnh và kỹ năng Dậm Đất Chấn Động.
        /// </summary>
        private IEnumerator PerformStunSlamRoutine()
        {
            _isPerformingSlam = true;

            // 1. Boss hơi nhấc mình lên chuẩn bị dậm
            Vector3 startPos = transform.position;
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                transform.position = startPos + Vector3.up * Mathf.Sin(t / 0.2f * Mathf.PI * 0.5f) * 0.45f;
                yield return null;
            }

            // 2. Dậm mạnh xuống đất & rung chấn màn hình
            transform.position = startPos;
            CameraController.ShakeBossStomp();

            // 3. Quét và làm choáng toàn bộ tháp trong phạm vi 6.0m
            Tower[] allTowers = Object.FindObjectsByType<Tower>();
            Vector3 bossPos = transform.position;
            int stunnedCount = 0;

            foreach (Tower tower in allTowers)
            {
                if (tower == null) continue;

                float dist = Vector3.Distance(bossPos, tower.transform.position);
                if (dist <= stunRadius)
                {
                    tower.Stun(stunDuration);
                    stunnedCount++;
                }
            }

            Debug.Log($"[Boss Orcus] DẬM ĐẤT CHẤN ĐỘNG! Đã làm choáng {stunnedCount} tháp phòng thủ trong bán kính {stunRadius}m!");

            // 4. Sinh hiệu ứng làn sóng chấn động mặt đất
            SpawnStunQuakeFX();

            yield return new WaitForSeconds(0.3f);
            _isPerformingSlam = false;
        }

        /// <summary>
        /// Tạo hiệu ứng làn sóng chấn động màu đỏ/vàng cam mở rộng bán kính 6.0m trên mặt đất.
        /// </summary>
        private void SpawnStunQuakeFX()
        {
            GameObject quake = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            quake.name = "Orcus_Quake_Ring";
            quake.transform.position = transform.position + new Vector3(0f, 0.05f, 0f);
            quake.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
            Destroy(quake.GetComponent<Collider>());

            Renderer r = quake.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material mat = new Material(s);
                mat.color = new Color(1.0f, 0.25f, 0.1f, 0.75f); // Đỏ lửa chấn động
                r.sharedMaterial = mat;
            }

            StartCoroutine(ExpandQuakeRoutine(quake, stunRadius * 2f));
        }

        private IEnumerator ExpandQuakeRoutine(GameObject ringObj, float targetDiameter)
        {
            float elapsed = 0f;
            float duration = 0.45f;
            Vector3 initialScale = new Vector3(0.5f, 0.01f, 0.5f);
            Vector3 targetScale = new Vector3(targetDiameter, 0.005f, targetDiameter);

            while (elapsed < duration)
            {
                if (ringObj == null) yield break;
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                ringObj.transform.localScale = Vector3.Lerp(initialScale, targetScale, Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }

            if (ringObj != null) Destroy(ringObj);
        }

        private void OnDrawGizmosSelected()
        {
            // Bán kính kỹ năng làm choáng tháp
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, stunRadius);
        }
    }
}
