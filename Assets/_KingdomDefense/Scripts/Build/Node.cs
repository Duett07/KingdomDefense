using UnityEngine;

namespace KingdomDefense.Build
{
    /// <summary>
    /// Đại diện cho một ô cắm tháp (Buildable Node) trên bản đồ.
    /// Có BoxCollider để bắt sự kiện chuột (OnMouseEnter, OnMouseExit, OnMouseDown),
    /// đổi màu highlight khi rê chuột và lưu trữ tham chiếu tháp đã được xây.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Node : MonoBehaviour
    {
        [Header("--- THIẾT LẬP HIỆU ỨNG CHUỘT ---")]
        [Tooltip("Màu highlight khi ô hợp lệ để xây")]
        [SerializeField] private Color hoverColor = new Color(0.2f, 0.9f, 0.3f, 0.8f);
        [Tooltip("Màu báo lỗi khi ô đã có tháp hoặc không đủ vàng")]
        [SerializeField] private Color notEnoughMoneyColor = new Color(0.9f, 0.2f, 0.2f, 0.8f);
        [Tooltip("Độ lệch độ cao khi đặt tháp lên ô")]
        [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0.2f, 0f);

        [Header("--- TRẠNG THÁI HIỆN TẠI ---")]
        [Tooltip("Tháp đang được đặt trên ô này (null nếu ô trống)")]
        [SerializeField] private GameObject currentTurret;

        private Renderer _renderer;
        private Color _originalColor;
        private Material _tempMaterial;
        private bool _isHovered;

        public bool HasTurret => currentTurret != null;
        public GameObject CurrentTurret => currentTurret;
        public bool IsHovered => _isHovered;

        private Vector3 _originalScale;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _renderer = GetComponent<Renderer>();
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }

            if (_renderer != null)
            {
                // Tạo instance material riêng để không làm đổi màu toàn bộ các node khác
                _tempMaterial = _renderer.material;
                _originalColor = _tempMaterial.color;
            }

            // Đảm bảo BoxCollider có kích thước vừa vặn cho raycast
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null && col.size == Vector3.one)
            {
                col.size = new Vector3(1.5f, 0.8f, 1.5f);
                col.center = new Vector3(0f, 0.4f, 0f);
            }
        }

        private void OnMouseEnter()
        {
            _isHovered = true;
            transform.localScale = _originalScale * 1.08f;

            if (_renderer == null) return;

            // Nếu đã có tháp, cảnh báo màu đỏ; nếu trống, highlight màu vàng sáng
            if (HasTurret)
            {
                _tempMaterial.color = notEnoughMoneyColor;
            }
            else
            {
                _tempMaterial.color = hoverColor;
            }
        }

        private void OnMouseExit()
        {
            _isHovered = false;
            transform.localScale = _originalScale;

            if (_renderer == null) return;
            _tempMaterial.color = _originalColor;
        }

        private void OnMouseDown()
        {
            if (HasTurret)
            {
                Debug.Log($"[Node] Ô tại {transform.position} đã có công trình: {currentTurret.name}");
                return;
            }

            if (BuildManager.Instance != null)
            {
                BuildManager.Instance.BuildTowerOn(this);
            }
            else
            {
                Debug.LogWarning("[Node] Chưa tìm thấy BuildManager trong Scene!");
            }
        }

        /// <summary>
        /// Trả về tọa độ chuẩn để đặt Prefab tháp lên trên ô này.
        /// </summary>
        public Vector3 GetBuildPosition()
        {
            return transform.position + positionOffset;
        }

        /// <summary>
        /// Đặt tháp vào ô này.
        /// </summary>
        public void SetTurret(GameObject turret)
        {
            currentTurret = turret;
        }

        private void OnDestroy()
        {
            if (_tempMaterial != null)
            {
                Destroy(_tempMaterial);
            }
        }
    }
}
