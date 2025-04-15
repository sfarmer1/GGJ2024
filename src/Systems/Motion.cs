using System;
using System.Numerics;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Content;
using Tactician.Messages;
using Tactician.Relations;
using Tactician.Utility;

namespace Tactician.Systems;

public class Motion : MoonTools.ECS.System
{
    Filter VelocityFilter;
    Filter InteractFilter;
    Filter SolidFilter;
    Filter AccelerateToPositionFilter;

    SpatialHash<Entity> InteractSpatialHash = new SpatialHash<Entity>(0, 0, Dimensions.GAME_W, Dimensions.GAME_H, 32);
    SpatialHash<Entity> SolidSpatialHash = new SpatialHash<Entity>(0, 0, Dimensions.GAME_W, Dimensions.GAME_H, 32);

    public Motion(World world) : base(world)
    {
        VelocityFilter = FilterBuilder.Include<Position>().Include<Velocity>().Build();
        InteractFilter = FilterBuilder.Include<Position>().Include<Rectangle>().Include<CanInteract>().Build();
        SolidFilter = FilterBuilder.Include<Position>().Include<Rectangle>().Include<Solid>().Build();
        AccelerateToPositionFilter = FilterBuilder.Include<Position>().Include<AccelerateToPosition>().Include<Velocity>().Build();
    }

    void ClearCanBeHeldSpatialHash()
    {
        InteractSpatialHash.Clear();
    }

    void ClearSolidSpatialHash()
    {
        SolidSpatialHash.Clear();
    }

    Rectangle GetWorldRect(Position p, Rectangle r)
    {
        return new Rectangle(p.X + r.X, p.Y + r.Y, r.Width, r.Height);
    }

    (Entity other, bool hit) CheckSolidCollision(Entity e, Rectangle rect)
    {
        foreach (var (other, otherRect) in SolidSpatialHash.Retrieve(e, rect))
        {
            if (rect.Intersects(otherRect))
            {
                return (other, true);
            }
        }

        return (default, false);
    }

    Position SweepTest(Entity e, float dt)
    {
        var velocity = Get<Velocity>(e);
        var position = Get<Position>(e);
        var r = Get<Rectangle>(e);

        var movement = new Vector2(velocity.X, velocity.Y) * dt;
        var targetPosition = position + movement;

        var xEnum = new IntegerEnumerator(position.X, targetPosition.X);
        var yEnum = new IntegerEnumerator(position.Y, targetPosition.Y);

        int mostRecentValidXPosition = position.X;
        int mostRecentValidYPosition = position.Y;

        bool xHit = false;
        bool yHit = false;

        foreach (var x in xEnum)
        {
            var newPos = new Position(x, position.Y);
            var rect = GetWorldRect(newPos, r);

            (var other, var hit) = CheckSolidCollision(e, rect);

            xHit = hit;

            if (xHit && Has<Solid>(other) && Has<Solid>(e))
            {
                movement.X = mostRecentValidXPosition - position.X;
                position = position.SetX(position.X); // truncates x coord
                break;
            }

            mostRecentValidXPosition = x;
        }

        foreach (var y in yEnum)
        {
            var newPos = new Position(mostRecentValidXPosition, y);
            var rect = GetWorldRect(newPos, r);

            (var other, var hit) = CheckSolidCollision(e, rect);
            yHit = hit;

            if (yHit && Has<Solid>(other) && Has<Solid>(e))
            {
                movement.Y = mostRecentValidYPosition - position.Y;
                position = position.SetY(position.Y); // truncates y coord
                break;
            }

            mostRecentValidYPosition = y;
        }

        return position + movement;
    }

    public override void Update(TimeSpan delta)
    {
        ClearCanBeHeldSpatialHash();
        ClearSolidSpatialHash();

        foreach (var entity in InteractFilter.Entities)
        {
            var position = Get<Position>(entity);
            var rect = Get<Rectangle>(entity);

            InteractSpatialHash.Insert(entity, GetWorldRect(position, rect));
        }

        foreach (var entity in InteractFilter.Entities)
        {
            foreach (var other in OutRelations<Colliding>(entity))
            {
                Unrelate<Colliding>(entity, other);
            }
        }

        foreach (var entity in InteractFilter.Entities)
        {
            var position = Get<Position>(entity);
            var rect = GetWorldRect(position, Get<Rectangle>(entity));

            foreach (var (other, otherRect) in InteractSpatialHash.Retrieve(rect))
            {
                if (rect.Intersects(otherRect))
                {
                    Relate(entity, other, new Colliding());
                }

            }
        }

        foreach (var entity in SolidFilter.Entities)
        {
            var position = Get<Position>(entity);
            var rect = Get<Rectangle>(entity);
            SolidSpatialHash.Insert(entity, GetWorldRect(position, rect));
        }

        foreach (var entity in VelocityFilter.Entities)
        {
            if (HasOutRelation<DontMove>(entity))
                continue;

            var pos = Get<Position>(entity);
            var vel = (Vector2)Get<Velocity>(entity);

            if (Has<Rectangle>(entity) && Has<Solid>(entity))
            {
                var result = SweepTest(entity, (float)delta.TotalSeconds);
                Set(entity, result);
            }
            else
            {
                var scaledVelocity = vel * (float)delta.TotalSeconds;
                if (Has<ForceIntegerMovement>(entity))
                {
                    scaledVelocity = new Vector2((int)scaledVelocity.X, (int)scaledVelocity.Y);
                }
                Set(entity, pos + scaledVelocity);
            }

            if (Has<FallSpeed>(entity))
            {
                var fallspeed = Get<FallSpeed>(entity).Speed;
                Set(entity, new Velocity(vel + Vector2.UnitY * fallspeed));
            }

            if (Has<MotionDamp>(entity))
            {
                var speed = Vector2.Distance(Vector2.Zero, vel) - Get<MotionDamp>(entity).Damping;
                speed = MathF.Max(speed, 0);
                vel = speed * MathUtilities.SafeNormalize(vel);
                Set(entity, new Velocity(vel));
            }

            if (Has<DestroyAtScreenBottom>(entity) && pos.Y > Dimensions.GAME_H - 32)
            {
                if (HasOutRelation<UpdateDisplayScoreOnDestroy>(entity))
                {
                    var outEntity = OutRelationSingleton<UpdateDisplayScoreOnDestroy>(entity);
                    var scoreEntity = OutRelationSingleton<HasScore>(outEntity);
                    var data = GetRelationData<UpdateDisplayScoreOnDestroy>(entity, outEntity);
                    var score = Get<DisplayScore>(scoreEntity).Value + (data.Negative ? -1 : 1);
                    Set(scoreEntity, new Text(Content.Fonts.KosugiID, FontSizes.SCORE, score.ToString()));
                    Set(scoreEntity, new DisplayScore(score));

                    // TODO: shouldn't tightly couple this exact money sound behavior to DestroyAtScreenBottom but hey it's a jam game
                    var pan = (((float)pos.X / Dimensions.GAME_W * 2f) - 1f) / 1.5f;
                    var pitch = .9f + (.1f * (float)score / 800);

                    Send(new PlayStaticSoundMessage(
                        Rando.GetRandomItem(AudioArrays.Coins),
                        Data.SoundCategory.Generic,
                        2f,
                        pitch,
                        pan
                    ));
                }

                Destroy(entity);
            }

            if (Has<DestroyWhenOutOfBounds>(entity))
            {
                if (pos.X < -100 || pos.X > Dimensions.GAME_W + 100 || pos.Y < -100 || pos.Y > Dimensions.GAME_H + 100)
                {
                    foreach (var heldEntity in OutRelations<Holding>(entity))
                    {
                        Destroy(heldEntity);
                    }

                    Destroy(entity);
                }
            }

            // update spatial hashes

            if (Has<CanInteract>(entity))
            {
                var position = Get<Position>(entity);
                var rect = Get<Rectangle>(entity);

                InteractSpatialHash.Insert(entity, GetWorldRect(position, rect));
            }

            if (Has<Solid>(entity))
            {
                var position = Get<Position>(entity);
                var rect = Get<Rectangle>(entity);
                SolidSpatialHash.Insert(entity, GetWorldRect(position, rect));
            }
        }

        foreach (var entity in SolidFilter.Entities)
        {
            UnrelateAll<TouchingSolid>(entity);
        }

        foreach (var entity in SolidFilter.Entities)
        {
            var position = Get<Position>(entity);
            var rectangle = Get<Rectangle>(entity);

            var leftPos = new Position(position.X - 1, position.Y);
            var rightPos = new Position(position.X + 1, position.Y);
            var upPos = new Position(position.X, position.Y - 1);
            var downPos = new Position(position.X, position.Y + 1);

            var leftRectangle = GetWorldRect(leftPos, rectangle);
            var rightRectangle = GetWorldRect(rightPos, rectangle);
            var upRectangle = GetWorldRect(upPos, rectangle);
            var downRectangle = GetWorldRect(downPos, rectangle);

            var (leftOther, leftCollided) = CheckSolidCollision(entity, leftRectangle);
            var (rightOther, rightCollided) = CheckSolidCollision(entity, rightRectangle);
            var (upOther, upCollided) = CheckSolidCollision(entity, upRectangle);
            var (downOther, downCollided) = CheckSolidCollision(entity, downRectangle);

            if (leftCollided)
            {
                Relate(entity, leftOther, new TouchingSolid());
            }

            if (rightCollided)
            {
                Relate(entity, rightOther, new TouchingSolid());
            }

            if (upCollided)
            {
                Relate(entity, upOther, new TouchingSolid());
            }
            if (downCollided)
            {
                Relate(entity, downOther, new TouchingSolid());
            }
        }

        foreach (var entity in AccelerateToPositionFilter.Entities)
        {
            var velocity = Get<Velocity>(entity).Value;
            var position = Get<Position>(entity);
            var accelTo = Get<AccelerateToPosition>(entity);
            var difference = accelTo.Target - position;
            velocity /= accelTo.MotionDampFactor * (1 + (float)delta.TotalSeconds); // TODO: IDK if this is deltatime friction but game is fixed fps rn anyway
            velocity += MathUtilities.SafeNormalize(difference) * accelTo.Acceleration * (float)delta.TotalSeconds;
            Set(entity, new Velocity(velocity));
        }
    }
}
