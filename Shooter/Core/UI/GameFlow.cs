using System;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Enemies;

namespace Shooter.UI
{
    /// <summary>
    /// Port of Managers/GameFlowManager.cs plus ObjectiveManager.cs (the scene's GameManager prefab: EndSceneLoadDelay 3, DelayBeforeFadeToBlack 4,
    /// WinGameMessage "All objectives completed", DelayBeforeWinMessage 2, VictorySound Notification_End on the HUDVictory mixer group, -5 dB).
    /// Pure logic: the game draws <see cref="FadeAlpha"/>, sets the master volume from <see cref="MasterVolume"/>, plays the victory sound and the message when
    /// the events fire, and opens the Win / Lose screen (Unity loads WinScene / LoseScene) on <see cref="EndScreen"/>.
    /// </summary>
    public class GameFlow
    {
        public const float EndSceneLoadDelay = 3f, DelayBeforeFadeToBlack = 4f, DelayBeforeWinMessage = 2f;
        public const string WinGameMessage = "All objectives completed";
        public const string VictorySound = "Audio/SFX/UI/Notification_End";
        public const float VictoryGain = 0.5623413f;   // HUDVictory -5 dB

        private EnemyDirector _director;
        private GameEvents _events;
        private float _time;
        private float _timeLoadEndScreen;
        private bool _objectivesCompleted;
        private bool _victoryPending;
        private float _victoryTime;

        public bool GameIsEnding { get; private set; }
        public bool Win { get; private set; }
        /// <summary>CanvasGroup alpha of the EndGameFadeCanvas (black Image), 0..1.</summary>
        public float FadeAlpha { get; private set; }
        /// <summary>AudioUtility.SetMasterVolume: 1 until the fade starts, then 1 - fade ratio.</summary>
        public float MasterVolume { get; private set; } = 1f;

        /// <summary>The Lose / Win scene is due (<c>true</c> = win).</summary>
        public event Action<bool> EndScreen;
        /// <summary>DisplayMessageEvent (text, delay before display).</summary>
        public event Action<string, float> DisplayMessage;
        /// <summary>VictorySound.PlayScheduled(dsp + DelayBeforeWinMessage): fires when that time arrives.</summary>
        public event Action PlayVictory;

        /// <summary>Watch a (new) director and player: like the scene loading again, everything restarts.</summary>
        public void Bind(EnemyDirector director)
        {
            Unbind();
            _director = director;
            _events = director != null ? director.Events : null;
            Reset();
            if (_events != null)
            {
                _events.AllObjectivesCompleted += OnAllObjectivesCompleted;
            }
            if (director != null)
            {
                director.PlayerHealth.OnDie += OnPlayerDeath;
            }
        }

        private void Unbind()
        {
            if (_events != null)
            {
                _events.AllObjectivesCompleted -= OnAllObjectivesCompleted;
            }
            _events = null;
            _director = null;
        }

        public void Reset()
        {
            _time = 0f;
            GameIsEnding = false;
            Win = false;
            FadeAlpha = 0f;
            MasterVolume = 1f;
            _objectivesCompleted = false;
            _victoryPending = false;
        }

        private void OnAllObjectivesCompleted() { EndGame(true); }
        private void OnPlayerDeath() { EndGame(false); }

        private void EndGame(bool win)
        {
            GameIsEnding = true;
            Win = win;
            if (win)
            {
                _timeLoadEndScreen = _time + EndSceneLoadDelay + DelayBeforeFadeToBlack;
                _victoryPending = true;
                _victoryTime = _time + DelayBeforeWinMessage;
                if (DisplayMessage != null)
                {
                    DisplayMessage(WinGameMessage, DelayBeforeWinMessage);
                }
            }
            else
            {
                _timeLoadEndScreen = _time + EndSceneLoadDelay;
            }
        }

        /// <summary>ObjectiveManager.Update, then GameFlowManager.Update.</summary>
        public void Update(float dt)
        {
            _time += dt;
            if (_director != null && !_objectivesCompleted && _director.Objective != null && !_director.Objective.IsBlocking())
            {
                _objectivesCompleted = true;
                _events.RaiseAllCompleted();
            }
            if (_victoryPending && _time >= _victoryTime)
            {
                _victoryPending = false;
                if (PlayVictory != null)
                {
                    PlayVictory();
                }
            }
            if (GameIsEnding)
            {
                float ratio = 1f - (_timeLoadEndScreen - _time) / EndSceneLoadDelay;
                FadeAlpha = Math.Max(0f, Math.Min(1f, ratio));
                MasterVolume = 1f - FadeAlpha;
                if (_time >= _timeLoadEndScreen)
                {
                    GameIsEnding = false;
                    FadeAlpha = 1f;
                    if (EndScreen != null)
                    {
                        EndScreen(Win);
                    }
                }
            }
        }
    }
}
