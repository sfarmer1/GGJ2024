using System;
using System.Numerics;
using MoonTools.ECS;
using Tactician.Components;
using Tactician.Utility;

namespace Tactician.Systems;

public class MotionSystem : MoonTools.ECS.System {
    private readonly Filter _accelerateToPositionFilter;
    private readonly Filter _interactFilter;

    private readonly SpatialHash<Entity> _interactSpatialHash = new(0, 0, Dimensions.GAME_W, Dimensions.GAME_H, 32);
    private readonly Filter _solidFilter;
    private readonly SpatialHash<Entity> _solidSpatialHash = new(0, 0, Dimensions.GAME_W, Dimensions.GAME_H, 32);
    private readonly Filter _velocityFilter;

    public MotionSystem(World world) : base(world) {
        _velocityFilter = FilterBuilder.Include<Position>().Include<Velocity>().Build();
        _interactFilter = FilterBuilder.Include<Position>().Include<Rectangle>().Include<CanInteract>().Build();
        _solidFilter = FilterBuilder.Include<Position>().Include<Rectangle>().Include<Solid>().Build();
        _accelerateToPositionFilter = FilterBuilder.Include<Position>().Include<AccelerateToPosition>()
            .Include<Velocity>().Build();
    }

    private void ClearCanBeHeldSpatialHash() {
        _interactSpatialHash.Clear();
    }

    private void ClearSolidSpatialHash() {
        _solidSpatialHash.Clear();
    }

    private Rectangle GetWorldRect(Position p, Rectangle r) {
        return new Rectangle(p.X + r.X, p.Y + r.Y, r.Width, r.Height);
    }

    private (Entity other, bool hit) CheckSolidCollision(Entity e, Rectangle rect) {
        foreach (var (other, otherRect) in _solidSpatialHash.Retrieve(e, rect))
            if (rect.Intersects(otherRect))
                return (other, true);

        return (default, false);
    }

    private Position SweepTest(Entity e, float dt) {
        var velocity = Get<Velocity>(e);
        var position = Get<Position>(e);
        var r = Get<Rectangle>(e);

        var movement = new Vector2(velocity.X, velocity.Y) * dt;
        var targetPosition = position + movement;

        var xEnum = new IntegerEnumerator(position.X, targetPosition.X);
        var yEnum = new IntegerEnumerator(position.Y, targetPosition.Y);

        var mostRecentValidXPosition = position.X;
        var mostRecentValidYPosition = position.Y;

        var xHit = false;
        var yHit = false;

        foreach (var x in xEnum) {
            var newPos = new Position(x, position.Y);
            var rect = GetWorldRect(newPos, r);

            var (other, hit) = CheckSolidCollision(e, rect);

            xHit = hit;

            if (xHit && Has<Solid>(other) && Has<Solid>(e)) {
                movement.X = mostRecentValidXPosition - position.X;
                position = position.SetX(position.X); // truncates x coord
                break;
            }

            mostRecentValidXPosition = x;
        }

        foreach (var y in yEnum) {
            var newPos = new Position(mostRecentValidXPosition, y);
            var rect = GetWorldRect(newPos, r);

            var (other, hit) = CheckSolidCollision(e, rect);
            yHit = hit;

            if (yHit && Has<Solid>(other) && Has<Solid>(e)) {
                movement.Y = mostRecentValidYPosition - position.Y;
                position = position.SetY(position.Y); // truncates y coord
                break;
            }

            mostRecentValidYPosition = y;
        }

        return position + movement;
    }

    public override void Update(TimeSpan delta) {
        ClearCanBeHeldSpatialHash();
        ClearSolidSpatialHash();

        foreach (var entity in _interactFilter.Entities) {
            var position = Get<Position>(entity);
            var rect = Get<Rectangle>(entity);

            _interactSpatialHash.Insert(entity, GetWorldRect(position, rect));
        }

        foreach (var entity in _interactFilter.Entities)
        foreach (var other in OutRelations<Colliding>(entity))
            Unrelate<Colliding>(entity, other);

        foreach (var entity in _interactFilter.Entities) {
            var position = Get<Position>(entity);
            var rect = GetWorldRect(position, Get<Rectangle>(entity));

            foreach (var (other, otherRect) in _interactSpatialHash.Retrieve(rect))
                if (rect.Intersects(otherRect))
                    Relate(entity, other, new Colliding());
        }

        foreach (var entity in _solidFilter.Entities) {
            var position = Get<Position>(entity);
            var rect = Get<Rectangle>(entity);
            _solidSpatialHash.Insert(entity, GetWorldRect(position, rect));
        }

        foreach (var entity in _velocityFilter.Entities) {
            if (HasOutRelation<DontMove>(entity))
                continue;

            var pos = Get<Position>(entity);
            var vel = (Vector2)Get<Velocity>(entity);

            if (Has<Rectangle>(entity) && Has<Solid>(entity)) {
                var result = SweepTest(entity, (float)delta.TotalSeconds);
                Set(entity, result);
            }
            else {
                var scaledVelocity = vel * (float)delta.TotalSeconds;
                if (Has<ForceIntegerMovement>(entity))
                    scaledVelocity = new Vector2((int)scaledVelocity.X, (int)scaledVelocity.Y);
                Set(entity, pos + scaledVelocity);
            }

            if (Has<FallSpeed>(entity)) {
                var fallspeed = Get<FallSpeed>(entity).Speed;
                Set(entity, new Velocity(vel + Vector2.UnitY * fallspeed));
            }

            if (Has<MotionDamp>(entity)) {
                var speed = Vector2.Distance(Vector2.Zero, vel) - Get<MotionDamp>(entity).Damping;
                speed = MathF.Max(speed, 0);
                vel = speed * MathUtilities.SafeNormalize(vel);
                Set(entity, new Velocity(vel));
            }

            if (Has<DestroyWhenOutOfBounds>(entity))
                if (pos.X < -100 || pos.X > Dimensions.GAME_W + 100 || pos.Y < -100 ||
                    pos.Y > Dimensions.GAME_H + 100) {
                    foreach (var heldEntity in OutRelations<Holding>(entity)) Destroy(heldEntity);

                    Destroy(entity);
                }

            // update spatial hashes

            if (Has<CanInteract>(entity)) {
                var position = Get<Position>(entity);
                var rect = Get<Rectangle>(entity);

                _interactSpatialHash.Insert(entity, GetWorldRect(position, rect));
            }

            if (Has<Solid>(entity)) {
                var position = Get<Position>(entity);
                var rect = Get<Rectangle>(entity);
                _solidSpatialHash.Insert(entity, GetWorldRect(position, rect));
            }
        }

        foreach (var entity in _solidFilter.Entities) UnrelateAll<TouchingSolid>(entity);

        foreach (var entity in _solidFilter.Entities) {
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

            if (leftCollided) Relate(entity, leftOther, new TouchingSolid());

            if (rightCollided) Relate(entity, rightOther, new TouchingSolid());

            if (upCollided) Relate(entity, upOther, new TouchingSolid());
            if (downCollided) Relate(entity, downOther, new TouchingSolid());
        }

        foreach (var entity in _accelerateToPositionFilter.Entities) {
            var velocity = Get<Velocity>(entity).Value;
            var position = Get<Position>(entity);
            var accelTo = Get<AccelerateToPosition>(entity);
            var difference = accelTo.Target - position;
            velocity /= accelTo.MotionDampFactor *
                        (1 + (float)delta
                            .TotalSeconds); // TODO: IDK if this is deltatime friction but game is fixed fps rn anyway
            velocity += MathUtilities.SafeNormalize(difference) * accelTo.Acceleration * (float)delta.TotalSeconds;
            Set(entity, new Velocity(velocity));
        }
    }
}