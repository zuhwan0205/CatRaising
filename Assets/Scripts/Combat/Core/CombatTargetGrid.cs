using System;
using System.Collections.Generic;
using UnityEngine;

// Rebuilt after movement, queried by all attackers. Original insertion order breaks ties.
public sealed class CombatTargetGrid<T> where T : class
{
    private struct Entry { public T value; public Vector2 position; public int order; }
    private readonly Dictionary<Vector2Int, List<Entry>> cells = new Dictionary<Vector2Int, List<Entry>>();
    private readonly Func<T, bool> alive;
    private int count;
    private const float CellSize = 2;
    public CombatTargetGrid(Func<T, bool> isAlive) { alive = isAlive; }
    private static Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x / CellSize), Mathf.FloorToInt(p.y / CellSize));
    public void Clear() { foreach (var bucket in cells.Values) bucket.Clear(); count = 0; }
    public void Add(T value, Vector2 position)
    {
        var key = Cell(position);
        if (!cells.TryGetValue(key, out var bucket)) cells[key] = bucket = new List<Entry>();
        bucket.Add(new Entry { value = value, position = position, order = count++ });
    }
    public T Nearest(Vector2 origin, float range)
    {
        var min = Cell(origin - Vector2.one * range);
        var max = Cell(origin + Vector2.one * range);
        T best = null;
        float distance = range * range;
        int order = int.MaxValue;
        for (int y = min.y; y <= max.y; y++)
        for (int x = min.x; x <= max.x; x++)
            if (cells.TryGetValue(new Vector2Int(x, y), out var bucket))
                foreach (var entry in bucket)
                {
                    if (!alive(entry.value)) continue;
                    float d = (entry.position - origin).sqrMagnitude;
                    if (d < distance || (best != null && d == distance && entry.order < order))
                    { best = entry.value; distance = d; order = entry.order; }
                }
        return best;
    }
}
