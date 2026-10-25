using UnityEngine;
using KingdomDefense.Waypoints;
using KingdomDefense.UI;

namespace KingdomDefense.Enemy
{
    /// <summary>
    /// Điều khiển quái vật di chuyển tuần tự qua các điểm nút Waypoints dọc theo đường mòn.
    /// Tự động xoay hướng mượt mà, chuyển mốc khi tiếp cận và trừ mạng người chơi khi chạm cổng thành.
    /// </summary>
    public class EnemyMovement : MonoBehaviour
    {
        [Header("--- THIẾT LẬP DI CHUYỂN ---")]
        [Tooltip("Tốc độ di chuyển tịnh tiến của quái vật")]
        [SerializeField] private float speed = 3.2f;

        [Tooltip("Tốc độ xoay mặt theo khúc cua (càng lớn xoay càng nhanh)")]
        [SerializeField] private float rotationSpeed = 12f;

        [Tooltip("Khoảng cách tối thiểu để tính là đã chạm mốc tiếp theo")]
        [SerializeField] private float waypointThreshold = 0.2f;

        private Transform _targetPoint;
        private int _waypointIndex = 0;
        private bool _hasEnded = false;
        private float _walkCycle = 0f;

        private float _baseSpeed;

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public float BaseSpeed
        {
            get => _baseSpeed > 0f ? _baseSpeed : speed;
            set => _baseSpeed = value;
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public void ResetSpeed()
        {
            speed = BaseSpeed;
        }

        private void Awake()
        {
            _baseSpeed = speed;
        }

        private void Start()
        {
            RestartPath();
        }

        /// <summary>
        /// Khởi động lại lộ trình di chuyển từ Waypoint 0 (sử dụng khi tái sinh từ Object Pool).
        /// </summary>
        public void RestartPath()
        {
            _hasEnded = false;
            _waypointIndex = 0;
            ResetSpeed();

            // Kiểm tra và lấy danh sách Waypoints
            if (Waypoints.Waypoints.Points == null || Waypoints.Waypoints.Points.Length == 0)
            {
                Debug.LogWarning("[EnemyMovement] Không tìm thấy danh sách Waypoints trong Scene!");
                return;
            }

            // Bắt đầu hành quân từ điểm mốc đầu tiên
            _targetPoint = Waypoints.Waypoints.Points[0];
            transform.position = _targetPoint.position;

            // Hướng tới điểm tiếp theo
            if (Waypoints.Waypoints.Points.Length > 1)
            {
                _waypointIndex = 1;
                _targetPoint = Waypoints.Waypoints.Points[_waypointIndex];
            }
        }

        private void Update()
        {
            if (_hasEnded || _targetPoint == null) return;

            // 1. Tính toán vector hướng tới điểm mốc mục tiêu (bỏ qua độ lệch trục Y nếu có)
            Vector3 targetPos = _targetPoint.position;
            Vector3 currentPos = transform.position;
            Vector3 dir = (targetPos - currentPos);
            dir.y = 0f;

            // 2. Tự động xoay mặt mượt mà theo hướng đi (LookRotation + Slerp)
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // 3. Di chuyển tịnh tiến về phía trước theo tốc độ
            transform.position += dir.normalized * (speed * Time.deltaTime);

            // 4. Hiệu ứng nhịp bước chân sinh động (Bobbing & Swaying tự nhiên)
            _walkCycle += Time.deltaTime * speed * 3.5f;
            float bob = Mathf.Abs(Mathf.Sin(_walkCycle)) * 0.06f;
            float sway = Mathf.Sin(_walkCycle) * 2.5f;

            // Đung đưa nhẹ mô hình 3D theo bước chạy
            if (transform.childCount > 0)
            {
                Transform visual = transform.GetChild(0);
                if (visual.name != "HealthBar")
                {
                    visual.localPosition = new Vector3(0f, bob, 0f);
                    visual.localRotation = Quaternion.Euler(0f, 0f, sway);
                }
            }

            // 5. Kiểm tra khoảng cách tiếp cận điểm mốc
            if (Vector3.Distance(transform.position, targetPos) <= waypointThreshold)
            {
                GetNextWaypoint();
            }
        }

        /// <summary>
        /// Tính tổng quãng đường còn lại của quái tới Cổng thành (dùng cho giải thuật chọn mục tiêu của tháp).
        /// </summary>
        public float GetDistanceToGoal()
        {
            if (_hasEnded || _targetPoint == null || Waypoints.Waypoints.Points == null) return 0f;

            float totalDistance = Vector3.Distance(transform.position, _targetPoint.position);
            for (int i = _waypointIndex; i < Waypoints.Waypoints.Points.Length - 1; i++)
            {
                totalDistance += Vector3.Distance(Waypoints.Waypoints.Points[i].position, Waypoints.Waypoints.Points[i + 1].position);
            }
            return totalDistance;
        }

        /// <summary>
        /// Chuyển sang điểm mốc tiếp theo dọc theo lộ trình.
        /// </summary>
        private void GetNextWaypoint()
        {
            _waypointIndex++;

            if (_waypointIndex >= Waypoints.Waypoints.Points.Length)
            {
                EndPath();
                return;
            }

            _targetPoint = Waypoints.Waypoints.Points[_waypointIndex];
        }

        /// <summary>
        /// Xử lý khi quái vật chạm điểm mốc cuối cùng (Cổng Thành Hoàng Gia):
        /// Trừ Lives người chơi, thông báo và tự hủy đối tượng.
        /// </summary>
        private void EndPath()
        {
            if (_hasEnded) return;
            _hasEnded = true;

            Debug.Log($"[EnemyMovement] Quái vật '{gameObject.name}' đã xâm nhập Cổng thành! Trừ 1 Mạng (Lives).");

            // Trừ 1 mạng người chơi qua GameHUD
            if (GameHUD.Instance != null)
            {
                GameHUD.Instance.ModifyLives(-1);
            }

            // Thông báo cho component Enemy (nếu có) để dọn dẹp
            Enemy enemy = GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.OnReachGoal();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
