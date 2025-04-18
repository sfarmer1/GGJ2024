using System;
using System.Numerics;
using MoonTools.ECS;
using MoonWorks.Graphics;
using Tactician.Components;
using Tactician.Content;
using Tactician.Data;
using Tactician.Messages;
using Tactician.Relations;
using Tactician.Utility;
using Filter = MoonTools.ECS.Filter;

namespace Tactician.Systems;

public class PlayerControllerSystem : MoonTools.ECS.System {
    private readonly float MaxSpeedBase = 128f;
    private readonly Filter PlayerFilter;

    public PlayerControllerSystem(World world) : base(world) {
        PlayerFilter =
            FilterBuilder
                .Include<Player>()
                .Include<Position>()
                .Build();
    }

    public Entity SpawnPlayer(int index) {
        var player = World.CreateEntity();
        World.Set(player, new Position(Dimensions.GAME_W * 0.47f + index * 48.0f, Dimensions.GAME_H * 0.25f));
        World.Set(player,
            new SpriteAnimation(index == 0 ? SpriteAnimations.Char_Walk_Down : SpriteAnimations.Char2_Walk_Down, 0));
        World.Set(player, new Player(index));
        World.Set(player, new Rectangle(-8, -8, 16, 16));
        World.Set(player, new CanInteract());
        World.Set(player, new CanInspect());
        World.Set(player, new CanHold());
        World.Set(player, new CanBeStolenFrom());
        World.Set(player, new Solid());
        World.Set(player, index == 0 ? Color.Green : Color.Blue);
        World.Set(player, new Depth(5));
        World.Set(player, new MaxSpeed(128));
        World.Set(player, new Velocity(Vector2.Zero));
        World.Set(player, new LastDirection(Vector2.Zero));
        World.Set(player, new AdjustFramerateToSpeed());
        World.Set(player, new InputState());
        World.Set(player, new DirectionalSprites(
            index == 0 ? SpriteAnimations.Char_Walk_Up.ID : SpriteAnimations.Char2_Walk_Up.ID,
            index == 0 ? SpriteAnimations.Char_Walk_UpRight.ID : SpriteAnimations.Char2_Walk_UpRight.ID,
            index == 0 ? SpriteAnimations.Char_Walk_Right.ID : SpriteAnimations.Char2_Walk_Right.ID,
            index == 0 ? SpriteAnimations.Char_Walk_DownRight.ID : SpriteAnimations.Char2_Walk_DownRight.ID,
            index == 0 ? SpriteAnimations.Char_Walk_Down.ID : SpriteAnimations.Char2_Walk_Down.ID,
            index == 0 ? SpriteAnimations.Char_Walk_DownLeft.ID : SpriteAnimations.Char2_Walk_DownLeft.ID,
            index == 0 ? SpriteAnimations.Char_Walk_Left.ID : SpriteAnimations.Char2_Walk_Left.ID,
            index == 0 ? SpriteAnimations.Char_Walk_UpLeft.ID : SpriteAnimations.Char2_Walk_UpLeft.ID
        ));

        return player;
    }

    public override void Update(TimeSpan delta) {
        if (!Some<GameInProgress>()) return;

        var deltaTime = (float)delta.TotalSeconds;

        foreach (var entity in PlayerFilter.Entities) {
            var playerIndex = Get<Player>(entity).Index;
            var direction = Vector2.Zero;

            #region Input

            var inputState = Get<InputState>(entity);

            if (inputState.Left.IsDown)
                direction.X = -1;
            else if (inputState.Right.IsDown) direction.X = 1;

            if (inputState.Up.IsDown)
                direction.Y = -1;
            else if (inputState.Down.IsDown) direction.Y = 1;

            #endregion

            if (inputState.Interact.IsPressed) Set(entity, new TryHold());

            // Movement
            var velocity = Get<Velocity>(entity).Value;

            var accelSpeed = 128;

            velocity += direction * accelSpeed * deltaTime * 60;

            if (Has<FunnyRunTimer>(entity)) {
                var time = Get<FunnyRunTimer>(entity).Time - deltaTime;
                if (time < 0)
                    Remove<FunnyRunTimer>(entity);
                else
                    Set(entity, new FunnyRunTimer(time));
            }

            var maxSpeed = Get<MaxSpeed>(entity).Value;

            // limit max speed
            if (velocity.Length() > maxSpeed) velocity = MathUtilities.SafeNormalize(velocity) * maxSpeed;

            if (direction.LengthSquared() > 0) {
                var dot = Vector2.Dot(MathUtilities.SafeNormalize(direction),
                    MathUtilities.SafeNormalize(Get<LastDirection>(entity).Direction));
                if (dot < 0) Set(entity, new CanFunnyRun());

                if (Has<CanFunnyRun>(entity)) {
                    maxSpeed = (maxSpeed + MaxSpeedBase) / 2f;
                    Remove<CanFunnyRun>(entity);
                    Set(entity, new FunnyRunTimer(.25f));
                }

                direction = MathUtilities.SafeNormalize(direction);

                var maxAdd = deltaTime * 30;
                if (HasOutRelation<Holding>(entity)) maxAdd /= 2;

                maxSpeed = Math.Min(maxSpeed + maxAdd, 300);
                Set(entity, new MaxSpeed(maxSpeed));
                Set(entity, new LastDirection(direction));
            }
            else {
                Set(entity, new CanFunnyRun());
                var speed = Get<Velocity>(entity).Value.Length();
                speed = Math.Max(speed - accelSpeed * deltaTime * 60, 0);
                velocity = MathUtilities.SafeNormalize(velocity) * speed;
                Set(entity, new MaxSpeed(MaxSpeedBase));
            }

            // #region walking sfx
            // if (!HasOutRelation<TimingFootstepAudio>(entity) && framerate > 0)
            // {
            // 	PlayRandomFootstep();

            // 	var footstepTimer = World.CreateEntity();
            // 	var footstepDuration = Math.Clamp(1f - (framerate / 50f), .5f, 1f);
            // 	Set(footstepTimer, new Timer(footstepDuration));
            // 	World.Relate(entity, footstepTimer, new TimingFootstepAudio());
            // }
            // #endregion

            Set(entity, new Velocity(velocity));
            var depth = float.Lerp(100, 10, Get<Position>(entity).Y / (float)Dimensions.GAME_H);
            Set(entity, new Depth(depth));
        }
    }

    private void PlayRandomFootstep() {
        Send(
            new PlayStaticSoundMessage(
                new[] {
                    StaticAudio.Footstep1,
                    StaticAudio.Footstep2,
                    StaticAudio.Footstep3,
                    StaticAudio.Footstep4,
                    StaticAudio.Footstep5
                }.GetRandomItem(),
                SoundCategory.Generic,
                Rando.Range(0.66f, 0.88f),
                Rando.Range(-.05f, .05f)
            )
        );
    }
}