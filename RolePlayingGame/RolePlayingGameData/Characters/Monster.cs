//-----------------------------------------------------------------------------
// Monster.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RolePlaying.Data
{
    /// <summary>
    /// An enemy NPC that fights you in combat.
    /// </summary>
    /// <remarks>
    /// Any combat many have many of the same monster, and they don't exist beyond 
    /// combat.  Therefore, current statistics are tracked in the runtime combat engine.
    /// </remarks>
    public class Monster : FightingCharacter
    {


        /// <summary>
        /// The chance that this monster will defend instead of attack.
        /// </summary>
        private int defendPercentage;

        /// <summary>
        /// The chance that this monster will defend instead of attack.
        /// </summary>
        public int DefendPercentage
        {
            get { return defendPercentage; }
            set { defendPercentage = (value > 100 ? 100 : (value < 0 ? 0 : value)); }
        }






        /// <summary>
        /// The possible gear drops from this monster.
        /// </summary>
        private List<GearDrop> gearDrops = new List<GearDrop>();

        /// <summary>
        /// The possible gear drops from this monster.
        /// </summary>
        public List<GearDrop> GearDrops
        {
            get { return gearDrops; }
            set { gearDrops = value; }
        }


        public int CalculateGoldReward(Random random)
        {
            return CharacterClass.BaseGoldValue * CharacterLevel;
        }


        public int CalculateExperienceReward(Random random)
        {
            return CharacterClass.BaseExperienceValue * CharacterLevel;
        }


        public List<string> CalculateGearDrop(Random random)
        {
            List<string> gearRewards = new List<string>();

            Random useRandom = random;
            if (useRandom == null)
            {
                useRandom = new Random();
            }

            foreach (GearDrop gearDrop in GearDrops)
            {
                if (random.Next(100) < gearDrop.DropPercentage)
                {
                    gearRewards.Add(gearDrop.GearName);
                }
            }

            return gearRewards;
        }

        internal static Monster Load(string monsterContentName, ContentManager contentManager)
        {
            var asset = XmlHelper.GetAssetElementFromXML(monsterContentName);
            var monster = new Monster
            {
                AssetName = monsterContentName,
                Name = asset.Element("Name").Value,
                Direction = Enum.TryParse<Direction>((string)asset.Element("Direction"), out var dir) ? dir : default,
                CharacterClassContentName = (string)asset.Element("CharacterClassContentName"),
                CharacterLevel = (int)asset.Element("CharacterLevel"),
                InitialEquipmentContentNames = asset.Element("InitialEquipmentContentNames")
                    .Elements("Item").Select(x => (string)x).ToList(),
                CombatSprite = AnimatingSprite.Load(asset.Element("CombatSprite"), contentManager),
                MapSprite = AnimatingSprite.Load(asset.Element("MapSprite"), contentManager),
                GearDrops = asset.Element("GearDrops")?.Elements("Item")
                    .Select(item => new GearDrop
                    {
                        GearName = (string)item.Element("GearName"),
                        DropPercentage = (int)item.Element("DropPercentage")
                    }).ToList()
            };

            monster.CharacterClass = CharacterClass.Load(Path.Combine("CharacterClasses", monster.CharacterClassContentName), contentManager);
            monster.ShadowTexture = contentManager.Load<Texture2D>(@"Textures\Characters\CharacterShadow");

            monster.AddStandardCharacterCombatAnimations();
            monster.AddStandardCharacterIdleAnimations();
            monster.AddStandardCharacterWalkingAnimations();

            monster.ResetAnimation(false);

            return monster;
        }

        /// <summary>
        /// Reads a Monster object from the content pipeline.
        /// </summary>
        public class MonsterReader : ContentTypeReader<Monster>
        {
            protected override Monster Read(ContentReader input,
                Monster existingInstance)
            {
                Monster monster = existingInstance;
                if (monster == null)
                {
                    monster = new Monster();
                }

                input.ReadRawObject<FightingCharacter>(monster as FightingCharacter);

                monster.DefendPercentage = input.ReadInt32();
                monster.GearDrops.AddRange(input.ReadObject<List<GearDrop>>());

                return monster;
            }
        }
    }
}