using System;
using MoonTools.ECS;
using MoonWorks.Graphics.Font;
using MoonWorks.Math;
using Tactician.Components;
using Tactician.Content;
using Tactician.Data;
using Tactician.Messages;
using Tactician.Utility;

namespace Tactician.Systems;

public class UpdateSpriteAnimationSystem : MoonTools.ECS.System {
    private readonly Filter _flickerFilter;
    private readonly Filter _slowDownAnimationFilter;
    private readonly Filter _spriteAnimationFilter;
    private readonly Filter _textFilter;

    public UpdateSpriteAnimationSystem(World world) : base(world) {
        _spriteAnimationFilter = FilterBuilder
            .Include<SpriteAnimation>()
            .Include<Position>()
            .Build();
        _flickerFilter = FilterBuilder.Include<ColorFlicker>().Build();
        _textFilter = FilterBuilder.Include<Text>().Build();
        _slowDownAnimationFilter = FilterBuilder.Include<SlowDownAnimation>().Include<Position>().Build();
    }

    public override void Update(TimeSpan delta) {
        foreach (var entity in _spriteAnimationFilter.Entities) UpdateSpriteAnimation(entity, (float)delta.TotalSeconds);


        // Slows down item animation
        foreach (var entity in _slowDownAnimationFilter.Entities) {
            var c = Get<SlowDownAnimation>(entity);
            var goal = c.BaseSpeed;
            var step = c.step;
            var currentAnimation = Get<SpriteAnimation>(entity);
            var frameRate = currentAnimation.FrameRate;
            frameRate = Math.Max(frameRate - step, goal);
            Set(entity, currentAnimation.ChangeFramerate(frameRate));
        }

        // Flicker
        foreach (var entity in _flickerFilter.Entities) {
            var flicker = Get<ColorFlicker>(entity);
            var frames = flicker.ElapsedFrames + 1;
            Set(entity, new ColorFlicker(frames, flicker.Color));
        }

        // Score screen text
        foreach (var entity in _textFilter.Entities)
            if (HasOutRelation<CountUpScore>(entity) && !HasOutRelation<DontDraw>(entity)) {
                var timerEntity = OutRelationSingleton<CountUpScore>(entity);
                var timeFactor = 1 - Get<Timer>(timerEntity).Remaining;
                var data = GetRelationData<CountUpScore>(entity, timerEntity);
                var value = (int)Math.Floor(float.Lerp(data.Start, data.End, Easing.InOutExpo(timeFactor)));
                Set(entity, new Text(
                    Fonts.KosugiID,
                    FontSizes.SCORE,
                    $"{value}",
                    HorizontalAlignment.Center,
                    VerticalAlignment.Middle
                ));

                var lastValue = Get<LastValue>(entity).value;
                if (lastValue != value)
                    Send(new PlayStaticSoundMessage(AudioArrays.Coins.GetRandomItem(), SoundCategory.Generic, 1f,
                        .9f + .1f * (value / 1000f)));
            }
    }

    public void UpdateSpriteAnimation(Entity entity, float dt) {
        var spriteAnimation = Get<SpriteAnimation>(entity).Update(dt);
        Set(entity, spriteAnimation);

        if (spriteAnimation.Finished) {
            /*
            if (Has<DestroyOnAnimationFinish>(entity))
            {
                Destroy(entity);
            }
            */
        }
    }
}