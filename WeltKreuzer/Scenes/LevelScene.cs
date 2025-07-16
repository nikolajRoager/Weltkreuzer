using System;
using System.Collections.Generic;
using MatrosEngine;
using MatrosEngine.Input;
using MatrosEngine.Particles;
using MatrosEngine.Scenes;
using Microsoft.Xna.Framework;
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
        var _EmdenTurret = new Turret(Content.Load<Texture2D>("images/105mm"),Content.Load<Texture2D>("images/debugDot"),1,new Vector2(8,8));
        
        _Emden = new Ship(Content.Load<Texture2D>("images/Emden"),Content.Load<Texture2D>("images/projectile"),new Vector2(200,200),0,_EmdenTurret);
        
        //TODO: This should be loaded from an xml file
        _smokeTemplate = new ParticleTemplate(Content.Load<Texture2D>("images/smoke"),4,4,0.2f,new Vector2(100,100));
        _smoke = new LinkedList<Particle>();
        
        
        _foamTemplate = new ParticleTemplate(Content.Load<Texture2D>("images/foam"),4,2,0.5f,new Vector2(0,0));
        _foam = new LinkedList<Particle>();
        
        _shells = new LinkedList<Shell>();
        _shells.AddLast(new Shell(Content.Load<Texture2D>("images/projectile"),new Vector2(400,400),new Vector2(20, 20),10));
        
        
        _powerIndicator= Content.Load<Texture2D>("images/powerIndicator");
        _powerIndicatorHandle= Content.Load<Texture2D>("images/powerIndicatorHandle");
        _speedIndicatorHandle= Content.Load<Texture2D>("images/SpeedHandle");
        _wheel = Content.Load<Texture2D>("images/Wheel");
        
        _font = Core.Content.Load<SpriteFont>("fonts/normalFont");


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
            //todo: Handle splas-down/hit detection
            
            
        }
        
        
        
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