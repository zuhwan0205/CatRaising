using System;
using UnityEngine;

// XZ 거리 기반 추적과 자동 공격. 이동 모드와 관계없이 공격을 처리합니다.
public class FieldCombatController : MonoBehaviour
{
    public event Action<long> GoldEarned;
    public event Action<string> MessageChanged;
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
    public float Range=1.7f;
    public float EnemySpeed=1.1f;
    public long GoldPerKill=10;
    public float RespawnDelay=2;

    public void Initialize(Transform target,FieldEnemy[] targets,BackendUserData playerData,CatGameCatalog definitions)
    { player=target;enemies=targets;data=playerData;catalog=definitions; }

    private void Update()
    {
        if(Blocked || player==null)return;
        float delta=Mathf.Min(Time.deltaTime,.1f);
        ResolveStats();
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
        AttackPerformed?.Invoke(player.position,closest.transform.position);
        bool killed=closest.Hit(attack);
        MessageChanged?.Invoke(killed?$"적 처치! 골드 +{GoldPerKill}":$"할퀴기 {attack:0.#} · 적 HP {closest.Health.Current:0.#}/{closest.Health.Maximum:0.#}");
        if(killed)
        {
            closest.RespawnRemaining=RespawnDelay;
            GoldEarned?.Invoke(GoldPerKill);
        }
    }

    private void ResolveStats()
    {
        var owned=data.characters.Find(x=>x!=null && x.characterId==data.loadout.characterId);
        int level=owned==null?0:owned.level;
        if(PlayerHealth!=null && selectedId==data.loadout.characterId && selectedLevel==level)return;
        selectedId=data.loadout.characterId;selectedLevel=level;cooldown=0;valid=false;
        var definition=catalog.characters.Find(x=>x!=null && x.id==selectedId);
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
        if(owned!=null && definition!=null && definition.attackId=="claw_melee" && definition.baseStats!=null && definition.statsPerLevel!=null)
        {
            int growth=Math.Max(0,level-1);
            attack=definition.baseStats.attack+(double)definition.statsPerLevel.attack*growth;
            speed=definition.baseStats.attackSpeed+(double)definition.statsPerLevel.attackSpeed*growth;
            valid=attack>0 && speed>0 && !double.IsNaN(attack) && !double.IsInfinity(attack) && !double.IsNaN(speed) && !double.IsInfinity(speed);
        }
        MessageChanged?.Invoke(valid?"자동 공격 중 · 가까운 적을 할퀩니다":"공격 설정이 없습니다. claw_melee 캐릭터 설정을 확인해주세요.");
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
