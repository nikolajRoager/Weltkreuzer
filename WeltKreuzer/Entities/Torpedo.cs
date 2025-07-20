using System.Net.Sockets;
using Microsoft.Xna.Framework;

namespace WeltKreuzer.Entities;

/// <summary>
/// A torpedo is much like
/// </summary>
public class Torpedo
{
    public Vector2 Position {get; set;}
    public Vector2 Velocity {get; set;}
    /// <summary>
    /// Time remaining till self-detonation
    /// </summary>
    public float Time {get; set;}
    
    public bool IsSplash => Time <= 0;
    
    public Ship Owner {get; set;}

    
    public float FoamTimerMax;
    private float _foamTimer;

    public bool ShouldSpawnFoam { get; set; } = false;
    public Torpedo(Ship owner,Vector2 position, Vector2 velocity, float time)
    {
        Owner = owner;
        FoamTimerMax = 0.05f;
        _foamTimer=FoamTimerMax;
        Position = position;
        Velocity = velocity;
        Time = time;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsSplash)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _foamTimer -= dt;

            if (_foamTimer < 0)
            {
                _foamTimer=FoamTimerMax;
                ShouldSpawnFoam = true;
            }
            else
            {
                ShouldSpawnFoam = false;
            }
            
            
            //The shell will be removed if it is dead
            Time -= dt;
            
            Position+=Velocity*dt;
        }
    }

    
}