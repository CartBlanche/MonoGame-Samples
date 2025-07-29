//-----------------------------------------------------------------------------
// GameStartDescriptionReader.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace RolePlaying.Data
{
    /// <summary>
    /// Read a GameStartDescription object from the content pipeline.
    /// </summary>
    public class GameStartDescriptionReader  : ContentTypeReader<GameStartDescription>
    {
        protected override GameStartDescription Read(ContentReader input,
            GameStartDescription existingInstance)
        {
            GameStartDescription desc = existingInstance;
            if (desc == null)
            {
                desc = new GameStartDescription();
            }

            desc.MapContentName = input.ReadString();
            desc.PlayerContentNames.AddRange(input.ReadObject<List<string>>());
            desc.QuestLineContentName = input.ReadString();

            return desc;
        }
    }
}