using System;
using UnityEditor;
using UnityEngine;

public static class CombatTargetGridChecks
{
    private sealed class Target { public Vector2 position; public bool alive = true; }
    [MenuItem("CatRaising/Checks/Combat Target Grid")]
    public static void Run()
    {
        var random = new System.Random(731);
        var grid = new CombatTargetGrid<Target>(target => target.alive);
        var targets = new Target[200];
        int comparisons = 0;
        for (int frame = 0; frame < 30; frame++)
        {
            grid.Clear();
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = new Target { position = new Vector2((float)random.NextDouble() * 14 - 2, (float)random.NextDouble() * 18 - 2) };
                grid.Add(targets[i], targets[i].position);
            }
            // Deaths after indexing simulate earlier towers/projectiles killing a candidate.
            for (int i = 0; i < targets.Length; i += 3) targets[i].alive = false;
            for (int query = 0; query < 100; query++)
            {
                var origin = new Vector2((float)random.NextDouble() * 10, (float)random.NextDouble() * 14);
                float range = (float)random.NextDouble() * 8;
                Target expected = null; float nearest = range * range;
                foreach (var target in targets)
                {
                    float d = (target.position - origin).sqrMagnitude;
                    if (target.alive && d < nearest) { expected = target; nearest = d; }
                }
                if (!ReferenceEquals(expected, grid.Nearest(origin, range))) throw new Exception("Target choice differs from full scan");
                comparisons++;
            }
        }
        grid.Clear();
        var first = new Target { position = Vector2.right };
        var second = new Target { position = Vector2.left };
        grid.Add(first, first.position); grid.Add(second, second.position);
        if (grid.Nearest(Vector2.zero, 1) != null || grid.Nearest(Vector2.zero, 2) != first)
            throw new Exception("Range boundary or equal-distance order changed");
        first.alive = false;
        if (grid.Nearest(Vector2.zero, 2) != second) throw new Exception("Dead target selected");
        grid.Clear();
        if (grid.Nearest(Vector2.zero, 10) != null) throw new Exception("Previous wave retained");
        Debug.Log("COMBAT_TARGET_GRID_PASS: " + comparisons + " full-scan comparisons, ties, boundary, deaths and reset.");
    }
}
