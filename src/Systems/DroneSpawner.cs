using MoonTools.ECS;
using System.Numerics;
using Tactician.Content;
using Tactician.Components;
using Tactician.Data;
using Tactician.Messages;
using Tactician.Relations;
using Tactician.Utility;

namespace Tactician.Systems;

public class DroneSpawner : Manipulator
{
    ProductSpawner Product;

    StaticSoundID[] DroneSounds =
    [
        StaticAudio.Drone1,
        StaticAudio.Drone2,
        StaticAudio.Drone3,
        StaticAudio.Drone4,
        StaticAudio.Drone5,
        StaticAudio.Drone6,
        StaticAudio.Drone7,
        StaticAudio.Drone8,
        StaticAudio.Drone9,
        StaticAudio.Drone10,
        StaticAudio.Drone11,
        StaticAudio.Drone12,
        StaticAudio.Drone13,
        StaticAudio.Drone14,
        StaticAudio.Drone15,
        StaticAudio.Drone16
    ];

    public StaticSoundID[] EvilDroneSounds =
    [
        StaticAudio.Evil1,
        StaticAudio.Evil2,
        StaticAudio.Evil3,
        StaticAudio.Evil4,
        StaticAudio.Evil5,
        StaticAudio.Evil6,
        StaticAudio.Evil7,
        StaticAudio.Evil8,
        StaticAudio.Evil9,
        StaticAudio.Evil10,
        StaticAudio.Evil11,
        StaticAudio.Evil12,
        StaticAudio.Evil13,
        StaticAudio.EvilDrone
    ];

    public DroneSpawner(World world) : base(world)
    {
        Product = new ProductSpawner(world);
    }

    public void SpawnRestockDrone(Entity emptyProductSpawner)
    {
        // spawn in random border position
        var xPosition = Rando.IntInclusive(0, 1) == 0 ? Rando.IntInclusive(-75, -25) : Rando.IntInclusive(Dimensions.GAME_W + 25, Dimensions.GAME_W + 75);
        var yPosition = Rando.IntInclusive(-25, Dimensions.GAME_H - 50);
        var position = new Position(xPosition, yPosition);

        var drone = World.CreateEntity();
        Set(drone, position);
        Set(drone, new SpriteAnimation(SpriteAnimations.NPC_Drone_Fly_Down, 60));
        Set(drone, new Rectangle(-8, -8, 16, 16));
        Set(drone, new CanInteract());
        Set(drone, new CanHold());
        Set(drone, new Depth(5));
        Set(drone, new DirectionalSprites(
            SpriteAnimations.NPC_Drone_Fly_Up.ID,
            SpriteAnimations.NPC_Drone_Fly_UpRight.ID,
            SpriteAnimations.NPC_Drone_Fly_Right.ID,
            SpriteAnimations.NPC_Drone_Fly_DownRight.ID,
            SpriteAnimations.NPC_Drone_Fly_Down.ID,
            SpriteAnimations.NPC_Drone_Fly_DownLeft.ID,
            SpriteAnimations.NPC_Drone_Fly_Left.ID,
            SpriteAnimations.NPC_Drone_Fly_UpLeft.ID
        ));
        Set(drone, new CanTargetProductSpawner());
        Set(drone, new Velocity(Vector2.Zero));
        Set(drone, new DestroyWhenOutOfBounds());
        Set(drone, new DestroyAtGameEnd());

        // spawn product related to spawner
        Entity product;
        if (Has<SpawnCategory>(emptyProductSpawner))
        {
            product = Product.SpawnProduct(position, Get<SpawnCategory>(emptyProductSpawner).Category);
        }
        else
        {
            product = Product.SpawnRandomProduct(position);
        }

        Relate(drone, product, new Holding());
        Relate(drone, emptyProductSpawner, new Targeting());
        Relate(product, emptyProductSpawner, new BelongsToProductSpawner());

        if (Rando.Value < 0.5f)
            Send(new PlayStaticSoundMessage(Rando.GetRandomItem(DroneSounds), SoundCategory.Drone, 0.5f));
    }

    public void SpawnEvilDrone(Entity productToSteal)
    {
        // spawn in random border position
        var xPosition = Rando.IntInclusive(0, 1) == 0 ? Rando.IntInclusive(-75, -25) : Rando.IntInclusive(Dimensions.GAME_W + 25, Dimensions.GAME_W + 75);
        var yPosition = Rando.IntInclusive(-25, Dimensions.GAME_H - 50);
        var position = new Position(xPosition, yPosition);

        var drone = World.CreateEntity();
        Set(drone, position);
        Set(drone, new Velocity(Vector2.Zero));
        Set(drone, new SpriteAnimation(SpriteAnimations.NPC_DroneEvil_Fly_Down, 60));
        Set(drone, new Rectangle(-8, -8, 16, 16));
        Set(drone, new CanInteract());
        Set(drone, new CanHold());
        Set(drone, new Depth(5));
        // TODO: evil drone sprites
        Set(drone, new DirectionalSprites(
            SpriteAnimations.NPC_DroneEvil_Fly_Up.ID,
            SpriteAnimations.NPC_DroneEvil_Fly_UpRight.ID,
            SpriteAnimations.NPC_DroneEvil_Fly_Right.ID,
            SpriteAnimations.NPC_DroneEvil_Fly_DownRight.ID,
            SpriteAnimations.NPC_DroneEvil_Fly_Down.ID,
            SpriteAnimations.NPC_DroneEvil_Fly_DownLeft.ID,
            SpriteAnimations.NPC_DroneEvil_Fly_Left.ID,
            SpriteAnimations.NPC_DroneEvil_Fly_UpLeft.ID
        ));
        Set(drone, new CanStealProducts());
        Set(drone, new DestroyWhenOutOfBounds());
        Set(drone, new DestroyAtGameEnd());

        Relate(drone, productToSteal, new Targeting());

        Send(new PlayStaticSoundMessage(Rando.GetRandomItem(EvilDroneSounds)));
    }
}
