using System;
using MatrosEngine;
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
    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();
    }
    
    public override void LoadContent()
    {
        _Emden = new Ship(Content.Load<Texture2D>("images/Emden"),new Vector2(200,200),96);
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
        
        
        
        _Emden.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        //Unlikely to be seen, but no harm if it is
        Core.GraphicsDevice.Clear(Color.LightBlue);
        
        Core.SpriteBatch.Begin();
        
        _Emden.Draw(Core.SpriteBatch);
        
        Core.SpriteBatch.Draw(_powerIndicator,new Vector2(0,Core.GraphicsDevice.Viewport.Height-_powerIndicator.Height), Color.White);
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

        float speedKn = _Emden.ForwardSpeed / (8 * 0.5144444f);
        Core.SpriteBatch.DrawString(_font, $"{speedKn.ToString("00.00")}", new Vector2(140, Core.GraphicsDevice.Viewport.Height-30), Color.Yellow, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
        Core.SpriteBatch.End();
    }
}