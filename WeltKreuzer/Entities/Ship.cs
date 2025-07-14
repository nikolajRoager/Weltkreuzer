using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WeltKreuzer.Entities;

public class Ship
{
    public Texture2D Texture { get; private set; }
    public Vector2 Position { get; private set; }
    
    /// <summary>
    /// Velocity in meters per second, in reality 20 knots is roughly 12 m/s
    /// In practice we will move 8 times faster because real time speed seems dull for a game
    /// </summary>
    public Vector2 Velocity { get; private set; }
    
    public float Rotation { get; private set; }
    
    public float Omega { get; private set; }

    public Vector2 Forward { get; private set; }
    public Vector2 Port { get; private set; }
    
    
    public float ForwardFriction { get; set; } = 0.1f;
    public float PortFriction { get; set; }=1f;
     
    public float TurnFriction { get; set; } = 1f;

    /// <summary>
    /// Rudder torque per speed(multiply with forward velocity to get torque) 
    /// </summary>
    public float RudderTorquePerSpeed { get; set; } = 0.01f;

    /// <summary>
    /// Max thrust force in arbitrary game units
    /// Can be gotten from max speed as 
    /// MaxThrust=Velocity*ForwardFriction
    /// Going with 21 knot = 12 m/s *8=96 m/s (multiplied by 8 because real-speed is dull)
    /// </summary>
    public float MaxThrust { get; private set; } = 9.6f;
    
    public float ForwardSpeed { get; private set; }

    /// <summary>
    /// Current power level (of thrust), goes from -1 (1/4 reverse thrust) to 4 (full thrust) in steps of 1/4 max power
    /// </summary>
    public int PowerLevel { get; private set; } = 0;

    /// <summary>
    /// Should be -1 (turn to port) 0 (dead ahead) or 1 (turn to starboard)
    /// </summary>
    public int Rudder { get; set; } = 0;
    
    


    public bool AddPower(bool Up)
    {
        Console.WriteLine(PowerLevel);
        if (Up)
        {
            if (PowerLevel < 4)
            {
                PowerLevel++;
                return true;
            }
            else
            {
                return false;
            }
            
        }
        else
        {
            if (PowerLevel < 0)
            {
                
                return false;
            }
            else
            {
                PowerLevel--;
                return true;
            }
        }
    }
    

    public Ship(Texture2D texture, Vector2 position, float speed, float rotation=Single.Pi*0.25f)
    {
        Texture = texture;
        Position = position;
        Velocity = new Vector2(speed, 0);
        
        Rotation = rotation;
        Omega = 0.4f;
        
        Forward = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
        Port = new Vector2(-Forward.Y, Forward.X);
    }

    public void Update(GameTime time)
    {
        float dt = (float)time.ElapsedGameTime.TotalSeconds;
        Forward = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
        Port = new Vector2(-Forward.Y, Forward.X);
        
        //velocity projected onto forward or port direction
        ForwardSpeed= Vector2.Dot(Velocity, Forward);
        var forwardVelocity = Forward*ForwardSpeed;
        var portVelocity = Port*Vector2.Dot(Velocity, Port);
        
        //Apply friction
        Velocity -= forwardVelocity * ForwardFriction*dt;
        //Starboard friction works too, it is just negative port friction
        Velocity -= portVelocity * PortFriction*dt;
        
        //Apply forward thrust
        Velocity += Forward * dt * MaxThrust*PowerLevel*0.25f;

        Omega += ForwardSpeed*RudderTorquePerSpeed*dt*Rudder;
        
        Omega -= Omega*dt*TurnFriction;
        
        Rotation += Omega * dt;
        //Apply to position, 1st degree motion only
        Position += Velocity * dt;

    }

    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
        
        Texture,
        Position, 
        null,
        Color.White,
        Rotation,
        Vector2.Zero, 
        1f,
        SpriteEffects.None,
        0.0f
        
        );
    }
    
    
}