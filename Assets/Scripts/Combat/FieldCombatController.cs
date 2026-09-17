using System;
using System.Collections.Generic;
using UnityEngine;

// XZ 거리 기반 추적과 자동 공격. 이동 모드와 관계없이 공격을 처리합니다.
public class FieldCombatController : MonoBehaviour
{
    public event Action<long> GoldEarned;
    public event Action<string> MessageChanged;
    public event Action<string> RelicMessageChanged;
    public event Action<double,double> HealthChanged;
    public event Action PlayerHit;
    public event Action<Vector3,Vector3> AttackPerformed;
    public CombatHealth PlayerHealth { get; private set; }
    public float ContactDistance=1.05f;
    public float ContactInterval=.75f;
    public double ContactDamage=5;
    public float PlayerRespawnDelay=3;
    private float contactCooldown;
    private float deathRemaining;
    private double defense;
    public bool Blocked;
    private Transform player;
    private FieldEnemy[] enemies;
    private BackendUserData data;
    private CatGameCatalog catalog;
    private float cooldown;
    private string selectedId;
    private int selectedLevel;
    private double attack, speed;
    private bool valid;
    private ICharacterAttack attackBehavior;
    private RelicEffectRunner relicEffects;
    private readonly List<FieldEnemy> attackTargets = new List<FieldEnemy>();
    private int killsThisFrame;
    public float Range=1.7f;
    public float EnemySpeed=1.1f;
    public long GoldPerKill=10;
    public float RespawnDelay=2;

    public void Initialize(Transform target,FieldEnemy[] targets,BackendUserData playerData,CatGameCatalog definitions)
    { player=target;enemies=targets;data=playerData;catalog=definitions; relicEffects=new RelicEffectRunner(data,catalog); }

    private void Update()
    {
        if(Blocked || player==null)return;
        float delta=Mathf.Min(Time.deltaTime,.1f);
        ResolveStats();
        relicEffects.Tick(delta);
        killsThisFrame=0;
        if (PlayerHealth.Dead)
        {
            deathRemaining-=delta;
            if(deathRemaining<=0)
            {
                player.position=Vector3.zero;
                foreach(var enemy in enemies) enemy.Respawn(SpawnPosition());
                PlayerHealth.Reset(); contactCooldown=1; cooldown=1;
                HealthChanged?.Invoke(PlayerHealth.Current,PlayerHealth.Maximum);
                MessageChanged?.Invoke("HP 회복 · 전투를 다시 시작합니다.");
            }
            return;
        }
        contactCooldown=Mathf.Max(0,contactCooldown-delta);
        cooldown=Mathf.Max(0,cooldown-delta);
        bool touching=false;
        FieldEnemy closest=null;
        float nearest=Range*Range;
        foreach(var enemy in enemies)
        {
            if(!enemy.Alive)
            {
                enemy.RespawnRemaining-=delta;
                if(enemy.RespawnRemaining<=0)enemy.Respawn(SpawnPosition());
                continue;
            }
            Vector3 toward=player.position-enemy.transform.position;toward.y=0;
            float distance=toward.magnitude;
            if(distance>.9f) enemy.transform.position+=toward.normalized*Mathf.Min(EnemySpeed*delta,distance-.9f);
            enemy.StepVisual(delta);
            float sqr=(enemy.transform.position-player.position).sqrMagnitude;
            if(sqr<=ContactDistance*ContactDistance)touching=true;
            if(sqr<=nearest){nearest=sqr;closest=enemy;}
        }
        if(touching && contactCooldown<=0)
        {
            contactCooldown=Mathf.Max(.1f,ContactInterval);
            bool died=PlayerHealth.Damage(Math.Max(1,ContactDamage-defense));
            if (!died) TriggerRelics(EffectTrigger.OnDamaged, null);
            PlayerHit?.Invoke();
            HealthChanged?.Invoke(PlayerHealth.Current,PlayerHealth.Maximum);
            if(died)
            {
                deathRemaining=Mathf.Max(.1f,PlayerRespawnDelay);
                MessageChanged?.Invoke($"쓰러졌습니다. {deathRemaining:0.#}초 후 회복합니다.");
                return;
            }
        }
        if(!valid || cooldown>0 || closest==null)return;
        cooldown=(float)(1/speed);
        // OnAttack은 공격 1회당, OnHit은 기본 공격 명중 대상마다 실행합니다.
        double attackBonus=0;
        TriggerRelics(EffectTrigger.OnAttack, value=>attackBonus+=value);
        attackBehavior.SelectTargets(player.position,closest,enemies,Range,attackTargets);
        double minDamage=double.PositiveInfinity, maxDamage=0, maxBonus=0;
        foreach (var target in attackTargets)
        {
            AttackPerformed?.Invoke(player.position,target.transform.position);
            double hitBonus=0;
            TriggerRelics(EffectTrigger.OnHit, value=>hitBonus+=value);
            double bonus=attackBonus+hitBonus;
            double damage=attack+bonus;
            minDamage=Math.Min(minDamage,damage); maxDamage=Math.Max(maxDamage,damage);
            maxBonus=Math.Max(maxBonus,bonus);
            if(target.Hit(damage))
            {
                target.RespawnRemaining=RespawnDelay;
                killsThisFrame++;
                TriggerRelics(EffectTrigger.OnKill, null);
            }
        }
        if (attackTargets.Count > 0)
        {
            string damageText=minDamage==maxDamage?$"{maxDamage:0.#}":$"{minDamage:0.#}~{maxDamage:0.#}";
            string bonusText=maxBonus>0?$" (유물 +{maxBonus:0.#})":"";
            string result=killsThisFrame>0?$" · {killsThisFrame}마리 처치":$" · {attackTargets.Count}마리 명중";
            if(attackTargets.Count==1)result+=$"\n적 HP {closest.Health.Current:0.#}/{closest.Health.Maximum:0.#}";
            MessageChanged?.Invoke($"공격 피해 {damageText}{bonusText}{result}");
        }
        // 범위 공격은 한 번에 합산 저장하여 저장 중 중복 보상 누락을 막습니다.
        if(killsThisFrame>0) GoldEarned?.Invoke(GoldPerKill*killsThisFrame);
    }

    private void TriggerRelics(EffectTrigger trigger, Action<double> bonusDamage)
    {
        double restored=0;
        relicEffects.Trigger(trigger, value =>
        {
            double before=PlayerHealth.Current;
            PlayerHealth.Heal(value);
            restored=PlayerHealth.Current-before;
            HealthChanged?.Invoke(PlayerHealth.Current,PlayerHealth.Maximum);
        }, bonusDamage, (definition,value) =>
        {
            string effect=definition.effectId=="heal"?$"HP +{restored:0.#}":$"추가 피해 +{value:0.#}";
            RelicMessageChanged?.Invoke($"{definition.displayName} 발동 · {effect}");
        });
    }

    private void ResolveStats()
    {
        var owned=data.characters.Find(x=>x!=null && x.characterId==data.loadout.characterId);
        int level=owned==null?0:owned.level;
        if(PlayerHealth!=null && selectedId==data.loadout.characterId && selectedLevel==level)return;
        selectedId=data.loadout.characterId;selectedLevel=level;cooldown=0;valid=false;
        var definition=catalog.FindCharacter(selectedId);
        attackBehavior=CharacterAttackFactory.Create(definition == null ? null : definition.attackId);
        Range=definition != null && definition.attackRange>0 && !float.IsInfinity(definition.attackRange) ? definition.attackRange : 1.7f;
        double maxHp=100;
        defense=0;
        if(owned!=null && definition!=null && definition.baseStats!=null && definition.statsPerLevel!=null)
        {
            int growth=Math.Max(0,level-1);
            maxHp=definition.baseStats.maxHealth+(double)definition.statsPerLevel.maxHealth*growth;
            defense=definition.baseStats.defense+(double)definition.statsPerLevel.defense*growth;
        }
        if(double.IsNaN(maxHp)||double.IsInfinity(maxHp)||maxHp<=0)maxHp=100;
        if(double.IsNaN(defense)||double.IsInfinity(defense))defense=0;
        double ratio=PlayerHealth==null?1:PlayerHealth.Current/PlayerHealth.Maximum;
        PlayerHealth=new CombatHealth(maxHp);
        PlayerHealth.Damage(maxHp*(1-ratio));
        HealthChanged?.Invoke(PlayerHealth.Current,PlayerHealth.Maximum);
        if(owned!=null && definition!=null && attackBehavior!=null && definition.baseStats!=null && definition.statsPerLevel!=null)
        {
            int growth=Math.Max(0,level-1);
            attack=definition.baseStats.attack+(double)definition.statsPerLevel.attack*growth;
            speed=definition.baseStats.attackSpeed+(double)definition.statsPerLevel.attackSpeed*growth;
            valid=attack>0 && speed>0 && !double.IsNaN(attack) && !double.IsInfinity(attack) && !double.IsNaN(speed) && !double.IsInfinity(speed);
        }
        MessageChanged?.Invoke(valid?"자동 공격 중":"공격 설정이 없습니다. 캐릭터의 Attack Id를 확인해주세요.");
    }

    private Vector3 SpawnPosition()
    {
        Vector2 direction=UnityEngine.Random.insideUnitCircle.normalized;
        if(direction.sqrMagnitude<.1f)direction=Vector2.right;
        Vector3 position=player.position+new Vector3(direction.x,0,direction.y)*7;
        position.x=Mathf.Clamp(position.x,-17,17);position.z=Mathf.Clamp(position.z,-17,17);
        return position;
    }
}
