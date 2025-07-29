#region File Description
//-----------------------------------------------------------------------------
// PortalWriter.cs
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

namespace RolePlaying.Processors
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class PortalWriter : RolePlayingGameWriter<Portal>
    {
        protected override void Write(ContentWriter output, Portal value)
        {
            output.Write(value.Name);

            output.WriteObject(value.LandingMapPosition);
            output.Write(value.DestinationMapContentName);
            output.Write(value.DestinationMapPortalName);
        }
    }
}
