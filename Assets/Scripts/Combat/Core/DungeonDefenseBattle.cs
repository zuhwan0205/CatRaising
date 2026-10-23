using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 던전 전용 고정 경로 전투. 재화와 배치는 이 전투 안에서만 유지됩니다.
public sealed class DungeonDefenseBattle : MonoBehaviour
{
    private sealed class Enemy
    {
        public RectTransform rect; public RawImage art; public int segment, kind;
        public float distance; public double hp; public bool alive, boss;
    }
    private sealed class Tower
    {
        public int choice; public Image image; public RectTransform shot; public Sprite[] frames; public float cooldown, attackTime;
    }
    public int Selected { get; set; }
    public int Coins { get; private set; } = 120;
    public bool CanStartWave => !running && !finished && wave < 3;
    public static int Cost(int choice) => 35 + choice * 10;
    private int stage, wave, spawned, total, lives = 20;
    private float spawnTime;
    private bool running, finished;
    private RectTransform map;
    private Vector2[] route;
    private RectTransform[] slots;
    private Tower[] towers;
    private IList<OwnedCharacter> team;
    private CharacterDefinition[] definitions;
    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly Texture2D[] textures = new Texture2D[4];
    private Action<string> hud, hint;
    private Action<bool> ended;

    public void Initialize(int level, RectTransform arena, Vector2[] path, RectTransform[] pads,
        IList<OwnedCharacter> cats, CharacterDefinition[] types, Action<string> status, Action<string> message, Action<bool> complete)
    {
        stage=level; map=arena; route=path; slots=pads; team=cats; definitions=types;
        towers=new Tower[pads.Length]; hud=status; hint=message; ended=complete;
        for(int i=0;i<DungeonStageRules.KindCount(stage);i++) textures[i]=Resources.Load<Texture2D>("MonsterArt/"+DungeonStageRules.MonsterArt[i]);
        // 전투 시작 전에 최대 웨이브 수만큼 준비하고 매 웨이브 재사용합니다.
        for(int i=0;i<DungeonStageRules.EnemyCount(stage,3);i++)
        {
            var obj=new GameObject("Path Enemy",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
            var rect=(RectTransform)obj.transform; rect.SetParent(map,false);
            rect.anchorMin=rect.anchorMax=Vector2.zero; rect.sizeDelta=new Vector2(62,62);
            var art=obj.GetComponent<RawImage>(); art.raycastTarget=false;
            enemies.Add(new Enemy { rect=rect, art=art }); obj.SetActive(false);
        }
        Refresh();
    }
    public bool Place(int slot)
    {
        if(finished || towers[slot]!=null) return false;
        int cost=Cost(Selected);
        if(Coins<cost) { hint("소환 재화가 부족해요"); return false; }
        var frames=CatAnimationFrames.Get(team[Selected].characterId);
        var obj=new GameObject("Placed Cat",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var rect=(RectTransform)obj.transform; rect.SetParent(slots[slot],false);
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=obj.GetComponent<Image>(); image.raycastTarget=false; image.preserveAspect=true;
        image.sprite=frames[0];
        if(image.sprite==null) { var fallback=CatAnimationFrames.GetFallback(); image.sprite=fallback[0]!=null?fallback[0]:fallback[4]; }
        var beam = new GameObject("Attack Spark",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var shot = (RectTransform)beam.transform; shot.SetParent(map,false);
        shot.anchorMin=shot.anchorMax=Vector2.zero; shot.pivot=new Vector2(0,.5f);
        beam.GetComponent<Image>().color=new Color(1,.83f,.32f,.8f);
        beam.GetComponent<Image>().raycastTarget=false; beam.SetActive(false);
        towers[slot]=new Tower { choice=Selected, image=image, shot=shot, frames=frames };
        Coins-=cost; hint("배치 완료 · 같은 고양이도 여러 칸에 배치할 수 있어요"); Refresh(); return true;
    }
    public void StartWave()
    {
        if(!CanStartWave) return;
        bool placed=false; foreach(var tower in towers) if(tower!=null) placed=true;
        if(!placed) { hint("고양이를 먼저 배치해주세요"); return; }
        wave++; total=DungeonStageRules.EnemyCount(stage,wave); spawned=0; spawnTime=0; running=true; Refresh();
    }
    private Vector2 Point(int i) => Vector2.Scale(route[i],map.rect.size);
    private void Update()
    {
        if(finished || map==null) return;
        float dt=Mathf.Min(Time.deltaTime,.05f);
        if(running && spawned<total)
        {
            spawnTime-=dt;
            if(spawnTime<=0)
            {
                var enemy=enemies[spawned]; enemy.kind=DungeonStageRules.Kind(stage,wave,spawned);
                enemy.boss=DungeonStageRules.IsBoss(stage,wave,spawned);
                enemy.hp=DungeonStageRules.Health(stage,wave,spawned); enemy.segment=0; enemy.distance=0; enemy.alive=true;
                enemy.art.texture=textures[enemy.kind]; enemy.rect.sizeDelta=Vector2.one*(enemy.boss?88:62);
                enemy.rect.anchoredPosition=Point(0); enemy.rect.gameObject.SetActive(true);
                spawned++; spawnTime=.8f;
            }
        }
        int living=0;
        foreach(var enemy in enemies)
        {
            if(!enemy.alive) continue;
            living++;
            float speed=(enemy.kind==1?62:enemy.kind==3?34:45)*Mathf.Max(.6f,map.rect.width/700f);
            enemy.distance+=speed*dt;
            while(enemy.segment<route.Length-1)
            {
                float length=Vector2.Distance(Point(enemy.segment),Point(enemy.segment+1));
                if(enemy.distance<length) break;
                enemy.distance-=length; enemy.segment++;
            }
            if(enemy.segment>=route.Length-1)
            {
                Remove(enemy); lives-=enemy.boss?5:1; Refresh();
                if(lives<=0) { Finish(false); return; } continue;
            }
            Vector2 from=Point(enemy.segment), to=Point(enemy.segment+1);
            enemy.rect.anchoredPosition=Vector2.Lerp(from,to,enemy.distance/Vector2.Distance(from,to));
            enemy.art.uvRect=new Rect(((int)(Time.time*6)%4)*.25f,.5f,.25f,.5f);
            var texture=enemy.art.texture;
            if(texture!=null) enemy.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,enemy.rect.rect.height*(texture.width*.25f)/(texture.height*.5f));
        }
        for(int i=0;i<towers.Length;i++)
        {
            var tower=towers[i]; if(tower==null) continue;
            tower.cooldown-=dt; tower.attackTime=Mathf.Max(0,tower.attackTime-dt);
            tower.shot.gameObject.SetActive(tower.attackTime>.18f);
            if(tower.frames[0]!=null) tower.image.sprite=tower.frames[tower.attackTime>0?8+Mathf.Min(3,(int)((.3f-tower.attackTime)/.075f)):(int)(Time.time*4)%4];
            if(tower.cooldown>0) continue;
            var definition=definitions[tower.choice]; var owned=team[tower.choice];
            float range=(145+Mathf.Clamp(definition.attackRange,1,7)*13)*map.rect.width/700;
            Vector2 position=Vector2.Scale(slots[i].anchorMin,map.rect.size);
            Enemy target=null; float nearest=range*range;
            foreach(var enemy in enemies)
                if(enemy.alive && (enemy.rect.anchoredPosition-position).sqrMagnitude<nearest)
                { target=enemy; nearest=(enemy.rect.anchoredPosition-position).sqrMagnitude; }
            if(target==null) continue;
            double damage=(definition.baseStats.attack+definition.statsPerLevel.attack*Mathf.Max(0,owned.level-1))*CatProgression.StarMultiplier(owned.ascension)*CatSkillTree.Attack(owned);
            float speed=Mathf.Max(.2f,(float)((definition.baseStats.attackSpeed+definition.statsPerLevel.attackSpeed*Mathf.Max(0,owned.level-1))*CatSkillTree.Speed(owned)));
            tower.cooldown=1/speed; tower.attackTime=.3f;
            tower.image.rectTransform.localScale=new Vector3(target.rect.anchoredPosition.x<position.x?-1:1,1,1);
            Vector2 direction=target.rect.anchoredPosition-position;
            tower.shot.anchoredPosition=position;
            tower.shot.sizeDelta=new Vector2(direction.magnitude,4);
            tower.shot.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            tower.shot.gameObject.SetActive(true);
            target.hp-=Math.Max(1,damage);
            if(target.hp<=0) { Remove(target); Coins+=target.boss?25:8; Refresh(); }
        }
        if(running && spawned==total && living==0)
        {
            running=false;
            if(wave>=3) { Finish(true); return; }
            Coins+=35; hint("웨이브 클리어! 추가 배치 후 다음 웨이브를 시작하세요"); Refresh();
        }
    }
    private void Remove(Enemy enemy) { enemy.alive=false; enemy.rect.gameObject.SetActive(false); }
    private void Refresh() => hud?.Invoke($"1-{stage}  ·  기지 {Mathf.Max(0,lives)}/20  ·  소환 {Coins}  ·  웨이브 {wave}/3");
    private void Finish(bool won) { finished=true; running=false; Refresh(); ended?.Invoke(won); }
}
