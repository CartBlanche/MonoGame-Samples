//-----------------------------------------------------------------------------
// WorldObject.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace RolePlaying.Data
{
    /// <summary>
    /// Common base class for all objects that are visible in the world.
    /// </summary>
    public abstract class WorldObject : ContentObject
    {


        /// <summary>
        /// The name of the object.
        /// </summary>
        private string name;

        /// <summary>
        /// The name of the object.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }






        /// <summary>
        /// Read a WorldObject object from the content pipeline.
        /// </summary>
        public class WorldObjectReader : ContentTypeReader<WorldObject>
        {
            /// <summary>
            /// Read a WorldObject object from the content pipeline.
            /// </summary>
            protected override WorldObject Read(ContentReader input, 
                WorldObject existingInstance)
            {
                // we cannot create this object, so there must be an existing instance
                if (existingInstance == null)
                {
                    throw new ArgumentNullException("existingInstance");
                }

                existingInstance.AssetName = input.AssetName;
                existingInstance.Name = input.ReadString();

                return existingInstance;
            }
        }


    }
}
