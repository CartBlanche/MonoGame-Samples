using System.Collections.Generic;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace Shooter.StaticPipeline
{
    /// <summary>
    /// ModelProcessor for the static (non-skinned) Unity models.
    ///  * Textures referenced inside the FBX/OBJ/MTL are dropped: they are absolute paths on the original
    ///    artist's machine (some do not exist) and the runtime binds textures from Unity's materials.json
    ///    (via AssetMap.json) instead.
    ///  * The source material name of every mesh part is stored in ModelMeshPart.Tag (string) so the
    ///    scene loader can match a part with the Unity material slot of the same name.
    ///  * Tangent frames come from the inherited GenerateTangentFrames option (enabled in Shooter.mgcb).
    /// </summary>
    [ContentProcessor(DisplayName = "Shooter Static Model")]
    public class StaticModelProcessor : ModelProcessor
    {
        private readonly Dictionary<MaterialContent, string> _names = new Dictionary<MaterialContent, string>();

        protected override MaterialContent ConvertMaterial(MaterialContent material, ContentProcessorContext context)
        {
            // OpenAssetImporter yields a plain MaterialContent which ModelProcessor would serialize
            // reflectively (and the runtime cannot read). Replace it with a texture-less BasicMaterialContent
            // that keeps the source name; the runtime swaps in the PBR effect anyway.
            string name = material.Name;
            var basic = new BasicMaterialContent { Name = name };
            MaterialContent result = base.ConvertMaterial(basic, context);
            result.Name = name;
            _names[result] = name;
            return result;
        }

        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            ModelContent model = base.Process(input, context);
            foreach (ModelMeshContent mesh in model.Meshes)
            {
                foreach (ModelMeshPartContent part in mesh.MeshParts)
                {
                    string name = null;
                    if (part.Material != null && !_names.TryGetValue(part.Material, out name))
                    {
                        name = part.Material.Name;
                    }
                    part.Tag = name ?? string.Empty;
                }
            }
            return model;
        }
    }
}
