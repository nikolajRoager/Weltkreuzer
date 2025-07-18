using System;
using System.Collections.Generic;
using MatrosEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace WeltKreuzer.Entities;

public class Turret
{
    /// <summary>
    /// Source of the gun texture
    /// </summary>
    private Texture2D _gunTexture;
    
    /// <summary>
    /// Texture for the gun target/retical 
    /// </summary>
    private Texture2D _target;
    
    /// <summary>
    /// I ended up not animated the turret, this ghost code is left here
    /// </summary>
    private List<Rectangle> _sourceRectangles;
    
    public float Length => _sourceRectangles[0].Width-Origin.X;
    
    
    
    /// <summary>
    /// Position relative to the ship or shore installation
    /// </summary>
    public Vector2 Position {get;set;}
    //Origin on the texture
    public Vector2 Origin;
    
    
    public float MaxRotation {get;set;}
    public float MinRotation {get;set;}
    
    public float Rotation {get;private set;}
    
    public float theta {get;set;}

    /// <summary>
    /// The gun is currently pointing at the target
    /// </summary>
    public bool Aimed {get;private set;}
    
    /// <summary>
    /// Distance we are aiming at
    /// </summary>
    public float Distance {get;private set;}

    /// <summary>
    /// How long till this gun is loaded?
    /// </summary>
    public float ReloadTimer { get; private set; } = 0;
    
    public float MaxReloadTimer { get; private set; } = 4;//15 RPM

    public bool IsLoaded => ReloadTimer <= 0;
    
    
    public SoundEffect BoomSound { get; private set; }

    public void Shoot()
    {
        if (Aimed && IsLoaded)
        {
            Core.Audio.PlaySoundEffect(BoomSound);
            ReloadTimer = MaxReloadTimer;
        }
    }

    public Turret Clone()
    {
        return new Turret(this,Position,MaxRotation,MinRotation);
    }

    public Turret(Texture2D gunTexture,Texture2D target, SoundEffect boomSound, float maxReloadTimer, Vector2 origin)
    {
        BoomSound = boomSound;
        MaxReloadTimer = maxReloadTimer;
        Position = Vector2.Zero;
        Origin = origin;
        _gunTexture = gunTexture;
        _target = target;
        
        MaxRotation = Single.Pi;
        MinRotation =-Single.Pi;
 
        MaxRotation = (MinRotation+Single.Pi*2)%(Single.Pi*2);
        MinRotation = (MinRotation+Single.Pi*2)%(Single.Pi*2);
        
        
        Rotation = (MaxRotation+MinRotation)*0.5f;

        //Some ghost code from when I thought the guns would be animated
        int nframes = 1;
        _sourceRectangles = new();
        for (int i = 0; i < nframes; i++)
            _sourceRectangles.Add(new Rectangle((int)(i*gunTexture.Width*1f/nframes),0,gunTexture.Width/nframes,gunTexture.Height));
    }

    public Turret(Turret other, Vector2 position, float maxRotation, float minRotation)
    {
        BoomSound = other.BoomSound;
        Position = position;
        Origin = other.Origin;
        _gunTexture = other._gunTexture;
        _target = other._target;
        _sourceRectangles = other._sourceRectangles;
        
        MaxRotation = maxRotation;
        MinRotation = minRotation;
        Rotation = (MaxRotation+MinRotation)*0.5f;
        
        MaxRotation =( MaxRotation+Single.Pi*2)%(Single.Pi*2);
        MinRotation = (MinRotation+Single.Pi*2)%(Single.Pi*2);
        
    }

    public void Update(GameTime gameTime)
    {
        if (ReloadTimer > 0)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            ReloadTimer -= dt;

            if (ReloadTimer < 0)
            {
                ReloadTimer = 0;
            }
        }
}

    public void SetTarget(Vector2 target, Vector2 globalPosition, float shipRotation)
    {
        var targetDirection =Vector2.Normalize( target - globalPosition);
        var currentDirection = new Vector2(MathF.Cos(Rotation+shipRotation), MathF.Sin(Rotation+shipRotation));
        var minDirection= new Vector2(MathF.Cos(MinRotation+shipRotation), MathF.Sin(MinRotation+shipRotation));
        var maxDirection= new Vector2(MathF.Cos(MaxRotation+shipRotation), MathF.Sin(MaxRotation+shipRotation));
        
        //Check if the angle is legal
        float detA=targetDirection.X*minDirection.Y-targetDirection.Y*minDirection.X;
        float detB=targetDirection.X*maxDirection.Y-targetDirection.Y*maxDirection.X;

        if (detA < 0 && detB > 0)
        {
            float theta = MathF.Atan2(targetDirection.Y, targetDirection.X);
            Rotation= theta -shipRotation;
            Aimed = true;
            Distance=Vector2.Distance(target,globalPosition);
        }
        else
        {
            Aimed=false;
            Rotation = (MinRotation + MaxRotation)*0.5f;
        }
        
        
        
    }
    
    public void Draw(SpriteBatch spriteBatch, Vector2 globalPosition,float shipRotation, GameTime gameTime,Vector2 cameraPosition)
    {
        theta +=(float)gameTime.ElapsedGameTime.TotalSeconds;

        //float Rotation = MinRotation + (MaxRotation - MinRotation) * 0.5f * (1 + float.Sin(theta));
        
        float globalRotation = Rotation+shipRotation;
        int phase = 0;
        spriteBatch.Draw(
            _gunTexture,
            globalPosition-cameraPosition, 
            _sourceRectangles[phase],
            new Color((byte)255,(byte)255,(byte)255,(byte)255),
            globalRotation,
            Origin,
            1f,
            SpriteEffects.None,
            0.0f
        );

        if (Aimed)
        {
            spriteBatch.Draw(
                _target,
                globalPosition+new Vector2(float.Cos(globalRotation),float.Sin(globalRotation))*Distance-cameraPosition, 
                null,
                new Color((byte)255,(byte)255,(byte)255,(byte)255),
                globalRotation,
                new Vector2(_target.Width/2f, _target.Height/2f),
                1f,
                SpriteEffects.None,
                0.0f
            );
        }
        
        
    }
}