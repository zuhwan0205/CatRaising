using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RelicHotPathChecks
{
    [MenuItem("CatRaising/Checks/Relic Hot Path")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first");
        var catalog = ScriptableObject.CreateInstance<CatGameCatalog>();
        var root = new GameObject("Relic Check");
        try
        {
            catalog.relics.Add(new RelicDefinition { id="bonus", effectId="bonus_damage", trigger=EffectTrigger.OnHit, effectValue=3, effectPerLevel=2 });
            var data = BackendUserData.CreateNew();
            data.relics.Add(new OwnedRelic { relicId="bonus", level=2 });
            data.loadout.relicIds.Add("bonus"); data.loadout.relicIds.Add("bonus");
            var fighter = root.AddComponent<FieldCombatController>();
            fighter.Initialize(root.transform, Array.Empty<FieldEnemy>(), data, catalog);
            var method = typeof(FieldCombatController).GetMethod("TriggerRelics", BindingFlags.Instance|BindingFlags.NonPublic);
            var trigger = (Func<EffectTrigger,bool,double>)Delegate.CreateDelegate(typeof(Func<EffectTrigger,bool,double>),fighter,method);
            if (trigger(EffectTrigger.OnHit,true)!=5 || trigger(EffectTrigger.OnAttack,true)!=0 || trigger(EffectTrigger.OnHit,false)!=0)
                throw new Exception("Bonus, duplicate equipment or trigger filtering changed");
            // Warm dictionary/hashset/delegates before measuring managed allocations.
            for(int i=0;i<10;i++) trigger(EffectTrigger.OnHit,true);
            long before=GC.GetAllocatedBytesForCurrentThread();
            double sum=0;
            for(int i=0;i<10000;i++) sum+=trigger(EffectTrigger.OnHit,true);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            if(sum!=50000 || allocated!=0) throw new Exception("Hot path allocation/bonus regression: "+allocated);
            data.relics[0].level=3;
            if(trigger(EffectTrigger.OnHit,true)!=7) throw new Exception("Upgrade not reflected");
            catalog.relics[0].cooldownSeconds=1;
            if(trigger(EffectTrigger.OnHit,true)!=7 || trigger(EffectTrigger.OnHit,true)!=0) throw new Exception("Cooldown changed");
            Debug.Log("RELIC_HOT_PATH_PASS: 10000 triggers, zero warmed managed allocations; duplicates, level, trigger and cooldown checked.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(catalog); }
    }
}
