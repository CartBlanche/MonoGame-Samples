using System.Text.Json;

namespace Shooter.Gameplay
{
    /// <summary>Reads serialised MonoBehaviour fields (raw Unity values) from the baked scene JSON.</summary>
    public static class JsonFields
    {
        public static float Float(JsonElement obj, string name, float fallback)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out JsonElement v))
            {
                if (v.ValueKind == JsonValueKind.Number)
                {
                    return (float)v.GetDouble();
                }
                // the extractor writes non-finite floats as strings (Turret AmmoReloadRate "Infinity")
                if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
                {
                    return (float)parsed;
                }
            }
            return fallback;
        }

        /// <summary>Asset reference field {path} -> content name: "Assets/FPS/Art/Textures/UI/X.png" -> "Textures/UI/X".</summary>
        public static string AssetName(JsonElement obj, string name)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Object &&
                v.TryGetProperty("path", out JsonElement p) && p.ValueKind == JsonValueKind.String)
            {
                string path = p.GetString();
                // Art (textures, models) and Audio keep their folder below Assets/FPS in the content names
                const string art = "Assets/FPS/Art/", fps = "Assets/FPS/";
                if (path.StartsWith(art))
                {
                    path = path.Substring(art.Length);
                }
                else if (path.StartsWith(fps))
                {
                    path = path.Substring(fps.Length);
                }
                int dot = path.LastIndexOf('.');
                return dot > 0 ? path.Substring(0, dot) : path;
            }
            return null;
        }

        /// <summary>Asset reference field {path} -> file name without folder or extension ("VFX_MuzzleFlashGreen" for a prefab).</summary>
        public static string AssetFileName(JsonElement obj, string name)
        {
            string n = AssetName(obj, name);
            return n == null ? null : System.IO.Path.GetFileName(n);
        }

        public static bool Bool(JsonElement obj, string name, bool fallback)
        {
            return Float(obj, name, fallback ? 1f : 0f) != 0f;
        }

        public static int Int(JsonElement obj, string name, int fallback)
        {
            return (int)System.Math.Round(Float(obj, name, fallback));
        }

        /// <summary>Unity Vector3 field {x,y,z} (Unity space, z NOT negated).</summary>
        public static Microsoft.Xna.Framework.Vector3 Vector3(JsonElement obj, string name, Microsoft.Xna.Framework.Vector3 fallback)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Object)
            {
                return new Microsoft.Xna.Framework.Vector3(Float(v, "x", 0f), Float(v, "y", 0f), Float(v, "z", 0f));
            }
            return fallback;
        }
    }
}
