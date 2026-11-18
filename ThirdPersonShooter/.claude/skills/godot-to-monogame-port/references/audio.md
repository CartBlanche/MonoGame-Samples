# Audio (verified headless and by log; NOT verified by ear)

## Content
- Music: `OggImporter` + `SongProcessor`; effects: `WavImporter` + `SoundEffectProcessor`. Both build on macOS arm64 Release (+5.7 MB). Sources referenced in place from the Godot project.
- Load the menu audio at boot and the level audio in the play load steps (`AudioManager.InitMenu` / `InitLevel`); `--sim` and `--shot` stay silent and device-free.

## Mixing
- Volume = `10^(-10/20)` x Master x (Music|SFX). The **-10 dB master bus is Godot's**, so defaults are quiet. Per-source and per-bus dB are in the scene; record them.
- Music fades 0.5 s between menu and level; settings sliders preview live, Cancel restores, Apply keeps.
- Reverb/EQ bus effects do not exist in plain MonoGame: list what is dropped (reverb zones not implemented).

## 3D sound
- Manual model (not OpenAL `Apply3D`): Godot inverse-distance attenuation (`unit_size`, `volume_db`) and pan from the listener matrix, then `SoundEffectInstance`.
- **OpenAL on DesktopGL does not pan stereo buffers**: 3D sounds are attenuated but centred. Fix (not done): mono-downmix content processor. All TPS effects except jump are stereo.
- Moving sources: robot sounds keep their play-time position (not followed).
- The `max_db` clamp (3) comes from memory of Godot source (unverified).

## What Godot actually plays (audit first)
- No UI focus/confirm sound exists (the menu Step node has no stream): do not invent one.
- The player's step sound is never played in Godot; the port adds footsteps from **foot-bone heights** of running_nogun and strafe_* (foot-contact phases, 0.32 s apart at the 1.3x walk scale).
- Robot walk uses the AnimationPlayer track keys (0.8 s, 2.033 s). Pitch is 1 everywhere.
- Doors out of scope.

## Evidence without listening
- `--test-audio` (4 checks): bus gain math, attenuation + pan vs the Godot formula, event table with Godot values and all xnb present, footstep phases.
- `--audio-log` (or `TPS_AUDIO_LOG`) with `--sim` prints each played sound, gain and pan: the expected sequence is land, shoot, robot walk/hit, bullet explode, robot explosion.
- Not heard: loudness balance, loop seams, music fade-out path, device playback of jump/shoot/robot/footstep.

## Reverb zones and 3D panning (verified headless on the TPS demo, not heard)
- OpenAL does not pan STEREO buffers: positional sounds must be MONO. Cheapest robust route: keep the 3D wavs as raw `/copy:` files and downmix to PCM at load (builds a mono `SoundEffect`, so `Pan` reaches OpenAL); 2D sounds and music stay stereo xnb.
- Godot routes only 3D players (AudioStreamPlayer3D) to an Area3D's `audio_bus_override` bus by emitter position; 2D players and music stay dry on Master. Zones = polygon prism (XZ polygon + depth) or capsule from `volumes` in the extractor JSON; test points/bus in `--test-audio`.
- MonoGame DesktopGL has no portable reverb: render a wet variant offline at first use (Freeverb-style: predelay with feedback, 8 damped combs, 4 allpass, hipass on the wet path, wet/dry from the bus's AudioEffectReverb) and cache it per (sound, bus). About 90 lines, 2-5 ms per render, no per-frame DSP; music is unaffected. Constants not in the .tres are Godot defaults recalled from memory (unverified).

