using System.Collections.Generic;
using System.Transactions;
using MatrosEngine;
using MatrosEngine.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace WeltKreuzer.Entities;

/// <summary>
/// The captain who controls the ship
/// This is a default captain who doesn't do anything
/// </summary>
public  class Captain
{
    protected Ship MyShip;

    
    /// <summary>
    /// Directions and distance to the scary player
    /// </summary>
    public Ship PlayerShip=null;

    public List<Ship> EnemyShips;
    
    public float VisionDistance { get; set; }
    
    
    public Captain()
    {
        VisionDistance = 500;
    }

    public void ViewShips(Ship player, IEnumerable<Ship> foes)
    {
        EnemyShips = new();
        if (Vector2.Distance(player.Position, MyShip.Position) <= VisionDistance)
            PlayerShip = player;
        else
            PlayerShip = null;//I can't see them, I guess I am safe
        
        foreach (var foe in foes)
            if (Vector2.Distance(foe.Position, MyShip.Position) <= VisionDistance)
                EnemyShips.Add(foe);
            
    }

    public virtual void Draw(SpriteBatch spriteBatch,Texture2D direction,Vector2 ShipPosition)
    {
        //By default, no drawings
        
    }
    
    

    public void TakeCommand(Ship ship)
    {
        MyShip = ship;
    }


    public virtual void Control()
    {
        //The base class doesn't do anything to the ship

    }

    public virtual Captain Clone()
    {
        return new Captain();
    }
}