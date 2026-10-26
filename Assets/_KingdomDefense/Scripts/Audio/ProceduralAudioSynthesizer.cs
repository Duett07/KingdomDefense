using UnityEngine;

namespace KingdomDefense.Audio
{
    /// <summary>
    /// Bộ tổng hợp âm thanh thủ tục (Procedural Audio Synthesizer) bằng mã nguồn thuần túy.
    /// Tự động sinh ra các AudioClip PCM chất lượng cao (44.1kHz 16-bit Mono) cho:
    /// - Nhạc nền (BGM) phong cách trung cổ hào hùng (seamless looping D-minor Dorian motif).
    /// - Bộ hiệu ứng âm thanh (SFX): Tiếng bắn nỏ, tiếng nổ đại bác, quái gầm gừ, click gỗ UI, tiếng tù và, sấm sét,...
    /// Giúp game hoạt động độc lập 100% không phụ thuộc tệp âm thanh bên ngoài và không phát sinh dung lượng thừa.
    /// </summary>
    public static class ProceduralAudioSynthesizer
    {
        private const int SAMPLE_RATE = 44100;

        /// <summary>
        /// Sinh nhạc nền BGM phong cách trung cổ trầm hùng lặp vô tận (12 giây).
        /// Kết hợp đàn Lute gảy arpeggio hợp âm Dm/Gm/C/Am mượt mà, đàn Cello đệm trầm
        /// và tiếng trống trận trung cổ sâu lắng.
        /// Triệt tiêu hoàn toàn các tần số chói rít, không dùng sóng sáo cao tần hay tiếng xì shaker.
        /// </summary>
        public static AudioClip GenerateMedievalBGM()
        {
            float duration = 12.0f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            // 110 BPM -> 1 nhịp = 60 / 110 = 0.5454s, 1 nốt móc đơn (eighth note) = 0.2727s
            float beatDuration = 60f / 110f;
            float eighthDuration = beatDuration * 0.5f;

            // Bảng tần số nốt nhạc chuẩn trung cổ (Hz) - Dải âm ấm áp từ 65Hz đến 350Hz
            float D2 = 73.42f, G2 = 98.00f, A2 = 110.00f, C3 = 130.81f;
            float D3 = 146.83f, E3 = 164.81f, F3 = 174.61f, G3 = 196.00f, A3 = 220.00f, Bb3 = 233.08f;
            float C4 = 261.63f, D4 = 293.66f, E4 = 329.63f;

            // Lute Arpeggio Notes qua 4 ô nhịp (32 nốt móc đơn):
            // Ô 1 (Dm): D3, F3, A3, D4, F3, A3, D4, A3
            // Ô 2 (Gm): G2, D3, G3, Bb3, D3, G3, Bb3, G3
            // Ô 3 (C) : C3, G3, C4, E4, G3, C4, E4, C4
            // Ô 4 (Am -> Dm): A2, E3, A3, C4, D3, F3, A3, D4
            float[] luteNotes = new float[]
            {
                // Bar 1 (Dm)
                D3, F3, A3, D4, F3, A3, D4, A3,
                // Bar 2 (Gm)
                G2, D3, G3, Bb3, D3, G3, Bb3, G3,
                // Bar 3 (C)
                C3, G3, C4, E4, G3, C4, E4, C4,
                // Bar 4 (Am/Dm)
                A2, E3, A3, C4, D3, F3, A3, D4
            };

            // Nốt đệm Cello / Contrabass trầm hùng cho từng ô nhịp (mỗi ô 8 móc đơn = 4 nhịp)
            float barDuration = beatDuration * 4f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // 1. Đàn Lute gảy arpeggio trung cổ mộc mạc, ấm áp
                int luteIndex = Mathf.Clamp(Mathf.FloorToInt(t / eighthDuration), 0, luteNotes.Length - 1);
                float luteTimeInNote = t - (luteIndex * eighthDuration);
                float luteFreq = luteNotes[luteIndex];
                float luteEnv = Mathf.Exp(-7.5f * luteTimeInNote);
                float luteSample = (Mathf.Sin(2f * Mathf.PI * luteFreq * luteTimeInNote)
                                  + 0.45f * Mathf.Sin(4f * Mathf.PI * luteFreq * luteTimeInNote)
                                  + 0.15f * Mathf.Sin(6f * Mathf.PI * luteFreq * luteTimeInNote)) * luteEnv * 0.38f;

                // 2. Dây Cello / Contrabass đệm nền trầm sâu lắng (Tránh hoàn toàn nốt sáo cao rít)
                int currentBar = Mathf.Clamp(Mathf.FloorToInt(t / barDuration), 0, 3);
                float barTime = t - (currentBar * barDuration);
                float bassFreq;
                if (currentBar == 0) bassFreq = D2;
                else if (currentBar == 1) bassFreq = G2;
                else if (currentBar == 2) bassFreq = C3 * 0.5f; // C2 = 65.4Hz
                else bassFreq = (barTime < barDuration * 0.5f) ? A2 : D2;

                float bassEnv = Mathf.Sin(Mathf.Clamp01(barTime / barDuration) * Mathf.PI);
                float bassSample = (Mathf.Sin(2f * Mathf.PI * bassFreq * barTime)
                                  + 0.35f * Mathf.Sin(4f * Mathf.PI * bassFreq * barTime)) * bassEnv * 0.28f;

                // 3. Trống trận trung cổ (War Drum) trầm ấm, nhịp nhàng trên nhịp 1 và 3
                float drumTimeInBeat = t % beatDuration;
                int currentBeat = Mathf.FloorToInt(t / beatDuration) % 4;
                float drumSample = 0f;
                if (currentBeat == 0 || currentBeat == 2)
                {
                    float drumFreq = Mathf.Lerp(80f, 40f, Mathf.Clamp01(drumTimeInBeat / 0.18f));
                    float drumEnv = Mathf.Exp(-12f * drumTimeInBeat);
                    drumSample = Mathf.Sin(2f * Mathf.PI * drumFreq * drumTimeInBeat) * drumEnv * 0.30f;
                }

                // Fade-in 0.05s và Fade-out 0.05s ở 2 đầu để triệt tiêu tiếng click khi Loop
                float edgeFade = 1f;
                if (t < 0.05f) edgeFade = t / 0.05f;
                else if (t > duration - 0.05f) edgeFade = (duration - t) / 0.05f;

                float finalMix = (luteSample + bassSample + drumSample) * edgeFade;
                samples[i] = Mathf.Clamp(finalMix, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_MedievalBGM", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng bắn tên / nỏ vút xé gió và bật dây cung (0.16s).
        /// </summary>
        public static AudioClip GenerateArrowShoot()
        {
            float duration = 0.16f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // Tiếng dây cung bật (Bowstring snap): 580Hz hạ xuống 220Hz
                float snapFreq = Mathf.Lerp(580f, 220f, t / duration);
                float snapEnv = Mathf.Exp(-40f * t);
                float snap = Mathf.Sin(2f * Mathf.PI * snapFreq * t) * snapEnv;

                // Tiếng rít xé gió (Air whoosh): dải ồn lọc cao
                float noise = (Mathf.PerlinNoise(t * 8000f, 0.2f) - 0.5f) * 2f;
                float whooshEnv = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI) * Mathf.Exp(-12f * t);

                samples[i] = Mathf.Clamp((snap * 0.65f + noise * whooshEnv * 0.35f), -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_ArrowShoot", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng đại bác bắn / khai hỏa nòng pháo (0.32s).
        /// </summary>
        public static AudioClip GenerateCannonFire()
        {
            float duration = 0.32f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // Tiếng đục nòng siêu trầm: 160Hz -> 38Hz
                float freq = Mathf.Lerp(160f, 38f, Mathf.Clamp01(t / 0.18f));
                float boomEnv = Mathf.Exp(-14f * t);
                float boom = Mathf.Sin(2f * Mathf.PI * freq * t) * boomEnv;

                // Khói lửa xì mạnh tại đầu nòng
                float noise = (Mathf.PerlinNoise(t * 6000f, 0.8f) - 0.5f) * 2f;
                float noiseEnv = Mathf.Exp(-18f * t);

                samples[i] = Mathf.Clamp(boom * 0.7f + noise * noiseEnv * 0.4f, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_CannonFire", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng nổ lớn diện rộng của đạn pháo chạm đất (0.55s).
        /// </summary>
        public static AudioClip GenerateCannonExplosion()
        {
            float duration = 0.55f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // Sub-bass rung chuyển mặt đất: 65Hz hạ xuống 28Hz
                float subFreq = Mathf.Lerp(65f, 28f, t / duration);
                float subEnv = Mathf.Exp(-6.5f * t);
                float sub = Mathf.Sin(2f * Mathf.PI * subFreq * t) * subEnv;

                // Tiếng nổ đất đá rách toạc
                float noise = (Mathf.PerlinNoise(t * 4500f, 1.5f) - 0.5f) * 2f;
                float noiseEnv = Mathf.Exp(-8.0f * t);

                // Tiếng mảnh văng lách tách
                float crackle = (Mathf.PerlinNoise(t * 18000f, 3.2f) > 0.6f ? 1f : 0f) * Mathf.Exp(-12f * t);

                samples[i] = Mathf.Clamp(sub * 0.55f + noise * noiseEnv * 0.45f + crackle * 0.15f, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_CannonExplosion", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng quái vật gầm gừ khi bị tiêu diệt (0.38s).
        /// </summary>
        public static AudioClip GenerateEnemyDeath()
        {
            float duration = 0.38f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // FM synth giả lập tiếng gầm đục: Carrier 105Hz -> 45Hz, Modulator 38Hz
                float carrierFreq = Mathf.Lerp(105f, 45f, t / duration);
                float modFreq = 38f;
                float modIndex = 3.2f * (1f - (t / duration));
                float modulation = Mathf.Sin(2f * Mathf.PI * modFreq * t) * modIndex;

                float growl = Mathf.Sin(2f * Mathf.PI * (carrierFreq + modulation * 25f) * t);
                float env = Mathf.Exp(-7.5f * t);

                // Tiếng va chạm ngã nhào
                float thud = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-16f * t);

                samples[i] = Mathf.Clamp(growl * env * 0.7f + thud * 0.3f, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_EnemyDeath", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng click chuột vào nút bấm UI / chọn tháp (0.06s).
        /// </summary>
        public static AudioClip GenerateUIClick()
        {
            float duration = 0.06f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                // Nốt pop đanh sắc nét tại 1250Hz hạ nhanh về 400Hz
                float freq = Mathf.Lerp(1250f, 400f, t / duration);
                float env = Mathf.Exp(-75f * t);
                samples[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.85f, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_UIClick", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng cắm tháp thành công / tiếng vàng leng keng (0.28s).
        /// </summary>
        public static AudioClip GenerateTowerPlaced()
        {
            float duration = 0.28f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            float tone1 = 659.25f; // E5
            float tone2 = 987.77f; // B5

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                float chime1 = (t < 0.2f) ? Mathf.Sin(2f * Mathf.PI * tone1 * t) * Mathf.Exp(-18f * t) : 0f;
                float chime2 = (t >= 0.07f) ? Mathf.Sin(2f * Mathf.PI * tone2 * (t - 0.07f)) * Mathf.Exp(-16f * (t - 0.07f)) : 0f;

                // Thêm hài âm kim loại (Metallic harmonics)
                float harm2 = (t >= 0.07f) ? Mathf.Sin(2f * Mathf.PI * tone2 * 2.76f * (t - 0.07f)) * Mathf.Exp(-24f * (t - 0.07f)) * 0.25f : 0f;

                samples[i] = Mathf.Clamp((chime1 * 0.45f + chime2 * 0.45f + harm2), -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_TowerPlaced", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng thiên thạch va chạm mặt đất cực mạnh (0.85s).
        /// </summary>
        public static AudioClip GenerateMeteorImpact()
        {
            float duration = 0.85f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // 1. Tiếng rít xé gió từ trên trời lao xuống trong 0.15s đầu
                float scream = 0f;
                if (t < 0.22f)
                {
                    float screamFreq = Mathf.Lerp(1200f, 240f, t / 0.22f);
                    scream = Mathf.Sin(2f * Mathf.PI * screamFreq * t) * (1f - t / 0.22f) * 0.45f;
                }

                // 2. Chấn động địa chấn siêu trầm (32Hz)
                float quake = 0f;
                if (t >= 0.12f)
                {
                    float tExplode = t - 0.12f;
                    float quakeFreq = Mathf.Lerp(55f, 24f, tExplode / 0.73f);
                    float quakeEnv = Mathf.Exp(-4.5f * tExplode);
                    quake = Mathf.Sin(2f * Mathf.PI * quakeFreq * tExplode) * quakeEnv * 0.65f;

                    // Tiếng nổ tung đất đá
                    float noise = (Mathf.PerlinNoise(t * 3000f, 2.8f) - 0.5f) * 2f;
                    float noiseEnv = Mathf.Exp(-5.5f * tExplode);
                    quake += noise * noiseEnv * 0.45f;
                }

                samples[i] = Mathf.Clamp(scream + quake, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_MeteorImpact", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng phóng tia sét ma thuật xuyên giáp (0.25s).
        /// </summary>
        public static AudioClip GenerateMagicZap()
        {
            float duration = 0.25f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // Tia điện tích chớp nhoáng: Tần số dao động nhanh từ 1400Hz xuống 350Hz
                float zapFreq = Mathf.Lerp(1400f, 350f, t / duration);
                float wobble = Mathf.Sin(2f * Mathf.PI * 45f * t) * 80f;
                float zap = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * (zapFreq + wobble) * t)) * 0.4f; // Dạng sóng vuông sắc lạnh

                float env = Mathf.Exp(-14f * t);
                float noise = (Mathf.PerlinNoise(t * 12000f, 0.9f) - 0.5f) * 2f * env * 0.35f;

                samples[i] = Mathf.Clamp(zap * env + noise, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_MagicZap", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng tinh thể băng giá nứt vỡ và đóng băng (0.32s).
        /// </summary>
        public static AudioClip GenerateFrostFreeze()
        {
            float duration = 0.32f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            float C7 = 2093.00f;
            float G7 = 3135.96f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // Âm sắc thủy tinh cao vút
                float crystal1 = Mathf.Sin(2f * Mathf.PI * C7 * t) * Mathf.Exp(-16f * t);
                float crystal2 = Mathf.Sin(2f * Mathf.PI * G7 * t) * Mathf.Exp(-22f * t);

                // Tiếng tinh thể tuyết lạo xạo
                float frostNoise = (Mathf.PerlinNoise(t * 15000f, 4.2f) - 0.5f) * 2f * Mathf.Exp(-12f * t) * 0.35f;

                samples[i] = Mathf.Clamp(crystal1 * 0.45f + crystal2 * 0.35f + frostNoise, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_FrostFreeze", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Tiếng tù và chiến trận hoàng gia (Royal War Horn) báo hiệu đợt quái tiến công (1.1s).
        /// Hùng tráng với 2 hồi kèn liên tiếp (D3 -> A3) cùng hòa âm kèn đồng dày ấm.
        /// </summary>
        public static AudioClip GenerateWaveHorn()
        {
            float duration = 1.1f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            float D3 = 146.83f;
            float A3 = 220.00f; // Quãng 5 hoàn hảo

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;

                // Hồi 1: Nốt trầm D3 (0s - 0.35s)
                // Hồi 2: Nốt vút cao A3 (0.35s - 1.1s)
                float freq;
                float noteTime;
                float noteDuration;
                if (t < 0.35f)
                {
                    freq = D3;
                    noteTime = t;
                    noteDuration = 0.35f;
                }
                else
                {
                    freq = A3;
                    noteTime = t - 0.35f;
                    noteDuration = 0.75f;
                }

                // Envelope mượt mà với attack êm và release ngân tự nhiên
                float env = 1f;
                float attack = 0.06f;
                float release = 0.22f;
                if (noteTime < attack) env = noteTime / attack;
                else if (noteTime > noteDuration - release) env = (noteDuration - noteTime) / release;

                // Hòa âm kèn đồng trung cổ ấm áp (Fundamental + 2nd & 3rd harmonics)
                float horn = Mathf.Sin(2f * Mathf.PI * freq * noteTime)
                           + 0.40f * Mathf.Sin(4f * Mathf.PI * freq * noteTime)
                           + 0.15f * Mathf.Sin(6f * Mathf.PI * freq * noteTime);

                samples[i] = Mathf.Clamp(horn * env * 0.58f, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_WaveHorn", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Điệp khúc chiến thắng huy hoàng (1.25s).
        /// </summary>
        public static AudioClip GenerateVictoryFanfare()
        {
            float duration = 1.25f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            // C4 -> E4 -> G4 -> C5
            float[] notes = new float[] { 261.63f, 329.63f, 392.00f, 523.25f };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float mix = 0f;

                for (int n = 0; n < notes.Length; n++)
                {
                    float noteStart = n * 0.18f;
                    if (t >= noteStart)
                    {
                        float timeInNote = t - noteStart;
                        float env = Mathf.Exp(-4.0f * timeInNote);
                        float tone = Mathf.Sin(2f * Mathf.PI * notes[n] * t)
                                   + 0.35f * Mathf.Sin(4f * Mathf.PI * notes[n] * t);
                        mix += tone * env * 0.25f;
                    }
                }

                samples[i] = Mathf.Clamp(mix, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_VictoryFanfare", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// SFX: Điệp khúc thất bại ai oán (1.25s).
        /// </summary>
        public static AudioClip GenerateDefeatFanfare()
        {
            float duration = 1.25f;
            int totalSamples = Mathf.RoundToInt(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            // A3 -> F3 -> D3 (Minor cadence)
            float[] notes = new float[] { 220.00f, 174.61f, 146.83f };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float mix = 0f;

                for (int n = 0; n < notes.Length; n++)
                {
                    float noteStart = n * 0.28f;
                    if (t >= noteStart)
                    {
                        float timeInNote = t - noteStart;
                        float tremolo = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                        float env = Mathf.Exp(-3.2f * timeInNote) * tremolo;
                        float tone = Mathf.Sin(2f * Mathf.PI * notes[n] * t)
                                   + 0.25f * Mathf.Sin(4f * Mathf.PI * notes[n] * t);
                        mix += tone * env * 0.32f;
                    }
                }

                samples[i] = Mathf.Clamp(mix, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create("Procedural_DefeatFanfare", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
