using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ThirdPersonShooter
{
    /// <summary>Draws the explosion debris parts (part_head.glb, part_shield.glb, material EvilRobo) through the level's PbrLit renderer, plus the fade glow of dying parts.</summary>
    public sealed class DebrisRenderer
    {
        readonly ModelMesh[] _mesh = new ModelMesh[2];
        readonly Matrix[] _meshWorld = new Matrix[2];
        readonly PbrMaterial[][] _mats = new PbrMaterial[2][];
        readonly LevelScene _scene = new LevelScene();

        public DebrisRenderer(ContentManager content, string contentRoot)
        {
            using var matDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(contentRoot, "Data", "Materials.json")));
            var mats = matDoc.RootElement.GetProperty("materials"); var map = matDoc.RootElement.GetProperty("glbMaterialMap");
            var mc = new System.Collections.Generic.Dictionary<string, PbrMaterial>(); var tc = new System.Collections.Generic.Dictionary<string, Texture2D>();
            string[] names = { "part_shield", "part_head" };
            for (int i = 0; i < 2; i++)
            {
                var model = content.Load<Model>("Models/" + names[i]);
                var abs = new Matrix[model.Bones.Count]; model.CopyAbsoluteBoneTransformsTo(abs);
                _mesh[i] = model.Meshes[0]; _meshWorld[i] = abs[_mesh[i].ParentBone.Index];
                _mats[i] = new PbrMaterial[_mesh[i].MeshParts.Count];
                for (int p = 0; p < _mats[i].Length; p++) _mats[i][p] = LevelScene.ResolveMaterial(map, mats, names[i], _mesh[i].MeshParts[p].Tag as string, mc, tc, content);
            }
        }

        public void Draw(GraphicsDevice gd, LevelRenderer env, DebrisSystem debris, float alpha, Matrix view, Matrix proj, Vector3 camPos, CombatEffects fx)
        {
            _scene.Items.Clear();
            foreach (var p in debris.All)
            {
                if (!p.Alive) continue;
                int k = p.IsHead ? 1 : 0; Matrix body = DebrisSystem.BodyWorld(p, alpha);
                _scene.Items.Add(new DrawItem { Mesh = _mesh[k], World = _meshWorld[k] * body, PartMaterials = _mats[k], Model = k == 1 ? "part_head" : "part_shield" });
                if (p.Fade > 0f) fx.Halo(body.Translation, p.IsHead ? 1.3f : 1.1f, System.Math.Min(1f, p.Fade * 2.5f));   // emission_cutout shell (part.gd fade_value)
            }
            if (_scene.Items.Count > 0) env.Draw(gd, _scene, view, proj, camPos);
        }
    }
}
