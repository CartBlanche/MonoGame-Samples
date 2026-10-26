//-----------------------------------------------------------------------------
// RacingGameModelProcessor.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace RacingGame.PipelineExtension
{
    /// <summary>
    /// RacingGame model processor for x files. Loads models the same way as
    /// the ModelProcessor class, but generates tangents and some additional
    /// data too.
    /// </summary>
    [ContentProcessor(DisplayName = "RacingGame Model (Tangent support)")]
    public class RacingGameModelProcessor : ModelProcessor
    {
        /// <summary>
        /// Process the model
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="context">Context for logging</param>
        /// <returns>Model content</returns>
        public override ModelContent Process(
            NodeContent input, ContentProcessorContext context)
        {
            // First generate tangent data because x files don't store them
            GenerateTangents(input, context);

            // Use the name of the bone for our mesh name if it is not set
            UseParentBoneNameIfMeshNameIsNotSet(input);

            // Store the current selected technique and if the texture uses alpha
            // into the mesh name for each mesh part.
            StoreEffectTechniqueInMeshName(input, context);

            // MonoGame's .x importer drops the EffectInstance data (textures,
            // colors, technique) of each material, read it ourselves.
            Dictionary<string, string> materialInfos =
                ReadXFileEffectInstances(input.Identity?.SourceFilename);

            // And let the rest be processed by the default model processor
            ModelContent model = base.Process(input, context);

            // Store the material info as string in the tag of each mesh part, it is
            // used in the game to set the right textures for the part.
            if (materialInfos.Count > 0)
                foreach (ModelMeshContent mesh in model.Meshes)
                    foreach (ModelMeshPartContent part in mesh.MeshParts)
                    {
                        string info;
                        if (part.Material != null &&
                            part.Material.Name != null &&
                            materialInfos.TryGetValue(part.Material.Name, out info))
                            part.Tag = info;
                    }
            return model;
        }

        /// <summary>
        /// Reads the EffectInstance of every material in a .x file. Returns the
        /// material name and "key=value;key=value" strings with diffuseTexture,
        /// normalTexture (names of existing texture files without extension),
        /// ambientColor, diffuseColor, specularColor, shininess and technique.
        /// </summary>
        private static Dictionary<string, string> ReadXFileEffectInstances(
            string xFilename)
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>();
            if (String.IsNullOrEmpty(xFilename) || File.Exists(xFilename) == false)
                return result;

            string text = File.ReadAllText(xFilename);
            string xDir = Path.GetDirectoryName(xFilename);
            foreach (Match m in Regex.Matches(text, @"\bMaterial\s+([^\s{]+)\s*\{"))
            {
                string block = GetBlock(text, m.Index + m.Length - 1);
                int effectPos = block.IndexOf("EffectInstance");
                if (effectPos < 0)
                    continue;

                StringBuilder info = new StringBuilder();
                foreach (Match p in Regex.Matches(block,
                    "EffectParamString\\s*\\{\\s*\"([^\"]+)\"\\s*;\\s*\"([^\"]*)\"\\s*;"))
                {
                    string name = p.Groups[1].Value;
                    if (name == "diffuseTexture" || name == "normalTexture")
                        info.Append(name + "=" +
                            FindTextureName(xDir, p.Groups[2].Value) + ";");
                }
                foreach (Match p in Regex.Matches(block,
                    "EffectParamFloats\\s*\\{\\s*\"([^\"]+)\"\\s*;\\s*\\d+\\s*;\\s*([^;]+);"))
                {
                    string name = p.Groups[1].Value;
                    if (name == "ambientColor" || name == "diffuseColor" ||
                        name == "specularColor" || name == "shininess")
                        info.Append(name + "=" +
                            Regex.Replace(p.Groups[2].Value, "\\s+", "") + ";");
                }
                foreach (Match p in Regex.Matches(block,
                    "EffectParamDWord\\s*\\{\\s*\"technique\"\\s*;\\s*(\\d+)\\s*;"))
                    info.Append("technique=" + p.Groups[1].Value + ";");

                result[m.Groups[1].Value] = info.ToString();
            }
            return result;
        }

        /// <summary>
        /// Returns the text of the {} block starting at the given '{' position.
        /// </summary>
        private static string GetBlock(string text, int openPos)
        {
            int depth = 0;
            for (int i = openPos; i < text.Length; i++)
            {
                if (text[i] == '{')
                    depth++;
                else if (text[i] == '}' && --depth == 0)
                    return text.Substring(openPos, i - openPos + 1);
            }
            return text.Substring(openPos);
        }

        /// <summary>
        /// Finds the real file name (case insensitive, the .x files were made on
        /// Windows) of a texture path like "..\\textures\\building3.tga" and returns it
        /// without the extension.
        /// </summary>
        private static string FindTextureName(string xDir, string path)
        {
            string file = path.Replace('\\', '/');
            string dir = Path.GetFullPath(
                Path.Combine(xDir, Path.GetDirectoryName(file) ?? ""));
            string name = Path.GetFileNameWithoutExtension(file);
            if (Directory.Exists(dir))
                foreach (string candidate in Directory.GetFiles(dir))
                    if (String.Equals(Path.GetFileNameWithoutExtension(candidate),
                        name, StringComparison.OrdinalIgnoreCase) &&
                        String.Equals(Path.GetExtension(candidate),
                        Path.GetExtension(file), StringComparison.OrdinalIgnoreCase))
                        return Path.GetFileNameWithoutExtension(candidate);
            return name;
        }
        /// <summary>
        /// Generate tangents helper method, x files do not have tangents
        /// exported, we have to generate them ourselfs.
        /// </summary>
        /// <param name="input">Input data</param>
        /// <param name="context">Context for logging</param>
        private void GenerateTangents(
            NodeContent input, ContentProcessorContext context)
        {
            MeshContent mesh = input as MeshContent;
            if (mesh != null)
            {
                // Generate trangents for the mesh. We don't want binormals,
                // so null is passed in for the last parameter.
                MeshHelper.CalculateTangentFrames(mesh,
                    VertexChannelNames.TextureCoordinate(0),
                    VertexChannelNames.Tangent(0), null);
            }

            // Go through all childs
            foreach (NodeContent child in input.Children)
            {
                GenerateTangents(child, context);
            }
        }
        /// <summary>
        /// Use parent bone's name if mesh's name is not set.
        /// </summary>
        /// <param name="input"></param>
        private void UseParentBoneNameIfMeshNameIsNotSet(NodeContent input)
        {
            if (String.IsNullOrEmpty(input.Name) &&
                input.Parent != null &&
                String.IsNullOrEmpty(input.Parent.Name) == false)
                input.Name = input.Parent.Name;

            foreach (NodeContent node in input.Children)
                UseParentBoneNameIfMeshNameIsNotSet(node);
        }
        /// <summary>
        /// Stores the current selected technique and if the texture uses alpha
        /// into the mesh name for each mesh part.
        /// </summary>
        private void StoreEffectTechniqueInMeshName(
            NodeContent input, ContentProcessorContext context)
        {
            MeshContent mesh = input as MeshContent;
            if (mesh != null)
            {
                foreach (GeometryContent geom in mesh.Geometry)
                {
                    EffectMaterialContent effectMaterial =
                        geom.Material as EffectMaterialContent;
                    if (effectMaterial != null)
                    {
                        if (effectMaterial.OpaqueData.ContainsKey("technique"))
                        {
                            // Store technique here! (OpaqueData["technique"] is an
                            // int32) If we have multiple mesh parts in our mesh object,
                            // there will be multiple techniques listed at the end of
                            // our mesh name.
                            input.Name =
                                input.Name + effectMaterial.OpaqueData["technique"];
                        }
                    }
                }
            }

            // Go through all childs
            foreach (NodeContent child in input.Children)
            {
                StoreEffectTechniqueInMeshName(child, context);
            }
        }
    }
}
