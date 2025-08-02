//-----------------------------------------------------------------------------
// FixedCombat.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Content;

namespace RolePlaying.Data
{
    /// <summary>
    /// The description of a fixed combat encounter in the world.
    /// </summary>
    public class FixedCombat : WorldObject
    {
        /// <summary>
        /// The content name and quantities of the monsters in this encounter.
        /// </summary>
        private List<ContentEntry<Monster>> entries = new List<ContentEntry<Monster>>();

        /// <summary>
        /// The content name and quantities of the monsters in this encounter.
        /// </summary>
        public List<ContentEntry<Monster>> Entries
        {
            get { return entries; }
            set { entries = value; }
        }

        internal static FixedCombat Load(string contentName, ContentManager contentManager)
        {
            var asset = XmlHelper.GetAssetElementFromXML(contentName);

            // Create a new FixedCombat instance and populate it with data from the XML asset
            var fixedCombat = new FixedCombat()
            {
                AssetName = contentName,
                Name = (string)asset.Element("Name"),
                Entries = asset.Element("Entries")?.Elements("Item")
                    .Select(entry =>
                    {
                        var monsterContentName = (string)entry.Element("ContentName");
                        var monsterCount = (int?)entry.Element("Count") ?? 1;

                        var monster = Monster.Load(Path.Combine("Characters", "Monsters", monsterContentName), contentManager);

                        return new ContentEntry<Monster>
                        {
                            ContentName = monsterContentName,
                            Count = monsterCount,
                            Content = monster
                        };
                    }).ToList(),
            };

            return fixedCombat;
        }




        /// <summary>
        /// Reads a FixedCombat object from the content pipeline.
        /// </summary>
        public class FixedCombatReader : ContentTypeReader<FixedCombat>
        {
            /// <summary>
            /// Reads a FixedCombat object from the content pipeline.
            /// </summary>
            protected override FixedCombat Read(ContentReader input,
                FixedCombat existingInstance)
            {
                FixedCombat fixedCombat = existingInstance;
                if (fixedCombat == null)
                {
                    fixedCombat = new FixedCombat();
                }

                input.ReadRawObject<WorldObject>(fixedCombat as WorldObject);

                fixedCombat.Entries.AddRange(
                    input.ReadObject<List<ContentEntry<Monster>>>());
                foreach (ContentEntry<Monster> fixedCombatEntry in fixedCombat.Entries)
                {
                    fixedCombatEntry.Content = input.ContentManager.Load<Monster>(
                        Path.Combine(@"Characters\Monsters", 
                            fixedCombatEntry.ContentName));
                }

                return fixedCombat;
            }
        }


    }
}
