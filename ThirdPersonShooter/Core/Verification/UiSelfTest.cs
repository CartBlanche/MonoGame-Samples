using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>`--test-ui`: headless checks of the settings and UI-kit logic (no GL, no content). Exit code = number of failures.</summary>
    public static class UiSelfTest
    {
        public static int Run()
        {
            int fail = 0, n = 0;
            void Check(string name, bool ok, string detail) { n++; if (!ok) fail++; Console.WriteLine($"[test-ui] {(ok ? "PASS" : "FAIL")} {name}: {detail}"); }
            string dir = Path.Combine(Path.GetTempPath(), "tps-test-ui-" + Environment.ProcessId); Directory.CreateDirectory(dir);
            try
            {
                // 1. settings JSON round-trip (every persisted field, including defaults not being re-applied over saved values)
                var a = new GameSettings { Fullscreen = true, ResolutionWidth = 1600, ResolutionHeight = 900, VSync = false, Bloom = false, Shadows = false, Rumble = false, MouseSensitivity = 1.75f, MasterVolume = 0.4f, MusicVolume = 0.25f, SfxVolume = 0.8f };
                string p = Path.Combine(dir, "settings.json"); a.Save(p); var b = GameSettings.Load(p);
                Check("settings json round-trip", b.ValueEquals(a) && File.ReadAllText(p).Contains("\"MouseSensitivity\""), $"fullscreen={b.Fullscreen} {b.ResolutionWidth}x{b.ResolutionHeight} vsync={b.VSync} bloom={b.Bloom} sens={b.MouseSensitivity} vol={b.MasterVolume}/{b.MusicVolume}/{b.SfxVolume}");

                // 2. Apply/Cancel: edits go to a copy; Cancel leaves the live settings (and the file) untouched, Apply commits + saves
                string p2 = Path.Combine(dir, "apply.json"); var live = new GameSettings();
                var e1 = new SettingsEditor(live, p2); e1.Working.Bloom = false; e1.Working.MasterVolume = 0.1f; e1.Cancel();
                bool cancelOk = live.Bloom && live.MasterVolume == 1f && !File.Exists(p2);
                var e2 = new SettingsEditor(live, p2); e2.Working.Bloom = false; e2.Working.MasterVolume = 0.1f; bool beforeApply = live.Bloom; e2.Apply();
                bool applyOk = beforeApply && !live.Bloom && live.MasterVolume == 0.1f && !GameSettings.Load(p2).Bloom;
                Check("apply/cancel semantics", cancelOk && applyOk, $"cancel keeps live={cancelOk}, apply commits+saves={applyOk}");

                // 3. UiFocus: Up/Down never lands on the disabled MULTIPLAYER button (keyboard nav, wrap, and mouse hover)
                var menu = new MainMenuScreen(null, () => { }, () => { }, () => { }); menu.Show(MainMenuScreen.Page.PlayList);
                var f = menu.Focus; bool startOk = f.Focused == menu.SingleplayerButton; bool never = true;
                for (int i = 0; i < 6; i++) { f.Update(new UiInput { Up = true }); never &= f.Focused != menu.MultiplayerButton; f.Update(new UiInput { Down = true }); never &= f.Focused != menu.MultiplayerButton; f.Update(new UiInput { Up = true }); never &= f.Focused != menu.MultiplayerButton; }
                f.SetFocus(menu.SingleplayerButton);
                var m = menu.MultiplayerButton.Center; f.Update(new UiInput { Mouse = new Vector2(m.X, m.Y), MouseMoved = true }); never &= f.Focused != menu.MultiplayerButton;
                bool activateIgnored = true; menu.MultiplayerButton.Clicked = () => activateIgnored = false; menu.MultiplayerButton.Activate();
                Check("focus nav skips disabled multiplayer", startOk && never && activateIgnored, $"initial focus singleplayer={startOk}, never on multiplayer={never}, disabled click ignored={activateIgnored}");

                // 4-6. UiMessageBox (pause "Leave?" default Stay, quit "Quit?" default Quit!): default focus, Esc = Left always, confirm, callback exactly once
                MessageBoxResult? res = null; int calls = 0;
                UiMessageBox Box(bool quit) { res = null; calls = 0; return new UiMessageBox(quit ? "Quit?" : "Leave?", quit ? "No" : "Stay", quit ? "Quit!" : "Leave!", quit ? MessageBoxResult.Right : MessageBoxResult.Left, r => { res = r; calls++; }); }
                var pb = Box(false); var qb = Box(true);
                Check("message box default button focused", pb.DefaultIsFocused(MessageBoxResult.Left) && qb.DefaultIsFocused(MessageBoxResult.Right), $"pause default Stay={pb.DefaultIsFocused(MessageBoxResult.Left)}, quit default Quit!={qb.DefaultIsFocused(MessageBoxResult.Right)}");
                bool escOk = true;
                foreach (bool q in new[] { false, true }) { var bx = Box(q); bx.Update(new UiInput { Back = true }, 0f); bx.Update(new UiInput { Back = true, Confirm = true }, 0f); escOk &= res == MessageBoxResult.Left && calls == 1; }
                Check("message box Esc = Left, once", escOk, $"Esc returns Left on both boxes (even when Quit! is the default) and dispatches once, later input ignored={escOk}");
                var b1 = Box(false); b1.Update(new UiInput { Right = true }, 0f); b1.Update(new UiInput { Confirm = true }, 0f); bool pauseRight = res == MessageBoxResult.Right && calls == 1;
                var b2 = Box(true); b2.Update(new UiInput { Confirm = true }, 0f); b2.Update(new UiInput { Confirm = true }, 0f); bool quitRight = res == MessageBoxResult.Right && calls == 1;
                var b3 = Box(true); b3.Update(new UiInput { Left = true }, 0f); b3.Update(new UiInput { Confirm = true }, 0f); bool quitLeft = res == MessageBoxResult.Left && calls == 1;
                Check("message box confirm focused button", pauseRight && quitRight && quitLeft, $"pause Right+Enter=Right {pauseRight}, quit Enter(default)=Right {quitRight}, quit Left+Enter=Left {quitLeft}, each dispatched once");

                // 7. loading plan covers every asset group the old LoadContent loaded (nothing skipped, nothing loaded twice): boot steps + play steps == the old full set
                var all = new System.Collections.Generic.List<string>(); foreach (var t in LoadTable.Boot) all.Add(t.Name); foreach (var t in LoadTable.Play) all.Add(t.Name);
                var sorted = new System.Collections.Generic.List<string>(all); sorted.Sort(StringComparer.Ordinal); var want = new System.Collections.Generic.List<string>(LoadTable.FullSet); want.Sort(StringComparer.Ordinal);
                Check("loading steps cover the old full set", sorted.Count == want.Count && sorted.SequenceEqual(want), $"boot {LoadTable.Boot.Length} + play {LoadTable.Play.Length} = {all.Count} steps vs {want.Count} old groups, same names={sorted.SequenceEqual(want)}");

                // 8. progress math over the play table (one step reports out-of-order fractions): monotonic, clamped to 0..1, exactly 1 after the last step
                var steps = new System.Collections.Generic.List<LoadStep>(); float[] odd = { 0.5f, 0.3f, 1.7f, -2f, 1f };
                foreach (var t in LoadTable.Play) { if (t.Name == "scene") { int k = 0; steps.Add(new LoadStep(t.Name, t.Weight, () => odd[k++])); } else steps.Add(LoadStep.Once(t.Name, t.Weight, () => { })); }
                var run = new LoadRunner(steps); float prev = 0f; bool mono = true, inRange = run.Progress == 0f; int ticks = 0;
                while (!run.Done && ticks++ < 100) { run.Tick(); mono &= run.Progress >= prev; inRange &= run.Progress >= 0f && run.Progress <= 1f; prev = run.Progress; }
                Check("loading progress math", mono && inRange && run.Done && run.Progress == 1f && ticks == LoadTable.Play.Length + 2, $"monotonic={mono}, in 0..1={inRange}, final={run.Progress:F2} done={run.Done} after {ticks} ticks");

                // 9. rumble service (headless sink): fire/laser pulses, linear decay, zero on pause/disconnect, silent when off or no pad
                {
                    bool pad = true, on = true; var rcalls = new System.Collections.Generic.List<(float, float)>();
                    var r = new RumbleService(() => pad, () => on, (l, h) => rcalls.Add((l, h)));
                    const float dt = 1f / 60f;
                    r.Update(dt, true); bool quiet = rcalls.Count == 0;
                    r.OnTrauma(PlayerConstants.ShootTrauma); r.Update(dt, true);
                    bool fire = rcalls.Count == 1 && Math.Abs(rcalls[0].Item1 - RumbleService.FireLow * (1f - dt / RumbleService.FireSecs)) < 0.01f && rcalls[0].Item2 == 0f;
                    r.OnTrauma(RedRobot.LaserTrauma); r.Update(dt, true);
                    var last = rcalls[rcalls.Count - 1]; bool laser = last.Item1 > 0.8f && last.Item2 > 0.8f;
                    r.OnTrauma(PlayerConstants.ShootTrauma); r.Update(dt, true); var after = rcalls[rcalls.Count - 1]; bool strongWins = after.Item1 > 0.7f; // soft pulse does not replace the laser
                    float prevHi = after.Item2; bool rmono = true; for (int i = 0; i < 40; i++) { r.Update(dt, true); var c = rcalls.Count > 0 ? rcalls[rcalls.Count - 1] : (0f, 0f); rmono &= c.Item2 <= prevHi + 1e-4f; prevHi = c.Item2; }
                    for (int i = 0; i < 10; i++) r.Update(dt, true);
                    var end = rcalls[rcalls.Count - 1]; bool decay = rmono && end == (0f, 0f) && !r.Sending;
                    r.OnTrauma(0.75f); r.Update(dt, true); bool med = rcalls[rcalls.Count - 1].Item1 > 0.4f && rcalls[rcalls.Count - 1].Item2 > 0.3f;
                    r.Update(dt, false); bool pauseZero = rcalls[rcalls.Count - 1] == (0f, 0f) && !r.Sending;
                    r.OnTrauma(RedRobot.LaserTrauma); r.Update(dt, true); r.OnTrauma(RedRobot.LaserTrauma); pad = false; r.Update(dt, true); bool discZero = rcalls[rcalls.Count - 1] == (0f, 0f) && !r.Sending;
                    int n0 = rcalls.Count; r.OnTrauma(RedRobot.LaserTrauma); r.Update(dt, true); r.Update(dt, false); bool noPadSilent = rcalls.Count == n0;
                    pad = true; on = false; r.OnTrauma(RedRobot.LaserTrauma); r.Update(dt, true); bool offSilent = rcalls.Count == n0;
                    Check("rumble service pulses and safety", quiet && fire && laser && strongWins && decay && med && pauseZero && discZero && noPadSilent && offSilent,
                        $"idle silent={quiet} fire={fire} laser={laser} strongest wins={strongWins} linear decay to 0={decay} medium={med} zero on pause={pauseZero} zero on disconnect={discZero} no calls without pad={noPadSilent} toggle off silent={offSilent}");
                }
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
            Console.WriteLine($"[test-ui] {n - fail}/{n} passed");
            return fail;
        }
    }
}
