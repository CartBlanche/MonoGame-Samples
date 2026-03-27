using System;

namespace MatchemPoker
{
    /// <summary>
    /// Class to run, use and render the particles
    /// </summary>
    public class ParticleEngine
    {
        // Max number of simultaneous particles.
        const int MAX_PARTICLES = 512;

        protected ITileRenderer m_renderer;
        protected int m_cp;
        protected Particle[] m_particles = new Particle[MAX_PARTICLES];

        System.Random rand = new System.Random();        

        public ParticleEngine(ITileRenderer renderer)
        {
            m_renderer = renderer;
            m_cp = 0;

            // initialize particles inactive
            for (int f = 0; f < MAX_PARTICLES; f++)
            {
                m_particles[f] = new Particle();
                m_particles[f].LifeTime = 0;
            }
        }

        /// <summary>
        /// Helper function for filling a particlespraytype structure.
        /// </summary>
        /// <param name="target">Target spray to be filled</param>
        /// <param name="firstBlock">First texture block which can be used by this spray</param>
        /// <param name="blockCount">How many blocks can be used by this spray after the first one</param>
        /// <param name="gravity">Y Gravity affecting this type of particles</param>
        /// <param name="fraction">Fraction (or air-resistance) for this type of particles</param>
        /// <param name="lifeTime">Static part of the lifetime for this type of particles. This is the minimum.</param>
        /// <param name="lifeTimeRandom">Random part of the lifetime. rand() * lifeTimeRandom will be added to each particle created with this spray.</param>
        /// <param name="size">Static part of a particle's size</param>
        /// <param name="sizeRandom">Random part of a particle's size</param>
        /// <param name="sizeInc">Static part of particle's sizeinc. How much the particle is groving or shrinking through time.</param>
        /// <param name="sizeIncRandom">Random part for the sizeinc.</param>
        /// <param name="angle">Static angle for the particle</param>
        /// <param name="angleRandom">Random maximum angle to be added to static angle.</param>
        /// <param name="angleInc">Static part of how much particle's created with this spray are rotating.</param>
        /// <param name="angleIncRandom">Random part for angleInc.</param>
        /// <param name="type">Currently unused. Qt version used this for indicating should the particle be rendered with alpha- or additive blending.</param>
        public void CreateSprayType(ParticleSprayType target,
                              int firstBlock,
                              int blockCount,
                              float gravity,
                              float fraction,
                              float lifeTime,
                              float lifeTimeRandom,
                              float size,
                              float sizeRandom,
                              float sizeInc,
                              float sizeIncRandom,
                              float angle,
                              float angleRandom,
                              float angleInc,
                              float angleIncRandom,
                              int type)
        {
            target.RenderType = type;
            target.Gravity = gravity;
            target.FirstBlock = firstBlock;
            target.BlockCount = blockCount;
            target.Fraction = fraction;
            target.LifeTime = lifeTime;
            target.LifeTimeRandom = lifeTimeRandom;
            target.Size = size;
            target.FirstBlock = firstBlock;
            target.BlockCount = blockCount;
            target.SizeRandom = sizeRandom;
            target.SizeInc = sizeInc;
            target.SizeIncRandom = sizeIncRandom;
            target.Angle = angle;
            target.AngleRandom = angleRandom;
            target.AngleInc = angleInc;
            target.AngleIncRandom = angleIncRandom;
        }

        /// <summary>
        /// Spray some particles
        /// </summary>
        /// <param name="count">How many particles</param>
        /// <param name="x">X position of an emit</param>
        /// <param name="y">Y position of an emit.</param>
        /// <param name="posrandom">Random R for modifying the position</param>
        /// <param name="dx">X direction of an emit</param>
        /// <param name="dy">Y direction of an emit</param>
        /// <param name="dirrandom">Random R for modifying the direction</param>
        /// <param name="userData">User data, just passed to the particle for later use.</param>
        /// <param name="spray">Which spray is used for emitting.</param>
        public void Spray( int count,
                    float x, float y, float posrandom,
                    float dx, float dy, float dirrandom,
                    int userData,
                    ParticleSprayType spray)
        {
            double l, nx, ny;
            System.Random r = rand;

            while (count > 0)
            {                
                Particle p = m_particles[m_cp];
                m_cp++;

                if (m_cp >= MAX_PARTICLES) 
                    m_cp = 0;

                p.Spray = spray;

                nx = (r.NextDouble() - 0.5) * 2.0;
                ny = (r.NextDouble() - 0.5) * 2.0;

                l = System.Math.Sqrt(nx * nx + ny * ny);
                if (l == 0) l = 1;
                
                     // random vactor
                double v = r.NextDouble();
                nx = nx * v / l;
                ny = ny * v / l;

                p.UserData = userData;

                p.X = ((float)nx * posrandom)  + x;
                p.Y = ((float)ny * posrandom)  + y;
                p.Dx = ((float)nx * dirrandom)  + dx;
                p.Dy = ((float)ny * dirrandom)  + dy;

                p.LifeTime = spray.LifeTime + ((float)r.NextDouble() * spray.LifeTimeRandom);
                p.TileIndex = spray.FirstBlock + r.Next(spray.BlockCount);

                p.Size = spray.Size + (float)r.NextDouble() *spray.SizeRandom;
                p.Sizeinc = spray.SizeInc + (float)r.NextDouble() * spray.SizeIncRandom;

                p.Angle = spray.Angle + (float)r.NextDouble() * spray.AngleRandom;
                p.Angleinc = spray.AngleInc + (float)r.NextDouble() * spray.AngleIncRandom;
                                
                count--;
            }            
        }

        /// <summary>
        /// Run all of the particles alive
        /// </summary>
        /// <param name="frameTime">How many seconds have passed since last frame.</param>
        public void Run(float frameTime)
        {
            float tx, ty, tsize;

            for (int f = 0; f < MAX_PARTICLES; f++) 
            {
                Particle p = m_particles[f];
                if (p.LifeTime > 0)
                {
                    p.LifeTime -= frameTime;

                    if (p.LifeTime>0) 
                    {
                        tx = (p.Dx * p.Spray.Fraction) * frameTime;
                        ty = (p.Dy * p.Spray.Fraction) * frameTime;
                        p.Dx -= tx;
                        p.Dy -= ty;
                        
                        p.Dy += p.Spray.Gravity * frameTime;
                        
                        tx = p.Dx * frameTime;
                        ty = p.Dy * frameTime;
                        tsize = p.Sizeinc * frameTime;

                        p.X += tx;
                        p.Y += ty;
                        p.Size += tsize;

                        p.Angle += p.Angleinc * frameTime;

                        if (p.Size <= 0) 
                            p.LifeTime = 0;		// die if zero sized    

                    }
                }
            }
        }


        /// <summary>
        /// Draw the particles which are alive.
        /// </summary>
        public void Draw()
        {
            int pindex = m_cp - 1;

            while (true)
            {
                if (pindex < 0)
                {
                    pindex = MAX_PARTICLES - 1;
                }

                if (pindex == m_cp)
                {
                    break;
                }

                Particle p = m_particles[pindex];

                if (p.LifeTime > 0)
                {
                    if (p.Spray.m_level != null)
                    {
                        p.Spray.m_level.ScoreParticleRenderFunction(p);
                    }
                    else
                    {
                        int a = (int)(p.LifeTime * 1024.0f);

                        if (a > 255)
                        {
                            a = 255;
                        }
                            
                        a = 255 - a;

                        m_renderer.RenderTile(p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size, p.Angle, p.Spray.RenderType, 
                                              ((uint)p.TileIndex | ((uint)a << 24)), p.UserData);
                    }
                }

                pindex--;
            }
        }
    };
}