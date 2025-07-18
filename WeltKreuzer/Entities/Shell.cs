using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WeltKreuzer.Entities;

public class Shell
{
    public Vector2 Position {get; set;}
    public Vector2 Velocity {get; set;}
    /// <summary>
    /// Time remaining till impact
    /// </summary>
    public float Time {get; set;}
    private Texture2D _texture;

    public bool IsSplash => Time <= 0;

    public Shell(Texture2D texture, Vector2 position, Vector2 velocity, float time)
    {
        _texture = texture;
        Position = position;
        Velocity = velocity;
        Time = time;
    }

    public void Update(GameTime gameTime)
    {
        if (!IsSplash)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            //The shell will be removed if it is dead
            Time -= dt;
            
            Position+=Velocity*dt;
        }
    }

    public void Draw(SpriteBatch spriteBatch,Vector2 cameraPosition)
    {
        
        spriteBatch.Draw(
            _texture,
            Position-new Vector2(_texture.Width*0.5f,_texture.Height*0.5f)-cameraPosition, 
            null,
            Color.White,
            0,
            Vector2.Zero, 
            1f,
            SpriteEffects.None,
            0.0f
        );
    }
}