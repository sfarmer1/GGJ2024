using System;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Relations;
using Components_Timer = Tactician.Components.Timer;
using Timer = Tactician.Components.Timer;

namespace Tactician.Systems;

public class Timing : MoonTools.ECS.System
{
    private Filter TimerFilter;

    public Timing(World world) : base(world)
    {
        TimerFilter = FilterBuilder
            .Include<Components_Timer>()
            .Build();
    }

    public override void Update(TimeSpan delta)
    {
        foreach (var entity in TimerFilter.Entities)
        {
            if (HasOutRelation<DontTime>(entity)) continue;

            var timer = Get<Components_Timer>(entity);
            var time = timer.Time - (float)delta.TotalSeconds;

            if (time <= 0)
            {
                if (HasOutRelation<TeleportToAtTimerEnd>(entity))
                {
                    var outEntity = OutRelationSingleton<TeleportToAtTimerEnd>(entity);
                    var data = World.GetRelationData<TeleportToAtTimerEnd>(entity, outEntity);
                    var entityToTeleportTo = data.TeleportTo;
                    var position = Get<Position>(entityToTeleportTo);
                    Set(outEntity, position);
                }

                if (Has<PlaySoundOnTimerEnd>(entity))
                {
                    var soundMessage = Get<PlaySoundOnTimerEnd>(entity).PlayStaticSoundMessage;
                    Send(soundMessage);
                }

                Destroy(entity);
                return;
            }

            Set(entity, timer with { Time = time });
        }
    }
}
