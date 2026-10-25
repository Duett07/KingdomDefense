using UnityEngine;
using KingdomDefense.Towers;
using KingdomDefense.UI;

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
        [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0.06f, 0f);

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

            // Tìm mặt quảng trường đá Foundation_Plaza để đổi màu highlight mượt mà
            Transform plaza = transform.Find("Foundation_Plaza");
            if (plaza != null)
            {
                _renderer = plaza.GetComponent<Renderer>();
            }
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
            }

            if (_renderer != null)
            {
                // Tạo instance material riêng để không làm đổi màu toàn bộ các node khác
                _tempMaterial = _renderer.material;
                _originalColor = _tempMaterial.color;
            }

            // Đảm bảo BoxCollider có kích thước vừa vặn bao quát toàn bộ đĩa đá tháp
            BoxCollider col = GetComponent<BoxCollider>();
            if (col != null)
            {
                col.size = new Vector3(2.2f, 0.8f, 2.2f);
                col.center = new Vector3(0f, 0.35f, 0f);
            }
        }

        private void OnMouseEnter()
        {
            _isHovered = true;
            transform.localScale = _originalScale * 1.08f;

            if (_renderer == null) return;

            // 1. Đã có tháp -> Đổi màu Đỏ cảnh báo
            if (HasTurret)
            {
                _tempMaterial.color = notEnoughMoneyColor;
                return;
            }

            // 2. Kiểm tra có đủ vàng để xây loại tháp đang chọn hay không
            if (BuildManager.Instance != null && BuildManager.Instance.SelectedTowerType != TowerType.None)
            {
                if (BuildManager.Instance.HasEnoughMoney(BuildManager.Instance.SelectedTowerType))
                {
                    _tempMaterial.color = hoverColor; // Xanh lá: Hợp lệ
                }
                else
                {
                    _tempMaterial.color = notEnoughMoneyColor; // Đỏ: Không đủ tiền
                }
            }
            else
            {
                _tempMaterial.color = hoverColor; // Xanh lá mặc định
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
                // Nếu người chơi không đang ở chế độ cắm tháp mới -> chọn tháp để nâng cấp / bán
                if (BuildManager.Instance == null || BuildManager.Instance.SelectedTowerType == TowerType.None)
                {
                    Tower tower = currentTurret.GetComponent<Tower>();
                    if (tower != null && TowerUI.Instance != null)
                    {
                        TowerUI.Instance.SelectTower(tower);
                    }
                }
                return;
            }

            if (BuildManager.Instance != null)
            {
                if (BuildManager.Instance.SelectedTowerType == TowerType.None)
                {
                    Debug.LogWarning("[Node] Hãy click chọn loại Tháp từ thanh công cụ phía dưới màn hình trước!");
                    return;
                }

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
        /// Đặt tháp vào ô này kèm hiệu ứng khói bụi xây dựng.
        /// </summary>
        public void SetTurret(GameObject turret)
        {
            currentTurret = turret;

            Tower tower = turret.GetComponent<Tower>();
            if (tower != null)
            {
                tower.ParentNode = this;
            }

            // Ẩn huy hiệu thợ xây trung tâm khi tháp đã được dựng lên
            Transform centerMarker = transform.Find("Center_Marker");
            if (centerMarker != null)
            {
                centerMarker.gameObject.SetActive(false);
            }

            SpawnBuildDustEffect(GetBuildPosition());
        }

        /// <summary>
        /// Giải phóng ô Node khi tháp bị bán.
        /// </summary>
        public void ClearTurret()
        {
            currentTurret = null;

            Transform centerMarker = transform.Find("Center_Marker");
            if (centerMarker != null)
            {
                centerMarker.gameObject.SetActive(true);
            }

            if (_tempMaterial != null)
            {
                _tempMaterial.color = _originalColor;
            }
        }

        /// <summary>
        /// Hiệu ứng khói bụi bung tỏa khi công trình vừa dựng xong.
        /// </summary>
        private void SpawnBuildDustEffect(Vector3 pos)
        {
            GameObject smoke = new GameObject("Build_Dust_Puff");
            smoke.transform.position = pos;

            for (int i = 0; i < 8; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.transform.SetParent(smoke.transform);
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Random.Range(0.2f, 0.6f), Mathf.Sin(angle));
                puff.transform.localPosition = dir * Random.Range(0.3f, 0.6f);
                puff.transform.localScale = Vector3.one * Random.Range(0.35f, 0.55f);
                Destroy(puff.GetComponent<Collider>());

                Renderer r = puff.GetComponent<Renderer>();
                if (r != null)
                {
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    Material mat = new Material(s);
                    mat.color = new Color(0.9f, 0.88f, 0.82f, 0.75f);
                    r.sharedMaterial = mat;
                }
            }

            Destroy(smoke, 0.45f);
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
