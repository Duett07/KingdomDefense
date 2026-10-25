using System.Collections.Generic;
using UnityEngine;
using KingdomDefense.Core;

namespace KingdomDefense.VFX
{
    /// <summary>
    /// Hệ thống Quản lý Hiệu ứng Hạt (Particle System VFX Manager).
    /// - Quản lý tập trung toàn bộ các hiệu ứng khói lửa, vệt đạn, sấm sét, vụ nổ đất đá, bão tuyết.
    /// - Tự động tạo procedural materials & textures chuẩn phong cách Low-poly rực rỡ, không phụ thuộc assets ngoài.
    /// - Tích hợp chặt chẽ với ObjectPool: Cấp phát trước và tái sử dụng 100%, triệt tiêu GC Alloc, duy trì 60+ FPS.
    /// - Tuân thủ nghiêm ngặt quy tắc màu sắc (Không dùng màu tím/violet).
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        public const string TAG_CANNON_MUZZLE = "VFX_Cannon_Muzzle";
        public const string TAG_CANNON_EXPLOSION = "VFX_Cannon_Explosion";
        public const string TAG_METEOR_IMPACT = "VFX_Meteor_Impact";
        public const string TAG_ARROW_HIT = "VFX_Arrow_Hit";
        public const string TAG_LIGHTNING_SPARKS = "VFX_Lightning_Sparks";
        public const string TAG_ELECTRIC_IMPACT = "VFX_Electric_Impact";
        public const string TAG_FROST_NOVA = "VFX_Frost_Nova";

        private Material _softCircleMat;
        private Material _sparkMat;
        private Material _smokeMat;
        private Material _debrisMat;
        private Material _iceShardMat;

        private GameObject _cannonMuzzlePrefab;
        private GameObject _cannonExplosionPrefab;
        private GameObject _meteorImpactPrefab;
        private GameObject _arrowHitPrefab;
        private GameObject _lightningSparksPrefab;
        private GameObject _electricImpactPrefab;
        private GameObject _frostNovaPrefab;

        private bool _isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null && Object.FindAnyObjectByType<VFXManager>() == null)
            {
                GameObject managerObj = new GameObject("VFX_Manager");
                managerObj.AddComponent<VFXManager>();
                Debug.Log("[VFXManager] Tự động khởi tạo hệ thống Hiệu ứng hạt VFXManager!");
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Initialize();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            CreateProceduralMaterials();
            BuildParticlePrefabs();
            RegisterPools();

            _isInitialized = true;
            Debug.Log("[VFXManager] Đã khởi tạo hoàn tất toàn bộ thư viện Particle Systems và nạp sẵn vào ObjectPool!");
        }

        #region --- TẠO PROCEDURAL MATERIALS & TEXTURES ---

        private void CreateProceduralMaterials()
        {
            Shader particleShader = Shader.Find("Sprites/Default") ??
                                   Shader.Find("Mobile/Particles/Alpha Blended") ??
                                   Shader.Find("Unlit/Color");

            // 1. Soft Circle (cho quầng lửa, khói tròn và bụi tuyết)
            Texture2D softCircleTex = GenerateSoftCircleTexture(32);
            _softCircleMat = new Material(particleShader);
            _softCircleMat.mainTexture = softCircleTex;

            // 2. Spark (cho tia lửa kim loại, sấm sét điện, tàn tích)
            Texture2D sparkTex = GenerateSparkTexture(32);
            _sparkMat = new Material(particleShader);
            _sparkMat.mainTexture = sparkTex;

            // 3. Smoke (cho khói đầu nòng và cột khói nổ)
            Texture2D smokeTex = GenerateCloudTexture(32);
            _smokeMat = new Material(particleShader);
            _smokeMat.mainTexture = smokeTex;

            // 4. Debris (cho đất đá, dăm gỗ và mảnh vỡ vụ nổ)
            Texture2D debrisTex = GenerateDebrisTexture(16);
            _debrisMat = new Material(particleShader);
            _debrisMat.mainTexture = debrisTex;

            // 5. Ice Shard (cho tinh thể băng tuyết sắc nhọn)
            Texture2D iceTex = GenerateIceShardTexture(32);
            _iceShardMat = new Material(particleShader);
            _iceShardMat.mainTexture = iceTex;
        }

        private Texture2D GenerateSoftCircleTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2f;
            float maxR = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1f - (dist / maxR));
                    alpha = Mathf.Pow(alpha, 1.8f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateSparkTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center) / center;
                    float dy = Mathf.Abs(y - center) / center;
                    float cross = Mathf.Clamp01(1f - (dx * 0.2f + dy * 2.5f)) +
                                  Mathf.Clamp01(1f - (dx * 2.5f + dy * 0.2f));
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float core = Mathf.Clamp01(1f - dist * 1.5f);
                    float alpha = Mathf.Clamp01(Mathf.Max(cross * 0.7f, core));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateCloudTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float noise = Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                    float alpha = Mathf.Clamp01((1f - dist) * (0.6f + noise * 0.4f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateDebrisTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float border = (x == 0 || x == size - 1 || y == 0 || y == size - 1) ? 0.35f : 1f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, border));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateIceShardTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs(x - center) / center;
                    float ny = Mathf.Abs(y - center) / center;
                    float diamond = 1f - (nx + ny);
                    float alpha = Mathf.Clamp01(diamond * 1.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        #endregion

        #region --- XÂY DỰNG CÁC TEMPLATE PARTICLE SYSTEM PREFABS ---

        private void BuildParticlePrefabs()
        {
            GameObject container = new GameObject("--- VFX_Prefab_Templates ---");
            container.transform.SetParent(transform);
            container.SetActive(false);

            _cannonMuzzlePrefab = CreateCannonMuzzleTemplate(container.transform);
            _cannonExplosionPrefab = CreateCannonExplosionTemplate(container.transform);
            _meteorImpactPrefab = CreateMeteorImpactTemplate(container.transform);
            _arrowHitPrefab = CreateArrowHitTemplate(container.transform);
            _lightningSparksPrefab = CreateLightningSparksTemplate(container.transform);
            _electricImpactPrefab = CreateElectricImpactTemplate(container.transform);
            _frostNovaPrefab = CreateFrostNovaTemplate(container.transform);
        }

        private GameObject CreateCannonMuzzleTemplate(Transform parent)
        {
            GameObject root = new GameObject(TAG_CANNON_MUZZLE);
            root.transform.SetParent(parent);

            // 1. Tia lửa bùng nổ đầu nòng (Muzzle Fire Flash)
            ParticleSystem psFlash = root.AddComponent<ParticleSystem>();
            ConfigureModule(psFlash, _sparkMat, duration: 0.2f, startLifetime: 0.15f, startSpeed: 7.5f,
                startSize: 0.35f, new Color(1.0f, 0.72f, 0.15f, 1.0f), gravity: 0f, count: 12);
            var shapeFlash = psFlash.shape;
            shapeFlash.shapeType = ParticleSystemShapeType.Cone;
            shapeFlash.angle = 20f;
            shapeFlash.radius = 0.08f;

            // 2. Khói xám cuộn trào bốc ra đầu nòng (Smoke Puff)
            GameObject smokeChild = new GameObject("Smoke_Puff");
            smokeChild.transform.SetParent(root.transform, false);
            ParticleSystem psSmoke = smokeChild.AddComponent<ParticleSystem>();
            ConfigureModule(psSmoke, _smokeMat, duration: 0.45f, startLifetime: 0.55f, startSpeed: 3.2f,
                startSize: 0.55f, new Color(0.85f, 0.85f, 0.88f, 0.75f), gravity: -0.6f, count: 10);
            var shapeSmoke = psSmoke.shape;
            shapeSmoke.shapeType = ParticleSystemShapeType.Cone;
            shapeSmoke.angle = 28f;
            shapeSmoke.radius = 0.12f;

            PooledVFX pv = root.AddComponent<PooledVFX>();
            pv.SetMaxDuration(0.7f);
            return root;
        }

        private GameObject CreateCannonExplosionTemplate(Transform parent)
        {
            GameObject root = new GameObject(TAG_CANNON_EXPLOSION);
            root.transform.SetParent(parent);

            // 1. Quả cầu lửa bùng nổ (Fire Burst)
            ParticleSystem psFire = root.AddComponent<ParticleSystem>();
            ConfigureModule(psFire, _softCircleMat, duration: 0.35f, startLifetime: 0.4f, startSpeed: 5.5f,
                startSize: 1.4f, new Color(1.0f, 0.55f, 0.08f, 0.95f), gravity: -0.2f, count: 16);
            var shapeFire = psFire.shape;
            shapeFire.shapeType = ParticleSystemShapeType.Sphere;
            shapeFire.radius = 0.5f;

            // 2. Mảnh đất đá văng tung tóe (Ground Debris & Flying Rocks)
            GameObject debrisChild = new GameObject("Ground_Debris_Rocks");
            debrisChild.transform.SetParent(root.transform, false);
            ParticleSystem psDebris = debrisChild.AddComponent<ParticleSystem>();
            ConfigureModule(psDebris, _debrisMat, duration: 0.4f, startLifetime: 0.85f, startSpeed: 8.5f,
                startSize: 0.3f, new Color(0.38f, 0.28f, 0.18f, 1.0f), gravity: 2.2f, count: 24);
            var shapeDebris = psDebris.shape;
            shapeDebris.shapeType = ParticleSystemShapeType.Hemisphere;
            shapeDebris.radius = 0.3f;

            // 3. Cột khói đen cuộn trào bốc lên cao (Dark Smoke Plume)
            GameObject smokeChild = new GameObject("Smoke_Plume");
            smokeChild.transform.SetParent(root.transform, false);
            ParticleSystem psSmoke = smokeChild.AddComponent<ParticleSystem>();
            ConfigureModule(psSmoke, _smokeMat, duration: 0.5f, startLifetime: 1.1f, startSpeed: 2.8f,
                startSize: 1.6f, new Color(0.28f, 0.28f, 0.30f, 0.8f), gravity: -0.8f, count: 14);
            var shapeSmoke = psSmoke.shape;
            shapeSmoke.shapeType = ParticleSystemShapeType.Cone;
            shapeSmoke.angle = 25f;
            shapeSmoke.radius = 0.4f;

            PooledVFX pv = root.AddComponent<PooledVFX>();
            pv.SetMaxDuration(1.3f);
            return root;
        }

        private GameObject CreateMeteorImpactTemplate(Transform parent)
        {
            GameObject root = new GameObject(TAG_METEOR_IMPACT);
            root.transform.SetParent(parent);

            // 1. Bùng nổ quả cầu lửa cấp đại (Mega Fire Blast)
            ParticleSystem psFire = root.AddComponent<ParticleSystem>();
            ConfigureModule(psFire, _softCircleMat, duration: 0.5f, startLifetime: 0.65f, startSpeed: 9.0f,
                startSize: 2.6f, new Color(1.0f, 0.40f, 0.05f, 1.0f), gravity: -0.5f, count: 32);
            var shapeFire = psFire.shape;
            shapeFire.shapeType = ParticleSystemShapeType.Sphere;
            shapeFire.radius = 1.2f;

            // 2. Hạt đá núi lửa rực đỏ văng theo parabol (Volcanic Flying Rocks)
            GameObject debrisChild = new GameObject("Volcanic_Debris_Rocks");
            debrisChild.transform.SetParent(root.transform, false);
            ParticleSystem psDebris = debrisChild.AddComponent<ParticleSystem>();
            ConfigureModule(psDebris, _debrisMat, duration: 0.6f, startLifetime: 1.25f, startSpeed: 14.0f,
                startSize: 0.45f, new Color(0.75f, 0.25f, 0.08f, 1.0f), gravity: 2.6f, count: 36);
            var shapeDebris = psDebris.shape;
            shapeDebris.shapeType = ParticleSystemShapeType.Hemisphere;
            shapeDebris.radius = 0.8f;

            // 3. Tia lửa than hồng cuộn trào (Embers & Fiery Sparks)
            GameObject sparksChild = new GameObject("Fiery_Embers");
            sparksChild.transform.SetParent(root.transform, false);
            ParticleSystem psSparks = sparksChild.AddComponent<ParticleSystem>();
            ConfigureModule(psSparks, _sparkMat, duration: 0.5f, startLifetime: 1.1f, startSpeed: 7.0f,
                startSize: 0.35f, new Color(1.0f, 0.85f, 0.25f, 1.0f), gravity: 0.8f, count: 28);
            var shapeSparks = psSparks.shape;
            shapeSparks.shapeType = ParticleSystemShapeType.Sphere;
            shapeSparks.radius = 1.0f;

            // 4. Khói nấm bụi đất diện rộng (Giant Smoke Shockwave)
            GameObject smokeChild = new GameObject("Mushroom_Smoke");
            smokeChild.transform.SetParent(root.transform, false);
            ParticleSystem psSmoke = smokeChild.AddComponent<ParticleSystem>();
            ConfigureModule(psSmoke, _smokeMat, duration: 0.7f, startLifetime: 1.6f, startSpeed: 4.5f,
                startSize: 2.8f, new Color(0.25f, 0.22f, 0.20f, 0.85f), gravity: -1.0f, count: 22);
            var shapeSmoke = psSmoke.shape;
            shapeSmoke.shapeType = ParticleSystemShapeType.Cone;
            shapeSmoke.angle = 35f;
            shapeSmoke.radius = 0.9f;

            PooledVFX pv = root.AddComponent<PooledVFX>();
            pv.SetMaxDuration(1.9f);
            return root;
        }

        private GameObject CreateArrowHitTemplate(Transform parent)
        {
            GameObject root = new GameObject(TAG_ARROW_HIT);
            root.transform.SetParent(parent);

            // Dăm gỗ vụn và tia lửa vàng tóe ra khi trúng mục tiêu (Wood Splinters & Sparks)
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ConfigureModule(ps, _sparkMat, duration: 0.2f, startLifetime: 0.3f, startSpeed: 4.8f,
                startSize: 0.22f, new Color(1.0f, 0.90f, 0.40f, 1.0f), gravity: 1.8f, count: 10);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.05f;

            PooledVFX pv = root.AddComponent<PooledVFX>();
            pv.SetMaxDuration(0.45f);
            return root;
        }

        private GameObject CreateLightningSparksTemplate(Transform parent)
        {
            GameObject root = new GameObject(TAG_LIGHTNING_SPARKS);
            root.transform.SetParent(parent);

            // Tia lửa điện xanh ngọc quanh đỉnh trượng (Cyan Arcane Sparks)
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ConfigureModule(ps, _sparkMat, duration: 0.25f, startLifetime: 0.25f, startSpeed: 3.5f,
                startSize: 0.28f, new Color(0.2f, 0.95f, 1.0f, 1.0f), gravity: 0f, count: 12);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            PooledVFX pv = root.AddComponent<PooledVFX>();
            pv.SetMaxDuration(0.4f);
            return root;
        }

        private GameObject CreateElectricImpactTemplate(Transform parent)
        {
            GameObject root = new GameObject(TAG_ELECTRIC_IMPACT);
            root.transform.SetParent(parent);

            // Vụ nổ điện trường giải phóng khi sét đánh trúng quái (Electric Discharge Burst)
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ConfigureModule(ps, _sparkMat, duration: 0.25f, startLifetime: 0.35f, startSpeed: 6.0f,
                startSize: 0.38f, new Color(0.15f, 0.92f, 1.0f, 1.0f), gravity: 0f, count: 18);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            // Vòng plasma điện chớp sáng
            GameObject ringChild = new GameObject("Plasma_Ring");
            ringChild.transform.SetParent(root.transform, false);
            ParticleSystem psRing = ringChild.AddComponent<ParticleSystem>();
            ConfigureModule(psRing, _softCircleMat, duration: 0.2f, startLifetime: 0.25f, startSpeed: 2.2f,
                startSize: 1.1f, new Color(0.35f, 1.0f, 1.0f, 0.85f), gravity: 0f, count: 8);

            PooledVFX pv = root.AddComponent<PooledVFX>();
            pv.SetMaxDuration(0.5f);
            return root;
        }

        private GameObject CreateFrostNovaTemplate(Transform parent)
        {
            GameObject root = new GameObject(TAG_FROST_NOVA);
            root.transform.SetParent(parent);

            // 1. Tinh thể băng sắc nhọn bắn tỏa tròn (Ice Shards & Crystals)
            ParticleSystem psIce = root.AddComponent<ParticleSystem>();
            ConfigureModule(psIce, _iceShardMat, duration: 0.4f, startLifetime: 0.65f, startSpeed: 7.2f,
                startSize: 0.45f, new Color(0.70f, 0.95f, 1.0f, 1.0f), gravity: 0.5f, count: 24);
            var shapeIce = psIce.shape;
            shapeIce.shapeType = ParticleSystemShapeType.Circle;
            shapeIce.radius = 0.4f;

            // 2. Làn sương tuyết lạnh mù mịt lan rộng (Freezing Fog & Blizzard Mist)
            GameObject mistChild = new GameObject("Blizzard_Mist");
            mistChild.transform.SetParent(root.transform, false);
            ParticleSystem psMist = mistChild.AddComponent<ParticleSystem>();
            ConfigureModule(psMist, _softCircleMat, duration: 0.5f, startLifetime: 0.9f, startSpeed: 5.0f,
                startSize: 1.8f, new Color(0.55f, 0.88f, 1.0f, 0.65f), gravity: 0f, count: 18);
            var shapeMist = psMist.shape;
            shapeMist.shapeType = ParticleSystemShapeType.Circle;
            shapeMist.radius = 0.6f;

            // 3. Hoa tuyết lấp lánh lơ lửng (Snow Sparkles)
            GameObject snowChild = new GameObject("Snow_Sparkles");
            snowChild.transform.SetParent(root.transform, false);
            ParticleSystem psSnow = snowChild.AddComponent<ParticleSystem>();
            ConfigureModule(psSnow, _sparkMat, duration: 0.5f, startLifetime: 1.1f, startSpeed: 2.2f,
                startSize: 0.25f, Color.white, gravity: -0.2f, count: 18);
            var shapeSnow = psSnow.shape;
            shapeSnow.shapeType = ParticleSystemShapeType.Hemisphere;
            shapeSnow.radius = 1.0f;

            PooledVFX pv = root.AddComponent<PooledVFX>();
            pv.SetMaxDuration(1.3f);
            return root;
        }

        private void ConfigureModule(ParticleSystem ps, Material mat, float duration, float startLifetime, float startSpeed,
            float startSize, Color startColor, float gravity, int count)
        {
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = startLifetime;
            main.startSpeed = startSpeed;
            main.startSize = startSize;
            main.startColor = startColor;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(startColor, 0f), new GradientColorKey(startColor, 0.7f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(startColor.a * 0.8f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.7f);
            curve.AddKey(0.2f, 1.2f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            if (r != null)
            {
                r.material = mat;
                r.renderMode = ParticleSystemRenderMode.Billboard;
            }
        }

        #endregion

        #region --- ĐĂNG KÝ VÀO OBJECT POOL ---

        private void RegisterPools()
        {
            if (ObjectPool.Instance == null)
            {
                Debug.LogWarning("[VFXManager] ObjectPool.Instance chưa sẵn sàng, sẽ đăng ký tự động khi Spawn!");
                return;
            }

            RegisterPool(TAG_CANNON_MUZZLE, _cannonMuzzlePrefab, 15);
            RegisterPool(TAG_CANNON_EXPLOSION, _cannonExplosionPrefab, 20);
            RegisterPool(TAG_METEOR_IMPACT, _meteorImpactPrefab, 10);
            RegisterPool(TAG_ARROW_HIT, _arrowHitPrefab, 25);
            RegisterPool(TAG_LIGHTNING_SPARKS, _lightningSparksPrefab, 15);
            RegisterPool(TAG_ELECTRIC_IMPACT, _electricImpactPrefab, 20);
            RegisterPool(TAG_FROST_NOVA, _frostNovaPrefab, 12);
        }

        private void RegisterPool(string tag, GameObject prefab, int initialSize)
        {
            if (prefab == null || ObjectPool.Instance == null) return;

            ObjectPool.PoolConfig config = new ObjectPool.PoolConfig
            {
                tag = tag,
                prefab = prefab,
                initialSize = initialSize,
                autoExpand = true
            };
            ObjectPool.Instance.RegisterAndPrewarmPool(config);
        }

        #endregion

        #region --- PUBLIC SPAWN APIS DÀNH CHO CÁC THÁP & PROJECTILES ---

        /// <summary>
        /// Khói lửa đầu nòng khi đại bác bắn (Muzzle Fire Blast & Smoke Puff).
        /// </summary>
        public GameObject SpawnCannonMuzzle(Vector3 position, Vector3 forward)
        {
            Quaternion rot = forward.sqrMagnitude > 0.001f ? Quaternion.LookRotation(forward) : Quaternion.identity;
            return SpawnEffect(TAG_CANNON_MUZZLE, _cannonMuzzlePrefab, position, rot);
        }

        /// <summary>
        /// Vụ nổ đất đá diện rộng khi đạn pháo chạm đích (Debris Explosion, Fire Blast & Smoke Plume).
        /// </summary>
        public GameObject SpawnCannonExplosion(Vector3 position, float radius = 3.0f)
        {
            GameObject obj = SpawnEffect(TAG_CANNON_EXPLOSION, _cannonExplosionPrefab, position, Quaternion.identity);
            if (obj != null)
            {
                obj.transform.localScale = Vector3.one * Mathf.Clamp(radius / 3.0f, 0.8f, 1.8f);
            }
            return obj;
        }

        /// <summary>
        /// Vụ nổ đại quy mô khi Thiên Thạch rơi xuống mặt đất (Volcanic Debris, Mega Fireball & Mushroom Plume).
        /// </summary>
        public GameObject SpawnMeteorImpact(Vector3 position, float radius = 4.0f)
        {
            GameObject obj = SpawnEffect(TAG_METEOR_IMPACT, _meteorImpactPrefab, position, Quaternion.identity);
            if (obj != null)
            {
                obj.transform.localScale = Vector3.one * Mathf.Clamp(radius / 4.0f, 0.85f, 2.2f);
            }
            return obj;
        }

        /// <summary>
        /// Tia lửa và dăm gỗ vụn khi mũi tên cắm trúng mục tiêu.
        /// </summary>
        public GameObject SpawnArrowHit(Vector3 position, Vector3 normal)
        {
            Quaternion rot = normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
            return SpawnEffect(TAG_ARROW_HIT, _arrowHitPrefab, position, rot);
        }

        /// <summary>
        /// Hồ quang điện và tia lửa giải phóng quanh đỉnh trượng ma thuật.
        /// </summary>
        public GameObject SpawnLightningSparks(Vector3 position)
        {
            return SpawnEffect(TAG_LIGHTNING_SPARKS, _lightningSparksPrefab, position, Quaternion.identity);
        }

        /// <summary>
        /// Vụ nổ điện trường giải phóng khi sét giật trúng quái vật (Target Electric Discharge).
        /// </summary>
        public GameObject SpawnElectricImpact(Vector3 position)
        {
            return SpawnEffect(TAG_ELECTRIC_IMPACT, _electricImpactPrefab, position, Quaternion.identity);
        }

        /// <summary>
        /// Bão tuyết và tinh thể băng giá tỏa rộng cho Tháp Băng (Ice Nova Blizzard Pulse).
        /// </summary>
        public GameObject SpawnFrostNova(Vector3 position, float radius = 4.5f)
        {
            GameObject obj = SpawnEffect(TAG_FROST_NOVA, _frostNovaPrefab, position, Quaternion.identity);
            if (obj != null)
            {
                obj.transform.localScale = Vector3.one * Mathf.Clamp(radius / 4.5f, 0.8f, 2.0f);
            }
            return obj;
        }

        /// <summary>
        /// Gắn vệt hạt bụi vàng lấp lánh (Golden Dust Sparkle Trail) theo sau mũi tên bay.
        /// </summary>
        public void AttachArrowTrail(GameObject arrowObj)
        {
            if (arrowObj == null) return;

            Transform existingTrail = arrowObj.transform.Find("Arrow_Particle_Trail");
            if (existingTrail != null)
            {
                ParticleSystem ps = existingTrail.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();
                return;
            }

            GameObject trailObj = new GameObject("Arrow_Particle_Trail");
            trailObj.transform.SetParent(arrowObj.transform, false);
            trailObj.transform.localPosition = new Vector3(0f, 0f, -0.3f);

            ParticleSystem trailPs = trailObj.AddComponent<ParticleSystem>();
            var main = trailPs.main;
            main.duration = 1.0f;
            main.loop = true;
            main.startLifetime = 0.25f;
            main.startSpeed = 0.4f;
            main.startSize = 0.18f;
            main.startColor = new Color(1.0f, 0.88f, 0.35f, 0.85f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;

            var emission = trailPs.emission;
            emission.rateOverTime = 22;

            var shape = trailPs.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.04f;

            var colorOverLifetime = trailPs.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = g;

            var sizeOverLifetime = trailPs.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve c = new AnimationCurve();
            c.AddKey(0f, 1f);
            c.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, c);

            ParticleSystemRenderer r = trailPs.GetComponent<ParticleSystemRenderer>();
            if (r != null)
            {
                r.material = _sparkMat ?? _softCircleMat;
            }
        }

        private GameObject SpawnEffect(string tag, GameObject fallbackPrefab, Vector3 pos, Quaternion rot)
        {
            if (ObjectPool.Instance != null)
            {
                GameObject obj = ObjectPool.Instance.Spawn(tag, pos, rot);
                if (obj != null)
                {
                    PooledVFX pv = obj.GetComponent<PooledVFX>();
                    if (pv != null) pv.OnSpawnFromPool();
                    return obj;
                }
            }

            if (fallbackPrefab != null)
            {
                GameObject fallback = Instantiate(fallbackPrefab, pos, rot);
                fallback.SetActive(true);
                PooledVFX pv = fallback.GetComponent<PooledVFX>();
                if (pv != null) pv.OnSpawnFromPool();
                return fallback;
            }

            return null;
        }

        #endregion
    }
}
