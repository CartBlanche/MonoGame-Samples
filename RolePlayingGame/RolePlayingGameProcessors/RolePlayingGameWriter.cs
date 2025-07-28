#region File Description
//-----------------------------------------------------------------------------
// RolePlayingGameWriter.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
#endregion

namespace RolePlayingGame.Processors
{
    public abstract class RolePlayingGameWriter<T> : ContentTypeWriter<T>
    {
        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            Type type = typeof(T);
            string readerType = $"{type.Namespace}.{type.Name}.{type.Name}Reader, {type.Assembly.FullName}";
            return readerType;
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(T).AssemblyQualifiedName;
        }
    }
}
