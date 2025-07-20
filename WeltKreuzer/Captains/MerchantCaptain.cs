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

    
    /// <summary>
    /// We tend to follow the nearest ship, in front of me
    /// </summary>
    public Ship Leader;

    
    public float PreferedDistance;
    
    public MerchantCaptain(float targetRotation) : base()
    {
        TargetRotation = targetRotation;
        PreferedDistance = 300;
    }

    public override Captain Clone()
    {
        return new MerchantCaptain(TargetRotation);
    }

    public override void Control()
    {
        //Find the leader, they are in front of me, and they are the nearest
        Leader = null;
        float nearestD2 = Single.PositiveInfinity;
        foreach (Ship ship in EnemyShips)
        {
            var dir = ship.Position - MyShip.Position;           
            float D2 = dir.X*dir.X+dir.Y*dir.Y;
            if (Vector2.Dot(dir, MyShip.Forward) > 0 && D2<nearestD2)
            {
                nearestD2 = D2;
                Leader = ship;
            }
        }
        
        
        
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
        {
            MyShip.ShallShoot = false;
            //Follow the leader; they surely know where we are going
            if (Leader != null)
            {
                var dir = Leader.Position-MyShip.Position;

                float dist = MathF.Sqrt(nearestD2);
                if (dist > PreferedDistance)
                {
                    if (MyShip.PowerLevel <= Leader.PowerLevel)
                        MyShip.AddPower(true);
                    else
                        MyShip.AddPower(false);
                }
                else
                {
                    
                    if (MyShip.PowerLevel >= Leader.PowerLevel)
                        MyShip.AddPower(false);
                    else
                        MyShip.AddPower(true);
                }
                ThisTargetRotation =MathF.Atan2(dir.Y, dir.X);
            }
        }


        
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
        else if (Leader == null)//If there is no leader, 
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
            Color.Blue,
            ThisTargetRotation,
            new Vector2(direction.Width*0.5f,direction.Height*0.5f),
            1f,
            SpriteEffects.None,
            0.0f
        );
        
        //We tend to follow the nearest ship (smallest dist^2) in front of me (dot product betwixt direction and forward is positive)
 //       foreach (Ship ship in EnemyShips)
        if (Leader != null)
        {
            var dir = Leader.Position - MyShip.Position;           
            spriteBatch.Draw(
                direction,
                shipPosition,
                null,
                Color.Green,
                MathF.Atan2(dir.Y, dir.X),
                new Vector2(direction.Width*0.5f,direction.Height*0.5f),
                1f,
                SpriteEffects.None,
                0.0f
            );
        }
        
    }
}