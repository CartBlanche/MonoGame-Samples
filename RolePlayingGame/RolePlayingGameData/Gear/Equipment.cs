//-----------------------------------------------------------------------------
// Equipment.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RolePlaying.Data
{
    /// <summary>
    /// Gear that may be equipped onto a FightingCharacter.
    /// </summary>
    public class Equipment : Gear
    {


        /// <summary>
        /// The statistics buff applied by this equipment to its owner.
        /// </summary>
        /// <remarks>Buff values are positive, and will be added.</remarks>
        private StatisticsValue ownerBuffStatistics = new StatisticsValue();

        /// <summary>
        /// The statistics buff applied by this equipment to its owner.
        /// </summary>
        /// <remarks>Buff values are positive, and will be added.</remarks>
        [ContentSerializer(Optional = true)]
        public StatisticsValue OwnerBuffStatistics
        {
            get { return ownerBuffStatistics; }
            set { ownerBuffStatistics = value; }
        }

        public static Equipment Load(string equipmentAssetName, ContentManager contentManager)
        {
            var equipmentAsset = XmlHelper.GetAssetElementFromXML(equipmentAssetName);
            var equipment = new Equipment()
            {
                AssetName = equipmentAssetName,
                Name = (string)equipmentAsset.Element("Name"),
                Description = (string)equipmentAsset.Element("Description"),
                GoldValue = (int)equipmentAsset.Element("GoldValue"),
                IconTextureName = (string)equipmentAsset.Element("IconTextureName"),
                IconTexture = contentManager.Load<Texture2D>(Path.Combine("Textures", "Gear", (string)equipmentAsset.Element("IconTextureName"))),
                MinimumCharacterLevel = int.Parse(equipmentAsset.Element("MinimumCharacterLevel").Value),
            };

            return equipment;
        }

        /// <summary>
        /// Read the Equipment type from the content pipeline.
        /// </summary>
        public class EquipmentReader : ContentTypeReader<Equipment>
        {
            /// <summary>
            /// Read the Equipment type from the content pipeline.
            /// </summary>
            protected override Equipment Read(ContentReader input,
                Equipment existingInstance)
            {
                Equipment equipment = existingInstance;

                if (equipment == null)
                {
                    throw new ArgumentException(
                        "Unable to create new Equipment objects.");
                }

                // read the gear settings
                input.ReadRawObject<Gear>(equipment as Gear);

                // read the equipment settings
                equipment.OwnerBuffStatistics = input.ReadObject<StatisticsValue>();

                return equipment;
            }
        }
    }
}