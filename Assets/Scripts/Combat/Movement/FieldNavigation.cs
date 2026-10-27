using System;
using System.Collections.Generic;
using UnityEngine;

// 정적인 원형 발자국과 작은 격자 경로망. 물리/내비메시 베이크 없이 런타임 필드를 지원합니다.
public sealed class FieldNavigation
{
    private struct Obstacle
    {
        public Vector3 center; public float radius; public bool blocksRaid;
    }
    private readonly List<Obstacle> obstacles = new List<Obstacle>();
    private const float BucketSize = 4;
    private readonly Dictionary<Vector2Int, List<Obstacle>> buckets = new Dictionary<Vector2Int, List<Obstacle>>();
    private const int Side = 109;
    private const float Half = FieldMapSize.HalfSize, BodyRadius = .42f;
    private readonly Vector3[] nodes = new Vector3[Side * Side];
    private readonly List<int>[] links = new List<int>[Side * Side];
    private readonly Dictionary<int, int[]> distances = new Dictionary<int, int[]>();
    private readonly Queue<int> distanceOrder = new Queue<int>();
    private readonly Queue<int> searchQueue = new Queue<int>(Side * Side);
    private int pathFrame = -1, pathsThisFrame;

    // 일반 몬스터의 비싼 우회 탐색을 프레임별로 분산합니다.
    public bool TryEnemyWaypoint(Vector3 from, Vector3 goal, out Vector3 waypoint)
    {
        waypoint = from;
        if (pathFrame != Time.frameCount) { pathFrame = Time.frameCount; pathsThisFrame = 0; }
        if (pathsThisFrame >= 8) return false;
        pathsThisFrame++;
        waypoint = Waypoint(from, goal);
        return true;
    }

    public void Add(Vector3 center, float radius, bool blocksRaid = true)
    {
        var obstacle = new Obstacle { center = center, radius = radius, blocksRaid = blocksRaid };
        obstacles.Add(obstacle);
        float extent = radius + BodyRadius;
        for (int z = Mathf.FloorToInt((center.z - extent) / BucketSize); z <= Mathf.FloorToInt((center.z + extent) / BucketSize); z++)
            for (int x = Mathf.FloorToInt((center.x - extent) / BucketSize); x <= Mathf.FloorToInt((center.x + extent) / BucketSize); x++)
            {
                var key = new Vector2Int(x, z);
                if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<Obstacle>();
                list.Add(obstacle);
            }
    }

    public FieldNavigation CreateRaidNavigation()
    {
        var navigation = new FieldNavigation();
        foreach (var obstacle in obstacles)
            if (obstacle.blocksRaid) navigation.Add(obstacle.center, obstacle.radius);
        navigation.Bake();
        return navigation;
    }

    public bool IsFree(Vector3 point)
    {
        if (Math.Abs(point.x) > Half || Math.Abs(point.z) > Half)
            return false;
        if (!buckets.TryGetValue(new Vector2Int(Mathf.FloorToInt(point.x / BucketSize), Mathf.FloorToInt(point.z / BucketSize)), out var nearby)) return true;
        foreach (var obstacle in nearby)
        {
            float dx = point.x - obstacle.center.x, dz = point.z - obstacle.center.z;
            float radius = obstacle.radius + BodyRadius;
            if (dx * dx + dz * dz < radius * radius)
                return false;
        }
        return true;
    }

    public bool Clear(Vector3 from, Vector3 to)
    {
        if (!IsFree(from) || !IsFree(to))
            return false;
        Vector3 line = to - from;
        line.y = 0;
        float length = line.sqrMagnitude;
        for (int z = Mathf.FloorToInt(Mathf.Min(from.z, to.z) / BucketSize); z <= Mathf.FloorToInt(Mathf.Max(from.z, to.z) / BucketSize); z++)
        for (int x = Mathf.FloorToInt(Mathf.Min(from.x, to.x) / BucketSize); x <= Mathf.FloorToInt(Mathf.Max(from.x, to.x) / BucketSize); x++)
        {
            if (!buckets.TryGetValue(new Vector2Int(x, z), out var nearby)) continue;
            foreach (var obstacle in nearby)
            {
            Vector3 offset = obstacle.center - from;
            offset.y = 0;
            float t = length <= .00001f ? 0 : Mathf.Clamp01(Vector3.Dot(offset, line) / length);
            Vector3 delta = from + line * t - obstacle.center;
            delta.y = 0;
            float radius = obstacle.radius + BodyRadius;
            if (delta.sqrMagnitude < radius * radius)
                return false;
        }
        }
        return true;
    }

    public void Bake()
    {
        distances.Clear();
        distanceOrder.Clear();
        searchQueue.Clear();
        pathFrame = -1;
        for (int z = 0; z < Side; z++)
            for (int x = 0; x < Side; x++)
            {
                int i = z * Side + x;
                nodes[i] = new Vector3(x - Half, 0, z - Half);
                links[i] = new List<int>(4);
            }
        for (int z = 0; z < Side; z++)
            for (int x = 0; x < Side; x++)
            {
                int i = z * Side + x;
                if (!IsFree(nodes[i]))
                    continue;
                if (x + 1 < Side)
                    Connect(i, i + 1);
                if (z + 1 < Side)
                    Connect(i, i + Side);
            }
    }

    private void Connect(int a, int b)
    {
        if (!Clear(nodes[a], nodes[b]))
            return;
        links[a].Add(b);
        links[b].Add(a);
    }

    public Vector3 Waypoint(Vector3 from, Vector3 goal)
    {
        if (Clear(from, goal))
            return goal;
        int end = NearestVisibleNode(goal, null);
        if (end < 0)
            return from;
        if (!distances.TryGetValue(end, out var costs))
        {
            // 목적지 8개를 유지하고 가장 오래된 배열 하나만 재사용합니다.
            if (distances.Count >= 8)
            {
                int oldest = distanceOrder.Dequeue();
                costs = distances[oldest];
                distances.Remove(oldest);
            }
            else costs = new int[nodes.Length];
            for (int i = 0; i < costs.Length; i++)
                costs[i] = -1;
            var queue = searchQueue;
            queue.Clear();
            queue.Enqueue(end);
            costs[end] = 0;
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                foreach (int next in links[i])
                    if (costs[next] < 0)
                    {
                        costs[next] = costs[i] + 1;
                        queue.Enqueue(next);
                    }
            }
            distances[end] = costs;
            distanceOrder.Enqueue(end);
        }
        int anchor = NearestVisibleNode(from, costs);
        if (anchor < 0)
            return from;
        // 현재 위치에서 볼 수 있는 경로의 가장 먼 지점까지 곧바로 이동합니다.
        int step = anchor;
        while (costs[step] > 0)
        {
            int next = -1;
            foreach (int candidate in links[step])
                if (costs[candidate] == costs[step] - 1)
                {
                    next = candidate;
                    break;
                }
            if (next < 0 || !Clear(from, nodes[next]))
                break;
            step = next;
        }
        return nodes[step];
    }

    private int NearestVisibleNode(Vector3 point, int[] costs)
    {
        int cx = Mathf.Clamp(Mathf.RoundToInt(point.x + Half), 0, Side - 1);
        int cz = Mathf.Clamp(Mathf.RoundToInt(point.z + Half), 0, Side - 1);
        int bestNode = -1;
        float best = float.MaxValue;
        // 가까운 격자부터 찾고, 장애물 때문에 연결할 수 없을 때만 전체를 검사합니다.
        for (int radius = 0; radius <= 3; radius++)
        {
            int minX = Math.Max(0, cx - radius), maxX = Math.Min(Side - 1, cx + radius);
            int minZ = Math.Max(0, cz - radius), maxZ = Math.Min(Side - 1, cz + radius);
            for (int z = minZ; z <= maxZ; z++)
            for (int x = minX; x <= maxX; x++)
            {
                if (Math.Max(Math.Abs(x - cx), Math.Abs(z - cz)) != radius) continue;
                int i = z * Side + x;
                if (links[i].Count == 0 || (costs != null && costs[i] < 0)) continue;
                float d = (nodes[i] - point).sqrMagnitude;
                if (d >= best || !Clear(point, nodes[i])) continue;
                best = d;
                bestNode = i;
            }
            // 다음 테두리보다 가까우면 전체 탐색과 동일한 최근접 지점입니다.
            float outside = radius + 1 - Mathf.Max(Mathf.Abs(point.x - nodes[cz * Side + cx].x),
                Mathf.Abs(point.z - nodes[cz * Side + cx].z));
            if (bestNode >= 0 && outside > 0 && best <= outside * outside) return bestNode;
        }
        for (int i = 0; i < nodes.Length; i++)
        {
            if (links[i].Count == 0 || (costs != null && costs[i] < 0)) continue;
            float d = (nodes[i] - point).sqrMagnitude;
            if (d >= best || !Clear(point, nodes[i])) continue;
            best = d;
            bestNode = i;
        }
        return bestNode;
    }

    public Vector3 Move(Vector3 from, Vector3 delta)
    {
        delta.y = 0;
        Vector3 to = from + delta;
        to.x = Mathf.Clamp(to.x, -Half, Half);
        to.z = Mathf.Clamp(to.z, -Half, Half);
        if (Clear(from, to))
            return to;
        Vector3 alongX = new Vector3(to.x, 0, from.z);
        if (Clear(from, alongX))
            from = alongX;
        Vector3 alongZ = new Vector3(from.x, 0, to.z);
        return Clear(from, alongZ) ? alongZ : from;
    }
}
