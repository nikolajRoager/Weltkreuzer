using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MatrosEngine.Particles;

public class ParticleTemplate
{
    public Texture2D Source;
    public int Nframes;

    public float LifeTime;
    
    public float Friction;

    //Wind is locally set to the particle, so we don't have to reference anybody else
    public Vector2 Wind;
    public ParticleTemplate(Texture2D source, int nframes, float lifetime, float friction, Vector2 wind)
    {
        Source = source;
        Nframes=nframes;
        LifeTime=lifetime;
        Friction=friction;
        Wind=wind;
    }
        
}