using UnityEngine;

namespace KingdomDefense.Waypoints
{
    /// <summary>
    /// Quản lý danh sách các điểm nút dẫn đường (Waypoints) dọc theo đường mòn.
    /// Quái vật (EnemyMovement) sẽ truy cập danh sách này để di chuyển từ Cổng xuất phát đến Cổng thành.
    /// </summary>
    public class Waypoints : MonoBehaviour
    {
        public static Transform[] Points { get; private set; }

        [Header("--- THIẾT LẬP HIỂN THỊ TRONG SCENE ---")]
        [SerializeField] private Color pathColor = Color.yellow;
        [SerializeField] private Color nodeColor = Color.red;
        [SerializeField] private float nodeRadius = 0.25f;

        private void Awake()
        {
            LoadPoints();
        }

        /// <summary>
        /// Nạp toàn bộ Transform của các GameObject con làm danh sách Waypoints.
        /// </summary>
        public void LoadPoints()
        {
            Points = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                Points[i] = transform.GetChild(i);
            }
        }

        private void OnDrawGizmos()
        {
            // Vẽ đường nối và các nút mốc trong Scene View để dễ quan sát lộ trình
            if (transform.childCount == 0) return;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Gizmos.color = (i == 0) ? Color.magenta : (i == transform.childCount - 1) ? Color.green : nodeColor;
                Gizmos.DrawSphere(child.position, nodeRadius);

                if (i < transform.childCount - 1)
                {
                    Gizmos.color = pathColor;
                    Gizmos.DrawLine(child.position, transform.GetChild(i + 1).position);
                }
            }
        }
    }
}
