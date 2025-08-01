//-----------------------------------------------------------------------------
// QuestNpc.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RolePlaying.Data
{
    /// <summary>
    /// An NPC that does not fight and does not join the party.
    /// </summary>
    public class QuestNpc : Character
    {


        /// <summary>
        /// The dialogue that the Npc says when it is greeted in the world.
        /// </summary>
        private string introductionDialogue;

        /// <summary>
        /// The dialogue that the Npc says when it is greeted in the world.
        /// </summary>
        public string IntroductionDialogue
        {
            get { return introductionDialogue; }
            set { introductionDialogue = value; }
        }

        public static QuestNpc Load(string npcPath, ContentManager contentManager)
        {
            var asset = XmlHelper.GetAssetElementFromXML(npcPath);
            
            var questNpc = new QuestNpc
            {
                AssetName = npcPath,
                Name = (string)asset.Element("Name"),
                IntroductionDialogue = (string)asset.Element("IntroductionDialogue"),
                MapIdleAnimationInterval = asset.Element("MapIdleAnimationInterval") != null
                    ? int.TryParse((string)asset.Element("MapIdleAnimationInterval"), out var interval) ? interval : default
                    : default,
                MapSprite = asset.Element("MapSprite") != null
                    ? new AnimatingSprite
                        {
                            TextureName = (string)asset.Element("MapSprite").Element("TextureName"),
                            Texture = contentManager.Load<Texture2D>(
                                Path.Combine(@"Textures\", (string)asset.Element("MapSprite").Element("TextureName"))),
                            FrameDimensions = new Point(
                                int.Parse(asset.Element("MapSprite").Element("FrameDimensions").Value.Split(' ')[0]),
                                int.Parse(asset.Element("MapSprite").Element("FrameDimensions").Value.Split(' ')[1])),
                            FramesPerRow = (int)asset.Element("MapSprite").Element("FramesPerRow"),
                            SourceOffset = new Vector2(
                                int.Parse(asset.Element("MapSprite").Element("SourceOffset").Value.Split(' ')[0]),
                                int.Parse(asset.Element("MapSprite").Element("SourceOffset").Value.Split(' ')[1])),
                            // Handle Animations if needed
                        }
                    : null,
            };

            questNpc.AddStandardCharacterIdleAnimations();
            questNpc.AddStandardCharacterWalkingAnimations();

            questNpc.ResetAnimation(false);

            // TODO Looks like player is floating. Offset issue. questNpc.ShadowTexture = contentManager.Load<Texture2D>(@"Textures\Characters\CharacterShadow");

            return questNpc;
        }

        /// <summary>
        /// Read a QuestNpc object from the content pipeline.
        /// </summary>
        public class QuestNpcReader : ContentTypeReader<QuestNpc>
        {
            protected override QuestNpc Read(ContentReader input,
                QuestNpc existingInstance)
            {
                QuestNpc questNpc = existingInstance;
                if (questNpc == null)
                {
                    questNpc = new QuestNpc();
                }

                input.ReadRawObject<Character>(questNpc as Character);

                questNpc.IntroductionDialogue = input.ReadString();

                return questNpc;
            }
        }
    }
}