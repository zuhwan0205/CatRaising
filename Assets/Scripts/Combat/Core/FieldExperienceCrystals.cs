using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FieldExperienceCrystals : MonoBehaviour
{
    private sealed class Crystal
    {
        public GameObject visual;
        public Vector3 position;
        public long amount;
        public SpriteRenderer art;
        public Vector2Int cell;
    }
    private readonly List<Crystal> collected = new List<Crystal>();
    private readonly Stack<Crystal> available = new Stack<Crystal>();
    private readonly HashSet<Crystal> active = new HashSet<Crystal>();
    private readonly Dictionary<Vector2Int, List<Crystal>> cells = new Dictionary<Vector2Int, List<Crystal>>();
    private readonly Stack<List<Crystal>> emptyBuckets = new Stack<List<Crystal>>();
    private bool hidden;
    private static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / 2), Mathf.FloorToInt(p.z / 2));
    private Transform player;
    private Camera cameraView;
    private Sprite sprite;
    private FieldCombatController combat;
    private CatFieldController field;
    public Func<long, bool> Collect;
    private float magnetCooldown;

    public void Initialize(Transform cat, Camera camera, Sprite square, FieldCombatController controller, CatFieldController owner)
    {
        player = cat; cameraView = camera; sprite = square; combat = controller; field = owner;
        combat.ExperienceDropped += Drop;
    }

    private void Drop(Vector3 position, int amount)
    {
        var item = available.Count > 0 ? available.Pop() : null;
        if (item == null)
        {
            var obj = new GameObject("Experience Crystal");
            obj.layer = 2;
            obj.transform.SetParent(transform, false);
            var art = obj.AddComponent<SpriteRenderer>();
            art.sprite = sprite;
            art.color = new Color(.25f, .85f, 1);
            item = new Crystal { visual = obj, art = art };
        }
        item.amount = amount;
        item.position = position;
        item.cell = Cell(position);
        if (!cells.TryGetValue(item.cell, out var bucket))
            cells[item.cell] = bucket = emptyBuckets.Count > 0 ? emptyBuckets.Pop() : new List<Crystal>();
        bucket.Add(item); active.Add(item);
        item.visual.transform.position = position + Vector3.up * .18f;
        item.visual.transform.rotation = cameraView.transform.rotation * Quaternion.Euler(0, 0, 45);
        item.visual.transform.localScale = new Vector3(.22f, .32f, 1);
        float depth = Vector3.Dot(position - cameraView.transform.position, cameraView.transform.forward);
        item.art.sortingOrder = 10000 - Mathf.RoundToInt(depth * 100);
        item.visual.SetActive(!hidden);
    }

    private void Update()
    {
        bool encounter = field.RaidBoss != null;
        if (hidden != encounter)
        {
            hidden = encounter;
            foreach (var item in active) item.visual.SetActive(!encounter);
        }
        if (encounter || combat.Blocked || combat.PlayerHealth == null || combat.PlayerHealth.Dead || Collect == null) return;
        magnetCooldown = Mathf.Max(0, magnetCooldown - Time.deltaTime);
        bool magnet = combat.MagnetRank > 0 && magnetCooldown <= 0;
        collected.Clear();
        long total = 0;
        var playerPosition = player.position;
        if (magnet)
        {
            foreach (var item in active) { total += item.amount; collected.Add(item); }
        }
        else
        {
            var min = Cell(playerPosition - new Vector3(1.2f, 0, 1.2f));
            var max = Cell(playerPosition + new Vector3(1.2f, 0, 1.2f));
            for (int z = min.y; z <= max.y; z++)
            for (int x = min.x; x <= max.x; x++)
                if (cells.TryGetValue(new Vector2Int(x, z), out var bucket))
                    foreach (var item in bucket)
                    {
                        var offset = playerPosition - item.position; offset.y = 0;
                        if (offset.sqrMagnitude > 1.2f * 1.2f) continue;
                        total += item.amount; collected.Add(item);
                    }
        }
        if (total == 0 || !Collect(total)) return;
        if (magnet) magnetCooldown = 6 - combat.MagnetRank;
        foreach (var item in collected)
        {
            var bucket = cells[item.cell]; bucket.Remove(item);
            if (bucket.Count == 0) { cells.Remove(item.cell); emptyBuckets.Push(bucket); }
            active.Remove(item); item.amount = 0; item.visual.SetActive(false); available.Push(item);
        }
    }

    private void OnDestroy()
    {
        if (combat != null) combat.ExperienceDropped -= Drop;
    }
}
