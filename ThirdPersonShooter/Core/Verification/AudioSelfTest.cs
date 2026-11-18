using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>`--test-audio`: headless checks of the pure audio parts (no audio device, no window). Exit code = number of failures.</summary>
    public static class AudioSelfTest
    {
        public static int Run(string contentRoot)
        {
            int fail = 0, n = 0;
            void Check(string name, bool ok, string detail) { n++; if (!ok) fail++; Console.WriteLine($"[test-audio] {(ok ? "PASS" : "FAIL")} {name}: {detail}"); }

            // 1. bus model: Master bus -10 dB x Master slider x bus slider
            float full = AudioMath.BusGain(1f, 1f), half = AudioMath.BusGain(0.5f, 0.5f);
            Check("bus gain = master(-10 dB) x Master x bus", Math.Abs(full - 0.31623f) < 1e-4f && Math.Abs(half - full * 0.25f) < 1e-5f && AudioMath.BusGain(0f, 1f) == 0f && AudioMath.BusGain(1f, 0f) == 0f,
                $"1x1={full:F4} (10^-0.5), 0.5x0.5={half:F4}, master 0 -> {AudioMath.BusGain(0f, 1f)}, bus 0 -> {AudioMath.BusGain(1f, 0f)}");

            // 2. 3D attenuation (Godot inverse distance, max_db 3) + pan
            float a1 = AudioMath.Attenuation3DDb(15f, 15f, 0f), a2 = AudioMath.Attenuation3DDb(30f, 15f, 0f), a3 = AudioMath.Attenuation3DDb(0.5f, 15f, 12f), a4 = AudioMath.Attenuation3DDb(60f, 20f, 0f);
            float panR = AudioMath.Pan(new Vector3(5, 0, 0), Vector3.Zero, Vector3.Right), panL = AudioMath.Pan(new Vector3(-5, 0, 0), Vector3.Zero, Vector3.Right), panC = AudioMath.Pan(new Vector3(0, 0, -5), Vector3.Zero, Vector3.Right);
            Check("3D inverse-distance attenuation + pan", Math.Abs(a1) < 1e-3f && Math.Abs(a2 + 6.0206f) < 1e-3f && Math.Abs(a3 - AudioMath.MaxDb) < 1e-5f && Math.Abs(a4 + 9.5424f) < 1e-3f && panR > 0.99f && panL < -0.99f && Math.Abs(panC) < 1e-4f,
                $"d=unit {a1:F2} dB, d=2 unit {a2:F2} dB, near+12 dB clamps to {a3:F1} dB (max_db), 60 m @unit 20 {a4:F2} dB; pan right {panR:F2} left {panL:F2} centre {panC:F2}");

            // 3. event -> sound table: every event has a def, asset built into the content, Godot values
            bool all = true; string missing = "";
            foreach (AudioEvent e in Enum.GetValues(typeof(AudioEvent)))
            {
                var d = AudioTable.Get(e);
                if (d == null || !File.Exists(Path.Combine(contentRoot, d.FilePath)) || (d.Is3D && d.UnitSize <= 0f)) { all = false; missing += e + " "; }
            }
            var rw = AudioTable.Get(AudioEvent.RobotWalk); var rx = AudioTable.Get(AudioEvent.RobotExplosion); var be = AudioTable.Get(AudioEvent.BulletExplode); var js = AudioTable.Get(AudioEvent.PlayerJump);
            bool godot = rw.Is3D && rw.UnitSize == 15f && rw.VolumeDb == 3f && rx.VolumeDb == 12f && be.UnitSize == 20f && !js.Is3D
                && AudioTable.Get(AudioEvent.RobotCharge).Channel == AudioTable.Get(AudioEvent.RobotShoot).Channel
                && AudioMath.Crossed(0.7f, 0.9f, 0.8f, 2.5f) && AudioMath.Crossed(2.4f, 3.4f, 0.8f, 2.5f) && !AudioMath.Crossed(0.9f, 2.0f, 0.8f, 2.5f);
            Check("event table complete, Godot values, wrap-aware keys", all && godot, $"{AudioTable.All.Length} sounds, all xnb present={all} {missing}; walk +3 dB/unit 15, explosion +12 dB, bullet unit 20, cannon charge+shoot share a channel, key crossing wraps");

            // 4. footsteps: contact phases derived from the clip foot bones (2 per run cycle, one per foot)
            var sc = new System.ComponentModel.Design.ServiceContainer();
            using var cm = new Microsoft.Xna.Framework.Content.ContentManager(sc, contentRoot);
            var model = cm.Load<SkinnedModel>("Models/player");
            var run = FootstepTable.For(model, "running_nogun"); var idle = FootstepTable.For(model, "Idle");
            float dur = model.Clips["running_nogun"].Duration;
            string cad = run.Length == 2 ? $"{run[0]:F3},{run[1]:F3} of {dur:F3}s -> steps at {run[0] * dur:F2}s and {run[1] * dur:F2}s, {dur / 2f / 1.3f:F2}s apart at the 1.3x walk scale" : "?";
            Check("footstep contacts from clip feet", run.Length == 2 && run[1] - run[0] > 0.3f && run[1] - run[0] < 0.7f && idle.Length == 0, $"running_nogun phases {cad}; Idle has none");

            // 5. audio zones: emitter position -> bus (level.scene.json volumes: Outside polygon prism x +-50 m in Y, Reactor capsule r 39 = sphere at the origin)
            AudioZones.Load(contentRoot);
            (string, Vector3, AudioBus)[] pts =
            {
                ("origin", new Vector3(0, 0, 0), AudioBus.Reactor), ("robot marker 3", new Vector3(-2.96f, -11.69f, 20.23f), AudioBus.Reactor), ("robot marker 4", new Vector3(-9.16f, -11.69f, -16.92f), AudioBus.Reactor),
                ("player spawn", new Vector3(64.8f, -1.08f, 78.76f), AudioBus.Outside), ("robot marker 1", new Vector3(71.59f, -6.06f, 46.27f), AudioBus.Outside), ("poly (60,0,60)", new Vector3(60, 0, 60), AudioBus.Outside),
                ("west of both (-40,0,-10)", new Vector3(-40, 0, -10), AudioBus.Master), ("south (20,0,-60)", new Vector3(20, 0, -60), AudioBus.Master),
                ("above the reactor (0,60,0)", new Vector3(0, 60, 0), AudioBus.Master), ("above the prism (60,60,60)", new Vector3(60, 60, 60), AudioBus.Master),
            };
            string zs = ""; bool zok = AudioZones.Count == 2;
            foreach (var (nm, p, want) in pts) { var got = AudioZones.BusAt(p); if (got != want) zok = false; zs += $"{nm}={got}{(got == want ? "" : "(want " + want + ")")}; "; }
            Check("audio zones: point -> bus", zok, $"{AudioZones.Count} volumes; {zs}");

            // 6. 3D sounds are mono buffers (OpenAL pans only mono), 2D sounds stay stereo as authored (source wavs of the 2D xnb)
            string repo = contentRoot; while (repo != null && !Directory.Exists(Path.Combine(repo, "tps-demo-master"))) repo = Path.GetDirectoryName(repo);
            string mono3 = "", st2 = ""; bool mok = true;
            foreach (var d in AudioTable.All)
            {
                if (d.Is3D) { var raw = SfxPcm.ReadWav(File.ReadAllBytes(Path.Combine(contentRoot, d.FilePath))); var m = SfxPcm.ToMono(raw); if (m.Channels != 1 || m.Frames != raw.Frames) mok = false; mono3 += $"{d.Asset}:{raw.Channels}->{m.Channels}ch {m.Frames}fr; "; }
            }
            if (repo != null)
                foreach (var f in new[] { "player/audio/shoot.wav", "player/audio/step.wav" }) { int ch = SfxPcm.ReadWav(File.ReadAllBytes(Path.Combine(repo, "tps-demo-master", f))).Channels; if (ch != 2) mok = false; st2 += $"{f}={ch}ch "; }
            else st2 = "(tps-demo-master not found: 2D source check skipped)";
            Check("3D sounds mono, 2D sounds stereo", mok, $"{mono3}2D sources: {st2}");

            // 7. reverb render of a real clip (robot hit) for both buses: tail = settings, finite, bounded, audible past the dry end
            var hit = SfxPcm.ToMono(SfxPcm.ReadWav(File.ReadAllBytes(Path.Combine(contentRoot, AudioTable.Get(AudioEvent.RobotHit).FilePath))));
            bool rok = true; float dpk = 0f, de = 0f; foreach (var v in hit.Samples) { dpk = MathF.Max(dpk, MathF.Abs(v)); de += v * v; }
            string rs = $"dry {hit.Seconds:F2}s peak {dpk:F2} rms {MathF.Sqrt(de / hit.Frames):F3}; ";
            float[] tails = new float[2]; int bi = 0;
            foreach (var b in new[] { AudioBus.Outside, AudioBus.Reactor })
            {
                var cfg = AudioTable.ReverbFor(b); var w = SfxPcm.Reverb(hit, cfg); float peak = 0f, e = 0f; bool nan = false;
                foreach (var v in w.Samples) { if (float.IsNaN(v) || float.IsInfinity(v)) nan = true; peak = MathF.Max(peak, MathF.Abs(v)); }
                int r0 = hit.Frames + (int)(0.3f * hit.Rate), r1 = Math.Min(w.Frames, r0 + (int)(0.3f * hit.Rate)); for (int i = r0; i < r1; i++) e += w.Samples[i] * w.Samples[i]; float rms = MathF.Sqrt(e / Math.Max(1, r1 - r0));
                float tail = w.Seconds - hit.Seconds; tails[bi++] = tail;
                if (nan || peak > 1.0001f || MathF.Abs(tail - SfxPcm.TailSeconds(cfg)) > 0.01f || rms < 1e-6f) rok = false;
                rs += $"{b}: {w.Seconds:F2}s (tail +{tail:F2}s, est {SfxPcm.TailSeconds(cfg):F2}s), peak {peak:F2}, rms 0.3-0.6s after end {rms:F4}, nan={nan}; ";
            }
            if (!(tails[0] > tails[1] && tails[1] > 1f)) rok = false;
            Check("reverb render: tail by room size, finite, bounded", rok, rs.TrimEnd());

            // info (not an assert): the music fade state machine, menu -> level, without a device
            bool oldLog = AudioManager.Log; AudioManager.Log = true; AudioManager.LogTime = 0f;
            AudioManager.PlayMusic(MusicTrack.Menu); for (int i = 0; i < 40; i++) AudioManager.Update(1f / 60f);
            AudioManager.PlayMusic(MusicTrack.Level); for (int i = 0; i < 80; i++) AudioManager.Update(1f / 60f);
            AudioManager.PlayMusic(MusicTrack.None); AudioManager.Log = oldLog;

            Console.WriteLine($"[test-audio] {n - fail}/{n} passed");
            return fail;
        }
    }
}
