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

    
    private Texture2D _powerIndicator;
    private Texture2D _powerIndicatorHandle;

    private Texture2D _speedIndicatorHandle;

    private Texture2D _wheel;


    private SoundEffect _splashSound;

    /// <summary>
    /// Sound of the player's engine is handled in the scene, not the ship
    /// </summary>
    private SoundEffect _EngineSound;
    private SoundEffectInstance _EngineSoundInstance;
    
    private SoundEffect _Ambience;
    private SoundEffectInstance _AmbienceInstance;
    
    
    
    private float _powerIndicatorAngle;
    
    private float _wheelAngle;
    
    private SpriteFont _font;

    
    /// <summary>
    /// Linked list, for easier deletion of dead particles, a deque would be better
    /// </summary>
    private LinkedList<Particle> _smoke;
    
    private LinkedList<Particle> _foam;

    /// <summary>
    /// Shells in the air
    /// </summary>
    private LinkedList<Shell> _shells;
    
    
    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();
    }

    
    private ParticleTemplate _smokeTemplate;
    private ParticleTemplate _foamTemplate;
    
    public override void LoadContent()
    {

        var aimingDot = Content.Load<Texture2D>("images/debugDot");
        
        
        SoundEffect CannonSound = Content.Load<SoundEffect>("Audio/184650__isaac200000__cannon1");
        _splashSound = Content.Load<SoundEffect>("Audio/519008__sheyvan__water-explosion");
        _EngineSound = Content.Load<SoundEffect>("Audio/594215__steaq__big-steam-engine-perfect-loop-24bit-flac");
        _EngineSoundInstance=Core.Audio.PlaySoundEffect(_EngineSound,0f,0,0,true);
        
        
        _Ambience = Content.Load<SoundEffect>("Audio/753972__klankbeeld__coast-far-shipping-1106-am-220905_0526");
        _AmbienceInstance=Core.Audio.PlaySoundEffect(_Ambience,1f,0,0,true);
        
        
        //Load a lot of pictures
        _smokeTemplate = new ParticleTemplate(Content.Load<Texture2D>("images/smoke"),4,4,0.2f,new Vector2(100,100));
        _smoke = new LinkedList<Particle>();
        
        
        _foamTemplate = new ParticleTemplate(Content.Load<Texture2D>("images/foam"),4,2,0.5f,new Vector2(0,0));
        _foam = new LinkedList<Particle>();
        
        _shells = new LinkedList<Shell>();
        
        
        _powerIndicator= Content.Load<Texture2D>("images/powerIndicator");
        _powerIndicatorHandle= Content.Load<Texture2D>("images/powerIndicatorHandle");
        _speedIndicatorHandle= Content.Load<Texture2D>("images/SpeedHandle");
        _wheel = Content.Load<Texture2D>("images/Wheel");
        
        _font = Core.Content.Load<SpriteFont>("fonts/normalFont");

        
        //Load all ships into memory
        string shipFilePath = Path.Combine(Core.Content.RootDirectory, "ships/AllShips.xml");

        
        Texture2D shellTexture = Content.Load<Texture2D>("images/projectile");
        
        
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
                    
                    ships.Add(Class ,new Ship(shipTexture,shellTexture,funnelLocations,shipTurrets,maxThrust,rudderTorquePerSpeed,turnFriction,portFriction,forwardFriction));
                }
            }
        }

        _Emden = ships["Emden"];
        
        
        _powerIndicatorAngle = (_Emden.PowerLevel + 2) * 0.16f * Single.Pi * 0.5f;
        


        base.LoadContent();
    }

    public override void Update(GameTime gameTime)
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
        
        Vector2 target = Core.Input.Mouse.Position.ToVector2();
        
        _Emden.SetTarget(target);

        if (Core.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
        {
            _Emden.Shoot(_smoke,_smokeTemplate,_shells);
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
            _foam.AddLast(new Particle(_foamTemplate.Source, shell.Position, _foamTemplate.Nframes,
                _foamTemplate.LifeTime, _foamTemplate.Friction, Vector2.Zero, Vector2.Zero));
                
            _shells.Remove(shell);
            Core.Audio.PlaySoundEffect(_splashSound);


        }

        _EngineSoundInstance.Volume = MathF.Abs(_Emden.PowerLevel) * 0.25f;
        _EngineSoundInstance.Pitch = (MathF.Abs(_Emden.PowerLevel)-1) * 0.25f;
        
        _Emden.Update(gameTime);
        _Emden.SpawnSmoke(_smoke,_smokeTemplate,gameTime);
        _Emden.SpawnFoam(_foam,_foamTemplate,gameTime);
        
        
        //Update particles, and remove dead once
        List<Particle> toRemove = new List<Particle>();
        
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
        
        //Draw one kind at the time to reduce texture swapping
        foreach (var foam in _foam)
            foam.Draw(Core.SpriteBatch);
        
        _Emden.Draw(Core.SpriteBatch,gameTime);
        

        
        foreach (var smoke in _smoke)
            smoke.Draw(Core.SpriteBatch);
        
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

        foreach (var shell in  _shells)
        {
            shell.Draw(Core.SpriteBatch);
        }

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