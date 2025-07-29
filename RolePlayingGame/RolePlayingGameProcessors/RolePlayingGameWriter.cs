#region File Description
//-----------------------------------------------------------------------------
// RolePlayingGameWriter.cs
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
#endregion

namespace RolePlaying.Processors
{
    public abstract class RolePlayingGameWriter<T> : ContentTypeWriter<T>
    {
        const string DATA_ASSEMBLY = "RolePlaying.Data";

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            Type type = typeof(T);

            string readerText = type.FullName;
            string shortTypeName = type.Name;
            if (shortTypeName.EndsWith("`1"))
            {
                // build the name of a templated type
                shortTypeName = shortTypeName.Substring(0, shortTypeName.Length - 2);
                readerText = readerText.Insert(readerText.IndexOf("`1") + 2, "+" +
                    shortTypeName + "Reader");
            }
            else
            {
                // build the name of a non-templated type
                readerText += "+" + shortTypeName + "Reader";
            }
            readerText += $", {DATA_ASSEMBLY}";

            System.Diagnostics.Debug.WriteLine("Reader:  " + readerText);

            return readerText;
        }


        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            Type type = typeof(T);

            string typeText = type.FullName + $", {DATA_ASSEMBLY}";

            System.Diagnostics.Debug.WriteLine("Type:  " + typeText);

            return typeText;
        }
    }
}
