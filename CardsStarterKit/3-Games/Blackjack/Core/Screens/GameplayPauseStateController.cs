using System.Collections.Generic;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;

namespace Blackjack
{
    /// <summary>
    /// Tracks gameplay component visibility/enabled states while a pause screen is active.
    /// </summary>
    internal sealed class GameplayPauseStateController
    {
        private readonly ScreenManager screenManager;
        private readonly List<DrawableGameComponent> pauseEnabledComponents = new List<DrawableGameComponent>();
        private readonly List<DrawableGameComponent> pauseVisibleComponents = new List<DrawableGameComponent>();

        public GameplayPauseStateController(ScreenManager screenManager)
        {
            this.screenManager = screenManager;
        }

        public void PauseGameplayComponents()
        {
            pauseEnabledComponents.Clear();
            pauseVisibleComponents.Clear();

            foreach (IGameComponent component in screenManager.Game.Components)
            {
                if (component is BetGameComponent ||
                    component is AnimatedGameComponent ||
                    component is GameTable)
                {
                    DrawableGameComponent pauseComponent = (DrawableGameComponent)component;
                    if (pauseComponent.Enabled)
                    {
                        pauseEnabledComponents.Add(pauseComponent);
                        pauseComponent.Enabled = false;
                    }
                    if (pauseComponent.Visible)
                    {
                        pauseVisibleComponents.Add(pauseComponent);
                        pauseComponent.Visible = false;
                    }
                }
            }
        }

        public void ResumeGameplayComponents()
        {
            foreach (DrawableGameComponent component in pauseEnabledComponents)
            {
                component.Enabled = true;
            }

            foreach (DrawableGameComponent component in pauseVisibleComponents)
            {
                component.Visible = true;
            }
        }
    }
}