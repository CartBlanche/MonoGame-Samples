//-----------------------------------------------------------------------------
// QuestLine.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;

namespace RolePlaying.Data
{
    /// <summary>
    /// A line of quests, presented to the player in order.
    /// </summary>
    /// <remarks>
    /// In other words, only one quest is presented at a time and 
    /// must be competed before the line can continue.
    /// </remarks>
    public class QuestLine : ContentObject
#if WINDOWS
, ICloneable
#endif
    {
        /// <summary>
        /// The name of the quest line.
        /// </summary>
        private string name;

        /// <summary>
        /// The name of the quest line.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }


        /// <summary>
        /// An ordered list of content names of quests that will be presented in order.
        /// </summary>
        private List<string> questContentNames = new List<string>();

        /// <summary>
        /// An ordered list of content names of quests that will be presented in order.
        /// </summary>
        public List<string> QuestContentNames
        {
            get { return questContentNames; }
            set { questContentNames = value; }
        }


        /// <summary>
        /// An ordered list of quests that will be presented in order.
        /// </summary>
        private List<Quest> quests = new List<Quest>();

        /// <summary>
        /// An ordered list of quests that will be presented in order.
        /// </summary>
        [ContentSerializerIgnore]
        public List<Quest> Quests
        {
            get { return quests; }
        }
    




        /// <summary>
        /// Reads a QuestLine object from the content pipeline.
        /// </summary>
        public class QuestLineReader : ContentTypeReader<QuestLine>
        {
            /// <summary>
            /// Reads a QuestLine object from the content pipeline.
            /// </summary>
            protected override QuestLine Read(ContentReader input, 
                QuestLine existingInstance)
            {
                QuestLine questLine = existingInstance;
                if (questLine == null)
                {
                    questLine = new QuestLine();
                }

                questLine.AssetName = input.AssetName;

                questLine.Name = input.ReadString();

                questLine.QuestContentNames.AddRange(input.ReadObject<List<string>>());
                foreach (string contentName in questLine.QuestContentNames)
                {
                    questLine.quests.Add(input.ContentManager.Load<Quest>(
                        Path.Combine("Quests", contentName)));

                }

                return questLine;
            }
        }






        public object Clone()
        {
            QuestLine questLine = new QuestLine();

            questLine.AssetName = AssetName;
            questLine.name = name;
            questLine.questContentNames.AddRange(questContentNames);
            foreach (Quest quest in quests)
            {
                questLine.quests.Add(quest.Clone() as Quest);
            }

            return questLine;
        }


    }
}
