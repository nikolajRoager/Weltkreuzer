using MatrosEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WeltKreuzer.Scenes;

namespace WeltKreuzer;

public class Game1 : Core
{
    public Game1() : base("Weltkreuzer")
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        // Start the game with the title scene.
        ChangeScene(new TitleScreen());
    }

    protected override void LoadContent()
    {
    }
}