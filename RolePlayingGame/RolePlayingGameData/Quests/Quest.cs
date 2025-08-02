//-----------------------------------------------------------------------------
// Quest.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace RolePlaying.Data
{
    /// <summary>
    /// A quest that the party can embark on, with goals and rewards.
    /// </summary>
    public class Quest : ContentObject
#if WINDOWS
, ICloneable
#endif
    {


        /// <summary>
        /// The possible stages of a quest.
        /// </summary>
        public enum QuestStage
        {
            NotStarted,
            InProgress,
            RequirementsMet,
            Completed
        };

        /// <summary>
        /// The current stage of this quest.
        /// </summary>
        private QuestStage stage = QuestStage.NotStarted;

        /// <summary>
        /// The current stage of this quest.
        /// </summary>
        [ContentSerializerIgnore]
        public QuestStage Stage
        {
            get { return stage; }
            set { stage = value; }
        }

        /// <summary>
        /// The name of the quest.
        /// </summary>
        private string name;

        /// <summary>
        /// The name of the quest.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// A description of the quest.
        /// </summary>
        private string description;

        /// <summary>
        /// A description of the quest.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// A message describing the objective of the quest, 
        /// presented when the player receives the quest.
        /// </summary>
        private string objectiveMessage;

        /// <summary>
        /// A message describing the objective of the quest, 
        /// presented when the player receives the quest.
        /// </summary>
        public string ObjectiveMessage
        {
            get { return objectiveMessage; }
            set { objectiveMessage = value; }
        }

        /// <summary>
        /// A message announcing the completion of the quest, 
        /// presented when the player reaches the goals of the quest.
        /// </summary>
        private string completionMessage;

        public string CompletionMessage
        {
            get { return completionMessage; }
            set { completionMessage = value; }
        }

        /// <summary>
        /// The gear that the player must have to finish the quest.
        /// </summary>
        private List<QuestRequirement<Gear>> gearRequirements =
            new List<QuestRequirement<Gear>>();

        /// <summary>
        /// The gear that the player must have to finish the quest.
        /// </summary>
        public List<QuestRequirement<Gear>> GearRequirements
        {
            get { return gearRequirements; }
            set { gearRequirements = value; }
        }

        /// <summary>
        /// The monsters that must be killed to finish the quest.
        /// </summary>
        private List<QuestRequirement<Monster>> monsterRequirements =
            new List<QuestRequirement<Monster>>();

        /// <summary>
        /// The monsters that must be killed to finish the quest.
        /// </summary>
        public List<QuestRequirement<Monster>> MonsterRequirements
        {
            get { return monsterRequirements; }
            set { monsterRequirements = value; }
        }

        /// <summary>
        /// Returns true if all requirements for this quest have been met.
        /// </summary>
        public bool AreRequirementsMet
        {
            get
            {
                foreach (QuestRequirement<Gear> gearRequirement in gearRequirements)
                {
                    if (gearRequirement.CompletedCount < gearRequirement.Count)
                    {
                        return false;
                    }
                }
                foreach (QuestRequirement<Monster> monsterRequirement
                    in monsterRequirements)
                {
                    if (monsterRequirement.CompletedCount < monsterRequirement.Count)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// The fixed combat encounters added to the world when this quest is active.
        /// </summary>
        private List<WorldEntry<FixedCombat>> fixedCombatEntries =
            new List<WorldEntry<FixedCombat>>();

        /// <summary>
        /// The fixed combat encounters added to the world when this quest is active.
        /// </summary>
        public List<WorldEntry<FixedCombat>> FixedCombatEntries
        {
            get { return fixedCombatEntries; }
            set { fixedCombatEntries = value; }
        }

        /// <summary>
        /// The chests added to thew orld when this quest is active.
        /// </summary>
        private List<WorldEntry<Chest>> chestEntries = new List<WorldEntry<Chest>>();

        /// <summary>
        /// The chests added to thew orld when this quest is active.
        /// </summary>
        public List<WorldEntry<Chest>> ChestEntries
        {
            get { return chestEntries; }
            set { chestEntries = value; }
        }

        /// <summary>
        /// The map with the destination Npc, if any.
        /// </summary>
        private string destinationMapContentName;

        /// <summary>
        /// The map with the destination Npc, if any.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string DestinationMapContentName
        {
            get { return destinationMapContentName; }
            set { destinationMapContentName = value; }
        }

        /// <summary>
        /// The Npc that the party must visit to finish the quest, if any.
        /// </summary>
        private string destinationNpcContentName;

        /// <summary>
        /// The Npc that the party must visit to finish the quest, if any.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string DestinationNpcContentName
        {
            get { return destinationNpcContentName; }
            set { destinationNpcContentName = value; }
        }

        /// <summary>
        /// The message shown when the party is eligible to complete the quest, if any.
        /// </summary>
        private string destinationObjectiveMessage;

        /// <summary>
        /// The message shown when the party is eligible to complete the quest, if any.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public string DestinationObjectiveMessage
        {
            get { return destinationObjectiveMessage; }
            set { destinationObjectiveMessage = value; }
        }

        /// <summary>
        /// The number of experience points given to each party member as a reward.
        /// </summary>
        private int experienceReward;

        /// <summary>
        /// The number of experience points given to each party member as a reward.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public int ExperienceReward
        {
            get { return experienceReward; }
            set { experienceReward = value; }
        }

        /// <summary>
        /// The amount of gold given to the party as a reward.
        /// </summary>
        private int goldReward;

        /// <summary>
        /// The amount of gold given to the party as a reward.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public int GoldReward
        {
            get { return goldReward; }
            set { goldReward = value; }
        }

        /// <summary>
        /// The content names of the gear given to the party as a reward.
        /// </summary>
        private List<string> gearRewardContentNames = new List<string>();

        /// <summary>
        /// The content names of the gear given to the party as a reward.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<string> GearRewardContentNames
        {
            get { return gearRewardContentNames; }
            set { gearRewardContentNames = value; }
        }

        /// <summary>
        /// The gear given to the party as a reward.
        /// </summary>
        private List<Gear> gearRewards = new List<Gear>();

        /// <summary>
        /// The gear given to the party as a reward.
        /// </summary>
        [ContentSerializerIgnore]
        public List<Gear> GearRewards
        {
            get { return gearRewards; }
            set { gearRewards = value; }
        }

        /// <summary>
        /// Reads a Quest object from the content pipeline.
        /// </summary>
        public class QuestReader : ContentTypeReader<Quest>
        {
            /// <summary>
            /// Reads a Quest object from the content pipeline.
            /// </summary>
            protected override Quest Read(ContentReader input, Quest existingInstance)
            {
                Quest quest = existingInstance;
                if (quest == null)
                {
                    quest = new Quest();
                }

                quest.AssetName = input.AssetName;

                quest.Name = input.ReadString();
                quest.Description = input.ReadString();
                quest.ObjectiveMessage = input.ReadString();
                quest.CompletionMessage = input.ReadString();

                quest.GearRequirements.AddRange(
                    input.ReadObject<List<QuestRequirement<Gear>>>());
                quest.MonsterRequirements.AddRange(
                    input.ReadObject<List<QuestRequirement<Monster>>>());

                // load the fixed combat entries
                Random random = new Random();
                quest.FixedCombatEntries.AddRange(
                    input.ReadObject<List<WorldEntry<FixedCombat>>>());
                foreach (WorldEntry<FixedCombat> fixedCombatEntry in
                    quest.FixedCombatEntries)
                {
                    fixedCombatEntry.Content =
                        input.ContentManager.Load<FixedCombat>(
                        Path.Combine("Maps", "FixedCombats",
                        fixedCombatEntry.ContentName));
                    // clone the map sprite in the entry, as there may be many entries
                    // per FixedCombat
                    fixedCombatEntry.MapSprite =
                        fixedCombatEntry.Content.Entries[0].Content.MapSprite.Clone()
                        as AnimatingSprite;
                    // play the idle animation
                    fixedCombatEntry.MapSprite.PlayAnimation("Idle",
                        fixedCombatEntry.Direction);
                    // advance in a random amount so the animations aren't synchronized
                    fixedCombatEntry.MapSprite.UpdateAnimation(
                        4f * (float)random.NextDouble());
                }

                quest.ChestEntries.AddRange(
                    input.ReadObject<List<WorldEntry<Chest>>>());
                foreach (WorldEntry<Chest> chestEntry in quest.ChestEntries)
                {
                    chestEntry.Content = input.ContentManager.Load<Chest>(
                        Path.Combine("Maps", "Chests", chestEntry.ContentName)).Clone() as Chest;
                }

                quest.DestinationMapContentName = input.ReadString();
                quest.DestinationNpcContentName = input.ReadString();
                quest.DestinationObjectiveMessage = input.ReadString();

                quest.experienceReward = input.ReadInt32();
                quest.goldReward = input.ReadInt32();

                quest.GearRewardContentNames.AddRange(
                    input.ReadObject<List<string>>());
                foreach (string contentName in quest.GearRewardContentNames)
                {
                    quest.GearRewards.Add(input.ContentManager.Load<Gear>(
                        Path.Combine("Gear", contentName)));
                }

                return quest;
            }
        }

        public object Clone()
        {
            Quest quest = new Quest();

            quest.AssetName = AssetName;
            foreach (WorldEntry<Chest> chestEntry in chestEntries)
            {
                WorldEntry<Chest> worldEntry = new WorldEntry<Chest>();
                worldEntry.Content = chestEntry.Content.Clone() as Chest;
                worldEntry.ContentName = chestEntry.ContentName;
                worldEntry.Count = chestEntry.Count;
                worldEntry.Direction = chestEntry.Direction;
                worldEntry.MapContentName = chestEntry.MapContentName;
                worldEntry.MapPosition = chestEntry.MapPosition;
                quest.chestEntries.Add(worldEntry);
            }
            quest.completionMessage = completionMessage;
            quest.description = description;
            quest.destinationMapContentName = destinationMapContentName;
            quest.destinationNpcContentName = destinationNpcContentName;
            quest.destinationObjectiveMessage = destinationObjectiveMessage;
            quest.experienceReward = experienceReward;
            quest.fixedCombatEntries.AddRange(fixedCombatEntries);
            quest.gearRequirements.AddRange(gearRequirements);
            quest.gearRewardContentNames.AddRange(gearRewardContentNames);
            quest.gearRewards.AddRange(gearRewards);
            quest.goldReward = goldReward;
            quest.monsterRequirements.AddRange(monsterRequirements);
            quest.name = name;
            quest.objectiveMessage = objectiveMessage;
            quest.stage = stage;

            return quest;
        }

        internal static Quest Load(string questContentName, ContentManager contentManager)
        {
            var questElement = XmlHelper.GetAssetElementFromXML(Path.Combine("Quests", questContentName));
            var quest = new Quest
            {
                AssetName = questContentName,
                Name = (string)questElement.Element("Name"),
                Description = (string)questElement.Element("Description"),
                ObjectiveMessage = (string)questElement.Element("ObjectiveMessage"),
                CompletionMessage = (string)questElement.Element("CompletionMessage"),
                DestinationMapContentName = (string)questElement.Element("DestinationMapContentName"),
                DestinationNpcContentName = (string)questElement.Element("DestinationNpcContentName"),
                DestinationObjectiveMessage = (string)questElement.Element("DestinationObjectiveMessage"),
                ExperienceReward = (int?)questElement.Element("ExperienceReward") ?? 0,
                GoldReward = (int?)questElement.Element("GoldReward") ?? 0,
                Stage = Enum.TryParse<QuestStage>((string)questElement.Element("Stage"), out var stage) ? stage : QuestStage.NotStarted,
                GearRewardContentNames = questElement.Element("GearRewardContentNames")?
                    .Elements("Item")
                    .Select(x => (string)x)
                    .ToList() ?? new List<string>(),
                GearRequirements = questElement.Element("GearRequirements")?
                    .Elements("Item")
                    .Select(gearElement =>
                    {
                        var gearContentName = (string)gearElement.Element("ContentName");
                        var gearCount = (int?)gearElement.Element("Count") ?? 1;

                        var gear = Equipment.Load(Path.Combine("Gear", gearContentName), contentManager);

                        return new QuestRequirement<Gear>
                        {
                            ContentName = gearContentName,
                            Count = gearCount,
                            Content = gear
                        };
                    }).ToList() ?? new List<QuestRequirement<Gear>>(),
                MonsterRequirements = questElement.Element("MonsterRequirements")?
                    .Elements("Item")
                    .Select(entry =>
                    {
                        var monsterContentName = (string)entry.Element("ContentName");
                        var monsterCount = (int?)entry.Element("Count") ?? 1;

                        var monster = Monster.Load(Path.Combine("Characters", "Monsters", monsterContentName), contentManager);

                        return new QuestRequirement<Monster>
                        {
                            ContentName = monsterContentName,
                            Count = monsterCount,
                            Content = monster
                        };
                    }).ToList() ?? new List<QuestRequirement<Monster>>(),
                FixedCombatEntries = questElement.Element("FixedCombatEntries")?.Elements("Item")
                    .Select(item =>
                    {
                        var contentName = (string)item.Element("ContentName");
                        var direction = Enum.TryParse<Direction>((string)item.Element("Direction"), out var dir) ? dir : default;
                        var mapPosition = new Point(
                            int.Parse(item.Element("MapPosition").Value.Split(' ')[0]),
                            int.Parse(item.Element("MapPosition").Value.Split(' ')[1]));

                        // Load the fixed combat asset XML using contentName
                        var fixedCombat = FixedCombat.Load(Path.Combine("Maps", "FixedCombats", contentName), contentManager);

                        AnimatingSprite animatingSprite = null;

                        if (fixedCombat.Entries.Count > 0)
                        {
                            animatingSprite = fixedCombat.Entries[0].Content.MapSprite.Clone() as AnimatingSprite;
                        }

                        return new WorldEntry<FixedCombat>
                        {
                            ContentName = contentName,
                            Content = fixedCombat,
                            Direction = direction,
                            MapPosition = mapPosition,
                            MapSprite = animatingSprite,
                        };
                    }).ToList() ?? new List<WorldEntry<FixedCombat>>(),
                ChestEntries = questElement.Element("ChestEntries")?
                    .Elements("Item")
                    .Select(chestRequirement =>
                    {
                        var contentName = (string)chestRequirement.Element("ContentName");
                        var direction = Enum.TryParse<Direction>((string)chestRequirement.Element("Direction"), out var dir) ? dir : default;
                        var mapPosition = new Point(
                            int.Parse(chestRequirement.Element("MapPosition").Value.Split(' ')[0]),
                            int.Parse(chestRequirement.Element("MapPosition").Value.Split(' ')[1]));

                        // Load the QuestNpc asset XML using contentName
                        var chestAsset = XmlHelper.GetAssetElementFromXML(Path.Combine("Maps", "Chests", contentName));
                        var chest = Chest.Load(chestAsset, contentManager);

                        return new WorldEntry<Chest>
                        {
                            ContentName = contentName,
                            Content = chest,
                            Direction = direction,
                            MapPosition = mapPosition
                        };
                    }).ToList() ?? new List<WorldEntry<Chest>>(),
            };

            return quest;
        }
    }
}