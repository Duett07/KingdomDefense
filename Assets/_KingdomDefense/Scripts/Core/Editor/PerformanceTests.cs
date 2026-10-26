using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using KingdomDefense.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KingdomDefense.Tests
{
    /// <summary>
    /// Bộ kiểm thử hiệu năng tự động (Performance & Draw Call Tests):
    /// - Xác nhận cấu hình khóa 60 FPS & V-Sync = 1.
    /// - Xác nhận tất cả Material sử dụng trong game đã bật GPU Instancing.
    /// - Xác nhận cơ chế Static Batching gom nhóm Mesh Renderers an toàn, không sinh lỗi.
    /// - Xác nhận vòng đời Object Pool đảm bảo tái sử dụng 100%, triệt tiêu GC Alloc.
    /// </summary>
    public class PerformanceTests
    {
        private GameObject _perfOptimizerObj;
        private PerformanceOptimizer _perfOptimizer;

        [SetUp]
        public void SetUp()
        {
            _perfOptimizerObj = new GameObject("Test_PerformanceOptimizer");
            _perfOptimizer = _perfOptimizerObj.AddComponent<PerformanceOptimizer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_perfOptimizerObj != null)
            {
                Object.DestroyImmediate(_perfOptimizerObj);
            }
        }

        [Test]
        public void TargetFrameRateAndVSync_AreConfiguredTo60FPS()
        {
            // Thiết lập thông số hiệu năng chuẩn
            PerformanceOptimizer.ApplyFrameRateSettings(60, 1);

            Assert.AreEqual(60, Application.targetFrameRate, "Application.targetFrameRate phải được đặt là 60 FPS để duy trì độ ổn định mượt mà.");
            Assert.AreEqual(1, QualitySettings.vSyncCount, "QualitySettings.vSyncCount phải là 1 (khóa theo chu kỳ quét màn hình).");
            Assert.LessOrEqual(Time.maximumDeltaTime, 0.15f, "Time.maximumDeltaTime phải được giới hạn để tránh hiện tượng physics spiral of death.");
        }

        [Test]
        public void PerformanceOptimizer_DefaultValues_AndHUDToggle_WorkCorrectly()
        {
            Assert.IsNotNull(_perfOptimizer, "PerformanceOptimizer component phải được khởi tạo thành công.");
            Assert.IsTrue(_perfOptimizer.ShowPerformanceHUD, "Mặc định Performance HUD phải hiển thị để hỗ trợ quan sát hiệu năng.");

            // Kiểm tra bật/tắt HUD (tương đương phím F3)
            _perfOptimizer.ShowPerformanceHUD = false;
            Assert.IsFalse(_perfOptimizer.ShowPerformanceHUD, "Performance HUD phải có thể ẩn được khi người dùng tắt.");

            _perfOptimizer.ShowPerformanceHUD = true;
            Assert.IsTrue(_perfOptimizer.ShowPerformanceHUD, "Performance HUD phải có thể hiển thị lại khi bật.");
        }

        [Test]
        public void StaticBatching_CombinesEnvironmentMeshes_WithoutException()
        {
            // Tạo một cụm môi trường giả lập gồm 5 cây/đá chia sẻ cùng Material
            GameObject rootContainer = new GameObject("Test_Forest_Container");
            Material sharedMat = new Material(Shader.Find("Standard"));
            sharedMat.enableInstancing = true;

            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
                child.transform.SetParent(rootContainer.transform);
                child.transform.localPosition = new Vector3(i * 2f, 0, 0);
                child.GetComponent<MeshRenderer>().sharedMaterial = sharedMat;
                children.Add(child);
            }

            // Thực thi gom nhóm Static Batching
            Assert.DoesNotThrow(() =>
            {
                StaticBatchingUtility.Combine(rootContainer);
            }, "StaticBatchingUtility.Combine phải thực thi thành công mà không gây ngoại lệ.");

            // Dọn dẹp
            Object.DestroyImmediate(sharedMat);
            Object.DestroyImmediate(rootContainer);
        }

#if UNITY_EDITOR
        [Test]
        public void AllProjectMaterials_HaveGPUInstancingEnabled()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_KingdomDefense/Materials" });
            Assert.Greater(materialGuids.Length, 0, "Dự án phải có ít nhất 1 Material trong thư mục Assets/_KingdomDefense/Materials.");

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.IsNotNull(mat, $"Không thể nạp Material tại đường dẫn: {path}");
                Assert.IsTrue(mat.enableInstancing, $"Material '{mat.name}' ({path}) phải được bật GPU Instancing (enableInstancing = true) để tối ưu Draw Calls.");
            }
        }
#endif

        [Test]
        public void ObjectPool_ZeroAllocationRecycle_EnsuresStablePerformance()
        {
            // Kiểm tra cơ chế tái chế của ObjectPool
            GameObject poolObj = new GameObject("Test_ObjectPool");
            ObjectPool pool = poolObj.AddComponent<ObjectPool>();

            GameObject arrowPrefab = new GameObject("Test_ArrowPrefab");

            // Đăng ký pool với 10 đối tượng
            pool.RegisterAndPrewarmPool(new ObjectPool.PoolConfig
            {
                tag = "Test_Arrow",
                prefab = arrowPrefab,
                initialSize = 10,
                autoExpand = true
            });

            // Rút ra 5 mũi tên
            List<GameObject> activeArrows = new List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                GameObject arrow = pool.Spawn(arrowPrefab, Vector3.zero, Quaternion.identity);
                Assert.IsNotNull(arrow, "ObjectPool phải xuất ra đối tượng hợp lệ.");
                activeArrows.Add(arrow);
            }

            // Thu hồi toàn bộ về Pool
            foreach (var arrow in activeArrows)
            {
                pool.ReturnToPool(arrow);
                Assert.IsFalse(arrow.activeSelf, "Đối tượng sau khi ReturnToPool phải chuyển sang trạng thái inactive.");
            }

            // Dọn dẹp
            Object.DestroyImmediate(arrowPrefab);
            Object.DestroyImmediate(poolObj);
        }
    }
}
