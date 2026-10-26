using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;

namespace Shooter.Pipeline
{
    /// <summary>
    /// FBX importer for skinned meshes and animation takes. It delegates to MonoGame's Assimp importer, which
    /// (verified on MonoGame 3.8.x) already delivers: the node hierarchy with FBX pivots/pre-rotations baked into
    /// node transforms, per-vertex BoneWeightCollection channels, and one keyframed AnimationContent ("Take 001")
    /// per animated node (keys are full node-local matrices). The stock ModelProcessor throws all of that away;
    /// SkinnedModelProcessor / AnimationClipProcessor consume it.
    /// </summary>
    [ContentImporter(".fbx", DisplayName = "Skinned FBX Importer - Shooter", DefaultProcessor = "SkinnedModelProcessor")]
    public sealed class SkinnedFbxImporter : ContentImporter<NodeContent>
    {
        public override NodeContent Import(string filename, ContentImporterContext context)
        {
            OpenAssetImporter importer = new OpenAssetImporter();
            return importer.Import(filename, context);
        }
    }
}
