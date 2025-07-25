//-----------------------------------------------------------------------------
// AnimatingSpriteWriter.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using RolePlaying.Data;

namespace RolePlayingGame.Processors
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to write the specified data type into binary .xnb format.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentTypeWriter]
    public class AnimatingSpriteWriter : RolePlayingGameWriter<AnimatingSprite>
    {
        protected override void Write(ContentWriter output, AnimatingSprite value)
        {
            output.Write(value.TextureName);
            output.WriteObject(value.FrameDimensions);
            output.Write(value.FramesPerRow);
            output.WriteObject(value.SourceOffset);
            output.WriteObject(value.Animations);
        }
    }
}
