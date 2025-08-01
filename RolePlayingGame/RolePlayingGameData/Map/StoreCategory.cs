//-----------------------------------------------------------------------------
// StoreCategory.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RolePlaying.Data
{
    /// <summary>
    /// A category of gear for sale in a store.
    /// </summary>
    public class StoreCategory
    {
        /// <summary>
        /// The display name of this store category.
        /// </summary>
        private string name;

        /// <summary>
        /// The display name of this store category.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }


        /// <summary>
        /// The content names for the gear available in this category.
        /// </summary>
        private List<string> availableContentNames = new List<string>();

        /// <summary>
        /// The content names for the gear available in this category.
        /// </summary>
        public List<string> AvailableContentNames
        {
            get { return availableContentNames; }
            set { availableContentNames = value; }
        }


        /// <summary>
        /// The gear available in this category.
        /// </summary>
        private List<Gear> availableGear = new List<Gear>();

        /// <summary>
        /// The gear available in this category.
        /// </summary>
        [ContentSerializerIgnore]
        public List<Gear> AvailableGear
        {
            get { return availableGear; }
            set { availableGear = value; }
        }




        /// <summary>
        /// Reads a StoreCategory object from the content pipeline.
        /// </summary>
        public class StoreCategoryReader : ContentTypeReader<StoreCategory>
        {
            /// <summary>
            /// Reads a StoreCategory object from the content pipeline.
            /// </summary>
            protected override StoreCategory Read(ContentReader input,
                StoreCategory existingInstance)
            {
                StoreCategory storeCategory = existingInstance;
                if (storeCategory == null)
                {
                    storeCategory = new StoreCategory();
                }

                storeCategory.Name = input.ReadString();
                storeCategory.AvailableContentNames.AddRange(
                    input.ReadObject<List<string>>());

                // populate the gear list based on the content names
                foreach (string gearName in storeCategory.AvailableContentNames)
                {
                    storeCategory.AvailableGear.Add(input.ContentManager.Load<Gear>(
                        System.IO.Path.Combine("Gear", gearName)));
                }

                return storeCategory;
            }
        }

		internal static StoreCategory Load(XElement storeCategoryElement, ContentManager contentManager)
		{
            var storeCategory = new StoreCategory
            {
                Name = storeCategoryElement.Element("Name").Value,
                AvailableContentNames = storeCategoryElement.Element("AvailableContentNames")
                            .Elements("Item")
                            .Select(contentNameElement => contentNameElement.Value)
                            .ToList(),
			};

			foreach (string gearName in storeCategory.AvailableContentNames)
			{
				var gearAsset = XmlHelper.GetAssetElementFromXML(System.IO.Path.Combine("Gear", gearName));
				
                var gear = new Item
                {
                    AssetName = gearName,
                    Name = gearAsset.Element("Name").Value,
                    Description = gearAsset.Element("Description").Value,
					GoldValue = int.Parse(gearAsset.Element("GoldValue").Value),
					IsDroppable = bool.Parse(gearAsset.Element("IsDroppable").Value),
					IsOffensive = bool.Parse(gearAsset.Element("IsOffensive").Value),
					MinimumCharacterLevel = int.Parse(gearAsset.Element("MinimumCharacterLevel").Value),
					IconTextureName = gearAsset.Element("IconTextureName").Value,
					IconTexture = contentManager.Load<Texture2D>(
                        System.IO.Path.Combine("Textures", "Gear", gearAsset.Element("IconTextureName").Value)),
					TargetDuration = int.Parse(gearAsset.Element("TargetDuration").Value),
					AdjacentTargets = int.Parse(gearAsset.Element("AdjacentTargets").Value),
					UsingCueName = gearAsset.Element("UsingCueName").Value,
					ImpactCueName = gearAsset.Element("ImpactCueName").Value,
					BlockCueName = gearAsset.Element("BlockCueName").Value,
				};

				// Load other properties of Gear as needed
				storeCategory.AvailableGear.Add(gear);
			}

			return storeCategory;
		}
	}
}