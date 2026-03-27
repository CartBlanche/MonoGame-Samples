
namespace MatchemPoker
{

    /// <summary>
    /// Class for holding all the information of a single particle
    /// </summary>
    public class Particle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Size { get; set; }
        public float Angle { get; set; }

        public float Dx { get; set; }
        public float Dy { get; set; }
        public float Sizeinc { get; set; }
        public float Angleinc { get; set; }
        public float LifeTime { get; set; }

        public int UserData { get; set; }
        public int TileIndex { get; set; } 

        // Type of this particle
        public ParticleSprayType Spray { get; set; } 
    }
}