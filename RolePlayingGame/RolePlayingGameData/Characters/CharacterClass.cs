//-----------------------------------------------------------------------------
// CharacterClass.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Content;

namespace RolePlaying.Data
{
    /// <summary>
    /// The definition of a type of character.
    /// </summary>
    public class CharacterClass : ContentObject
    {


        /// <summary>
        /// The name of the character class.
        /// </summary>
        private string name;

        /// <summary>
        /// The name of the character class.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }






        /// <summary>
        /// The initial statistics of characters that use this class.
        /// </summary>
        private StatisticsValue initialStatistics = new StatisticsValue();

        /// <summary>
        /// The initial statistics of characters that use this class.
        /// </summary>
        public StatisticsValue InitialStatistics
        {
            get { return initialStatistics; }
            set { initialStatistics = value; }
        }






        /// <summary>
        /// Statistics changes for leveling up characters that use this class.
        /// </summary>
        private CharacterLevelingStatistics levelingStatistics;

        /// <summary>
        /// Statistics changes for leveling up characters that use this class.
        /// </summary>
        public CharacterLevelingStatistics LevelingStatistics
        {
            get { return levelingStatistics; }
            set { levelingStatistics = value; }
        }


        /// <summary>
        /// Entries of the requirements and rewards for each level of this class.
        /// </summary>
        private List<CharacterLevelDescription> levelEntries =
            new List<CharacterLevelDescription>();

        /// <summary>
        /// Entries of the requirements and rewards for each level of this class.
        /// </summary>
        public List<CharacterLevelDescription> LevelEntries
        {
            get { return levelEntries; }
            set { levelEntries = value; }
        }


        /// <summary>
        /// Calculate the statistics of a character of this class and the given level.
        /// </summary>
        public StatisticsValue GetStatisticsForLevel(int characterLevel)
        {
            // check the parameter
            if (characterLevel <= 0)
            {
                throw new ArgumentOutOfRangeException("characterLevel");
            }

            // start with the initial statistics
            StatisticsValue output = initialStatistics;

            // add each level of leveling statistics
            for (int i = 1; i < characterLevel; i++)
            {
                if ((levelingStatistics.LevelsPerHealthPointsIncrease > 0) &&
                    ((i % levelingStatistics.LevelsPerHealthPointsIncrease) == 0))
                {
                    output.HealthPoints += levelingStatistics.HealthPointsIncrease;
                }
                if ((levelingStatistics.LevelsPerMagicPointsIncrease > 0) &&
                    ((i % levelingStatistics.LevelsPerMagicPointsIncrease) == 0))
                {
                    output.MagicPoints += levelingStatistics.MagicPointsIncrease;
                }
                if ((levelingStatistics.LevelsPerPhysicalOffenseIncrease > 0) &&
                    ((i % levelingStatistics.LevelsPerPhysicalOffenseIncrease) == 0))
                {
                    output.PhysicalOffense += levelingStatistics.PhysicalOffenseIncrease;
                }
                if ((levelingStatistics.LevelsPerPhysicalDefenseIncrease > 0) &&
                    ((i % levelingStatistics.LevelsPerPhysicalDefenseIncrease) == 0))
                {
                    output.PhysicalDefense += levelingStatistics.PhysicalDefenseIncrease;
                }
                if ((levelingStatistics.LevelsPerMagicalOffenseIncrease > 0) &&
                    ((i % levelingStatistics.LevelsPerMagicalOffenseIncrease) == 0))
                {
                    output.MagicalOffense += levelingStatistics.MagicalOffenseIncrease;
                }
                if ((levelingStatistics.LevelsPerMagicalDefenseIncrease > 0) &&
                    ((i % levelingStatistics.LevelsPerMagicalDefenseIncrease) == 0))
                {
                    output.MagicalDefense += levelingStatistics.MagicalDefenseIncrease;
                }
            }

            return output;
        }


        /// <summary>
        /// Build a list of all spells available to a character 
        /// of this class and the given level.
        /// </summary>
        public List<Spell> GetAllSpellsForLevel(int characterLevel)
        {
            // check the parameter
            if (characterLevel <= 0)
            {
                throw new ArgumentOutOfRangeException("characterLevel");
            }

            // go through each level and add the spells to the output list
            List<Spell> spells = new List<Spell>();

            for (int i = 0; i < characterLevel; i++)
            {
                if (i >= levelEntries.Count)
                {
                    break;
                }

                // add new spells, and level up existing ones
                foreach (Spell spell in levelEntries[i].Spells)
                {
                    Spell existingSpell = spells.Find(
                        delegate (Spell testSpell)
                        {
                            return spell.AssetName == testSpell.AssetName;
                        });
                    if (existingSpell == null)
                    {
                        spells.Add(spell.Clone() as Spell);
                    }
                    else
                    {
                        existingSpell.Level++;
                    }
                }
            }

            return spells;
        }

        internal static CharacterClass Load(string characterAssetName, ContentManager contentManager)
        {
            var characterAsset = XmlHelper.GetAssetElementFromXML(characterAssetName);

            var characterClass = new CharacterClass
            {
                Name = (string)characterAsset.Element("Name"),
                InitialStatistics = new StatisticsValue
                {
                    HealthPoints = (int?)characterAsset.Element("InitialStatistics").Element("HealthPoints") ?? 0,
                    MagicPoints = (int?)characterAsset.Element("InitialStatistics").Element("MagicPoints") ?? 0,
                    PhysicalOffense = (int?)characterAsset.Element("InitialStatistics").Element("PhysicalOffense") ?? 0,
                    PhysicalDefense = (int?)characterAsset.Element("InitialStatistics").Element("PhysicalDefense") ?? 0,
                    MagicalOffense = (int?)characterAsset.Element("InitialStatistics").Element("MagicalOffense") ?? 0,
                    MagicalDefense = (int?)characterAsset.Element("InitialStatistics").Element("MagicalDefense") ?? 0,
                },
                LevelingStatistics = new CharacterLevelingStatistics
                {
                    HealthPointsIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("HealthPointsIncrease") ?? 0,
                    LevelsPerHealthPointsIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("LevelsPerHealthPointsIncrease") ?? 0,
                    MagicPointsIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("MagicPointsIncrease") ?? 0,
                    LevelsPerMagicPointsIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("LevelsPerMagicPointsIncrease") ?? 0,
                    PhysicalOffenseIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("PhysicalOffenseIncrease") ?? 0,
                    LevelsPerPhysicalOffenseIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("LevelsPerPhysicalOffenseIncrease") ?? 0,
                    PhysicalDefenseIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("PhysicalDefenseIncrease") ?? 0,
                    LevelsPerPhysicalDefenseIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("LevelsPerPhysicalDefenseIncrease") ?? 0,
                    MagicalOffenseIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("MagicalOffenseIncrease") ?? 0,
                    LevelsPerMagicalOffenseIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("LevelsPerMagicalOffenseIncrease") ?? 0,
                    MagicalDefenseIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("MagicalDefenseIncrease") ?? 0,
                    LevelsPerMagicalDefenseIncrease = (int?)characterAsset.Element("LevelingStatistics").Element("LevelsPerMagicalDefenseIncrease") ?? 0,
                },
                LevelEntries = characterAsset.Element("LevelEntries")
                    .Elements("Item")
                    .Select(item => CharacterLevelDescription.Load(item, contentManager))
                    .ToList(),
                BaseExperienceValue = (int?)characterAsset.Element("BaseExperienceValue") ?? 0,
                BaseGoldValue = (int?)characterAsset.Element("BaseGoldValue") ?? 0
            };

            return characterClass;
        }

        /// <summary>
        /// The base experience value of Npcs of this character class.
        /// </summary>
        /// <remarks>Used for calculating combat rewards.</remarks>
        private int baseExperienceValue;

        /// <summary>
        /// The base experience value of Npcs of this character class.
        /// </summary>
        /// <remarks>Used for calculating combat rewards.</remarks>
        public int BaseExperienceValue
        {
            get { return baseExperienceValue; }
            set { baseExperienceValue = value; }
        }


        /// <summary>
        /// The base gold value of Npcs of this character class.
        /// </summary>
        /// <remarks>Used for calculating combat rewards.</remarks>
        private int baseGoldValue;

        /// <summary>
        /// The base gold value of Npcs of this character class.
        /// </summary>
        /// <remarks>Used for calculating combat rewards.</remarks>
        public int BaseGoldValue
        {
            get { return baseGoldValue; }
            set { baseGoldValue = value; }
        }

        /// <summary>
        /// Reads a CharacterClass object from the content pipeline.
        /// </summary>
        public class CharacterClassReader : ContentTypeReader<CharacterClass>
        {
            /// <summary>
            /// Reads a CharacterClass object from the content pipeline.
            /// </summary>
            protected override CharacterClass Read(ContentReader input,
                CharacterClass existingInstance)
            {
                CharacterClass characterClass = existingInstance;
                if (characterClass == null)
                {
                    characterClass = new CharacterClass();
                }

                characterClass.AssetName = input.AssetName;

                characterClass.Name = input.ReadString();
                characterClass.InitialStatistics =
                    input.ReadObject<StatisticsValue>();
                characterClass.LevelingStatistics =
                    input.ReadObject<CharacterLevelingStatistics>();
                characterClass.LevelEntries.AddRange(
                    input.ReadObject<List<CharacterLevelDescription>>());
                characterClass.BaseExperienceValue = input.ReadInt32();
                characterClass.BaseGoldValue = input.ReadInt32();

                return characterClass;
            }
        }
    }
}