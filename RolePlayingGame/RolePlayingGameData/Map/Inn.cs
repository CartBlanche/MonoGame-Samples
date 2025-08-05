//-----------------------------------------------------------------------------
// Inn.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RolePlaying.Data
{
    /// <summary>
    /// An inn in the game world, where the party can rest and restore themselves.
    /// </summary>
    public class Inn : WorldObject
    {


        /// <summary>
        /// The amount that the party has to pay for each member to stay.
        /// </summary>
        private int chargePerPlayer;

        /// <summary>
        /// The amount that the party has to pay for each member to stay.
        /// </summary>
        public int ChargePerPlayer
        {
            get { return chargePerPlayer; }
            set { chargePerPlayer = value; }
        }






        /// <summary>
        /// The message shown when the player enters the inn.
        /// </summary>
        private string welcomeMessage;

        /// <summary>
        /// The message shown when the player enters the inn.
        /// </summary>
        public string WelcomeMessage
        {
            get { return welcomeMessage; }
            set { welcomeMessage = value; }
        }


        /// <summary>
        /// The message shown when the party successfully pays to stay the night.
        /// </summary>
        private string paidMessage;

        /// <summary>
        /// The message shown when the party successfully pays to stay the night.
        /// </summary>
        public string PaidMessage
        {
            get { return paidMessage; }
            set { paidMessage = value; }
        }


        /// <summary>
        /// The message shown when the party tries to stay but lacks the funds.
        /// </summary>
        private string notEnoughGoldMessage;

        /// <summary>
        /// The message shown when the party tries to stay but lacks the funds.
        /// </summary>
        public string NotEnoughGoldMessage
        {
            get { return notEnoughGoldMessage; }
            set { notEnoughGoldMessage = value; }
        }






        /// <summary>
        /// The content name of the texture for the shopkeeper.
        /// </summary>
        private string shopkeeperTextureName;

        /// <summary>
        /// The content name of the texture for the shopkeeper.
        /// </summary>
        public string ShopkeeperTextureName
        {
            get { return shopkeeperTextureName; }
            set { shopkeeperTextureName = value; }
        }


        /// <summary>
        /// The texture for the shopkeeper.
        /// </summary>
        private Texture2D shopkeeperTexture;

        /// <summary>
        /// The texture for the shopkeeper.
        /// </summary>
        [ContentSerializerIgnore]
        public Texture2D ShopkeeperTexture
        {
            get { return shopkeeperTexture; }
            set { shopkeeperTexture = value; }
        }






        /// <summary>
        /// Reads an Inn object from the content pipeline.
        /// </summary>
        public class InnReader : ContentTypeReader<Inn>
        {
            protected override Inn Read(ContentReader input, Inn existingInstance)
            {
                Inn inn = existingInstance;
                if (inn == null)
                {
                    inn = new Inn();
                }

                input.ReadRawObject<WorldObject>(inn as WorldObject);

                inn.ChargePerPlayer = input.ReadInt32();
                inn.WelcomeMessage = input.ReadString();
                inn.PaidMessage = input.ReadString();
                inn.NotEnoughGoldMessage = input.ReadString();
                inn.ShopkeeperTextureName = input.ReadString();
                inn.shopkeeperTexture = input.ContentManager.Load<Texture2D>(Path.Combine("Textures", "Characters", "Portraits", inn.ShopkeeperTextureName));

                return inn;
            }
        }

        internal static Inn Load(string contentName, ContentManager contentManager)
        {
            var asset = XmlHelper.GetAssetElementFromXML(contentName);
            var inn = new Inn
            {
                AssetName = contentName,
                Name = asset.Element("Name").Value,
                ChargePerPlayer = int.Parse(asset.Element("ChargePerPlayer").Value),
                WelcomeMessage = asset.Element("WelcomeMessage").Value,
                PaidMessage = asset.Element("PaidMessage").Value,
                NotEnoughGoldMessage = asset.Element("NotEnoughGoldMessage").Value,
                ShopkeeperTextureName = asset.Element("ShopkeeperTextureName").Value,
                ShopkeeperTexture = contentManager.Load<Texture2D>(
                    System.IO.Path.Combine("Textures", "Characters", "Portraits",
                    asset.Element("ShopkeeperTextureName").Value)),
            };
            return inn;
        }
    }
}
