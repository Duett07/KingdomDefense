using NUnit.Framework;
using UnityEngine;
using KingdomDefense.Cameras;

namespace KingdomDefense.Tests
{
    public class CameraShakeTests
    {
        private GameObject _cameraObj;
        private Camera _camera;
        private CameraController _cameraController;

        [SetUp]
        public void SetUp()
        {
            _cameraObj = new GameObject("Test_IsometricCamera");
            _camera = _cameraObj.AddComponent<Camera>();
            _cameraController = _cameraObj.AddComponent<CameraController>();
            _cameraController.SetPosition(new Vector3(0f, 20f, -15f));
        }

        [TearDown]
        public void TearDown()
        {
            if (_cameraObj != null)
            {
                Object.DestroyImmediate(_cameraObj);
            }
        }

        [Test]
        public void CameraShake_InitializesWithDefaultSettings()
        {
            Assert.IsTrue(_cameraController.EnableScreenShake, "Screen shake mặc định phải được bật (EnableScreenShake == true).");
            Assert.AreEqual(1.0f, _cameraController.ShakeIntensityMultiplier, 0.001f, "ShakeIntensityMultiplier mặc định phải bằng 1.0f.");
            Assert.IsFalse(_cameraController.IsShaking, "Camera ban đầu không được ở trạng thái rung chấn.");
            Assert.AreEqual(Vector3.zero, _cameraController.CurrentShakeOffset, "Độ dời rung chấn ban đầu phải bằng Vector3.zero.");
        }

        [Test]
        public void CameraShake_TriggerShake_CalculatesOffsetAndDecays()
        {
            _cameraController.TriggerShake(0.5f, 0.45f);

            Assert.IsTrue(_cameraController.IsShaking, "Camera phải ở trạng thái IsShaking sau khi TriggerShake.");
            Assert.AreEqual(0.5f, _cameraController.RemainingShakeDuration, 0.001f, "Thời lượng rung còn lại phải bằng 0.5s.");
            Assert.AreEqual(0.45f, _cameraController.ShakeMagnitude, 0.001f, "Độ rung phải bằng 0.45f.");

            // Giả lập trôi qua 0.1s
            _cameraController.UpdateShake(0.1f);
            Assert.AreEqual(0.4f, _cameraController.RemainingShakeDuration, 0.001f, "Sau 0.1s thời lượng rung phải giảm còn 0.4s.");
            Assert.IsTrue(_cameraController.IsShaking, "Sau 0.1s camera vẫn đang rung.");

            // Giả lập trôi hết thời gian còn lại (0.45s > 0.4s)
            _cameraController.UpdateShake(0.45f);
            Assert.IsFalse(_cameraController.IsShaking, "Sau khi hết thời lượng rung, IsShaking phải chuyển về false.");
            Assert.AreEqual(0f, _cameraController.RemainingShakeDuration, 0.001f, "Thời lượng rung còn lại phải bằng 0.");
            Assert.AreEqual(Vector3.zero, _cameraController.CurrentShakeOffset, "Độ dời rung chấn phải triệt tiêu về Vector3.zero.");
        }

        [Test]
        public void CameraShake_Accessibility_DisabledShakeProducesZeroOffset()
        {
            _cameraController.EnableScreenShake = false;
            _cameraController.TriggerShake(0.5f, 0.45f);

            Assert.IsFalse(_cameraController.IsShaking, "Khi EnableScreenShake bị tắt, IsShaking phải bằng false.");
            Assert.AreEqual(Vector3.zero, _cameraController.CurrentShakeOffset, "Khi EnableScreenShake bị tắt, CurrentShakeOffset phải bằng Vector3.zero.");

            // Bật lại rồi tắt giữa chừng
            _cameraController.EnableScreenShake = true;
            _cameraController.TriggerShake(0.5f, 0.45f);
            Assert.IsTrue(_cameraController.IsShaking);

            _cameraController.EnableScreenShake = false;
            Assert.IsFalse(_cameraController.IsShaking, "Tắt EnableScreenShake giữa chừng phải lập tức dừng rung.");
            Assert.AreEqual(Vector3.zero, _cameraController.CurrentShakeOffset, "Tắt EnableScreenShake giữa chừng phải lập tức reset offset về zero.");
        }

        [Test]
        public void CameraShake_Accessibility_MultiplierScalesShake()
        {
            _cameraController.ShakeIntensityMultiplier = 0.5f;
            Assert.AreEqual(0.5f, _cameraController.ShakeIntensityMultiplier, 0.001f);

            // Kiểm tra clamp giới hạn multiplier [0, 2]
            _cameraController.ShakeIntensityMultiplier = 3.5f;
            Assert.AreEqual(2.0f, _cameraController.ShakeIntensityMultiplier, 0.001f, "ShakeIntensityMultiplier phải bị giới hạn tối đa 2.0f.");

            _cameraController.ShakeIntensityMultiplier = -1.0f;
            Assert.AreEqual(0.0f, _cameraController.ShakeIntensityMultiplier, 0.001f, "ShakeIntensityMultiplier phải bị giới hạn tối thiểu 0.0f.");

            // Khi multiplier = 0, rung chấn không được kích hoạt
            _cameraController.TriggerShake(0.5f, 0.45f);
            Assert.IsFalse(_cameraController.IsShaking, "Khi ShakeIntensityMultiplier = 0, camera không được rung chấn.");
        }

        [Test]
        public void CameraShake_ConvenienceMethods_TriggerAppropriateMagnitudes()
        {
            // 1. Meteor Strike: 0.5s, 0.45 magnitude
            CameraController.ShakeMeteor();
            Assert.AreEqual(0.5f, _cameraController.RemainingShakeDuration, 0.001f, "ShakeMeteor duration phải là 0.5s.");
            Assert.AreEqual(0.45f, _cameraController.ShakeMagnitude, 0.001f, "ShakeMeteor magnitude phải là 0.45f.");

            _cameraController.StopShake();

            // 2. Castle Breach: 0.35s, 0.35 magnitude
            CameraController.ShakeCastleBreach();
            Assert.AreEqual(0.35f, _cameraController.RemainingShakeDuration, 0.001f, "ShakeCastleBreach duration phải là 0.35s.");
            Assert.AreEqual(0.35f, _cameraController.ShakeMagnitude, 0.001f, "ShakeCastleBreach magnitude phải là 0.35f.");

            _cameraController.StopShake();

            // 3. Cannon Explosion: 0.25s, 0.20 magnitude
            CameraController.ShakeCannonExplosion();
            Assert.AreEqual(0.25f, _cameraController.RemainingShakeDuration, 0.001f, "ShakeCannonExplosion duration phải là 0.25s.");
            Assert.AreEqual(0.20f, _cameraController.ShakeMagnitude, 0.001f, "ShakeCannonExplosion magnitude phải là 0.20f.");

            _cameraController.StopShake();

            // 4. Boss Stomp: 0.2s, 0.15 magnitude
            CameraController.ShakeBossStomp();
            Assert.AreEqual(0.2f, _cameraController.RemainingShakeDuration, 0.001f, "ShakeBossStomp duration phải là 0.2s.");
            Assert.AreEqual(0.15f, _cameraController.ShakeMagnitude, 0.001f, "ShakeBossStomp magnitude phải là 0.15f.");
        }

        [Test]
        public void CameraShake_ZeroDrift_RestoresBasePosition()
        {
            Vector3 initialBasePos = new Vector3(2.5f, 18.0f, -12.0f);
            _cameraController.SetPosition(initialBasePos);

            // Kích hoạt rung cực mạnh
            _cameraController.TriggerShake(0.5f, 0.45f);

            // Giả lập rung trong 10 frame
            for (int i = 0; i < 10; i++)
            {
                _cameraController.UpdateShake(0.05f);
            }

            // Giả lập rung hết thời gian còn lại
            _cameraController.UpdateShake(0.3f);

            Assert.IsFalse(_cameraController.IsShaking, "Rung chấn phải kết thúc hoàn toàn.");
            Assert.AreEqual(Vector3.zero, _cameraController.CurrentShakeOffset, "CurrentShakeOffset phải hoàn toàn bằng Vector3.zero.");
            Assert.AreEqual(initialBasePos.x, _cameraController.CurrentBasePosition.x, 0.001f, "Tọa độ Base X không được bị trôi lệch sau rung chấn.");
            Assert.AreEqual(initialBasePos.y, _cameraController.CurrentBasePosition.y, 0.001f, "Tọa độ Base Y không được bị trôi lệch sau rung chấn.");
            Assert.AreEqual(initialBasePos.z, _cameraController.CurrentBasePosition.z, 0.001f, "Tọa độ Base Z không được bị trôi lệch sau rung chấn.");
        }
    }
}
