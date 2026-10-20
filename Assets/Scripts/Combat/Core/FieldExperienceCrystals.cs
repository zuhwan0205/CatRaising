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
    }
    private readonly List<Crystal> crystals = new List<Crystal>();
    private readonly List<Crystal> collected = new List<Crystal>();
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
        var item = crystals.Find(x => x.amount == 0);
        if (item == null && crystals.Count >= 512)
        {
            // 장시간 미수집 시 가까운 결정에 합쳐 경험치를 보존합니다.
            item = crystals[0];
            foreach (var candidate in crystals)
                if ((candidate.position - position).sqrMagnitude < (item.position - position).sqrMagnitude) item = candidate;
            item.amount += amount;
            return;
        }
        if (item == null)
        {
            var obj = new GameObject("Experience Crystal");
            obj.layer = 2;
            obj.transform.SetParent(transform, false);
            var art = obj.AddComponent<SpriteRenderer>();
            art.sprite = sprite;
            art.color = new Color(.25f, .85f, 1);
            item = new Crystal { visual = obj };
            crystals.Add(item);
        }
        item.amount = amount;
        item.position = position;
        item.visual.transform.position = position + Vector3.up * .18f;
        item.visual.transform.rotation = cameraView.transform.rotation * Quaternion.Euler(0, 0, 45);
        item.visual.transform.localScale = new Vector3(.22f, .32f, 1);
        float depth = Vector3.Dot(position - cameraView.transform.position, cameraView.transform.forward);
        item.visual.GetComponent<SpriteRenderer>().sortingOrder = 10000 - Mathf.RoundToInt(depth * 100);
        item.visual.SetActive(true);
    }

    private void Update()
    {
        bool encounter = field.RaidBoss != null;
        foreach (var item in crystals)
            if (item.amount > 0 && item.visual.activeSelf == encounter) item.visual.SetActive(!encounter);
        if (encounter || combat.Blocked || combat.PlayerHealth == null || combat.PlayerHealth.Dead || Collect == null) return;
        magnetCooldown = Mathf.Max(0, magnetCooldown - Time.deltaTime);
        bool magnet = combat.MagnetRank > 0 && magnetCooldown <= 0;
        collected.Clear();
        long total = 0;
        foreach (var item in crystals)
        {
            if (item.amount == 0) continue;
            var offset = player.position - item.position;
            offset.y = 0;
            if (!magnet && offset.sqrMagnitude > 1.2f * 1.2f) continue;
            total += item.amount;
            collected.Add(item);
        }
        if (total == 0 || !Collect(total)) return;
        if (magnet) magnetCooldown = 6 - combat.MagnetRank;
        foreach (var item in collected) { item.amount = 0; item.visual.SetActive(false); }
    }

    private void OnDestroy()
    {
        if (combat != null) combat.ExperienceDropped -= Drop;
    }
}
