using System;
using MatrosEngine;
using MatrosEngine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace WeltKreuzer.Scenes;

public class TitleScreen : Scene
{
    Texture2D background;
    
    
    private SpriteFont _TitleFont;
    private SpriteFont _font;
    
    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();
    }

    public override void LoadContent()
    {
        background=Content.Load<Texture2D>("background/Emden");
        
        // Load the font for the standard text.
        _TitleFont = Core.Content.Load<SpriteFont>("fonts/NewspaperHeadline");
        _font = Core.Content.Load<SpriteFont>("fonts/normalFont");
        
        base.LoadContent();
    }

    public override void Update(GameTime gameTime)
    {
        // If the user presses enter, switch to the game scene.
        if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Enter))
        {
            Core.ChangeScene(new LevelScene());
        }
    }

    public override void Draw(GameTime gameTime)
    {
        //Unlikely to be seen, but no harm if it is
        Core.GraphicsDevice.Clear(Color.Gray);
        
        Core.SpriteBatch.Begin();
        
        Core.SpriteBatch.Draw(
            background,
            Vector2.Zero, 
            null,
            Color.White,
            0,
            Vector2.Zero, 
            Core.GraphicsDevice.Viewport.Width/(float)background.Width,
            SpriteEffects.None,
            0.0f
        );
        
        Core.SpriteBatch.DrawString(_TitleFont, "Weltkreuzer", new Vector2(10, 10), Color.Yellow, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
        Core.SpriteBatch.DrawString(_font, "The Adventure of SMS Emden", new Vector2(60, 120), Color.Yellow, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
        Core.SpriteBatch.DrawString(_font, "Press Enter to start", new Vector2(60, 200), Color.Yellow, 0.0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
        
        Core.SpriteBatch.End();
    }

}