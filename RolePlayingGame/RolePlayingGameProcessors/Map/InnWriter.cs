#region File Description
//-----------------------------------------------------------------------------
// InnWriter.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using RolePlaying.Data;
#endregion

namespace RolePlayingGame.Processors
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class InnWriter : RolePlayingGameWriter<Inn>
    {
        WorldObjectWriter worldObjectWriter = null;

        protected override void Initialize(ContentCompiler compiler)
        {
            worldObjectWriter = compiler.GetTypeWriter(typeof(WorldObject))
                as WorldObjectWriter;

            base.Initialize(compiler);
        }

        protected override void Write(ContentWriter output, Inn value)
        {
            output.WriteRawObject<WorldObject>(value as WorldObject, worldObjectWriter);

            output.Write(value.ChargePerPlayer);
            output.Write(value.WelcomeMessage);
            output.Write(value.PaidMessage);
            output.Write(value.NotEnoughGoldMessage);
            output.Write(value.ShopkeeperTextureName);
        }
    }
}
