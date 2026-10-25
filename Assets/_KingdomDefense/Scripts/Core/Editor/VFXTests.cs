using NUnit.Framework;
using UnityEngine;
using KingdomDefense.VFX;
using KingdomDefense.Core;

namespace KingdomDefense.Tests
{
    public class VFXTests
    {
        private GameObject _vfxManagerObj;
        private VFXManager _vfxManager;
        private GameObject _poolObj;
        private ObjectPool _objectPool;

        [SetUp]
        public void SetUp()
        {
            _poolObj = new GameObject("Test_ObjectPool");
            _objectPool = _poolObj.AddComponent<ObjectPool>();

            _vfxManagerObj = new GameObject("Test_VFXManager");
            _vfxManager = _vfxManagerObj.AddComponent<VFXManager>();
            _vfxManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_vfxManagerObj != null)
            {
                Object.DestroyImmediate(_vfxManagerObj);
            }
            if (_poolObj != null)
            {
                Object.DestroyImmediate(_poolObj);
            }
        }

        [Test]
        public void VFXManager_InitializesSuccessfully_CreatesSingleton()
        {
            Assert.IsNotNull(VFXManager.Instance, "VFXManager singleton phải được khởi tạo thành công.");
        }

        [Test]
        public void VFXManager_CannonMuzzle_SpawnsParticleSystem()
        {
            GameObject muzzle = _vfxManager.SpawnCannonMuzzle(Vector3.zero, Vector3.forward);
            Assert.IsNotNull(muzzle, "Hiệu ứng khói lửa đầu nòng Cannon Muzzle phải sinh thành công.");

            PooledVFX pv = muzzle.GetComponent<PooledVFX>();
            Assert.IsNotNull(pv, "Hiệu ứng đầu nòng phải có PooledVFX component để tái sử dụng.");

            ParticleSystem[] systems = muzzle.GetComponentsInChildren<ParticleSystem>(true);
            Assert.GreaterOrEqual(systems.Length, 1, "Cannon Muzzle phải chứa ít nhất 1 ParticleSystem (tia lửa / khói).");
        }

        [Test]
        public void VFXManager_CannonExplosion_SpawnsParticleSystemAndDebris()
        {
            GameObject exp = _vfxManager.SpawnCannonExplosion(new Vector3(5f, 0f, 5f), 3.0f);
            Assert.IsNotNull(exp, "Vụ nổ đạn pháo Cannon Explosion phải sinh thành công.");

            ParticleSystem[] systems = exp.GetComponentsInChildren<ParticleSystem>(true);
            Assert.GreaterOrEqual(systems.Length, 2, "Vụ nổ đạn pháo phải có đa tầng ParticleSystem (lửa, đất đá, khói).");

            bool hasDebris = false;
            foreach (var ps in systems)
            {
                if (ps.gameObject.name.ToLower().Contains("debris") || ps.gameObject.name.ToLower().Contains("rock"))
                {
                    hasDebris = true;
                    break;
                }
            }
            Assert.IsTrue(hasDebris, "Vụ nổ đạn pháo phải chứa hiệu ứng hạt đất đá văng tung tóe.");
        }

        [Test]
        public void VFXManager_MeteorImpact_SpawnsVolcanicMegaExplosion()
        {
            GameObject meteorExp = _vfxManager.SpawnMeteorImpact(new Vector3(10f, 0f, 10f), 4.0f);
            Assert.IsNotNull(meteorExp, "Vụ nổ thiên thạch cấp đại Meteor Impact phải sinh thành công.");

            ParticleSystem[] systems = meteorExp.GetComponentsInChildren<ParticleSystem>(true);
            Assert.GreaterOrEqual(systems.Length, 3, "Vụ nổ thiên thạch phải chứa tối thiểu 3 hệ hạt (quả cầu lửa, đá núi lửa, khói nấm).");
        }

        [Test]
        public void VFXManager_ArrowHitAndTrail_SpawnsSuccessfully()
        {
            GameObject dummyArrow = new GameObject("Dummy_Arrow");
            _vfxManager.AttachArrowTrail(dummyArrow);

            Transform trailTr = dummyArrow.transform.Find("Arrow_Particle_Trail");
            Assert.IsNotNull(trailTr, "Mũi tên phải được gắn thêm Arrow_Particle_Trail.");

            ParticleSystem trailPs = trailTr.GetComponent<ParticleSystem>();
            Assert.IsNotNull(trailPs, "Vệt bụi vàng mũi tên phải là một ParticleSystem.");

            GameObject hit = _vfxManager.SpawnArrowHit(Vector3.up, Vector3.up);
            Assert.IsNotNull(hit, "Hiệu ứng cắm tên Arrow Hit phải sinh thành công.");

            Object.DestroyImmediate(dummyArrow);
        }

        [Test]
        public void VFXManager_LightningAndElectricDischarge_SpawnsSuccessfully()
        {
            GameObject sparks = _vfxManager.SpawnLightningSparks(new Vector3(0f, 2f, 0f));
            Assert.IsNotNull(sparks, "Tia lửa điện đầu trượng Lightning Sparks phải sinh thành công.");

            GameObject impact = _vfxManager.SpawnElectricImpact(new Vector3(2f, 0.5f, 2f));
            Assert.IsNotNull(impact, "Hiệu ứng điện trường Electric Impact khi sét đánh trúng quái phải sinh thành công.");
        }

        [Test]
        public void VFXManager_FrostNova_SpawnsIceCrystalsAndBlizzardMist()
        {
            GameObject frost = _vfxManager.SpawnFrostNova(Vector3.zero, 4.5f);
            Assert.IsNotNull(frost, "Hiệu ứng bão tuyết Frost Nova phải sinh thành công.");

            ParticleSystem[] systems = frost.GetComponentsInChildren<ParticleSystem>(true);
            Assert.GreaterOrEqual(systems.Length, 2, "Frost Nova phải chứa ít nhất 2 hệ hạt (tinh thể băng và sương tuyết).");
        }

        [Test]
        public void VFXManager_ObjectPool_RecyclesPooledVFXCleanly()
        {
            GameObject exp = _vfxManager.SpawnCannonExplosion(Vector3.zero, 3.0f);
            Assert.IsNotNull(exp);

            PooledObject po = exp.GetComponent<PooledObject>();
            Assert.IsNotNull(po, "Hiệu ứng phải gắn PooledObject để hỗ trợ thu hồi vào Pool.");

            po.Despawn();
            Assert.IsFalse(exp.activeSelf, "Sau khi Despawn, hiệu ứng phải được đưa về trạng thái ẩn trong Pool.");
        }
    }
}
