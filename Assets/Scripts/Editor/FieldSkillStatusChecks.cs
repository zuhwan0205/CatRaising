using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class FieldSkillStatusChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    [MenuItem("CatRaising/Checks/Status Sleep")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first");
        var root = new GameObject("Status Check");
        var catalog = ScriptableObject.CreateInstance<CatGameCatalog>();
        float previousScale = Time.timeScale;
        try
        {
            Time.timeScale = 1;
            var source = root.AddComponent<FieldCombatController>();
            source.Initialize(root.transform, Array.Empty<FieldEnemy>(), BackendUserData.CreateNew(), catalog);
            typeof(FieldCombatController).GetMethod("ResolveStats", Flags).Invoke(source, null);
            var targetObject = new GameObject("Enemy"); targetObject.transform.SetParent(root.transform);
            var enemy = targetObject.AddComponent<FieldEnemy>();
            var art = targetObject.AddComponent<SpriteRenderer>();
            var bar = new GameObject("Bar").transform; bar.SetParent(targetObject.transform);
            enemy.Initialize(art, bar, 100);
            var status = targetObject.AddComponent<FieldSkillStatus>();
            // Lifecycle callbacks are explicit for a synchronous edit-mode check.
            var enable = typeof(FieldSkillStatus).GetMethod("OnEnable", Flags);
            var disable = typeof(FieldSkillStatus).GetMethod("OnDisable", Flags);
            var update = typeof(FieldSkillStatus).GetMethod("Update", Flags);
            enable.Invoke(status, null);
            Require(!status.enabled, "Empty component must sleep");
            status.Apply(source, enemy, 5, 5);
            Require(status.enabled, "Apply must wake");
            var poison = typeof(FieldSkillStatus).GetField("poison", Flags).GetValue(status);
            var burn = typeof(FieldSkillStatus).GetField("burn", Flags).GetValue(status);
            for (int i = 0; i < 6; i++) status.Apply(source, enemy, 5, 5);
            Require((int)poison.GetType().GetField("stacks").GetValue(poison) == 5, "Stack cap");
            Set(poison, "remaining", 0f); update.Invoke(status, null);
            Require(status.enabled, "Burn must survive poison expiry");
            Set(burn, "remaining", 0f); update.Invoke(status, null);
            Require(!status.enabled, "Both expired must sleep");
            status.Apply(source, enemy, 1, 0);
            source.Blocked = true; update.Invoke(status, null);
            Require(status.enabled && (float)poison.GetType().GetField("remaining").GetValue(poison) == 5, "Blocked source pauses effects");
            source.Blocked = false;
            disable.Invoke(status, null); enable.Invoke(status, null);
            Require(!status.enabled, "Pool reuse must not resume old effects");
            status.Apply(source, enemy, 0, 1);
            Require(status.enabled, "Reused enemy must wake on new effect");
            source.enabled = false; update.Invoke(status, null);
            Require(!status.enabled, "Invalid source must sleep");
            Debug.Log("FIELD_STATUS_SLEEP_PASS: wake, expiry, dual effects, stack cap, blocked source and pool reuse.");
        }
        finally { Time.timeScale = previousScale; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(catalog); }
    }
    private static void Set(object state, string field, float value) => state.GetType().GetField(field).SetValue(state, value);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
