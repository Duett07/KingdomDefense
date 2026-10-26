using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomDefense.Audio
{
    /// <summary>
    /// Hệ thống quản lý Âm thanh Toàn cục (AudioManager) phong cách Kingdom Rush.
    /// Bao gồm:
    /// - Nhạc nền (BGM) phong cách trung cổ hào hùng (tự động lặp vô tận, chuyển âm lượng mượt mà).
    /// - Hiệu ứng âm thanh (SFX) đa kênh: Bắn tên, nổ đại bác, quái gầm rú, click UI, tù và chiến trận,...
    /// - Hệ thống AudioSource Pool (16 kênh âm thanh động) kèm Voice Limiting chống rè âm khi quá tải.
    /// - Bộ lưu trữ âm lượng (Master, BGM, SFX, Mute) tự động đồng bộ qua PlayerPrefs.
    /// - Tích hợp fallback thông minh: Tự sinh Procedural Clips nếu Inspector chưa gán tệp âm thanh.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string PREF_MASTER_VOL = "KingdomDefense_MasterVolume";
        private const string PREF_BGM_VOL = "KingdomDefense_BGMVolume";
        private const string PREF_SFX_VOL = "KingdomDefense_SFXVolume";
        private const string PREF_IS_MUTED = "KingdomDefense_IsMuted";
        private const string PREF_BGM_MUTED = "KingdomDefense_BgmMuted";
        private const string PREF_SFX_MUTED = "KingdomDefense_SfxMuted";

        [Header("--- THIẾT LẬP ÂM LƯỢNG (0.0 - 1.0) ---")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.75f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [SerializeField] private bool isMuted = false;
        [SerializeField] private bool isBgmMuted = false;
        [SerializeField] private bool isSfxMuted = false;

        [Header("--- TÙY CHỌN AUDIOCLIP NGOÀI (TỰ SINH NẾU ĐỂ TRỐNG) ---")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip arrowShootClip;
        [SerializeField] private AudioClip cannonFireClip;
        [SerializeField] private AudioClip cannonExplosionClip;
        [SerializeField] private AudioClip enemyDeathClip;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip towerPlacedClip;
        [SerializeField] private AudioClip meteorImpactClip;
        [SerializeField] private AudioClip magicZapClip;
        [SerializeField] private AudioClip frostFreezeClip;
        [SerializeField] private AudioClip waveHornClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        // Quản lý AudioSources
        private AudioSource _bgmSource;
        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private const int SFX_POOL_SIZE = 16;
        private int _currentSfxIndex = 0;

        // Giới hạn tần suất phát (Voice Limiting / Cooldown) để tránh nổ âm lượng
        private readonly Dictionary<string, float> _lastPlayedTime = new Dictionary<string, float>();
        private const float VOICE_COOLDOWN = 0.045f;

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
                SaveSettings();
            }
        }

        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
                SaveSettings();
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                ApplyVolumeSettings();
                SaveSettings();
            }
        }

        public bool IsMuted
        {
            get => isMuted;
            set
            {
                isMuted = value;
                ApplyVolumeSettings();
                SaveSettings();
            }
        }

        public bool IsBgmMuted
        {
            get => isBgmMuted;
            set
            {
                isBgmMuted = value;
                ApplyVolumeSettings();
                SaveSettings();
            }
        }

        public bool IsSfxMuted
        {
            get => isSfxMuted;
            set
            {
                isSfxMuted = value;
                ApplyVolumeSettings();
                SaveSettings();
            }
        }

        public static AudioManager GetOrCreateInstance()
        {
            if (Instance == null)
            {
                Instance = Object.FindAnyObjectByType<AudioManager>();
                if (Instance == null)
                {
                    GameObject audioObj = new GameObject("AudioManager");
                    Instance = audioObj.AddComponent<AudioManager>();
                }
            }
            return Instance;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
                Initialize();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Initialize()
        {
            LoadSettings();
            SetupAudioSources();
            EnsureClipsInitialized();
            ApplyVolumeSettings();

            if (Application.isPlaying)
            {
                PlayBGM();
            }
        }

        private void SetupAudioSources()
        {
            // 1. AudioSource dành riêng cho BGM
            if (_bgmSource == null)
            {
                GameObject bgmChild = new GameObject("BGM_Source");
                bgmChild.transform.SetParent(transform, false);
                _bgmSource = bgmChild.AddComponent<AudioSource>();
                _bgmSource.loop = true;
                _bgmSource.playOnAwake = false;
                _bgmSource.spatialBlend = 0f; // 2D Stereo
            }

            // 2. AudioSource Pool (16 kênh) dành cho SFX
            if (_sfxPool.Count < SFX_POOL_SIZE)
            {
                _sfxPool.Clear();
                GameObject sfxContainer = new GameObject("SFX_Pool");
                sfxContainer.transform.SetParent(transform, false);

                for (int i = 0; i < SFX_POOL_SIZE; i++)
                {
                    GameObject sfxObj = new GameObject($"SFX_Source_{i}");
                    sfxObj.transform.SetParent(sfxContainer.transform, false);
                    AudioSource src = sfxObj.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.spatialBlend = 0f; // 2D sound cho Game chiến thuật
                    _sfxPool.Add(src);
                }
            }
        }

        public void EnsureClipsInitialized()
        {
            if (bgmClip == null) bgmClip = ProceduralAudioSynthesizer.GenerateMedievalBGM();
            if (arrowShootClip == null) arrowShootClip = ProceduralAudioSynthesizer.GenerateArrowShoot();
            if (cannonFireClip == null) cannonFireClip = ProceduralAudioSynthesizer.GenerateCannonFire();
            if (cannonExplosionClip == null) cannonExplosionClip = ProceduralAudioSynthesizer.GenerateCannonExplosion();
            if (enemyDeathClip == null) enemyDeathClip = ProceduralAudioSynthesizer.GenerateEnemyDeath();
            if (uiClickClip == null) uiClickClip = ProceduralAudioSynthesizer.GenerateUIClick();
            if (towerPlacedClip == null) towerPlacedClip = ProceduralAudioSynthesizer.GenerateTowerPlaced();
            if (meteorImpactClip == null) meteorImpactClip = ProceduralAudioSynthesizer.GenerateMeteorImpact();
            if (magicZapClip == null) magicZapClip = ProceduralAudioSynthesizer.GenerateMagicZap();
            if (frostFreezeClip == null) frostFreezeClip = ProceduralAudioSynthesizer.GenerateFrostFreeze();
            if (waveHornClip == null) waveHornClip = ProceduralAudioSynthesizer.GenerateWaveHorn();
            if (victoryClip == null) victoryClip = ProceduralAudioSynthesizer.GenerateVictoryFanfare();
            if (defeatClip == null) defeatClip = ProceduralAudioSynthesizer.GenerateDefeatFanfare();
        }

        public void ApplyVolumeSettings()
        {
            if (_bgmSource != null)
            {
                float targetBGM = (isMuted || isBgmMuted) ? 0f : (masterVolume * bgmVolume);
                _bgmSource.volume = targetBGM;
            }

            float currentSFX = (isMuted || isSfxMuted) ? 0f : (masterVolume * sfxVolume);
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                if (_sfxPool[i] != null)
                {
                    _sfxPool[i].volume = currentSFX;
                }
            }
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
        }

        public void ToggleBGM()
        {
            IsBgmMuted = !IsBgmMuted;
        }

        public void ToggleSFX()
        {
            IsSfxMuted = !IsSfxMuted;
        }

        public void PlayBGM(AudioClip clip = null)
        {
            if (_bgmSource == null) return;

            AudioClip toPlay = clip != null ? clip : bgmClip;
            if (toPlay == null) return;

            if (_bgmSource.clip == toPlay && _bgmSource.isPlaying) return;

            _bgmSource.clip = toPlay;
            _bgmSource.volume = (isMuted || isBgmMuted) ? 0f : (masterVolume * bgmVolume);
            _bgmSource.Play();
        }

        public void StopBGM()
        {
            if (_bgmSource != null)
            {
                _bgmSource.Stop();
            }
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitch = 1.0f)
        {
            if (clip == null || isMuted || isSfxMuted) return;

            // Kiểm tra Voice Limiting để tránh spam rè loa khi nhiều đòn đánh cùng xảy ra
            float now = Time.unscaledTime;
            string clipKey = clip.name;
            if (_lastPlayedTime.TryGetValue(clipKey, out float lastTime))
            {
                if (now - lastTime < VOICE_COOLDOWN)
                {
                    return; // Bỏ qua nếu âm thanh này vừa mới phát tức thì
                }
            }
            _lastPlayedTime[clipKey] = now;

            AudioSource src = GetNextSFXSource();
            if (src != null)
            {
                src.pitch = pitch;
                src.volume = masterVolume * sfxVolume * Mathf.Clamp01(volumeScale);
                src.PlayOneShot(clip);
            }
        }

        private AudioSource GetNextSFXSource()
        {
            if (_sfxPool.Count == 0) return null;

            // Tìm AudioSource đang rảnh
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                int index = (_currentSfxIndex + i) % _sfxPool.Count;
                if (!_sfxPool[index].isPlaying)
                {
                    _currentSfxIndex = (index + 1) % _sfxPool.Count;
                    return _sfxPool[index];
                }
            }

            // Nếu tất cả đều đang phát, mượn nguồn xoay vòng
            AudioSource fallback = _sfxPool[_currentSfxIndex];
            _currentSfxIndex = (_currentSfxIndex + 1) % _sfxPool.Count;
            return fallback;
        }

        // --- CÁC HÀM TIỆN ÍCH PHÁT ÂM THANH GAMEPLAY CHUYÊN BIỆT ---

        public void PlayArrowShoot() => PlaySFX(arrowShootClip, 0.75f, Random.Range(0.95f, 1.05f));
        public void PlayCannonFire() => PlaySFX(cannonFireClip, 0.90f, Random.Range(0.92f, 1.04f));
        public void PlayCannonExplosion() => PlaySFX(cannonExplosionClip, 1.0f, Random.Range(0.90f, 1.05f));
        public void PlayEnemyDeath() => PlaySFX(enemyDeathClip, 0.70f, Random.Range(0.92f, 1.08f));
        public void PlayUIClick() => PlaySFX(uiClickClip, 0.85f, 1.0f);
        public void PlayTowerPlaced() => PlaySFX(towerPlacedClip, 0.90f, 1.0f);
        public void PlayMeteorImpact() => PlaySFX(meteorImpactClip, 1.0f, 1.0f);
        public void PlayMagicZap() => PlaySFX(magicZapClip, 0.80f, Random.Range(0.96f, 1.04f));
        public void PlayFrostFreeze() => PlaySFX(frostFreezeClip, 0.80f, Random.Range(0.95f, 1.05f));
        public void PlayWaveHorn() => PlaySFX(waveHornClip, 0.95f, 1.0f);
        public void PlayVictory() => PlaySFX(victoryClip, 1.0f, 1.0f);
        public void PlayDefeat() => PlaySFX(defeatClip, 1.0f, 1.0f);

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(PREF_MASTER_VOL, masterVolume);
            PlayerPrefs.SetFloat(PREF_BGM_VOL, bgmVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOL, sfxVolume);
            PlayerPrefs.SetInt(PREF_IS_MUTED, isMuted ? 1 : 0);
            PlayerPrefs.SetInt(PREF_BGM_MUTED, isBgmMuted ? 1 : 0);
            PlayerPrefs.SetInt(PREF_SFX_MUTED, isSfxMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 1.0f);
            bgmVolume = PlayerPrefs.GetFloat(PREF_BGM_VOL, 0.75f);
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.85f);
            isMuted = PlayerPrefs.GetInt(PREF_IS_MUTED, 0) == 1;
            isBgmMuted = PlayerPrefs.GetInt(PREF_BGM_MUTED, 0) == 1;
            isSfxMuted = PlayerPrefs.GetInt(PREF_SFX_MUTED, 0) == 1;
        }
    }
}
