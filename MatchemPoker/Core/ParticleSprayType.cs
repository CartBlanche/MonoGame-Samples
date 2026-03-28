
namespace MatchemPoker
{
    /// <summary>
    /// Class to hold information for a type of particles. 
    /// </summary>
    public class ParticleSprayType
    {

        public int RenderType { get; set; }

        public float Gravity { get; set; }
        public float Fraction { get; set; }

        public float LifeTime { get; set; }
        public float LifeTimeRandom { get; set; }

        public float Size { get; set; }
        public float SizeRandom { get; set; }

        public float SizeInc { get; set; }
        public float SizeIncRandom { get; set; }

        public float Angle { get; set; }
        public float AngleRandom { get; set; }

        public float AngleInc { get; set; }
        public float AngleIncRandom { get; set; }

        public int FirstBlock { get; set; }
        public int BlockCount { get; set; }

        public NpcLevel m_level { get; set; }       // if != 0, scoreparticlerender-function is used.

        public ParticleSprayType()
        {
            m_level = null;
        }

        /// <summary>
        /// Sets level to given one
        /// </summary>
        /// <param name="level">Level to set</param>
        public void setLevel(NpcLevel level)
        {
            m_level = level;
        }
    }
}
