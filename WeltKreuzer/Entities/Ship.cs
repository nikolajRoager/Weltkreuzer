using System;
using System.Collections.Generic;
using MatrosEngine;
using MatrosEngine.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace WeltKreuzer.Entities;

public class Ship
{
    public Texture2D DirectionMarker { get; private set; }
    
    public Texture2D Texture { get; private set; }
    
    /// <summary>
    /// How many frames for the ship sinking do we have
    /// </summary>
    public int SinkFrames { get; private set; }
    
    private List<Rectangle> _rectangles;

    /// <summary>
    /// Lengthwise compartments, there is always port and starboard compartments
    /// </summary>
    public int LengthCompartments;
    public int[,] CompartmentsDamage;

    
    
    /// <summary>
    /// Gets total number of flooded compartments
    /// </summary>
    public int FloodedCompartments
    {
        get
        {
            int flooded=0;
            foreach (var i in CompartmentsDamage)
            {
                if (i>=CompartmentMaxDamage)
                    ++flooded;
            }
            return flooded;
        }
    }

    public int UnbalancedCompartments
    {
        get
        {
            int portCompartmentsFlooded = 0;
            for (int i = 0; i < LengthCompartments; i++)
            {
                portCompartmentsFlooded += CompartmentsDamage[i,0]==CompartmentMaxDamage ? 1 : 0;
                portCompartmentsFlooded -= CompartmentsDamage[i,1]==CompartmentMaxDamage ? 1 : 0;
            }
            return Math.Abs(portCompartmentsFlooded);
        }
    }

    /// <summary>
    /// If more than half our compartments are flooded we are transfering to the uboot arm
    /// </summary>
    public int MaxFlooding => LengthCompartments;

    
    
    /// <summary>
    /// The ship is sinking or has sunk, I decided against allowing capsizing, too much for the player to keep track off
    /// </summary>
    public bool IsSinking => FloodedCompartments >= MaxFlooding;// || UnbalancedCompartments >= LengthCompartments/2+1;
    private bool _playingSinkingAnimation = false;
    
    
    /// <summary>
    /// Damage level which causes compartment to flood
    /// </summary>
    public int CompartmentMaxDamage = 3;
    
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


    /// <summary>
    /// Number of second betwixt fire from damaged compartments
    /// </summary>
    public float DamageCompartmentFireTimerMax=0.05f;
    private float _damageCompartmentFireTimer;

    
    public float FoamTimerMax;
    private float _foamTimer;
 
    //Form going out from a sinking ship use a different timer
    private float _sinkingFoamTimer;
    
    
    /// <summary>
    /// All ships take 2 seconds to go down
    /// </summary>
    private readonly float _maxSinkingTimer =2;
    public float SinkingTimer { get; private set; }=2;

    public bool IsSunk => IsSinking && SinkingTimer < 0;
    
    
    
    public List<Turret> Turrets { get; private set; }
    
    
    public Captain MyCaptain { get; set; }
    
    
    //Shall the ship shoot this update cycle
    public bool ShallShoot { get; set; } = false;
    
    public bool ShallTorpedo { get; set; } = false;
    
    public int NTorpedoes;
    public int TorpedoSpread;
    
    public Vector2 Target { get; set; }=Vector2.Zero;
    
    public void Shoot(ICollection<Particle> smoke, ParticleTemplate smokeTemplate, ICollection<Shell> shells)
    {
        
        Random r = new Random();
        
        foreach (var turret in Turrets)
        {
            if (turret.IsLoaded && turret.Aimed)
            {
                turret.Shoot();
                {
                    
                    //Angle offset addition, from -1 to 1 if power level is high (engine vibrations
                    // -0.3 to 0.3 otherwise
                    float randomAngleOffset = 2f*(r.NextSingle()-0.5f);
                    
                    //+- 10% distance offset at high power, +-3% otherwise
                    float randomDistanceOffset = 0.1f*(r.NextSingle()-0.5f);
                    if (PowerLevel < 4)
                    {
                        randomAngleOffset *= 0.3f;
                        randomDistanceOffset*= 0.3f;
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
                    shells.Add(new Shell(ShellTexture,pos,gunFacingVector*ShellSpeed, (1+randomDistanceOffset)*turret.Distance/ShellSpeed));
                }
            }
        }
    }
    
    public bool AddPower(bool Up)
    {
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
        Target =target;
        foreach (var turret in Turrets)
        {
            turret.SetTarget(target,Position+turret.Position.X*Forward+turret.Position.Y*Port,Rotation);
        }
    }


    private Texture2D _torpedoAim;

    public Ship(Texture2D texture, Texture2D shellTexture, Texture2D torpedoAim, Texture2D directionMarker, List<Vector2> funnelLocations, List<Turret> turrets,float maxThrust,float rudderTorquePerSpeed,float turnFriction,float portFriction,float forwardFriction, int lengthCompartments, int sinkFrames, int compartmentMaxDamage, int nTorpedoes, int torpedoSpread, Captain myCaptain)
    {
        NTorpedoes = nTorpedoes;
        TorpedoSpread = torpedoSpread;
        _torpedoAim=torpedoAim;
        
        MyCaptain = myCaptain;
        myCaptain.TakeCommand(this);
        DirectionMarker = directionMarker;
        
        //Start at cruising speed
        PowerLevel = 3;
        CompartmentMaxDamage = compartmentMaxDamage;
        LengthCompartments=lengthCompartments;
        CompartmentsDamage=new int[LengthCompartments,2];
        for (int i = 0; i < LengthCompartments; i++)
            for (int j = 0; j < 2; j++)
                CompartmentsDamage[i,j]=0;
        
        Texture = texture;
        ShellTexture = shellTexture;
        Turrets = turrets;
        FunnelLocations = funnelLocations;
        MaxThrust = maxThrust;
        RudderTorquePerSpeed = rudderTorquePerSpeed;
        TurnFriction = turnFriction;
        PortFriction = portFriction;
        ForwardFriction = forwardFriction;
        SinkFrames = sinkFrames;
        
        
        _rectangles=new List<Rectangle>();
        for (int i = 0; i < sinkFrames+1; ++i)
        {
            _rectangles.Add(new Rectangle(i*Texture.Width/(sinkFrames+1),0,Texture.Width/(sinkFrames+1),Texture.Height));
        }
        

        Rotation = 0.0f;
        Omega = 0.0f;
        
        Forward = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));
        Port = new Vector2(-Forward.Y, Forward.X);
        
        FunnnelSmokeTimerMax=0.2f;
        _funnelSmokeTimer=FunnnelSmokeTimerMax;

        DamageCompartmentFireTimerMax = 0.1f;
        _damageCompartmentFireTimer=DamageCompartmentFireTimerMax;
        
        FoamTimerMax=0.1f;
        _foamTimer=FoamTimerMax;
        _sinkingFoamTimer=FoamTimerMax;
        
    }

    public Ship Clone(Captain newCaptain,Vector2 position, float rotation)
    {
        var clonedTurrets = new List<Turret>();
        foreach (var turret in Turrets)
        {
            clonedTurrets.Add(turret.Clone());
        }
        var Out = new Ship(Texture,ShellTexture,_torpedoAim,DirectionMarker,FunnelLocations,clonedTurrets,MaxThrust,RudderTorquePerSpeed,TurnFriction,PortFriction,ForwardFriction,LengthCompartments,SinkFrames,CompartmentMaxDamage,NTorpedoes,TorpedoSpread,newCaptain);
        Out.Position=position;
        Out.Rotation=rotation;
        
        return Out;
    }
    public bool HitTest(Shell shell)
    {
        var vecToShell = shell.Position - Position;
        float distForward = Vector2.Dot(vecToShell, Forward);
        float distPort = Vector2.Dot(vecToShell, Port);


        if (distForward > -_rectangles[0].Width* 0.5f && distForward < _rectangles[0].Width* 0.5f
                                                      &&
                                                      distPort > -Texture.Height * 0.5f && distPort < Texture.Height * 0.5f
           )
        {
            //Test which compartment has been hit
            int nCompartmentLength=(int)(distForward + _rectangles[0].Width* 0.5f) * LengthCompartments / _rectangles[0].Width;
            if (distPort > 0) //Starboard hit
            {
                CompartmentsDamage[nCompartmentLength, 1]= Math.Min(CompartmentMaxDamage,
                    CompartmentsDamage[nCompartmentLength, 1]+ 1);
            }
            else
            {
                CompartmentsDamage[nCompartmentLength, 0]= Math.Min(CompartmentMaxDamage,
                    CompartmentsDamage[nCompartmentLength, 0]+ 1);
            }
            
            return true;
        }
        
        return false;
    }


    public bool HitTest(Torpedo Torp)
    {
        //No self torpedoing
        if (Torp.Owner == this)
            return false;
        
        var vecToShell = Torp.Position - Position;
        float distForward = Vector2.Dot(vecToShell, Forward);
        float distPort = Vector2.Dot(vecToShell, Port);


        if (distForward > -_rectangles[0].Width* 0.5f && distForward < _rectangles[0].Width* 0.5f
                                                                    &&
                                                                    distPort > -Texture.Height * 0.5f && distPort < Texture.Height * 0.5f
           )
        {
            //Test which compartment has been hit
            int nCompartmentLength=(int)(distForward + _rectangles[0].Width* 0.5f) * LengthCompartments / _rectangles[0].Width;
            if (distPort > 0) //Starboard hit
            {
                CompartmentsDamage[nCompartmentLength, 1] = CompartmentMaxDamage;
                CompartmentsDamage[nCompartmentLength, 0]= Math.Min(CompartmentMaxDamage,
                    CompartmentsDamage[nCompartmentLength, 0]+ 1);

                if (nCompartmentLength > 0)
                {
                    CompartmentsDamage[nCompartmentLength-1, 1]= Math.Min(CompartmentMaxDamage,
                        CompartmentsDamage[nCompartmentLength-1, 1]+ 1);
                }
                if (nCompartmentLength+1 < LengthCompartments)
                {
                    CompartmentsDamage[nCompartmentLength+1, 1]= Math.Min(CompartmentMaxDamage,
                        CompartmentsDamage[nCompartmentLength+1, 1]+ 1);
                }
            }
            else
            {
                CompartmentsDamage[nCompartmentLength, 0] = CompartmentMaxDamage;
                CompartmentsDamage[nCompartmentLength, 1]= Math.Min(CompartmentMaxDamage,
                    CompartmentsDamage[nCompartmentLength, 1]+ 1);
                if (nCompartmentLength > 0)
                {
                    CompartmentsDamage[nCompartmentLength-1, 0]= Math.Min(CompartmentMaxDamage,
                        CompartmentsDamage[nCompartmentLength-1, 0]+ 1);
                }
                if (nCompartmentLength+1 < LengthCompartments)
                {
                    CompartmentsDamage[nCompartmentLength+1, 0]= Math.Min(CompartmentMaxDamage,
                        CompartmentsDamage[nCompartmentLength+1, 0]+ 1);
                }
            }
            
            return true;
        }
        
        return false;
    }

    public void Update(GameTime time,ICollection<Particle> smoke, ParticleTemplate smokeTemplate, ICollection<Shell> shells, ICollection<Torpedo> torpedoes, Ship playerShip, IEnumerable<Ship> enemyShips)
    {
        float dt = (float)time.ElapsedGameTime.TotalSeconds;
        
        MyCaptain.ViewShips(playerShip,enemyShips);
        MyCaptain.Control();

        if (ShallShoot)
        {
            ShallShoot=false;
            Shoot(smoke,smokeTemplate,shells);
        }
        if (ShallTorpedo && NTorpedoes>0)
        {
            ShallTorpedo=false;

            var dir = Vector2.Normalize(Target - Position);
            //Torpedoes are only permitted within +-45% of port or in other words
            
            float portDir = Vector2.Dot(dir, Port);
            float forwardDir = Vector2.Dot(dir, Forward);

            if (MathF.Abs(forwardDir) < MathF.Abs(portDir))
            {
                float ang;
                //Custom spread patterns for each spread type
                switch (Math.Min(TorpedoSpread,NTorpedoes))
                {
                    case 0:
                        break;
                    default:
                    case 1:
                        torpedoes.Add(new Torpedo(this,Position, dir*136, 10)); 
                        break;
                    case 2:
                        //Torpedoes offset by roughly 5 degrees (0.1 radians)
                        ang = MathF.Atan2(dir.Y, dir.X);
                        torpedoes.Add(new Torpedo(this,Position, new Vector2(MathF.Cos(ang+0.05f),MathF.Sin(ang+0.05f))*136, 10)); 
                        torpedoes.Add(new Torpedo(this,Position, new Vector2(MathF.Cos(ang-0.05f),MathF.Sin(ang-0.05f))*136, 10));
                        break;
                    case 3:
                        //1 torpedo on target, flanked by two at 5 degrees off
                        ang = MathF.Atan2(dir.Y, dir.X);
                        torpedoes.Add(new Torpedo(this,Position, dir*136, 10)); 
                        torpedoes.Add(new Torpedo(this,Position, new Vector2(MathF.Cos(ang+0.1f),MathF.Sin(ang+0.1f))*136, 10)); 
                        torpedoes.Add(new Torpedo(this,Position, new Vector2(MathF.Cos(ang-0.1f),MathF.Sin(ang-0.1f))*136, 10));
                        break;
                    case 4:
                        //A total spread of 10 degrees
                        ang = MathF.Atan2(dir.Y, dir.X);
                        torpedoes.Add(new Torpedo(this,Position, new Vector2(MathF.Cos(ang+0.33f),MathF.Sin(ang+0.33f))*136, 10)); 
                        torpedoes.Add(new Torpedo(this,Position, new Vector2(MathF.Cos(ang-0.33f),MathF.Sin(ang-0.33f))*136, 10));
                        torpedoes.Add(new Torpedo(this,Position, new Vector2(MathF.Cos(ang+0.1f),MathF.Sin(ang+0.1f))*136, 10)); 
                        torpedoes.Add(new Torpedo(this,Position, new Vector2(MathF.Cos(ang-0.1f),MathF.Sin(ang-0.1f))*136, 10));
                        break;
                }

                NTorpedoes-=Math.Min(TorpedoSpread, NTorpedoes);

            }
            
        }
        
        
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


        float DamageSpeedReduction = (MaxFlooding-FloodedCompartments) / (float)(MaxFlooding);
        //Apply forward thrust
        Velocity += Forward * dt * MaxThrust*PowerLevel*0.25f*DamageSpeedReduction;

        Omega += ForwardSpeed*RudderTorquePerSpeed*dt*Rudder;
        
        Omega -= Omega*dt*TurnFriction;
        
        Rotation += Omega * dt;
        //Apply to position, 1st degree motion only
        Position += Velocity * dt;

        if (!IsSinking)
        {


            foreach (var turret in Turrets)
                turret.Update(time);
        }
        else
        {
            //Engine fail when sinking
            PowerLevel = 0;
            
            //This is the first time we noticed we are sinking
            if (!_playingSinkingAnimation)
            {
                _playingSinkingAnimation = true;

            }

            SinkingTimer -= dt;
        }

    }

    /// <summary>
    /// The ship may spawn smoke through its funnels
    /// </summary>
    /// <param name="particles"></param>
    public void SpawnSmoke(ICollection<Particle> particles, ParticleTemplate template, GameTime time)
    {
        float dt = (float)time.ElapsedGameTime.TotalSeconds;

        //Faster ships make more foam
        _funnelSmokeTimer -= dt * Math.Abs(PowerLevel) * 0.25f*FunnelLocations.Count;
        _damageCompartmentFireTimer -= dt;

        if (!IsSinking || SinkingTimer> 0)
        {
            if (_funnelSmokeTimer < 0 && FunnelLocations.Count > 0)
            {
                Random r = new Random();

                var funnel = FunnelLocations[r.Next(FunnelLocations.Count)];

                particles.Add(new Particle(template.Source, Position + funnel.X * Forward + funnel.Y * Port,
                    template.Nframes, template.LifeTime, template.Friction, template.Wind, Velocity * 0.5f));
                //Reset timer and make a puff of smoke
                _funnelSmokeTimer = FunnnelSmokeTimerMax;
            }

            if (_damageCompartmentFireTimer < 0)
            {
                //Each compartment has a dmg/max damage chance of spawning smoke
                Random r = new Random();

                for (int y = 0; y < 2; ++y)
                for (int x = 0; x < LengthCompartments; ++x)
                {
                    int roll = r.Next(0, CompartmentMaxDamage);
                    if (roll < CompartmentsDamage[x, y])
                    {
                        var pos = Position
                                  - _rectangles[0].Width * 0.5f * Forward
                                  - _rectangles[0].Height * 0.5f * Port
                                  + (x + r.NextSingle()) * Forward * _rectangles[0].Width / LengthCompartments
                                  + (y + r.NextSingle()) * Port * _rectangles[0].Height * 0.5f
                            ;

                        particles.Add(new Particle(template.Source, pos, template.Nframes, template.LifeTime,
                            template.Friction, template.Wind, Velocity * 0.5f));

                    }
                }

                _damageCompartmentFireTimer = DamageCompartmentFireTimerMax;
            }
        }
    }
    public void SpawnFoam(ICollection<Particle> particles, ParticleTemplate template, GameTime time)
    {
        float dt = (float)time.ElapsedGameTime.TotalSeconds;

        //Subtract timer from smoke timer depending on power level
        _foamTimer -= dt*Math.Abs(ForwardSpeed/50) ;
        
        if (_foamTimer < 0 && !(IsSinking && SinkingTimer <=0))
        {
            
            particles.Add(new Particle(template.Source,Position+_rectangles[0].Width*Forward*0.5f,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f+Port*10));
            particles.Add(new Particle(template.Source,Position+_rectangles[0].Width*Forward*0.5f,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f-Port*10));
            
            particles.Add(new Particle(template.Source,Position-_rectangles[0].Width*Forward*0.5f,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f+Port*10));
            particles.Add(new Particle(template.Source,Position-_rectangles[0].Width*Forward*0.5f,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f-Port*10));
            //Reset timer and make a puff of smoke
            _foamTimer =FoamTimerMax;
        }

        //If is sinking, but has not slipped beneath the waves yet
        if (IsSinking && SinkingTimer >0)
        {
            _sinkingFoamTimer -= dt*100;

            if (_sinkingFoamTimer < 0)
            {
                Random r = new Random();
                float X = (r.NextSingle()-0.5f)*_rectangles[0].Width;
                float Y = (r.NextSingle()-0.5f)*_rectangles[0].Height;

                var spawnLocation = X*Forward + Y*Port;
 //               particles.Add(new Particle(template.Source,Position+spawnLocation,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f));
                particles.Add(new Particle(template.Source,Position+spawnLocation,template.Nframes,template.LifeTime, template.Friction,template.Wind,Velocity*0.5f+Vector2.Normalize(spawnLocation)*0.5f));
                
                _sinkingFoamTimer=FoamTimerMax;
            }
        }
    }

    
    
    public void Draw(SpriteBatch spriteBatch, GameTime time,Vector2 cameraPosition)
    {
        if (!IsSinking)
        {
            spriteBatch.Draw(
                Texture,
                Position-cameraPosition, 
                _rectangles[0],
                Color.White,
                Rotation,
                new Vector2(_rectangles[0].Width*0.5f, Texture.Height*0.5f), 
                1f,
                SpriteEffects.None,
                0.0f
            );

            foreach (var turret in Turrets)
            {
                turret.Draw(spriteBatch,Position+turret.Position.X*Forward+turret.Position.Y*Port,Rotation,time,cameraPosition);
            }
            
            //My captain may have some debug drawing functions
            MyCaptain.Draw(spriteBatch,DirectionMarker,Position-cameraPosition);

            //Mark torpedo
            if (NTorpedoes > 0)
            {
                var dir = Vector2.Normalize(Target - Position);
                //Torpedoes are only permitted within +-45% of port or in other words

                float portDir = Vector2.Dot(dir, Port);
                float forwardDir = Vector2.Dot(dir, Forward);

                if (MathF.Abs(forwardDir) < MathF.Abs(portDir))
                {
                    spriteBatch.Draw(
                        _torpedoAim,
                        Target-cameraPosition,
                        null,
                        new Color((byte)255,(byte)255,(byte)255,(byte)255),
                        0,
                        new Vector2(_torpedoAim.Width/2f, _torpedoAim.Height/2f),
                        1f,
                        SpriteEffects.None,
                        0.0f
                    );
                }
            }
        }
        else if (SinkingTimer > 0)
        {
            int frame =SinkFrames-(int) (SinkFrames *SinkingTimer/_maxSinkingTimer);
            
            spriteBatch.Draw(
                Texture,
                Position-cameraPosition, 
                _rectangles[frame],
                Color.White,
                Rotation,
                new Vector2(_rectangles[0].Width*0.5f, Texture.Height*0.5f), 
                1f,
                SpriteEffects.None,
                0.0f
            );
            if (frame < SinkFrames/2)
                foreach (var turret in Turrets)
                {
                    turret.Draw(spriteBatch,Position+turret.Position.X*Forward+turret.Position.Y*Port,Rotation,time,cameraPosition);
                }
        }
    }
}