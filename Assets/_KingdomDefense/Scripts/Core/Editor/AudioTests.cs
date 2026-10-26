using NUnit.Framework;
using UnityEngine;
using KingdomDefense.Audio;
using KingdomDefense.UI;

namespace KingdomDefense.Tests
{
    public class AudioTests
    {
        private GameObject _audioObj;
        private AudioManager _audioManager;

        [SetUp]
        public void SetUp()
        {
            _audioObj = new GameObject("Test_AudioManager");
            _audioManager = _audioObj.AddComponent<AudioManager>();
            _audioManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_audioObj != null)
            {
                Object.DestroyImmediate(_audioObj);
            }
        }

        [Test]
        public void AudioManager_Initializes_CreatesSingleton()
        {
            Assert.IsNotNull(AudioManager.Instance, "AudioManager singleton phải được khởi tạo thành công.");
            Assert.AreEqual(_audioManager, AudioManager.Instance, "Instance phải trỏ đúng tới component vừa tạo.");
        }

        [Test]
        public void ProceduralAudioSynthesizer_GeneratesAllAudioClipsSuccessfully()
        {
            // 1. Kiểm tra BGM
            AudioClip bgm = ProceduralAudioSynthesizer.GenerateMedievalBGM();
            Assert.IsNotNull(bgm, "Procedural Medieval BGM clip phải sinh thành công.");
            Assert.Greater(bgm.length, 5.0f, "BGM phải có thời lượng đủ dài (> 5s).");
            Assert.AreEqual(44100, bgm.frequency, "Tần số lấy mẫu phải là 44.1kHz.");

            // 2. Kiểm tra bộ SFX chiến trận
            AudioClip arrow = ProceduralAudioSynthesizer.GenerateArrowShoot();
            Assert.IsNotNull(arrow, "SFX ArrowShoot phải sinh thành công.");
            Assert.Greater(arrow.samples, 0);

            AudioClip cannonFire = ProceduralAudioSynthesizer.GenerateCannonFire();
            Assert.IsNotNull(cannonFire, "SFX CannonFire phải sinh thành công.");

            AudioClip cannonExplode = ProceduralAudioSynthesizer.GenerateCannonExplosion();
            Assert.IsNotNull(cannonExplode, "SFX CannonExplosion phải sinh thành công.");

            AudioClip enemyDeath = ProceduralAudioSynthesizer.GenerateEnemyDeath();
            Assert.IsNotNull(enemyDeath, "SFX EnemyDeath phải sinh thành công.");

            AudioClip uiClick = ProceduralAudioSynthesizer.GenerateUIClick();
            Assert.IsNotNull(uiClick, "SFX UIClick phải sinh thành công.");

            AudioClip towerPlaced = ProceduralAudioSynthesizer.GenerateTowerPlaced();
            Assert.IsNotNull(towerPlaced, "SFX TowerPlaced phải sinh thành công.");

            AudioClip meteor = ProceduralAudioSynthesizer.GenerateMeteorImpact();
            Assert.IsNotNull(meteor, "SFX MeteorImpact phải sinh thành công.");

            AudioClip magicZap = ProceduralAudioSynthesizer.GenerateMagicZap();
            Assert.IsNotNull(magicZap, "SFX MagicZap phải sinh thành công.");

            AudioClip frostFreeze = ProceduralAudioSynthesizer.GenerateFrostFreeze();
            Assert.IsNotNull(frostFreeze, "SFX FrostFreeze phải sinh thành công.");

            AudioClip waveHorn = ProceduralAudioSynthesizer.GenerateWaveHorn();
            Assert.IsNotNull(waveHorn, "SFX WaveHorn phải sinh thành công.");

            AudioClip victory = ProceduralAudioSynthesizer.GenerateVictoryFanfare();
            Assert.IsNotNull(victory, "SFX VictoryFanfare phải sinh thành công.");

            AudioClip defeat = ProceduralAudioSynthesizer.GenerateDefeatFanfare();
            Assert.IsNotNull(defeat, "SFX DefeatFanfare phải sinh thành công.");
        }

        [Test]
        public void AudioManager_VolumeControls_ClampsAndAppliesVolume()
        {
            // Kiểm tra giá trị mặc định
            Assert.AreEqual(1.0f, _audioManager.MasterVolume, 0.001f);
            Assert.AreEqual(0.75f, _audioManager.BGMVolume, 0.001f);
            Assert.AreEqual(0.85f, _audioManager.SFXVolume, 0.001f);
            Assert.IsFalse(_audioManager.IsMuted);

            // Kiểm tra gán và giới hạn [0, 1]
            _audioManager.MasterVolume = 1.8f;
            Assert.AreEqual(1.0f, _audioManager.MasterVolume, 0.001f, "MasterVolume phải được giới hạn tối đa 1.0.");

            _audioManager.MasterVolume = -0.5f;
            Assert.AreEqual(0.0f, _audioManager.MasterVolume, 0.001f, "MasterVolume phải được giới hạn tối thiểu 0.0.");

            _audioManager.MasterVolume = 0.8f;
            _audioManager.BGMVolume = 0.5f;
            _audioManager.SFXVolume = 0.6f;

            Assert.AreEqual(0.8f, _audioManager.MasterVolume, 0.001f);
            Assert.AreEqual(0.5f, _audioManager.BGMVolume, 0.001f);
            Assert.AreEqual(0.6f, _audioManager.SFXVolume, 0.001f);

            // Kiểm tra tính năng Tắt tiếng (Mute)
            _audioManager.ToggleMute();
            Assert.IsTrue(_audioManager.IsMuted);

            _audioManager.ToggleMute();
            Assert.IsFalse(_audioManager.IsMuted);

            _audioManager.ToggleBGM();
            Assert.IsTrue(_audioManager.IsBgmMuted);

            _audioManager.ToggleSFX();
            Assert.IsTrue(_audioManager.IsSfxMuted);
        }

        [Test]
        public void AudioManager_PlaybackAPIs_ExecuteWithoutExceptions()
        {
            Assert.DoesNotThrow(() =>
            {
                _audioManager.PlayArrowShoot();
                _audioManager.PlayCannonFire();
                _audioManager.PlayCannonExplosion();
                _audioManager.PlayEnemyDeath();
                _audioManager.PlayUIClick();
                _audioManager.PlayTowerPlaced();
                _audioManager.PlayMeteorImpact();
                _audioManager.PlayMagicZap();
                _audioManager.PlayFrostFreeze();
                _audioManager.PlayWaveHorn();
                _audioManager.PlayVictory();
                _audioManager.PlayDefeat();
            }, "Các phương thức phát SFX không được ném ra exception.");
        }

        [Test]
        public void GameHUD_AudioButtonLabels_ReflectsMuteState()
        {
            GameObject hudObj = new GameObject("Test_GameHUD");
            GameHUD hud = hudObj.AddComponent<GameHUD>();

            _audioManager.IsBgmMuted = false;
            _audioManager.IsSfxMuted = false;
            _audioManager.IsMuted = false;

            Assert.IsTrue(hud.GetBgmButtonLabel().Contains("BẬT"), "Khi BGM chưa mute, nhãn phải hiển thị BẬT.");
            Assert.IsTrue(hud.GetSfxButtonLabel().Contains("BẬT"), "Khi SFX chưa mute, nhãn phải hiển thị BẬT.");

            _audioManager.IsBgmMuted = true;
            Assert.IsTrue(hud.GetBgmButtonLabel().Contains("TẮT"), "Khi BGM đã mute, nhãn phải hiển thị TẮT.");

            _audioManager.IsSfxMuted = true;
            Assert.IsTrue(hud.GetSfxButtonLabel().Contains("TẮT"), "Khi SFX đã mute, nhãn phải hiển thị TẮT.");

            Object.DestroyImmediate(hudObj);
        }
    }
}
