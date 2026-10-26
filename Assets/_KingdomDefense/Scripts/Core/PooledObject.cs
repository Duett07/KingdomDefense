using UnityEngine;

namespace KingdomDefense.Core
{
    /// <summary>
    /// Gắn lên mọi GameObject được quản lý bởi ObjectPool để truy xuất nguồn gốc Pool và hỗ trợ Despawn nhanh.
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledObject : MonoBehaviour
    {
        [SerializeField] private string poolTag;
        public string PoolTag => poolTag;

        public void SetPoolTag(string tag)
        {
            poolTag = tag;
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
    }
}
