using System;
using Microsoft.Xna.Framework;
using CardsFramework;
using CardsFramework.Core;

namespace Blackjack
{
    internal sealed class BlackjackSplitActionSetup
    {
        public Vector2 FirstHandOffset { get; init; }
        public Vector2 SecondHandOffsetPositive { get; init; }
        public AnimatedGameComponentAnimation FirstHandAnimation { get; init; }
        public AnimatedGameComponentAnimation SecondHandAnimation { get; init; }
        public BlackjackAnimatedPlayerHandComponent SecondHandComponent { get; init; }
    }

    internal static class BlackjackSplitActionEngine
    {
        public static BlackjackSplitActionSetup PrepareSplit(
            BlackjackPlayer player,
            int playerIndex,
            BlackjackAnimatedPlayerHandComponent sourceHandComponent,
            Vector2 secondHandOffset,
            float animationSpeedMultiplier,
            BlackjackCardGame game,
            BetGameComponent betGameComponent,
            ScreenManager screenManager)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (sourceHandComponent == null)
                throw new ArgumentNullException(nameof(sourceHandComponent));
            if (game == null)
                throw new ArgumentNullException(nameof(game));
            if (betGameComponent == null)
                throw new ArgumentNullException(nameof(betGameComponent));
            if (screenManager?.SpriteBatch == null)
                throw new InvalidOperationException(
                    "ScreenManager.SpriteBatch is null. Ensure ScreenManager.LoadContent() has been called before attempting to split.");

            player.InitializeSecondHand();

            Vector2 firstHandOffset = new Vector2(-secondHandOffset.X, secondHandOffset.Y);
            Vector2 secondHandOffsetPositive = new Vector2(secondHandOffset.X, secondHandOffset.Y);

            sourceHandComponent.ApplyAdditionalOffset(firstHandOffset);

            Vector2 firstCardSourcePosition = sourceHandComponent.GetCardGameComponent(0).CurrentPosition;
            Vector2 firstCardTargetPosition = firstCardSourcePosition + firstHandOffset;
            AnimatedGameComponentAnimation firstHandAnimation = new TransitionGameComponentAnimation(
                firstCardSourcePosition, firstCardTargetPosition)
            {
                StartDelay = TimeSpan.Zero,
                Duration = TimeSpan.FromSeconds(0.5f * animationSpeedMultiplier)
            };

            Vector2 secondCardSourcePosition = sourceHandComponent.GetCardGameComponent(1).CurrentPosition;
            Vector2 secondCardTargetPosition = sourceHandComponent.GetCardGameComponent(0).CurrentPosition +
                                               secondHandOffsetPositive;
            AnimatedGameComponentAnimation secondHandAnimation = new TransitionGameComponentAnimation(
                secondCardSourcePosition, secondCardTargetPosition)
            {
                StartDelay = TimeSpan.Zero,
                Duration = TimeSpan.FromSeconds(0.5f * animationSpeedMultiplier)
            };

            sourceHandComponent.GetCardGameComponent(0).AddAnimation(firstHandAnimation);

            player.SplitHand();
            betGameComponent.AddChips(playerIndex, player.BetAmount, false, true);

            var secondHandComponent = new BlackjackAnimatedPlayerHandComponent(
                playerIndex,
                secondHandOffsetPositive,
                player.SecondHand,
                game,
                screenManager.SpriteBatch,
                screenManager.GlobalTransformation);

            game.Game.Components.Add(secondHandComponent);

            AnimatedCardsGameComponent animatedSecondCardComponent = secondHandComponent.GetCardGameComponent(0);
            animatedSecondCardComponent.IsFaceDown = false;
            animatedSecondCardComponent.AddAnimation(secondHandAnimation);

            return new BlackjackSplitActionSetup
            {
                FirstHandOffset = firstHandOffset,
                SecondHandOffsetPositive = secondHandOffsetPositive,
                FirstHandAnimation = firstHandAnimation,
                SecondHandAnimation = secondHandAnimation,
                SecondHandComponent = secondHandComponent
            };
        }
    }
}