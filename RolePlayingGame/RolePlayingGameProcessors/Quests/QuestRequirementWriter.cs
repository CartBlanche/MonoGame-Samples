#region File Description
//-----------------------------------------------------------------------------
// QuestRequirementWriter.cs
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
    public class QuestRequirementWriter<T> : RolePlayingGameWriter<QuestRequirement<T>>
        where T : ContentObject
    {
        ContentEntryWriter<T> contentEntryWriter = null;

        protected override void Initialize(ContentCompiler compiler)
        {
            contentEntryWriter = compiler.GetTypeWriter(typeof(ContentEntry<T>))
                as ContentEntryWriter<T>;

            base.Initialize(compiler);
        }

        protected override void Write(ContentWriter output, QuestRequirement<T> value)
        {
            output.WriteRawObject<ContentEntry<T>>(value as ContentEntry<T>, 
                contentEntryWriter);
        }
    }
}
