using System.Collections.Generic;
using UnityEngine;

public interface IAttackArea
{
    bool Contains(Vector2 origin, Vector2 target, Vector2 enemy, float range);
}

// Immutable, shared strategies: no per-frame/per-attack allocations.
public static class AttackAreaStrategies
{
    private sealed class Radius : IAttackArea
    {
        private readonly bool aroundTarget;
        public Radius(bool aroundTarget) { this.aroundTarget = aroundTarget; }
        public bool Contains(Vector2 origin, Vector2 target, Vector2 enemy, float range)
        {
            float radius = aroundTarget ? CharacterCombatRules.SplashRadius : range;
            return (enemy - (aroundTarget ? target : origin)).sqrMagnitude <= radius * radius;
        }
    }
    private sealed class Forward : IAttackArea
    {
        public bool Contains(Vector2 origin, Vector2 target, Vector2 enemy, float range)
        {
            var direction = Direction(origin, target);
            var offset = enemy - origin;
            float along = Vector2.Dot(offset, direction);
            return along >= 0 && along <= range && Mathf.Abs(direction.x * offset.y - direction.y * offset.x) <= 1;
        }
    }
    private sealed class Cone : IAttackArea
    {
        public bool Contains(Vector2 origin, Vector2 target, Vector2 enemy, float range)
        {
            var offset = enemy - origin;
            float along = Vector2.Dot(offset, Direction(origin, target));
            return offset.sqrMagnitude <= range * range && along >= 0 &&
                (offset.sqrMagnitude < .000001f || along >= offset.magnitude * .8660254f);
        }
    }
    private static Vector2 Direction(Vector2 origin, Vector2 target)
    {
        var direction = (target - origin).normalized;
        return direction.sqrMagnitude < .000001f ? Vector2.up : direction;
    }
    private static readonly IAttackArea radial = new Radius(false);
    private static readonly Dictionary<string, IAttackArea> areas = new Dictionary<string, IAttackArea>
    {
        { "spin_melee", radial }, { "trio_melee", radial },
        { "target_area", new Radius(true) }, { "forward_area", new Forward() }, { "breath_area", new Cone() }
    };
    public static IAttackArea Find(string id) => id != null && areas.TryGetValue(id, out var area) ? area : null;
}
