//-----------------------------------------------------------------------------
// Chest.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RolePlaying.Data
{
    /// <summary>
    /// A treasure chest in the game world.
    /// </summary>
    public class Chest : WorldObject
#if WINDOWS
, ICloneable
#endif
    {
        /// <summary>
        /// The amount of gold in the chest.
        /// </summary>
        private int gold = 0;

        /// <summary>
        /// The amount of gold in the chest.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public int Gold
        {
            get { return gold; }
            set { gold = value; }
        }

        /// <summary>
        /// The gear in the chest, along with quantities.
        /// </summary>
        private List<ContentEntry<Gear>> entries = new List<ContentEntry<Gear>>();

        /// <summary>
        /// The gear in the chest, along with quantities.
        /// </summary>
        public List<ContentEntry<Gear>> Entries
        {
            get { return entries; }
            set { entries = value; }
        }

        /// <summary>
        /// Array accessor for the chest's contents.
        /// </summary>
        public ContentEntry<Gear> this[int index]
        {
            get { return entries[index]; }
        }

        /// <summary>
        /// Returns true if the chest is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return ((gold <= 0) && (entries.Count <= 0)); }
        }

        /// <summary>
        /// The content name of the texture for this chest.
        /// </summary>
        private string textureName;

        /// <summary>
        /// The content name of the texture for this chest.
        /// </summary>
        public string TextureName
        {
            get { return textureName; }
            set { textureName = value; }
        }

        /// <summary>
        /// The texture for this chest.
        /// </summary>
        private Texture2D texture;

        /// <summary>
        /// The texture for this chest.
        /// </summary>
        [ContentSerializerIgnore]
        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }

        /// <summary>
        /// Reads a Chest object from the content pipeline.
        /// </summary>
        public class ChestReader : ContentTypeReader<Chest>
        {
            protected override Chest Read(ContentReader input,
                Chest existingInstance)
            {
                Chest chest = existingInstance;
                if (chest == null)
                {
                    chest = new Chest();
                }

                input.ReadRawObject<WorldObject>(chest as WorldObject);

                chest.Gold = input.ReadInt32();

                chest.Entries.AddRange(
                    input.ReadObject<List<ContentEntry<Gear>>>());
                foreach (ContentEntry<Gear> contentEntry in chest.Entries)
                {
                    contentEntry.Content = input.ContentManager.Load<Gear>(
                        System.IO.Path.Combine(@"Gear",
                        contentEntry.ContentName));
                }

                chest.TextureName = input.ReadString();
                chest.Texture = input.ContentManager.Load<Texture2D>(
                    System.IO.Path.Combine(@"Textures\Chests", chest.TextureName));

                return chest;
            }
        }

        /// <summary>
        /// Clone implementation for chest copies.
        /// </summary>
        /// <remarks>
        /// The game has to handle chests that have had some contents removed
        /// without modifying the original chest (and all chests that come after).
        /// </remarks>
        public object Clone()
        {
            // create the new chest
            Chest chest = new Chest();

            // copy the data
            chest.Gold = Gold;
            chest.Name = Name;
            chest.Texture = Texture;
            chest.TextureName = TextureName;

            // recreate the list and entries, as counts may have changed
            chest.entries = new List<ContentEntry<Gear>>();
            foreach (ContentEntry<Gear> originalEntry in Entries)
            {
                ContentEntry<Gear> newEntry = new ContentEntry<Gear>();
                newEntry.Count = originalEntry.Count;
                newEntry.ContentName = originalEntry.ContentName;
                newEntry.Content = originalEntry.Content;
                chest.Entries.Add(newEntry);
            }

            return chest;
        }

        internal static Chest Load(XElement chestAsset, ContentManager contentManager)
        {
            var chest = new Chest
            {
                Name = (string)chestAsset.Element("Name"),
                Gold = (int)chestAsset.Element("Gold"),
                Entries = chestAsset.Element("Entries")?.Elements("Item").Select(chestItem =>
                {
                    var contentName = (string)chestItem.Element("ContentName");
                    var gearAsset = XmlHelper.GetAssetElementFromXML(Path.Combine(@"Gear", contentName));
                    var gear = new Equipment
                    {
                        AssetName = contentName,
                        Name = (string)gearAsset.Element("Name"),
                        Description = (string)gearAsset.Element("Description"),
                        GoldValue = (int?)gearAsset.Element("GoldValue") ?? 0,
                        IconTextureName = (string)gearAsset.Element("IconTextureName"),
                        IconTexture = contentManager.Load<Texture2D>(
                            Path.Combine(@"Textures\Gear", (string)gearAsset.Element("IconTextureName"))),
                        IsDroppable = (bool?)gearAsset.Element("IsDroppable") ?? true,

                        // Add other properties as needed
                    };

                    return new ContentEntry<Gear>
                    {
                        ContentName = contentName,
                        Content = gear,
                        Count = (int?)chestItem.Element("Count") ?? 1,
                    };
                }).ToList(),
                TextureName = (string)chestAsset.Element("TextureName"),
                Texture = contentManager.Load<Texture2D>(Path.Combine("Textures", "Chests", (string)chestAsset.Element("TextureName"))),
            };
            return chest;
        }
    }
}