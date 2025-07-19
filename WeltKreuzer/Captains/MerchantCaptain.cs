using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WeltKreuzer.Entities;

/// <summary>
/// Merchant captains try to sail for their destination, but scatter at full speed at the least sign of danger
/// </summary>
public class MerchantCaptain : Captain
{


    /// <summary>
    /// Target destination, we return to once not in danger
    /// </summary>
    public float TargetRotation;
    /// <summary>
    /// Current target, away from danger at all times
    /// </summary>
    public float ThisTargetRotation;
    
    public MerchantCaptain(float targetRotation) : base()
    {
        TargetRotation = targetRotation;
    }

    public override Captain Clone()
    {
        return new MerchantCaptain(TargetRotation);
    }

    public override void Control()
    {
        ThisTargetRotation = TargetRotation;

        bool isPanicked = false;
        if (PlayerShip != null)
        {
            var dir = MyShip.Position - PlayerShip.Position;
            ThisTargetRotation =MathF.Atan2(dir.Y, dir.X);
            isPanicked = true;
            
            //Merchants are not very good shots/have no on-board rangefinders
            var r = new Random();
            MyShip.SetTarget(PlayerShip.Position+new Vector2(r.NextSingle()*20-10,r.NextSingle()*20-10));
            MyShip.ShallShoot = true;
        }
        else
            MyShip.ShallShoot = false;


        
        //Use the determinant to tell if we are to the left or to the right
        var thisTargetDir = new Vector2(MathF.Cos(ThisTargetRotation), MathF.Sin(ThisTargetRotation));
        var myDirection = MyShip.Forward;
        
        float det = thisTargetDir.X*myDirection.Y-thisTargetDir.Y*myDirection.X;
        
        MyShip.Rudder = 0;
        if (det < 0)
        {
            MyShip.Rudder = 1;
        }
        else if (det > 0)
        {
            MyShip.Rudder =-1;
        }

        //Increase power if there is a scary cruiser
        if (isPanicked)
            MyShip.AddPower(true);
        else
        {
            //Economical cruising speed
            if (MyShip.PowerLevel>2)
                MyShip.AddPower(false);
        }
    }
    
    public override void Draw(SpriteBatch spriteBatch,Texture2D direction,Vector2 shipPosition)
    {
        
        
        spriteBatch.Draw(
            direction,
            shipPosition,
            null,
            Color.White,
            ThisTargetRotation,
            new Vector2(direction.Width*0.5f,direction.Height*0.5f),
            1f,
            SpriteEffects.None,
            0.0f
        );
    }
}