using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using MatrosEngine;
using MatrosEngine.Input;
using MatrosEngine.Particles;
using MatrosEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WeltKreuzer.Entities;

namespace WeltKreuzer.Scenes;

public class LevelScene : Scene
{
    private Ship _Emden;
    private LinkedList<Ship> _EnemyShips;

    private Texture2D _EmdenCompartments;

    
    private Texture2D _powerIndicator;
    private Texture2D _powerIndicatorHandle;

    private Texture2D _speedIndicatorHandle;

    private Texture2D _wheel;
    private Texture2D _shellTexture;


    private SoundEffect _splashSound;

    /// <summary>
    /// Sound of the player's engine is handled in the scene, not the ship
    /// </summary>
    private SoundEffect _engineSound;
    private SoundEffectInstance _engineSoundInstance;
    
    private SoundEffect _ambience;
    private SoundEffectInstance _ambienceInstance;

    private SoundEffect _shellExplosionSound;
    
    
    
    private float _powerIndicatorAngle;
    
    private float _wheelAngle;
    
    private SpriteFont _font;

    
    /// <summary>
    /// Linked list, for easier deletion of dead particles, a deque would be better
    /// I use seperate lists, so we can draw particles seperately based on their type (reduce texture swapping, improve performance, so I have heard)
    /// </summary>
    private LinkedList<Particle> _smoke;
    /// <summary>
    /// Linked list, for easier deletion of dead particles, a deque would be better
    /// I use seperate lists, so we can draw particles seperately based on their type (reduce texture swapping, improve performance, so I have heard)
    /// </summary>
    private LinkedList<Particle> _explosions;
    /// <summary>
    /// Linked list, for easier deletion of dead particles, a deque would be better
    /// I use seperate lists, so we can draw particles seperately based on their type (reduce texture swapping, improve performance, so I have heard)
    /// </summary>
    private LinkedList<Particle> _foam;

    /// <summary>
    /// Shells in the air
    /// </summary>
    private LinkedList<Shell> _shells;
    
    
    public override void Initialize()
    {
        _smoke = new LinkedList<Particle>();
        _foam = new LinkedList<Particle>();
        _explosions=new LinkedList<Particle>();
        _shells = new LinkedList<Shell>();
        _EnemyShips = new LinkedList<Ship>();
        
        // LoadContent is called during base.Initialize().
        base.Initialize();
    }

    
    private ParticleTemplate _explosionTemplate;
    private ParticleTemplate _smokeTemplate;
    private ParticleTemplate _foamTemplate;

    public LevelScene()
    {
    }

    public override void LoadContent()
    {

        var aimingDot = Content.Load<Texture2D>("images/debugDot");
        
        

        _EmdenCompartments=Content.Load<Texture2D>("images/EmdenCompartments");

        _shellExplosionSound = Content.Load<SoundEffect>("Audio/621002__samsterbirdies__cannon-explosion-sound-3");
        
        SoundEffect CannonSound = Content.Load<SoundEffect>("Audio/184650__isaac200000__cannon1");
        _splashSound = Content.Load<SoundEffect>("Audio/519008__sheyvan__water-explosion");
        _engineSound = Content.Load<SoundEffect>("Audio/594215__steaq__big-steam-engine-perfect-loop-24bit-flac");
        _engineSoundInstance=Core.Audio.PlaySoundEffect(_engineSound,0f,0,0,true);
        
        
        _ambience = Content.Load<SoundEffect>("Audio/753972__klankbeeld__coast-far-shipping-1106-am-220905_0526");
        _ambienceInstance=Core.Audio.PlaySoundEffect(_ambience,1f,0,0,true);
        
        
        //Load a lot of pictures
        _smokeTemplate = new ParticleTemplate(Content.Load<Texture2D>("images/smoke"),4,4,0.2f,new Vector2(100,100));
        
        
        _foamTemplate = new ParticleTemplate(Content.Load<Texture2D>("images/foam"),4,2,0.5f,new Vector2(0,0));
        
        _explosionTemplate= new ParticleTemplate(Content.Load<Texture2D>("images/Explosion"),4,1,0.5f,new Vector2(100,100));
        
        
        _powerIndicator= Content.Load<Texture2D>("images/powerIndicator");
        _powerIndicatorHandle= Content.Load<Texture2D>("images/powerIndicatorHandle");
        _speedIndicatorHandle= Content.Load<Texture2D>("images/SpeedHandle");
        _wheel = Content.Load<Texture2D>("images/Wheel");
        
        _font = Core.Content.Load<SpriteFont>("fonts/normalFont");
        
        //Load all ships into memory
        string shipFilePath = Path.Combine(Core.Content.RootDirectory, "ships/AllShips.xml");

        
        _shellTexture = Content.Load<Texture2D>("images/projectile");
        
        
        Dictionary<string, Ship> ships = new Dictionary<string, Ship>();
        Dictionary<string, Turret> turrets = new Dictionary<string, Turret>();
        
        using (Stream stream = TitleContainer.OpenStream(shipFilePath))
        {
            using (XmlReader reader = XmlReader.Create(stream))
            {
                XDocument doc = XDocument.Load(reader);
                XElement root = doc.Root;

                foreach(XElement ship in root.Elements("ship"))
                {
                    var Class = ship.Attribute("class")?.Value ?? "null";
                    var shipTexture = Content.Load<Texture2D>("images/"+Class);
                    
                    List<Turret> shipTurrets = new List<Turret>();
                    
                    //Read all funnel locations
                    List<Vector2> funnelLocations = new List<Vector2>();
                    foreach(XElement funnel in ship.Elements("funnel"))
                    {
                        funnelLocations.Add(new Vector2(float.Parse(funnel.Attribute("X").Value),float.Parse(funnel.Attribute("Y").Value)));
                    }

                    foreach (XElement turret in ship.Elements("turret"))
                    {
                        string model = turret.Attribute("Model")?.Value ?? "null";
                        //If this already exists
                        Turret turretType;
                        if (turrets.ContainsKey(model))
                        {
                            turretType = turrets[model];
                        }
                        else
                        {
                            turretType = new Turret(Content.Load<Texture2D>("images/"+model),aimingDot,CannonSound ,float.Parse(turret.Attribute("ReloadTime")?.Value ?? "4"),new Vector2(float.Parse(turret.Attribute("OriginX")?.Value ?? "0"),float.Parse(turret.Attribute("OriginY")?.Value ?? "0")));
                            //Save for later re-usage
                            turrets[model] = turretType;
                            
                        }
                        
                        float MaxRotation = float.Parse(turret.Attribute("maxRotation")?.Value ?? "0");
                        float MinRotation = float.Parse(turret.Attribute("minRotation")?.Value ?? "0");
                        Vector2 turretLocation = new Vector2(float.Parse(turret.Attribute("X")?.Value ?? "0"),
                            float.Parse(turret.Attribute("Y")?.Value ?? "0"));
                        
                        shipTurrets.Add(new Turret(turretType,turretLocation,MaxRotation,MinRotation));
                    }
                    
                    //<ship class="Emden" MaxThrust="9.6" RudderTorquePerSpeed="0.01" TurnFriction="1" PortFriction="1" ForwardFriction="0.1">
                    float maxThrust = float.Parse(ship.Attribute("MaxThrust")?.Value ?? "9.6");
                    float rudderTorquePerSpeed = float.Parse(ship.Attribute("RudderTorquePerSpeed")?.Value ?? "0.01");
                    float turnFriction = float.Parse(ship.Attribute("TurnFriction")?.Value ?? "1");
                    float portFriction = float.Parse(ship.Attribute("PortFriction")?.Value ?? "1");
                    float forwardFriction= float.Parse(ship.Attribute("ForwardFriction")?.Value ?? "0.1");
                    int lengthCompartments = int.Parse(ship.Attribute("LengthCompartments")?.Value ?? "1");
                    int sinkFrames = int.Parse(ship.Attribute("SinkFrames")?.Value ?? "1");
                    int compartmentMaxDamage = int.Parse(ship.Attribute("CompartmentMaxDamage")?.Value ?? "1");
                    ships.Add(Class ,new Ship(shipTexture,_shellTexture,funnelLocations,shipTurrets,maxThrust,rudderTorquePerSpeed,turnFriction,portFriction,forwardFriction,lengthCompartments,sinkFrames,compartmentMaxDamage));
                }
            }
        }

        _Emden = ships["Emden"].Clone();
        _EnemyShips.AddLast(ships["Emden"].Clone());
        
        _powerIndicatorAngle = (_Emden.PowerLevel + 2) * 0.16f * Single.Pi * 0.5f;
        


        base.LoadContent();
    }

    public override void Update(GameTime gameTime)
    {
        
        //Control the Emden
        if (!_Emden.IsSinking)
        {
            if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Up) || Core.Input.Keyboard.WasKeyJustPressed(Keys.W))
            {
                _Emden.AddPower(true);
            }

            if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Down) || Core.Input.Keyboard.WasKeyJustPressed(Keys.S))
            {
                _Emden.AddPower(false);
            }

            _Emden.Rudder = 0;
            if (Core.Input.Keyboard.IsKeyDown(Keys.Left) || Core.Input.Keyboard.IsKeyDown(Keys.A))
            {
                --_Emden.Rudder;
            }

            if (Core.Input.Keyboard.IsKeyDown(Keys.Right) || Core.Input.Keyboard.IsKeyDown(Keys.D))
            {
                ++_Emden.Rudder;
            }

            Vector2 target = Core.Input.Mouse.Position.ToVector2() -
                             new Vector2(Core.GraphicsDevice.Viewport.Width * 0.5f,
                                 Core.GraphicsDevice.Viewport.Height * 0.5f) +
                             _Emden.Position;


            _Emden.SetTarget(target);

            if (Core.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
            {
                //Check if the mouse is within the screen

                var mousePosition = Core.Input.Mouse.Position.ToVector2();
                if (mousePosition.X < Core.GraphicsDevice.Viewport.Width &&
                    mousePosition.Y < Core.GraphicsDevice.Viewport.Height && mousePosition.X > 0 && mousePosition.Y > 0)
                {
                    _Emden.Shoot(_smoke,_smokeTemplate,_shells);

                    //TEMP, spawn a shell at the mouse
                    //_shells.AddLast(new Shell(_shellTexture, target, Vector2.Zero, 1));
                }

            }
        }
        
        

        List<Shell> shellsToRemove=new();

        foreach (var shell in _shells)
        {
            shell.Update(gameTime);
            
            if (shell.IsSplash)
                shellsToRemove.Add(shell);
        }

        foreach (var shell in shellsToRemove)
        {
            bool hit = _Emden.HitTest(shell);

            foreach (var ship in _EnemyShips)
            {
                hit |= ship.HitTest(shell);
            }

            if (hit)
            {
                //Play boom
                Core.Audio.PlaySoundEffect(_shellExplosionSound);
                _explosions.AddLast(new Particle(_explosionTemplate.Source, shell.Position, _explosionTemplate.Nframes,
                    _explosionTemplate.LifeTime, _explosionTemplate.Friction, Vector2.Zero, Vector2.Zero));

            }
            else
            {
                //Splashed down in wate
                _foam.AddLast(new Particle(_foamTemplate.Source, shell.Position, _foamTemplate.Nframes,
                    _foamTemplate.LifeTime, _foamTemplate.Friction, Vector2.Zero, Vector2.Zero));
            
                Core.Audio.PlaySoundEffect(_splashSound);
            }
            
            _shells.Remove(shell);


        }

        _engineSoundInstance.Volume = MathF.Abs(_Emden.PowerLevel) * 0.25f;
        _engineSoundInstance.Pitch = (MathF.Abs(_Emden.PowerLevel)-1) * 0.25f;
        
        _Emden.SpawnSmoke(_smoke,_smokeTemplate,gameTime);
        _Emden.Update(gameTime);
        _Emden.SpawnFoam(_foam,_foamTemplate,gameTime);

        foreach (Ship ship in _EnemyShips)
        {
            ship.SpawnSmoke(_smoke,_smokeTemplate,gameTime);
            ship.Update(gameTime);
            ship.SpawnFoam(_foam,_foamTemplate,gameTime);
        }
        
        
        //Update particles, and remove dead once
        List<Particle> toRemove = new List<Particle>();
 
        foreach (var explosion in _explosions)
        {
            explosion.Update(gameTime);
            if (explosion.Dead)
            {
                toRemove.Add(explosion);
            }
        }
        foreach (var p in toRemove)
        {
            _explosions.Remove(p);
        }
        
        toRemove = new List<Particle>();
        
        foreach (var smoke in _smoke)
        {
            smoke.Update(gameTime);
            if (smoke.Dead)
            {
                toRemove.Add(smoke);
            }
        }

        foreach (var p in toRemove)
        {
            _smoke.Remove(p);
        }
        toRemove = new List<Particle>();
        
        foreach (var foam in _foam)
        {
            foam.Update(gameTime);
            if (foam.Dead)
            {
                toRemove.Add(foam);
            }
        }
        foreach (var p in toRemove)
        {
            _foam.Remove(p);
        }
        
        
    }

    public override void Draw(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        //Unlikely to be seen, but no harm if it is
        Core.GraphicsDevice.Clear(Color.LightBlue);
        
        Core.SpriteBatch.Begin();

        Vector2 cameraOffset = -new Vector2(Core.GraphicsDevice.Viewport.Width *0.5f, Core.GraphicsDevice.Viewport.Height *0.5f);
        
        
        //Draw one kind at the time to reduce texture swapping
        foreach (var foam in _foam)
            foam.Draw(Core.SpriteBatch,_Emden.Position+cameraOffset );
        
        _Emden.Draw(Core.SpriteBatch,gameTime,_Emden.Position+cameraOffset);
        
        foreach (Ship ship in _EnemyShips)
        {
            ship.Draw(Core.SpriteBatch,gameTime,_Emden.Position+cameraOffset);
        }

        
        foreach (var smoke in _smoke)
            smoke.Draw(Core.SpriteBatch,_Emden.Position+cameraOffset);
        
        foreach (var explosion in _explosions)
            explosion.Draw(Core.SpriteBatch,_Emden.Position+cameraOffset);
        
        foreach (var shell in  _shells)
        {
            shell.Draw(Core.SpriteBatch,_Emden.Position+cameraOffset);
        }

        
        //Now draw the UI
        //First the speed and power and turning indicator in the bottom left
        Core.SpriteBatch.Draw(_powerIndicator,new Vector2(0,Core.GraphicsDevice.Viewport.Height-_powerIndicator.Height), Color.White);


        float powerIndicatorTarget = (_Emden.PowerLevel + 2) * 0.16f * Single.Pi * 0.5f;
        if (powerIndicatorTarget > _powerIndicatorAngle)
        {
            _powerIndicatorAngle = MathF.Min(_powerIndicatorAngle+dt,powerIndicatorTarget ) ;
        }
        else if (powerIndicatorTarget < _powerIndicatorAngle)
        {
            _powerIndicatorAngle = MathF.Max(_powerIndicatorAngle-dt,powerIndicatorTarget ) ;
        }

        float targetWheelAngle = 0;

        if (_Emden.Rudder == -1)
        {
            targetWheelAngle = -3;
        }
        else if (_Emden.Rudder == 0)
        {
            targetWheelAngle = 0;
        }
        else if (_Emden.Rudder == 1)
        {
            targetWheelAngle = 3;
        }

        if (_wheelAngle < targetWheelAngle)
        {
            _wheelAngle += dt*4;
            
            if (_wheelAngle > targetWheelAngle)
                _wheelAngle = targetWheelAngle;
        }
        else if (_wheelAngle > targetWheelAngle)
        {
            _wheelAngle -= dt*4;
            if (_wheelAngle < targetWheelAngle)
                _wheelAngle = targetWheelAngle;
        }
        
        float speedKn = _Emden.ForwardSpeed / (8 * 0.5144444f);
        
        Core.SpriteBatch.Draw(
            _speedIndicatorHandle,
            new Vector2(192,Core.GraphicsDevice.Viewport.Height), 
            null,
            Color.White,
            (speedKn/20-0.5f)*Single.Pi,
            new Vector2(_speedIndicatorHandle.Width*0.5f, _speedIndicatorHandle.Height-2), 
            1f,
            SpriteEffects.None,
            0.0f
        );
        
        Core.SpriteBatch.Draw(
            _powerIndicatorHandle,
            new Vector2(0,Core.GraphicsDevice.Viewport.Height), 
            null,
            Color.White,
            _powerIndicatorAngle,
            new Vector2(_powerIndicatorHandle.Width*0.5f, _powerIndicatorHandle.Height-2), 
            1f,
            SpriteEffects.None,
            0.0f
        );
        
        Core.SpriteBatch.Draw(
            _wheel,
            new Vector2(200,Core.GraphicsDevice.Viewport.Height-180), 
            null,
            Color.White,
            _wheelAngle,
            new Vector2(_wheel.Width*0.5f, _wheel.Height*0.5f), 
            1f,
            SpriteEffects.None,
            0.0f
        );
 
        
        
        //Draw the compartments with damages
        for (int x = 0; x < _Emden.LengthCompartments; ++x)
        for (int y = 0; y < 2; ++y)
        {
            int dmg = _Emden.CompartmentsDamage[x, y];
            Color compartmentColor = dmg==_Emden.CompartmentMaxDamage?Color.Blue : new Color(255,255*(_Emden.CompartmentMaxDamage-dmg)/_Emden.CompartmentMaxDamage,255*(_Emden.CompartmentMaxDamage-dmg)/_Emden.CompartmentMaxDamage);
            
            Core.SpriteBatch.Draw(
                _EmdenCompartments,
                new Vector2(Core.GraphicsDevice.Viewport.Width-20-_EmdenCompartments.Width+x*_EmdenCompartments.Width/(float)_Emden.LengthCompartments,y*_EmdenCompartments.Height*0.5f+50), 
                new Rectangle(x*_EmdenCompartments.Width/_Emden.LengthCompartments,y*_EmdenCompartments.Height/2,_EmdenCompartments.Width/_Emden.LengthCompartments,_EmdenCompartments.Height/2),
                compartmentColor,
                0,
                Vector2.Zero, 
                1f,
                SpriteEffects.None,
                0.0f
            );
            
        }
        
        Core.SpriteBatch.DrawString(_font, $"Flooding: {_Emden.FloodedCompartments}/{_Emden.MaxFlooding}", 
            new Vector2(Core.GraphicsDevice.Viewport.Width-20-_EmdenCompartments.Width,_EmdenCompartments.Height+50) 
            , Color.Black, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
 
        Core.SpriteBatch.DrawString(_font, $"Capsizing: {_Emden.UnbalancedCompartments}/{(_Emden.LengthCompartments)/2+1}", 
            new Vector2(Core.GraphicsDevice.Viewport.Width-20-_EmdenCompartments.Width,_EmdenCompartments.Height+80) 
            , Color.Black, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
        
        
        
        
        //Draw aiming assistance: number of loaded guns
        int loadedGuns = 0;
        foreach (var turret in _Emden.Turrets)
        {
            if (turret.Aimed && turret.IsLoaded)
            {
                ++loadedGuns;
            }
        }
        Vector2 target = Core.Input.Mouse.Position.ToVector2();
        
        Core.SpriteBatch.DrawString(_font, $"{loadedGuns}", target+new Vector2(0,-36), Color.Red, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
        
        
        if (_Emden.PowerLevel==4)
            Core.SpriteBatch.DrawString(_font, $"Engine Vibrations reduce accuracy!", new Vector2(Core.GraphicsDevice.Viewport.Width*0.2f,0), Color.Red, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
        
        Core.SpriteBatch.End();
    }
}