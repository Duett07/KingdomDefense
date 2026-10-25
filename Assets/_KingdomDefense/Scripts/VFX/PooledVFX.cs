using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.Core;

namespace KingdomDefense.VFX
{
    /// <summary>
    /// Gắn lên mọi đối tượng hiệu ứng hạt Particle System được quản lý bởi ObjectPool.
    /// - Quản lý tự động Play/Stop/Clear khi xuất và thu hồi về Pool.
    /// - Tự động đếm thời gian tồn tại và trả về ObjectPool khi tất cả các hạt tan biến.
    /// </summary>
    [RequireComponent(typeof(PooledObject))]
    public class PooledVFX : MonoBehaviour, IPoolable
    {
        [Tooltip("Thời gian tồn tại tối đa trước khi tự động trả về Pool (nếu hạt vẫn chưa tắt)")]
        [SerializeField] private float maxDuration = 2.0f;

        private ParticleSystem[] _particleSystems;
        private float _timer;
        private bool _isSpawned;

        private void Awake()
        {
            CacheParticleSystems();
        }

        private void CacheParticleSystems()
        {
            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }
        }

        public void SetMaxDuration(float duration)
        {
            maxDuration = Mathf.Max(0.2f, duration);
        }

        public void OnSpawnFromPool()
        {
            CacheParticleSystems();
            _timer = 0f;
            _isSpawned = true;

            if (_particleSystems != null)
            {
                for (int i = 0; i < _particleSystems.Length; i++)
                {
                    if (_particleSystems[i] != null)
                    {
                        _particleSystems[i].Clear(true);
                        _particleSystems[i].Play(true);
                    }
                }
            }
        }

        public void OnReturnToPool()
        {
            _isSpawned = false;

            if (_particleSystems != null)
            {
                for (int i = 0; i < _particleSystems.Length; i++)
                {
                    if (_particleSystems[i] != null)
                    {
                        _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }
        }

        private void Update()
        {
            if (!_isSpawned) return;

            _timer += Time.deltaTime;

            // Kiểm tra xem tất cả các hạt đã phát xạ xong và tan biến hết chưa
            bool stillAlive = false;
            if (_particleSystems != null)
            {
                for (int i = 0; i < _particleSystems.Length; i++)
                {
                    if (_particleSystems[i] != null && _particleSystems[i].IsAlive(true))
                    {
                        stillAlive = true;
                        break;
                    }
                }
            }

            // Nếu đã tan biến hết hoặc quá thời gian tối đa -> Thu hồi về Pool
            if (!stillAlive || _timer >= maxDuration)
            {
                _isSpawned = false;
                PooledObject po = GetComponent<PooledObject>();
                if (po != null)
                {
                    po.Despawn();
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
