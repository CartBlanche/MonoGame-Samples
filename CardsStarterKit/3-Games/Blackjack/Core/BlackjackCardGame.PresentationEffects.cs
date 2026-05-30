//-----------------------------------------------------------------------------
// BlackjackCardGame.PresentationEffects.cs
//
// Partial class containing card presentation, cue, and related audio helpers.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;

namespace Blackjack
{
    partial class BlackjackCardGame
    {
        /// <summary>
        /// Display an animation when a card is dealt.
        /// </summary>
        /// <param name="card">The card being dealt.</param>
        /// <param name="animatedHand">The animated hand into which the card
        /// is dealt.</param>
        /// <param name="flipCard">Should the card be flipped after dealing it.</param>
        /// <param name="duration">The animations desired duration.</param>
        /// <param name="startDelay">The delay before the animation should start.</param>
        public void AddDealAnimation(TraditionalCard card, AnimatedHandGameComponent
            animatedHand, bool flipCard, TimeSpan duration, TimeSpan startDelay)
        {
            // Safety check: if animatedHand is null, we can't create animations
            if (animatedHand == null)
            {
                Debug.WriteLine($"[AddDealAnimation] ERROR: animatedHand parameter is null! Cannot create animation.");
                return;
            }

            // Get the card location and card component
            int cardLocationInHand = animatedHand.GetCardLocationInHand(card);
            AnimatedCardsGameComponent cardComponent = animatedHand.GetCardGameComponent(cardLocationInHand);

            // Cards are dealt from the deck position in the top-right corner
            Rectangle tableBounds = GameTable.TableBounds;
            int cardWidth = UIConstants.GetCardWidth(screenManager.SafeArea.Width);
            int cardHeight = UIConstants.GetCardHeight(screenManager.SafeArea.Height);
            float deckX = tableBounds.Right - cardWidth - 40; // Match deck display position
            float deckY = tableBounds.Top + 40;
            Vector2 deckPosition = new Vector2(deckX, deckY);

            var cardAnimation = new TransitionGameComponentAnimation(deckPosition,
                animatedHand.CurrentPosition +
                animatedHand.GetCardRelativePosition(cardLocationInHand))
            {
                StartDelay = startDelay,
                PerformBeforeStart = ShowCardAndPlayDealSound,
                PerformBeforeStartArgs = new object[] { cardComponent, animatedHand }
            };
            cardAnimation.Duration = duration;

            // Add the transition animation
            cardComponent.AddAnimation(cardAnimation);

            if (flipCard)
            {
                // Add the flip animation
                cardComponent.AddAnimation(new FlipGameComponentAnimation
                {
                    IsFromFaceDownToFaceUp = true,
                    Duration = duration,
                    StartDelay = startDelay + duration,
                    PerformWhenDone = PlayFlipSound
                });
            }
        }

        /// <summary>
        /// Helper method to show card component and play deal sound with contextual pitch/volume/panning.
        /// Called when card animation starts.
        /// </summary>
        /// <param name="obj">Array containing [cardComponent, animatedHand]</param>
        void ShowCardAndPlayDealSound(object obj)
        {
            var args = (object[])obj;
            var cardComponent = (AnimatedCardsGameComponent)args[0];
            var animatedHand = (AnimatedHandGameComponent)args[1];

            cardComponent.Visible = true;

            // Determine who is receiving the card
            float pitch = 0f;
            float volumeMultiplier = 1.0f;

            if (animatedHand == dealerHandComponent)
            {
                // Dealer dealing to itself: default pitch and volume
                pitch = 0f;
                volumeMultiplier = 1.0f;
            }
            else
            {
                // Find which player is receiving the card
                int playerIndex = -1;
                for (int i = 0; i < animatedHands.Length; i++)
                {
                    if (animatedHands[i] == animatedHand || animatedSecondHands[i] == animatedHand)
                    {
                        playerIndex = i;
                        break;
                    }
                }

                if (playerIndex == LocalPlayerIndex)
                {
                    // Human player: higher pitch and louder
                    pitch = (float)(random.NextDouble() * 0.15 + 0.15); // Range: 0.15 to 0.30
                    volumeMultiplier = 1.15f;
                }
                else if (playerIndex >= 0)
                {
                    // NPC player: slightly higher pitch and medium volume
                    pitch = (float)(random.NextDouble() * 0.10 + 0.05); // Range: 0.05 to 0.15
                    volumeMultiplier = 1.08f;
                }
            }

            // Calculate stereo panning based on card's X position on screen
            // Get the target position where the card is heading
            Vector2 targetPosition = animatedHand.CurrentPosition;
            float screenCenterX = screenManager.SafeArea.Width / 2f;

            // Calculate pan: -1.0 (left) to 1.0 (right), with 0.0 at screen center
            // We'll use a moderate panning range to keep it subtle
            float pan = (targetPosition.X - screenCenterX) / screenCenterX;
            pan = MathHelper.Clamp(pan * 0.7f, -0.7f, 0.7f); // Scale to 70% max for subtlety

            // Calculate final volume based on settings
            float volume = GameSettings.Instance.SoundVolume * volumeMultiplier;
            volume = MathHelper.Clamp(volume, 0f, 1f);

            AudioManager.PlaySound("Deal", pitch: pitch, volume: volume, pan: pan);
        }

        /// <summary>
        /// Helper method to play flip sound
        /// </summary>
        /// <param name="obj"></param>
        void PlayFlipSound(object obj)
        {
            AudioManager.PlaySound("Flip");
        }

        /// <summary>
        /// Helper method to play card removal sound when cards leave the table.
        /// Called when cards are animated off-screen at the end of a round.
        /// </summary>
        /// <param name="obj"></param>
        void PlayCardRemovalSound(object obj)
        {
            AudioManager.PlaySound("CardRemoval", pitch: (float)(random.NextDouble() * 0.1 - 0.05)); // Slight pitch variation
        }

        /// <summary>
        /// Adds an animation which displays an asset over a player's hand. The asset
        /// will appear above the hand and appear to "fall" on top of it.
        /// </summary>
        /// <param name="player">The player over the hand of which to place the
        /// animation.</param>
        /// <param name="assetName">Name of the asset to display above the hand.</param>
        /// <param name="animationHand">Which hand to put cue over.</param>
        /// <param name="waitForHand">Start the cue animation when the animation
        /// of this hand over null of the animation of the currentHand</param>
        void CueOverPlayerHand(BlackjackPlayer player, string assetName,
            HandTypes animationHand, AnimatedHandGameComponent waitForHand)
        {
            int humanIndex = LocalPlayerIndex >= 0 ? LocalPlayerIndex : 0;
            bool playWinSoundOnCueStart = players.IndexOf(player) == humanIndex &&
                (assetName == "win" || assetName == "blackjack");

            // Get the position of the relevant hand
            int playerIndex = players.IndexOf(player);
            AnimatedHandGameComponent currentAnimatedHand;
            Vector2 currentPosition;
            if (playerIndex >= 0)
            {
                switch (animationHand)
                {
                    case HandTypes.First:
                        currentAnimatedHand = animatedHands[playerIndex];
                        currentPosition = currentAnimatedHand.CurrentPosition;
                        break;
                    case HandTypes.Second:
                        currentAnimatedHand = animatedSecondHands[playerIndex];
                        // CurrentPosition already includes the hand's offset, so don't add secondHandOffset again
                        currentPosition = currentAnimatedHand.CurrentPosition;
                        break;
                    default:
                        throw new Exception(
                            "Player has an unsupported hand type.");
                }
            }
            else
            {
                currentAnimatedHand = dealerHandComponent;
                currentPosition = currentAnimatedHand.CurrentPosition;
            }

            // Add the animation component
            AnimatedGameComponent animationComponent =
                new AnimatedGameComponent(this, cardsAssets[assetName], screenManager.SpriteBatch,
                    screenManager.GlobalTransformation)
                {
                    CurrentPosition = currentPosition,
                    Visible = false
                };
            Game.Components.Add(animationComponent);

            // Calculate when to start the animation.
            // In suspense-sensitive flows (e.g., round result reveal), wait for both
            // the dealer reveal/deal sequence and the current hand animation completion.
            TimeSpan estimatedTimeToCompleteAnimations =
                currentAnimatedHand.EstimatedTimeForAnimationsCompletion();

            if (waitForHand != null)
            {
                TimeSpan waitForHandCompletion = waitForHand.EstimatedTimeForAnimationsCompletion();
                if (waitForHandCompletion > estimatedTimeToCompleteAnimations)
                {
                    estimatedTimeToCompleteAnimations = waitForHandCompletion;
                }
            }

            // Add a brief suspense beat before revealing outcome cues.
            TimeSpan suspenseRevealOffset = TimeSpan.FromMilliseconds(120 * AnimationSpeedMultiplier);
            estimatedTimeToCompleteAnimations += suspenseRevealOffset;

            // Add a scale effect animation
            animationComponent.AddAnimation(new ScaleGameComponentAnimation(2.0f, 1.0f)
            {
                StartDelay = estimatedTimeToCompleteAnimations,
                Duration = TimeSpan.FromSeconds(1f * AnimationSpeedMultiplier),
                PerformBeforeStart = ShowComponentAndMaybePlayWinSound,
                PerformBeforeStartArgs = Tuple.Create(animationComponent, playWinSoundOnCueStart)
            });
        }
    }
}
