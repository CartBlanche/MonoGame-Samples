using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;

namespace XNAPacMan {
    /// <summary>
    /// All four ghosts use the same sounds, and only one can be played at a time. So, instead of having to
    /// synchronize each other, they use this class.
    /// </summary>
    static class GhostSoundsManager {

        static public void Init(Game game) {
            soundBank_ = (SoundBank)game.Services.GetService(typeof(SoundBank));
            InitCues();
        }

        static public void playLoopAttack() {
            playLoop(ref loopAttack_);
        }

        static public void playLoopAttackFast() {
            playLoop(ref loopAttackFast_);
        }

        static public void playLoopAttackVeryFast() {
            playLoop(ref loopAttackVeryFast_);
        }

        static public void playLoopBlue() {
            playLoop(ref loopBlue_);
        }

        static public void playLoopDead() {
            playLoop(ref loopDead_);
        }


        static void playLoop(ref Cue cue) {
            if (!cue.IsPlaying) {
                StopLoops();
                InitCues();
                cue.Play();
            }
        }

        static void InitCues() {
            loopAttack_ = soundBank_.GetCue("GhostNormalLoop1");
            loopAttackFast_ = soundBank_.GetCue("GhostFastLoop");
            loopAttackVeryFast_ = soundBank_.GetCue("GhostVFastLoop");
            loopDead_ = soundBank_.GetCue("GhostRunningHome");
            loopBlue_ = soundBank_.GetCue("GhostChased");
        }

        static public void StopLoops() {
            loopAttack_.Stop(AudioStopOptions.AsAuthored);
            loopAttackFast_.Stop(AudioStopOptions.AsAuthored);
            loopAttackVeryFast_.Stop(AudioStopOptions.AsAuthored);
            loopDead_.Stop(AudioStopOptions.AsAuthored);
            loopBlue_.Stop(AudioStopOptions.AsAuthored);
        }

        static public void PauseLoops() {
            loopAttack_.Pause();
            loopAttackFast_.Pause();
            loopAttackVeryFast_.Pause();
            loopDead_.Pause();
            loopBlue_.Pause();
        }

        static public void ResumeLoops() {
            loopAttack_.Resume();
            loopAttackFast_.Resume();
            loopAttackVeryFast_.Resume();
            loopDead_.Resume();
            loopBlue_.Resume();
        }

        static SoundBank soundBank_;
        static Cue loopAttack_;
        static Cue loopAttackFast_;
        static Cue loopAttackVeryFast_;
        static Cue loopBlue_;
        static Cue loopDead_;
    }
}
