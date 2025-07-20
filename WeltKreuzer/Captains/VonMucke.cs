using System;
using MatrosEngine;
using MatrosEngine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace WeltKreuzer.Entities;

/// <summary>
/// Helmuth Von Mücke, the captain of the Emden allows the player to control the ship
/// </summary>
public class VonMucke : Captain
{
    public VonMucke() : base()
    {
        //We can spot enemy smoke from quite a distance
        VisionDistance = 2500;
    }

    public override void Control()
    {
        
        if (!MyShip.IsSinking)
        {
            
            if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Up) || Core.Input.Keyboard.WasKeyJustPressed(Keys.W))
            {
                MyShip.AddPower(true);
            }

            if (Core.Input.Keyboard.WasKeyJustPressed(Keys.Down) || Core.Input.Keyboard.WasKeyJustPressed(Keys.S))
            {
                MyShip.AddPower(false);
            }

            MyShip.Rudder = 0;
            if (Core.Input.Keyboard.IsKeyDown(Keys.Left) || Core.Input.Keyboard.IsKeyDown(Keys.A))
            {
                --MyShip.Rudder;
            }

            if (Core.Input.Keyboard.IsKeyDown(Keys.Right) || Core.Input.Keyboard.IsKeyDown(Keys.D))
            {
                ++MyShip.Rudder;
            }

            Vector2 target = Core.Input.Mouse.Position.ToVector2() -
                             new Vector2(Core.GraphicsDevice.Viewport.Width * 0.5f,
                                 Core.GraphicsDevice.Viewport.Height * 0.5f) +
                             MyShip.Position;//Assume the screen is centered on my ship


            MyShip.SetTarget(target);
            

            if (Core.Input.Mouse.WasButtonJustPressed(MouseButton.Left))
            {
                //Check if the mouse is within the screen

                var mousePosition = Core.Input.Mouse.Position.ToVector2();
                if (mousePosition.X < Core.GraphicsDevice.Viewport.Width &&
                    mousePosition.Y < Core.GraphicsDevice.Viewport.Height && mousePosition.X > 0 && mousePosition.Y > 0)
                {
                    MyShip.ShallShoot = true;
                }
            }

            if (Core.Input.Mouse.WasButtonJustPressed(MouseButton.Right))
            {
                
                var mousePosition = Core.Input.Mouse.Position.ToVector2();
                if (mousePosition.X < Core.GraphicsDevice.Viewport.Width &&
                    mousePosition.Y < Core.GraphicsDevice.Viewport.Height && mousePosition.X > 0 && mousePosition.Y > 0)
                {
                    MyShip.ShallTorpedo= true;
                }
            }
            
        }
    }

    public override Captain Clone()
    {
        return new VonMucke();
    }
    
    public override void Draw(SpriteBatch spriteBatch,Texture2D direction,Vector2 shipPosition)
    {
        foreach (Ship ship in EnemyShips)
        {
            var dir = ship.Position - MyShip.Position;           
            spriteBatch.Draw(
                direction,
                shipPosition,
                null,
                Color.Red,
                MathF.Atan2(dir.Y, dir.X),
                new Vector2(direction.Width*0.5f,direction.Height*0.5f),
                1f,
                SpriteEffects.None,
                0.0f
            );
        }
    }
    
}