using System;
using MoonTools.ECS;
using MoonWorks;
using Tactician.Content;
using Tactician.Components;
using Tactician.Messages;
using Tactician.Relations;
using Tactician.Systems;

namespace Tactician.GameStates;

public class GameplayState : GameState
{
    TacticianGame Game;

    Renderer Renderer;
    World World;
    InputSystem InputSystem;
    MotionSystem MotionSystem;
    AudioSystem AudioSystem;
    HoldSystem HoldSystem;
    ProductSpawner ProductSpawner;
    ShelfSpawner ShelfSpawner;
    TickerSystem TickerSystem;
    Systems.GameTimerSystem GameTimerSystem;
    TimingSystem TimingSystem;
    OrdersSystem OrdersSystem;
    SetSpriteAnimationSystem SetSpriteAnimationSystem;
    DirectionalAnimationSystem DirectionalAnimationSystem;
    UpdateSpriteAnimationSystem UpdateSpriteAnimationSystem;
    ColorAnimationSystem ColorAnimationSystem;
    NPCControllerSystem NPCControllerSystem;
    DroneControllerSystem DroneControllerSystem;
    PlayerControllerSystem PlayerControllerSystem;
    GameState TransitionState;

    public GameplayState(TacticianGame game, GameState transitionState)
    {
        Game = game;
        TransitionState = transitionState;
    }

    public override void Start()
    {
        World = new World();

        GameTimerSystem = new GameTimerSystem(World);
        TimingSystem = new TimingSystem(World);
        InputSystem = new InputSystem(World, Game.Inputs);
        MotionSystem = new MotionSystem(World);
        AudioSystem = new AudioSystem(World, Game.AudioDevice);
        PlayerControllerSystem = new PlayerControllerSystem(World);
        HoldSystem = new HoldSystem(World);
        OrdersSystem = new OrdersSystem(World);
        ProductSpawner = new ProductSpawner(World);
        ShelfSpawner = new ShelfSpawner(World);
        SetSpriteAnimationSystem = new SetSpriteAnimationSystem(World);
        UpdateSpriteAnimationSystem = new UpdateSpriteAnimationSystem(World);
        ColorAnimationSystem = new ColorAnimationSystem(World);
        DirectionalAnimationSystem = new DirectionalAnimationSystem(World);
        NPCControllerSystem = new NPCControllerSystem(World);
        DroneControllerSystem = new DroneControllerSystem(World);

        CategoriesAndIngredients cats = new CategoriesAndIngredients(World);
        cats.Initialize(World);

        TickerSystem = new TickerSystem(World, cats);

        Renderer = new Renderer(World, Game.GraphicsDevice, Game.MainWindow.SwapchainFormat);

        var topBorder = World.CreateEntity();
        World.Set(topBorder, new Position(0, 65));
        World.Set(topBorder, new Rectangle(0, 0, Dimensions.GAME_W, 10));
        World.Set(topBorder, new Solid());

        var leftBorder = World.CreateEntity();
        World.Set(leftBorder, new Position(-10, 0));
        World.Set(leftBorder, new Rectangle(0, 0, 10, Dimensions.GAME_H));
        World.Set(leftBorder, new Solid());

        var rightBorder = World.CreateEntity();
        World.Set(rightBorder, new Position(Dimensions.GAME_W, 0));
        World.Set(rightBorder, new Rectangle(0, 0, 10, Dimensions.GAME_H));
        World.Set(rightBorder, new Solid());

        var bottomBorder = World.CreateEntity();
        World.Set(bottomBorder, new Position(0, Dimensions.GAME_H));
        World.Set(bottomBorder, new Rectangle(0, 0, Dimensions.GAME_W, 10));
        World.Set(bottomBorder, new Solid());

        var background = World.CreateEntity();
        World.Set(background, new Position(0, 0));
        World.Set(background, new Depth(999));
        World.Set(background, new SpriteAnimation(Content.SpriteAnimations.BG, 0));

        var uiTickerBackground = World.CreateEntity();
        World.Set(uiTickerBackground, new Position(0, 0));
        World.Set(uiTickerBackground, new Depth(1));
        World.Set(uiTickerBackground, new SpriteAnimation(Content.SpriteAnimations.HUD_Ticker, 0));

        var uiBottomBackground = World.CreateEntity();
        World.Set(uiBottomBackground, new Position(0, Dimensions.GAME_H - 40));
        World.Set(uiBottomBackground, new Depth(9));
        World.Set(uiBottomBackground, new SpriteAnimation(Content.SpriteAnimations.HUD_Bottom, 0));

        OrdersSystem.InitializeOrders();

        var cashRegisterLeftCollision = World.CreateEntity();
        World.Set(cashRegisterLeftCollision, new Position(15, 70));
        World.Set(cashRegisterLeftCollision, new Rectangle(0, 0, 60, 50));
        World.Set(cashRegisterLeftCollision, new Solid());

        var cashRegisterLeftInteraction = World.CreateEntity();
        World.Set(cashRegisterLeftInteraction, new Position(8, 70));
        World.Set(cashRegisterLeftInteraction, new Rectangle(0, 0, 80, 90));
        World.Set(cashRegisterLeftInteraction, new CanInteract());
        World.Set(cashRegisterLeftInteraction, new CanFillOrders());

        var cashRegisterRightCollision = World.CreateEntity();
        World.Set(cashRegisterRightCollision, new Position(Dimensions.GAME_W, 70));
        World.Set(cashRegisterRightCollision, new Rectangle(-80, 0, 80, 50));
        World.Set(cashRegisterRightCollision, new Solid());

        var cashRegisterRight = World.CreateEntity();
        World.Set(cashRegisterRight, new Position(Dimensions.GAME_W, 70));
        World.Set(cashRegisterRight, new Rectangle(-80, 0, 80, 90));
        World.Set(cashRegisterRight, new CanInteract());
        World.Set(cashRegisterRight, new CanFillOrders());

        var exit = World.CreateEntity();
        World.Set(exit, new Position(Dimensions.GAME_W * 0.5f - 44, 0));
        World.Set(exit, new Rectangle(0, 0, 80, 88));
        World.Set(exit, new StoreExit());
        World.Set(exit, new CanInteract());

        var timer = World.CreateEntity();
        World.Set(timer, new Components.GameTimer(5));
        World.Set(timer, new Position(Dimensions.GAME_W * 0.5f, 38));
        World.Set(timer, new TextDropShadow(1, 1));

        var scoreOne = World.CreateEntity();
        World.Set(scoreOne, new Position(80, 345));
        World.Set(scoreOne, new Score(0));
        World.Set(scoreOne, new DisplayScore(0));
        World.Set(scoreOne, new Text(Fonts.KosugiID, FontSizes.SCORE, "0"));

        var scoreTwo = World.CreateEntity();
        World.Set(scoreTwo, new Position(560, 345));
        World.Set(scoreTwo, new Score(0));
        World.Set(scoreTwo, new DisplayScore(0));

        World.Set(scoreTwo, new Text(Fonts.KosugiID, FontSizes.SCORE, "0"));

        var playerOne = PlayerControllerSystem.SpawnPlayer(0);
        var playerTwo = PlayerControllerSystem.SpawnPlayer(1);

        World.Relate(playerOne, scoreOne, new HasScore());
        World.Relate(playerTwo, scoreTwo, new HasScore());

        var gameInProgressEntity = World.CreateEntity();
        World.Set(gameInProgressEntity, new GameInProgress());

        ShelfSpawner.SpawnShelves();
        ProductSpawner.SpawnAllProducts();
        World.Send(new PlaySongMessage());

    }

    public override void Update(TimeSpan dt)
    {
        TimingSystem.Update(dt);
        UpdateSpriteAnimationSystem.Update(dt);
        GameTimerSystem.Update(dt);
        TickerSystem.Update(dt);
        InputSystem.Update(dt);
        PlayerControllerSystem.Update(dt);
        NPCControllerSystem.Update(dt);
        DroneControllerSystem.Update(dt);
        MotionSystem.Update(dt);
        HoldSystem.Update(dt);
        OrdersSystem.Update(dt);
        DirectionalAnimationSystem.Update(dt);
        SetSpriteAnimationSystem.Update(dt);
        ColorAnimationSystem.Update(dt);
        AudioSystem.Update(dt);

        if (World.SomeMessage<EndGame>())
        {
            World.FinishUpdate();
            AudioSystem.Cleanup();
            World.Dispose();
            Game.SetState(TransitionState);
            return;
        }

        World.FinishUpdate();
    }

    public override void Draw(Window window, double alpha)
    {
        Renderer.Render(Game.MainWindow);
    }

    public override void End()
    {

    }

    public void SetTransitionState(GameState state)
    {
        TransitionState = state;
    }
}
