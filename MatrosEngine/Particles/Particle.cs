using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MatrosEngine.Particles;

public class Particle
{
    public Texture2D Source;
    public Vector2 Position;
    public Vector2 Velocity;
    //Wind is locally set to the particle, so we don't have to reference anybody else
    public Vector2 Wind;
    public float Friction;
    private List<Rectangle> _sourceRectangles;

    public float LifeTime;
    public float Age;
    
    public bool Dead => Age>=LifeTime;


    public Particle(Texture2D source, Vector2 position, int nframes, float lifetime, float friction, Vector2 wind, Vector2 velocity)
    {
        Source = source;
        Position = position;
        Velocity = velocity;
        LifeTime = lifetime;
        Friction = friction;
        Wind = wind;
        Age = 0;

        _sourceRectangles = new();
        for (int i = 0; i < nframes; i++)
            _sourceRectangles.Add(new Rectangle(i*source.Width/nframes,0,source.Width/nframes,source.Height));
    }

    public void Update(GameTime gameTime)
    {
        float dt=(float)gameTime.ElapsedGameTime.TotalSeconds;
        Velocity-=Velocity*dt*Friction;
        Velocity-=Wind*dt*Friction;
        Position += Velocity * dt;
        Age += dt;
    }

    public void Draw(SpriteBatch spriteBatch,Vector2 cameraPosition)
    {
        int phase = Math.Max(0, Math.Min((int)(Age * _sourceRectangles.Count / LifeTime), _sourceRectangles.Count - 1));
        spriteBatch.Draw(
            Source,
            Position-cameraPosition, 
            _sourceRectangles[phase],
            new Color((byte)255,(byte)255,(byte)255,(byte)Math.Max(0,128*(LifeTime-Age)/LifeTime)),
            0,
            new Vector2(_sourceRectangles[phase].Width*0.5f,_sourceRectangles[phase].Height*0.5f), 
            1f,
            SpriteEffects.None,
            0.0f
        );
    }
    
}