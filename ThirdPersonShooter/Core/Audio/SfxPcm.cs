using System;

namespace ThirdPersonShooter
{
    /// <summary>16-bit PCM clip as floats (-1..1), interleaved.</summary>
    public sealed class Pcm
    {
        public int Rate, Channels; public float[] Samples;
        public int Frames => Channels == 0 ? 0 : Samples.Length / Channels;
        public float Seconds => Rate == 0 ? 0f : (float)Frames / Rate;
    }

    /// <summary>Godot AudioEffectReverb parameters (default_bus_layout.tres sets room_size, hipass, wet; the rest are the Godot defaults).</summary>
    public sealed class ReverbSettings
    {
        public float RoomSize = 0.8f, Damping = 0.5f, Hipass = 0f, Wet = 0.5f, Dry = 1f, PredelayMs = 150f, PredelayFeedback = 0.4f;
    }

    /// <summary>
    /// Device-free PCM tools: WAV reader, mono downmix (OpenAL only pans/positions MONO buffers) and an offline Schroeder/Freeverb-style reverb render that approximates
    /// Godot's AudioEffectReverb (predelay with feedback -> 8 damped comb filters -> 4 allpass -> hipass -> wet/dry). Not bit-exact with Godot's own reverb (written from the
    /// Freeverb constants, not from the Godot source); rendered once per (sound, bus) into a new SoundEffect, never per frame.
    /// </summary>
    public static class SfxPcm
    {
        public const float MaxTailSeconds = 4f;        // cap of the rendered reverb tail (room 0.98 would ring ~8 s)
        const float FadeSeconds = 0.5f;                // linear fade over the end of a capped tail
        static readonly int[] CombDelays = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };   // Freeverb, at 44.1 kHz
        static readonly int[] AllpassDelays = { 556, 441, 341, 225 };

        /// <summary>Parses a RIFF/WAVE file with 16-bit PCM (any channel count).</summary>
        public static Pcm ReadWav(byte[] b)
        {
            if (b.Length < 12 || b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F') throw new FormatException("not a RIFF file");
            int rate = 0, ch = 0, bits = 0, tag = 0, p = 12; byte[] data = null; int dataOff = 0, dataLen = 0;
            while (p + 8 <= b.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(b, p, 4); int len = BitConverter.ToInt32(b, p + 4), body = p + 8;
                if (len < 0 || body + len > b.Length) len = b.Length - body;
                if (id == "fmt ") { tag = BitConverter.ToInt16(b, body); ch = BitConverter.ToInt16(b, body + 2); rate = BitConverter.ToInt32(b, body + 4); bits = BitConverter.ToInt16(b, body + 14); }
                else if (id == "data") { data = b; dataOff = body; dataLen = len; break; }
                p = body + len + (len & 1);
            }
            if (data == null || tag != 1 || bits != 16 || ch < 1) throw new FormatException($"unsupported wav (tag {tag}, {bits} bit, {ch} ch)");
            int frames = dataLen / (2 * ch); var s = new float[frames * ch];
            for (int i = 0; i < s.Length; i++) s[i] = BitConverter.ToInt16(data, dataOff + 2 * i) / 32768f;
            return new Pcm { Rate = rate, Channels = ch, Samples = s };
        }

        /// <summary>Average of all channels (a mono clip is returned as is).</summary>
        public static Pcm ToMono(Pcm src)
        {
            if (src.Channels == 1) return src;
            int n = src.Frames; var m = new float[n];
            for (int i = 0; i < n; i++) { float a = 0f; for (int c = 0; c < src.Channels; c++) a += src.Samples[i * src.Channels + c]; m[i] = a / src.Channels; }
            return new Pcm { Rate = src.Rate, Channels = 1, Samples = m };
        }

        public static byte[] ToBytes16(Pcm p)
        {
            var o = new byte[p.Samples.Length * 2];
            for (int i = 0; i < p.Samples.Length; i++) { float v = p.Samples[i]; v = v < -1f ? -1f : v > 1f ? 1f : v; short s = (short)MathF.Round(v * 32767f); o[2 * i] = (byte)(s & 0xff); o[2 * i + 1] = (byte)((s >> 8) & 0xff); }
            return o;
        }

        /// <summary>Freeverb comb feedback for a Godot room_size (0.7 + 0.28 x room) and the resulting decay estimate (-60 dB), capped at <see cref="MaxTailSeconds"/>.</summary>
        public static float CombFeedback(float room) => 0.7f + 0.28f * Math.Clamp(room, 0f, 1f);
        public static float TailSeconds(ReverbSettings r)
        {
            float loop = 1356f / 44100f, fb = CombFeedback(r.RoomSize);                       // mean comb delay
            float t60 = 3f * loop / -MathF.Log10(fb);
            return MathF.Min(MaxTailSeconds, r.PredelayMs * 0.001f + t60);
        }

        /// <summary>Offline reverb of a mono clip: dry x Dry + wet x Wet x 3 (Freeverb scalewet). Output = clip + <see cref="TailSeconds"/>; soft-limited to <= 1.0 above 0.9 (the clips are already near full scale).</summary>
        public static Pcm Reverb(Pcm mono, ReverbSettings r)
        {
            if (mono.Channels != 1) throw new ArgumentException("mono only");
            int rate = mono.Rate, n = mono.Frames, outLen = n + (int)(TailSeconds(r) * rate); float k = rate / 44100f;
            int pd = Math.Max(1, (int)(r.PredelayMs * 0.001f * rate)); var pre = new float[pd]; int pi = 0;
            var comb = new float[8][]; var ci = new int[8]; var store = new float[8];
            for (int i = 0; i < 8; i++) comb[i] = new float[Math.Max(1, (int)(CombDelays[i] * k))];
            var ap = new float[4][]; var ai = new int[4];
            for (int i = 0; i < 4; i++) ap[i] = new float[Math.Max(1, (int)(AllpassDelays[i] * k))];
            float fb = CombFeedback(r.RoomSize), damp1 = Math.Clamp(r.Damping, 0f, 1f) * 0.4f, damp2 = 1f - damp1, wet = r.Wet * 3f;
            float hpA = r.Hipass <= 0f ? 0f : 1f - MathF.Exp(-2f * MathF.PI * (r.Hipass * 1000f) / rate), hpState = 0f;   // one-pole hipass on the wet signal, fc = hipass x 1 kHz (approximation)
            var o = new float[outLen]; 
            for (int t = 0; t < outLen; t++)
            {
                float x = t < n ? mono.Samples[t] : 0f;
                float d = pre[pi]; pre[pi] = x + r.PredelayFeedback * d; if (++pi == pd) pi = 0;     // predelay line with feedback
                float inp = d * 0.015f, sum = 0f;
                for (int i = 0; i < 8; i++)
                {
                    float y = comb[i][ci[i]]; store[i] = y * damp2 + store[i] * damp1; comb[i][ci[i]] = inp + store[i] * fb; if (++ci[i] == comb[i].Length) ci[i] = 0; sum += y;
                }
                for (int i = 0; i < 4; i++) { float bo = ap[i][ai[i]]; float y = -sum + bo; ap[i][ai[i]] = sum + bo * 0.5f; if (++ai[i] == ap[i].Length) ai[i] = 0; sum = y; }
                if (hpA > 0f) { hpState += hpA * (sum - hpState); sum -= hpState; }
                float v = x * r.Dry + sum * wet; o[t] = v;
            }
            int fade = (int)(FadeSeconds * rate);
            for (int i = 0; i < fade && outLen - 1 - i >= n; i++) o[outLen - 1 - i] *= (float)i / fade;      // fade the capped tail out
            for (int i = 0; i < outLen; i++) { float a = MathF.Abs(o[i]); if (a > 0.9f) o[i] = MathF.Sign(o[i]) * (0.9f + 0.1f * MathF.Tanh((a - 0.9f) / 0.1f)); }   // soft limiter above 0.9: the dry level stays, never clips
            return new Pcm { Rate = rate, Channels = 1, Samples = o };
        }
    }
}
