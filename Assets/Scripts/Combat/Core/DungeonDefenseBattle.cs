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
        public int slot, choice; public Image image; public Sprite[] frames; public float cooldown, attackTime;
    }
    public Action<string> InspectCat;
    public Action<int> WaveCleared;
    public Action<int,int> TowerMoved;
    private bool draggingTower;
    private RectTransform rangeCircle;
    private int rangeSlot = -1;
    public int Selected { get; set; }
    public int WaveNumber => wave;
    public int Coins { get; private set; } = 120;
    public bool CanStartWave => !running && !finished && wave < DungeonStageRules.WaveCount;
    public static int Cost(ItemRarity rarity) => rarity == ItemRarity.Legendary ? 100 : rarity == ItemRarity.Unique ? 70 : rarity == ItemRarity.Epic ? 45 : 30;
    private int stage, wave, spawned, total, lives = 20;
    private float spawnTime;
    private bool running, finished;
    private RectTransform map;
    private Vector2[] route;
    private Vector2[] routePoints;
    private float[] segmentLengths;
    private Vector2 layoutSize;
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
        routePoints=new Vector2[path.Length]; segmentLengths=new float[path.Length-1];
        towers=new Tower[pads.Length]; hud=status; hint=message; ended=complete;
        for(int i=0;i<DungeonStageRules.KindCount(stage);i++) textures[i]=Resources.Load<Texture2D>("MonsterArt/"+DungeonStageRules.MonsterArt[i]);
        // 전투 시작 전에 최대 웨이브 수만큼 준비하고 매 웨이브 재사용합니다.
        for(int i=0;i<DungeonStageRules.EnemyCount(stage,DungeonStageRules.WaveCount);i++)
        {
            var obj=new GameObject("Path Enemy",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
            var rect=(RectTransform)obj.transform; rect.SetParent(map,false);
            rect.anchorMin=rect.anchorMax=Vector2.zero; rect.sizeDelta=new Vector2(62,62);
            // 이동 기준은 타일 중심에 두고 그림만 높이의 18%만큼 위로 올립니다.
            rect.pivot=new Vector2(.5f,.32f);
            var art=obj.GetComponent<RawImage>(); art.raycastTarget=false;
            enemies.Add(new Enemy { rect=rect, art=art }); obj.SetActive(false);
        }
        Refresh();
    }
    public bool Place(int slot)
    {
        if(finished || towers[slot]!=null) return false;
        int cost=Cost(definitions[Selected].rarity);
        if(Coins<cost) { hint("소환 재화가 부족해요"); return false; }
        var frames=CatAnimationFrames.Get(team[Selected].characterId);
        var obj=new GameObject("Placed Cat",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var rect=(RectTransform)obj.transform; rect.SetParent(slots[slot],false);
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=obj.GetComponent<Image>(); image.raycastTarget=true; image.preserveAspect=true;
        image.sprite=frames[0];
        if(image.sprite==null) { var fallback=CatAnimationFrames.GetFallback(); image.sprite=fallback[0]!=null?fallback[0]:fallback[4]; }
        var placed = new Tower { slot=slot, choice=Selected, image=image, frames=frames };
        towers[slot]=placed;
        var press=obj.AddComponent<DefenseCatPress>();
        press.Click=()=>ShowRange(placed.slot);
        press.Hold=()=> { ShowRange(placed.slot, false); InspectCat?.Invoke(TowerDescription(placed.slot)); };
        Vector3 dragOffset=Vector3.zero;
        press.BeginDrag=e=>
        {
            if(!CanStartWave || draggingTower) return false;
            if(!RectTransformUtility.ScreenPointToWorldPointInRectangle(map,e.position,e.pressEventCamera,out var point)) return false;
            draggingTower=true; dragOffset=rect.position-point;
            if(rangeCircle!=null) rangeCircle.gameObject.SetActive(false);
            rangeSlot=-1;
            hint("빈 잔디 타일에 놓으세요 · 이동 비용 없음");
            return true;
        };
        press.Drag=e=>
        {
            if(RectTransformUtility.ScreenPointToWorldPointInRectangle(map,e.position,e.pressEventCamera,out var point)) rect.position=point+dragOffset;
        };
        press.EndDrag=e=>
        {
            draggingTower=false;
            int destination=-1;
            if(e!=null && CanStartWave && RectTransformUtility.ScreenPointToLocalPointInRectangle(map,e.position,e.pressEventCamera,out var point) && map.rect.Contains(point))
            {
                Vector2 normalized=(point-map.rect.min);
                int x=Mathf.FloorToInt(normalized.x/map.rect.width*DefenseTileBoard.Width);
                int y=Mathf.FloorToInt(normalized.y/map.rect.height*DefenseTileBoard.Height);
                for(int i=0;i<slots.Length;i++)
                    if(towers[i]==null && Mathf.FloorToInt(slots[i].anchorMin.x*DefenseTileBoard.Width)==x && Mathf.FloorToInt(slots[i].anchorMin.y*DefenseTileBoard.Height)==y) { destination=i; break; }
            }
            if(destination>=0)
            {
                int previous=placed.slot;
                towers[previous]=null; towers[destination]=placed; placed.slot=destination;
                rect.SetParent(slots[destination],false);
                TowerMoved?.Invoke(previous,destination);
                hint("고양이 위치를 옮겼어요");
            }
            else if(e!=null) hint("빈 잔디 타일에만 옮길 수 있어요");
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        };
        Coins-=cost; hint("배치 완료 · 같은 고양이도 여러 칸에 배치할 수 있어요"); Refresh(); return true;
    }
    public void StartWave()
    {
        if(!CanStartWave || draggingTower) return;
        bool placed=false; foreach(var tower in towers) if(tower!=null) placed=true;
        if(!placed) { hint("고양이를 먼저 배치해주세요"); return; }
        foreach(var shot in shots) { shot.active=false; shot.root.gameObject.SetActive(false); }
        wave++; total=DungeonStageRules.EnemyCount(stage,wave); spawned=0; spawnTime=0; running=true; Refresh();
    }
    private Vector2 Point(int i) => routePoints[i];

    private void UpdateLayout()
    {
        Vector2 size = map.rect.size;
        if (layoutSize == size) return;
        layoutSize = size;
        for (int i = 0; i < route.Length; i++) routePoints[i] = Vector2.Scale(route[i], size);
        for (int i = 0; i < segmentLengths.Length; i++)
            segmentLengths[i] = Vector2.Distance(routePoints[i], routePoints[i + 1]);
        foreach (var enemy in enemies) if (enemy.alive) SizeEnemy(enemy);
    }

    private void SizeEnemy(Enemy enemy)
    {
        float height = layoutSize.y / DefenseTileBoard.Height * (enemy.boss ? 1.65f : 1.20f);
        var texture = enemy.art.texture;
        float aspect = texture != null ? texture.width * .5f / texture.height : 1;
        enemy.rect.sizeDelta = new Vector2(height * aspect, height);
    }
    private void Update()
    {
        if(finished || map==null) return;
        UpdateLayout();
        float dt=Mathf.Min(Time.deltaTime,.05f);
        if(running && spawned<total)
        {
            spawnTime-=dt;
            if(spawnTime<=0)
            {
                var enemy=enemies[spawned]; enemy.kind=DungeonStageRules.Kind(stage,wave,spawned);
                enemy.boss=DungeonStageRules.IsBoss(stage,wave,spawned);
                enemy.hp=DungeonStageRules.Health(stage,wave,spawned); enemy.segment=0; enemy.distance=0; enemy.alive=true;
                enemy.art.texture=textures[enemy.kind]; SizeEnemy(enemy);
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
                float length=segmentLengths[enemy.segment];
                if(enemy.distance<length) break;
                enemy.distance-=length; enemy.segment++;
            }
            if(enemy.segment>=route.Length-1)
            {
                Remove(enemy); lives-=enemy.boss?5:1; Refresh();
                if(lives<=0) { Finish(false); return; } continue;
            }
            Vector2 from=Point(enemy.segment), to=Point(enemy.segment+1);
            enemy.rect.anchoredPosition=Vector2.Lerp(from,to,enemy.distance/segmentLengths[enemy.segment]);
            enemy.art.uvRect=new Rect(((int)(Time.time*6)%4)*.25f,.5f,.25f,.5f);
        }
        TickShots(dt);
        for(int i=0;i<towers.Length;i++)
        {
            var tower=towers[i]; if(tower==null) continue;
            tower.cooldown-=dt; tower.attackTime=Mathf.Max(0,tower.attackTime-dt);

            if(tower.frames[0]!=null) tower.image.sprite=tower.frames[tower.attackTime>0?8+Mathf.Min(3,(int)((.3f-tower.attackTime)/.075f)):(int)(Time.time*4)%4];
            if(tower.cooldown>0) continue;
            float range=Range(tower.choice);
            Vector2 position=Vector2.Scale(slots[i].anchorMin,map.rect.size);
            Enemy target=null; float nearest=range*range;
            foreach(var enemy in enemies)
                if(enemy.alive && (enemy.rect.anchoredPosition-position).sqrMagnitude<nearest)
                { target=enemy; nearest=(enemy.rect.anchoredPosition-position).sqrMagnitude; }
            if(target==null) continue;
            CombatStats(tower.choice, out double damage, out float speed);
            tower.cooldown=1/speed; tower.attackTime=.3f;
            tower.image.rectTransform.localScale=new Vector3(target.rect.anchoredPosition.x<position.x?-1:1,1,1);
            LaunchShot(position, target, team[tower.choice].characterId, damage);
        }
        if(running && spawned==total && living==0)
        {
            running=false;
            if(wave>=DungeonStageRules.WaveCount) { Finish(true); return; }
            Coins+=35; hint("웨이브 클리어! 추가 배치 후 다음 웨이브를 시작하세요"); Refresh();
            WaveCleared?.Invoke(wave);
        }
    }
    private sealed class Shot
    {
        public RectTransform root; public Image[] parts;
        public Vector2 source; public Enemy target; public string id;
        public float age; public double damage; public bool active;
    }
    private readonly List<Shot> shots = new List<Shot>();
    private void LaunchShot(Vector2 source, Enemy target, string id, double damage)
    {
        Shot shot=null;
        foreach(var candidate in shots) if(!candidate.active) { shot=candidate; break; }
        if(shot==null)
        {
            var obj=new GameObject("Cat Attack Effect",typeof(RectTransform));
            var root=(RectTransform)obj.transform; root.SetParent(map,false);
            root.anchorMin=root.anchorMax=Vector2.zero;
            shot=new Shot { root=root, parts=new Image[3] };
            for(int i=0;i<3;i++)
            {
                var part=new GameObject("Effect Part",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                part.transform.SetParent(root,false);
                shot.parts[i]=part.GetComponent<Image>(); shot.parts[i].raycastTarget=false;
                shot.parts[i].rectTransform.sizeDelta=Vector2.one;
            }
            shots.Add(shot);
        }
        shot.source=source; shot.target=target; shot.id=id; shot.damage=damage; shot.age=0; shot.active=true;
        shot.root.gameObject.SetActive(true);
        RenderShot(shot,0);
    }
    private void TickShots(float dt)
    {
        foreach(var shot in shots)
        {
            if(!shot.active) continue;
            shot.age+=dt;
            if(!shot.target.alive || shot.age>=.25f)
            {
                shot.active=false; shot.root.gameObject.SetActive(false);
                if(shot.target.alive)
                {
                    shot.target.hp-=shot.damage;
                    if(shot.target.hp<=0) { Remove(shot.target); Coins+=shot.target.boss?25:8; Refresh(); }
                }
                continue;
            }
            RenderShot(shot,shot.age/.25f);
        }
    }
    private void RenderShot(Shot shot,float progress)
    {
        bool arrow=shot.id=="cat_ninja", ice=shot.id=="cat_shadow", magic=shot.id=="cat_moon" || ice;
        bool fire=shot.id=="cat_lion", hammer=shot.id=="cat_tiger";
        bool projectile=arrow || magic || fire;
        Vector2 target=shot.target.rect.anchoredPosition + Vector2.up * (shot.target.rect.rect.height * .18f);
        Vector2 direction=target-shot.source;
        shot.root.anchoredPosition=projectile ? Vector2.Lerp(shot.source,target,progress) : target;
        shot.root.localRotation=arrow ? Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg) : Quaternion.identity;
        float unit=map.rect.width/DefenseTileBoard.Width;
        shot.root.localScale=Vector3.one*(projectile?unit:unit*(.65f+progress*.6f));
        for(int i=0;i<3;i++)
        {
            var part=CatAttackVisualStyle.Sample(i,progress,ice,magic,fire,arrow,hammer);
            shot.parts[i].rectTransform.localPosition=part.Position;
            shot.parts[i].rectTransform.localRotation=part.Rotation;
            shot.parts[i].rectTransform.localScale=part.Scale;
            shot.parts[i].color=part.Color;
        }
    }

    private float Range(int choice) => (145+Mathf.Clamp(definitions[choice].attackRange,1,7)*13)*map.rect.width/700;
    private void ShowRange(int slot, bool toggle = true)
    {
        if(rangeCircle==null)
        {
            var obj=new GameObject("Cat Attack Range",typeof(RectTransform),typeof(CanvasRenderer),typeof(DungeonMapOval));
            rangeCircle=(RectTransform)obj.transform; rangeCircle.SetParent(map,false);
            rangeCircle.anchorMin=rangeCircle.anchorMax=Vector2.zero;
            var graphic=obj.GetComponent<DungeonMapOval>(); graphic.color=new Color(.30f,.80f,1f,.24f); graphic.raycastTarget=false;
            rangeCircle.SetSiblingIndex(1);
        }
        if(toggle && rangeSlot==slot && rangeCircle.gameObject.activeSelf) { rangeCircle.gameObject.SetActive(false); rangeSlot=-1; return; }
        rangeSlot=slot; rangeCircle.gameObject.SetActive(true);
        rangeCircle.anchoredPosition=Vector2.Scale(slots[slot].anchorMin,map.rect.size);
        rangeCircle.sizeDelta=Vector2.one*Range(towers[slot].choice)*2;
    }
    // 실제 공격과 정보 팝업이 동일한 계산을 사용합니다.
    private void CombatStats(int choice, out double attack, out float speed)
    {
        var def = definitions[choice];
        var cat = team[choice];
        int growth = Mathf.Max(0, cat.level - 1);
        attack = Math.Max(1, (def.baseStats.attack + def.statsPerLevel.attack * growth)
            * CatProgression.StarMultiplier(cat.ascension) * CatSkillTree.Attack(cat));
        speed = Mathf.Max(.2f, (float)((def.baseStats.attackSpeed + def.statsPerLevel.attackSpeed * growth)
            * CatSkillTree.Speed(cat)));
    }

    private string TowerDescription(int slot)
    {
        int choice = towers[slot].choice;
        var def = definitions[choice];
        var cat = team[choice];
        CombatStats(choice, out double attack, out float speed);
        return $"{def.displayName}\nLv.{cat.level} · {cat.ascension}성\n\n공격력  {attack:0.##}\n공격 속도  초당 {speed:0.##}회\n사거리  {Range(choice)/(map.rect.width/DefenseTileBoard.Width):0.##} 타일";
    }

    private void Remove(Enemy enemy) { enemy.alive=false; enemy.rect.gameObject.SetActive(false); }
    private void Refresh() => hud?.Invoke($"1-{stage}  ·  기지 {Mathf.Max(0,lives)}/20  ·  소환 {Coins}  ·  웨이브 {wave}/{DungeonStageRules.WaveCount}");
    private void Finish(bool won) { finished=true; running=false; Refresh(); ended?.Invoke(won); }
}
