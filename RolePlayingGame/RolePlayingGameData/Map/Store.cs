//-----------------------------------------------------------------------------
// Store.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RolePlaying.Data
{
    /// <summary>
    /// A gear store, where the party can buy and sell gear, organized into categories.
    /// </summary>
    public class Store : WorldObject
    {


        /// <summary>
        /// A purchasing multiplier applied to the price of all gear.
        /// </summary>
        private float buyMultiplier;

        /// <summary>
        /// A purchasing multiplier applied to the price of all gear.
        /// </summary>
        public float BuyMultiplier
        {
            get { return buyMultiplier; }
            set { buyMultiplier = value; }
        }


        /// <summary>
        /// A sell-back multiplier applied to the price of all gear.
        /// </summary>
        private float sellMultiplier;

        /// <summary>
        /// A sell-back multiplier applied to the price of all gear.
        /// </summary>
        public float SellMultiplier
        {
            get { return sellMultiplier; }
            set { sellMultiplier = value; }
        }


        /// <summary>
        /// The categories of gear in this store.
        /// </summary>
        private List<StoreCategory> storeCategories = new List<StoreCategory>();

        /// <summary>
        /// The categories of gear in this store.
        /// </summary>
        public List<StoreCategory> StoreCategories
        {
            get { return storeCategories; }
            set { storeCategories = value; }
        }






        /// <summary>
        /// The message shown when the party enters the store.
        /// </summary>
        private string welcomeMessage;

        /// <summary>
        /// The message shown when the party enters the store.
        /// </summary>
        public string WelcomeMessage
        {
            get { return welcomeMessage; }
            set { welcomeMessage = value; }
        }






        /// <summary>
        /// The content path and name of the texture for the shopkeeper.
        /// </summary>
        private string shopkeeperTextureName;

        /// <summary>
        /// The content path and name of the texture for the shopkeeper.
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

        internal static Store Load(string contentName, ContentManager contentManager)
        {
            var asset = XmlHelper.GetAssetElementFromXML(contentName);
            var store = new Store
            {
                AssetName = contentName,
                Name = asset.Element("Name").Value,
                BuyMultiplier = float.Parse(asset.Element("BuyMultiplier").Value),
                SellMultiplier = float.Parse(asset.Element("SellMultiplier").Value),
                WelcomeMessage = asset.Element("WelcomeMessage").Value,
                ShopkeeperTextureName = asset.Element("ShopkeeperTextureName").Value,
                ShopkeeperTexture = contentManager.Load<Texture2D>(
                    System.IO.Path.Combine(@"Textures\Characters\Portraits",
                    asset.Element("ShopkeeperTextureName").Value)),
                StoreCategories = asset.Element("StoreCategories")
                    .Elements("Item")
                    .Select(storeCategory => StoreCategory.Load(storeCategory, contentManager)).ToList(),
            };

            return store;
        }

        /// <summary>
        /// Reads an Store object from the content pipeline.
        /// </summary>
        public class StoreReader : ContentTypeReader<Store>
        {
            protected override Store Read(ContentReader input, Store existingInstance)
            {
                Store store = existingInstance;
                if (store == null)
                {
                    store = new Store();
                }

                input.ReadRawObject<WorldObject>(store as WorldObject);

                store.BuyMultiplier = input.ReadSingle();
                store.SellMultiplier = input.ReadSingle();
                store.StoreCategories.AddRange(input.ReadObject<List<StoreCategory>>());
                store.WelcomeMessage = input.ReadString();
                store.ShopkeeperTextureName = input.ReadString();
                store.shopkeeperTexture = input.ContentManager.Load<Texture2D>(
                    System.IO.Path.Combine(@"Textures\Characters\Portraits",
                    store.ShopkeeperTextureName));

                return store;
            }
        }
    }
}