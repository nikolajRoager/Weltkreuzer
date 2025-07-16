using System;
using System.Collections.Generic;
using MatrosEngine.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WeltKreuzer.Entities;

public class Ship
{
    public Texture2D Texture { get; private set; }
    
    /// <summary>
    /// texture of the shells fired by this ship. Each ship may use its own type of shell
    /// </summary>
    public Texture2D ShellTexture { get; private set; }

    /// <summary>
    /// The speed of shell this ship shoots
    /// </summary>
    public float ShellSpeed { get; set; } = 300;//Half real-life
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


    /// <summary>
    /// Locations of the funnels which may spawn particles
    /// </summary>
    public List<Vector2> FunnelLocations;



    /// <summary>
    /// Number of seconds betwixt puffs of smoke at full power
    /// </summary>
    public float FunnnelSmokeTimerMax;
    /// <summary>
    /// Number of seconds until the next puff
    /// </summary>
    private float _funnelSmokeTimer;

    
    public float FoamTimerMax;
    private float _foamTimer;
    
    
    
    public List<Turret> Turrets { get; private set; }


    public void Shoot(ICollection<Particle> smoke, ParticleTemplate smokeTemplate, ICollection<Shell> shells)
    {
        Console.WriteLine("Fire");
        
        Random r = new Random();
        
        foreach (var turret in Turrets)
        {
            if (turret.IsLoaded && turret.Aimed)
            {
                turret.Shoot();
                Console.WriteLine("Boom");
                {
                    
                    //Angle offset factor, from -1 to 1 if power level is high (engine vibrations
                    // -0.3 to 0.3 otherwise
                    float randomAngleOffset = 2f*(r.NextSingle()-0.5f);
                    if (PowerLevel < 4)
                    {
                        randomAngleOffset *= 0.3f;
                    }
                    //May be +- 5 degree
                    randomAngleOffset *= 5*Single.Pi/180;
                    
                    var gunFacing = Rotation+randomAngleOffset  + turret.Rotation;

                    var gunFacingVector = new Vector2(MathF.Cos(gunFacing), MathF.Sin(gunFacing));
                    
                    var pos = Position + turret.Position.X * Forward + turret.Position.Y * Port
                              + gunFacingVector*turret.Length;
                    
                    //Spawn one big puff of smoke
                    for (int angle = -5; angle <= 5; angle++)
                    {
                        smoke.Add(new Particle(smokeTemplate.Source,pos,smokeTemplate.Nframes,smokeTemplate.LifeTime, smokeTemplate.Friction,smokeTemplate.Wind,Velocity+new Vector2(MathF.Cos(gunFacing+angle*Single.Pi/5f), MathF.Sin(gunFacing+angle*Single.Pi/5f))*5+gunFacingVector*10));
                    }
                    
                    //Spawn a shell moving really rather quick
                    shells.Add(new Shell(ShellTexture,pos,gunFacingVector*ShellSpeed, turret.Distance/ShellSpeed));
                }
            }
        }
    }
    
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


    public void SetTarget(Vector2 target)
    {
        foreach (var turret in Turrets)
        {
            turret.SetTarget(target,Position+turret.Position.X*Forward+turret.Position.Y*Port,Rotation);
        }
    }


    
    /// <summary>
    /// Default constructor, creates Emden
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="shellTexture"></param>
    /// <param name="position"></param>
    /// <param name="speed"></param>
    /// <param name="turretTemplate"></param>
    /// <param name="rotation"></param>
    public Ship(Texture2D texture, Texture2D shellTexture, Vector2 position, float speed, Turret turretTemplate,float rotation=0)
    {
        
        ShellTexture = shellTexture;
        
        Texture = texture;
        Position = position;
        Velocity = new Vector2(speed, 0);

        Turrets = new List<Turret>()
        {
          new Turret(turretTemplate,new Vector2(-50,4),Single.Pi+0.5f,Single.Pi -1.5f),
          new Turret(turretTemplate,new Vector2(-50,-2),Single.Pi+1.5f,Single.Pi-0.5f),
          
          new Turret(turretTemplate,new Vector2(-36,6),Single.Pi, Single.Pi-2),
          new Turret(turretTemplate,new Vector2(-36,-6),Single.Pi+2,Single.Pi),
          
          new Turret(turretTemplate,new Vector2(0,7),Single.Pi*0.7f, Single.Pi*0.3f),
          new Turret(turretTemplate,new Vector2(0,-7),-Single.Pi*0.3f, -Single.Pi*0.7f),
          
          new Turret(turretTemplate,new Vector2(36,6),2,0),
          new Turret(turretTemplate,new Vector2(36,-6),0,-2),
          
          new Turret(turretTemplate,new Vector2(50,4),1.5f,-0.5f),
          new Turret(turretTemplate,new Vector2(50,-2),0.5f,-1.5f),
            
        };
        
        
        Rotation = rotation;
        Omega = 0.0f;
        
        Forward = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
        Port = new Vector2(-Forward.Y, Forward.X);

        FunnelLocations = new ()
        {
            new Vector2(-2,0),
            new Vector2(10,0),
            new Vector2(24,0),
        };
        
        FunnnelSmokeTimerMax=0.1f;
        _funnelSmokeTimer=FunnnelSmokeTimerMax;
        
        FoamTimerMax=0.1f;
        _foamTimer=FoamTimerMax;
        
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

        
        foreach (var turret in Turrets)
            turret.Update(time);
        
    }

    /// <summary>
    /// The ship may spawn smoke through its funnels
    /// </summary>
    /// <param name="particles"></param>
    public void SpawnSmoke(ICollection<Particle> particles, ParticleTemplate template, GameTime time)
    {
        float dt = (float)time.ElapsedGameTime.TotalSeconds;

        //Faster ships make more foam
        _funnelSmokeTimer -= dt * Math.Abs(PowerLevel) * 0.25f;

        if (_funnelSmokeTimer < 0)
        {
            Random r = new Random();
            
            var funnel = FunnelLocations[r.Next(FunnelLocations.Count)];
            
            particles.Add(new Particle(template.Source,Position+funnel.X*Forward+funnel.Y*Port,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f));
            //Reset timer and make a puff of smoke
            _funnelSmokeTimer=FunnnelSmokeTimerMax;
        }
    }
    public void SpawnFoam(ICollection<Particle> particles, ParticleTemplate template, GameTime time)
    {
        float dt = (float)time.ElapsedGameTime.TotalSeconds;

        //Subtract timer from smoke timer depending on power level
        _foamTimer -= dt*Math.Abs(ForwardSpeed/50) ;

        if (_foamTimer < 0)
        {
            particles.Add(new Particle(template.Source,Position+Texture.Width*Forward*0.5f,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f+Port*10));
            particles.Add(new Particle(template.Source,Position+Texture.Width*Forward*0.5f,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f-Port*10));
            
            particles.Add(new Particle(template.Source,Position-Texture.Width*Forward*0.5f,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f+Port*10));
            particles.Add(new Particle(template.Source,Position-Texture.Width*Forward*0.5f,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f-Port*10));
            //Reset timer and make a puff of smoke
            _foamTimer =FoamTimerMax;
        }
    }

    
    
    public void Draw(SpriteBatch spriteBatch, GameTime time)
    {
        spriteBatch.Draw(
            Texture,
            Position, 
            null,
            Color.White,
            Rotation,
            new Vector2(Texture.Width*0.5f, Texture.Height*0.5f), 
            1f,
            SpriteEffects.None,
            0.0f
        );


        foreach (var turret in Turrets)
        {
            turret.Draw(spriteBatch,Position+turret.Position.X*Forward+turret.Position.Y*Port,Rotation,time);
        }
    }
    
    
}