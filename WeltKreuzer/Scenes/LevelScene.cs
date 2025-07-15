using System;
using System.Collections.Generic;
using MatrosEngine;
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
    
    private SpriteFont _font;

    
    /// <summary>
    /// Linked list, for easier deletion of dead particles, a deque would be better
    /// </summary>
    private LinkedList<Particle> _smoke;
    
    private LinkedList<Particle> _foam;

    
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
        
        _Emden = new Ship(Content.Load<Texture2D>("images/Emden"),new Vector2(200,200),0,_EmdenTurret);
        
        //TODO: This should be loaded from an xml file
        _smokeTemplate = new ParticleTemplate(Content.Load<Texture2D>("images/smoke"),4,4,0.2f,new Vector2(100,100));
        _smoke = new LinkedList<Particle>();
        
        
        _foamTemplate = new ParticleTemplate(Content.Load<Texture2D>("images/foam"),4,2,0.5f,new Vector2(0,0));
        _foam = new LinkedList<Particle>();
        
        _powerIndicator= Content.Load<Texture2D>("images/powerIndicator");
        _powerIndicatorHandle= Content.Load<Texture2D>("images/powerIndicatorHandle");
        
        _font = Core.Content.Load<SpriteFont>("fonts/normalFont");
        
        
        base.LoadContent();
    }

    public override void Update(GameTime gameTime)
    {

        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Up))
        {
            _Emden.AddPower(true);
        }
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Down))
        {
            _Emden.AddPower(false);
        }

        _Emden.Rudder = 0;
        if (Core.Input.Keyboard.IsKeyDown(Keys.Left))
        {
            --_Emden.Rudder;
        }
        if (Core.Input.Keyboard.IsKeyDown(Keys.Right))
        {
            ++_Emden.Rudder;
        }
        
        Vector2 target = Core.Input.Mouse.Position.ToVector2();
        
        _Emden.SetTarget(target);
        
        
        
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
        //Unlikely to be seen, but no harm if it is
        Core.GraphicsDevice.Clear(Color.LightBlue);
        
        Core.SpriteBatch.Begin();
        
        //Draw one kind at the time to reduce texture swapping
        foreach (var foam in _foam)
            foam.Draw(Core.SpriteBatch);
        
        _Emden.Draw(Core.SpriteBatch,gameTime);
        

        
        
        Core.SpriteBatch.Draw(
            _powerIndicatorHandle,
            new Vector2(0,Core.GraphicsDevice.Viewport.Height), 
            null,
            Color.White,
            (_Emden.PowerLevel+2)*0.16f*Single.Pi*0.5f,
            new Vector2(_powerIndicatorHandle.Width*0.5f, _powerIndicatorHandle.Height-2), 
            1f,
            SpriteEffects.None,
            0.0f
        );
        
        foreach (var smoke in _smoke)
            smoke.Draw(Core.SpriteBatch);
        
        Core.SpriteBatch.Draw(_powerIndicator,new Vector2(0,Core.GraphicsDevice.Viewport.Height-_powerIndicator.Height), Color.White);
        
        float speedKn = _Emden.ForwardSpeed / (8 * 0.5144444f);
        Core.SpriteBatch.DrawString(_font, $"{speedKn.ToString("00.00")}", new Vector2(140, Core.GraphicsDevice.Viewport.Height-30), Color.Yellow, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
        Core.SpriteBatch.End();
    }
}