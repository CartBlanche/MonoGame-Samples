//-----------------------------------------------------------------------------
// GameStartDescription.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RolePlaying.Data
{
    /// <summary>
    /// The data needed to start a new game.
    /// </summary>
    public class GameStartDescription
    {
        /// <summary>
        /// The content name of the  map for a new game.
        /// </summary>
        private string mapContentName;

        /// <summary>
        /// The content name of the  map for a new game.
        /// </summary>
        public string MapContentName
        {
            get { return mapContentName; }
            set { mapContentName = value; }
        }

        /// <summary>
        /// The content names of the players in the party from the beginning.
        /// </summary>
        private List<string> playerContentNames = new List<string>();

        /// <summary>
        /// The content names of the players in the party from the beginning.
        /// </summary>
        public List<string> PlayerContentNames
        {
            get { return playerContentNames; }
            set { playerContentNames = value; }
        }

        /// <summary>
        /// The quest line in action when the game starts.
        /// </summary>
        /// <remarks>The first quest will be started before the world is shown.</remarks>
        private string questLineContentName;

        /// <summary>
        /// The quest line in action when the game starts.
        /// </summary>
        /// <remarks>The first quest will be started before the world is shown.</remarks>
        [ContentSerializer(Optional = true)]
        public string QuestLineContentName
        {
            get { return questLineContentName; }
            set { questLineContentName = value; }
        }

        public static GameStartDescription Load(string description)
        {
            XElement asset = XmlHelper.GetAssetElementFromXML(description);

            var gameStartDescription = new GameStartDescription
            {
                MapContentName = (string)asset.Element("MapContentName"),
                PlayerContentNames = asset.Element("PlayerContentNames")
                    .Elements("Item")
                    .Select(x => (string)x)
                    .ToList(),
                QuestLineContentName = (string)asset.Element("QuestLineContentName"),
            };

            return gameStartDescription;
        }
    }
}