//-----------------------------------------------------------------------------
// CombatantPlayer.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using RolePlaying.Data;

namespace RolePlaying.Core
{
    /// <summary>
    /// Encapsulates all of the combat-runtime data for a particular player combatant.
    /// </summary>
    class CombatantPlayer : Combatant
    {
        /// <summary>
        /// The Player object encapsulated by this object.
        /// </summary>
        private Player player;

        /// <summary>
        /// The Player object encapsulated by this object.
        /// </summary>
        public Player Player
        {
            get { return player; }
        }
        
        /// <summary>
        /// The character encapsulated by this combatant.
        /// </summary>
        public override FightingCharacter Character
        {
            get { return player as FightingCharacter; }
        }




        /// <summary>
        /// The current state of this combatant.
        /// </summary>
        public override Character.CharacterState State
        {
            get { return player.State; }
            set
            {
                if (value == player.State)
                {
                    return;
                }
                player.State = value;
                switch (player.State)
                {
                    case RolePlaying.Data.Character.CharacterState.Idle:
                        CombatSprite.PlayAnimation("Idle");
                        break;

                    case RolePlaying.Data.Character.CharacterState.Hit:
                        CombatSprite.PlayAnimation("Hit");
                        break;

                    case RolePlaying.Data.Character.CharacterState.Dying:
                        player.StatisticsModifiers.HealthPoints = 
                            -1 * player.CharacterStatistics.HealthPoints;
                        CombatSprite.PlayAnimation("Die");
                        break;
                }
            }
        }






        /// <summary>
        /// Accessor for the combat sprite for this combatant.
        /// </summary>
        public override AnimatingSprite CombatSprite
        {
            get { return player.CombatSprite; }
        }






        /// <summary>
        /// The current statistics of this combatant.
        /// </summary>
        public override StatisticsValue Statistics
        {
            get { return player.CurrentStatistics + CombatEffects.TotalStatistics; }
        }


        /// <summary>
        /// Heals the combatant by the given amount.
        /// </summary>
        public override void Heal(StatisticsValue healingStatistics, int duration)
        {
            if (duration > 0)
            {
                CombatEffects.AddStatistics(healingStatistics, duration);
            }
            else
            {
                player.StatisticsModifiers += healingStatistics;
                player.StatisticsModifiers.ApplyMaximum(new StatisticsValue());
            }
            base.Heal(healingStatistics, duration);
        }


        /// <summary>
        /// Damages the combatant by the given amount.
        /// </summary>
        public override void Damage(StatisticsValue damageStatistics, int duration)
        {
            if (duration > 0)
            {
                CombatEffects.AddStatistics(new StatisticsValue() - damageStatistics,
                    duration);
            }
            else
            {
                player.StatisticsModifiers -= damageStatistics;
                player.StatisticsModifiers.ApplyMaximum(new StatisticsValue());
            }
            base.Damage(damageStatistics, duration);
        }


        /// <summary>
        /// Pay the cost for the given spell.
        /// </summary>
        /// <returns>True if the cost could be paid (and therefore was paid).</returns>
        public override bool PayCostForSpell(Spell spell)
        {
            // check the parameter.
            if (spell == null)
            {
                throw new ArgumentNullException("spell");
            }

            // check the requirements
            if (Statistics.MagicPoints < spell.MagicPointCost)
            {
                return false;
            }

            // reduce the player's magic points by the spell's cost
            player.StatisticsModifiers.MagicPoints -= spell.MagicPointCost;

            return true;
        }






        /// <summary>
        /// Construct a new CombatantPlayer object containing the given player.
        /// </summary>
        public CombatantPlayer(Player player) : base()
        {
            // check the parameter
            if (player == null)
            {
                throw new ArgumentNullException("player");
            }

            // assign the parameters
            this.player = player;

            // if the player starts dead, make sure the sprite is already "dead"
            if (IsDeadOrDying)
            {
                if (Statistics.HealthPoints > 0)
                {
                    State = RolePlaying.Data.Character.CharacterState.Idle;
                }
                else
                {
                    CombatSprite.PlayAnimation("Die");
                    CombatSprite.AdvanceToEnd();
                }
            }
            else
            {
                State = RolePlaying.Data.Character.CharacterState.Idle;
                CombatSprite.PlayAnimation("Idle");
            }
        }






        /// <summary>
        /// Update the player for this frame.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }


    }
}
