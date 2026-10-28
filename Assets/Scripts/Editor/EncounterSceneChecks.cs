using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 배치 실행 전용: -executeMethod EncounterSceneChecks.Run (-quit 없이 실행)
// 서버 요청 없는 미리보기 데이터로 실제 씬을 왕복합니다. 에셋/저장 데이터는 변경하지 않습니다.
[InitializeOnLoad]
public static class EncounterSceneChecks
{
    private const string Pending = "CatRaising.EncounterChecks.Pending";
    static EncounterSceneChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.EraseBool(Pending);
            CheckFlow();
        };
    }

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("이 검사는 배치 모드에서만 실행합니다.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        EditorApplication.LockReloadAssemblies();
        EditorApplication.EnterPlaymode();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Bounded(Task work)
    {
        if (await Task.WhenAny(work, Task.Delay(45000)) != work) throw new TimeoutException("씬 전환 시간 초과");
        await work;
        await Task.Delay(250); // 새 씬의 Start/Update와 이전 씬의 OnDestroy까지 실행합니다.
    }

    private static void AssertMain(CatGameRuntime runtime, CatGameSession session, BackendUserData data)
    {
        Require(SceneManager.GetActiveScene().name == CatGameRuntime.MainScene, "메인 씬 복귀 실패");
        Require(ReferenceEquals(runtime.Session, session) && ReferenceEquals(runtime.Data, data), "데이터/세션이 교체됨");
        Require(UnityEngine.Object.FindObjectsByType<CatGameRuntime>().Length == 1, "관리자 중복");
        Require(UnityEngine.Object.FindObjectsByType<CatGameScene>().Length == 1, "메인 진입점 수 오류");
        Require(UnityEngine.Object.FindObjectsByType<CatEncounterScene>().Length == 0, "전투 씬 오브젝트 잔류");
        Require(UnityEngine.Object.FindObjectsByType<DungeonDefenseBattle>().Length == 0, "던전 엔진 잔류");
        Require(session.Events.SubscriberCount<SessionBusyChanged>() == 1, "저장 상태 구독 누적");
        Require(session.Events.SubscriberCount<SessionMessage>() == 1, "메시지 구독 누적");
        Require(session.Events.SubscriberCount<GameplayBlockChanged>() == 1, "전투 상태 구독 누적");
        Require(session.Events.SubscriberCount<WalletChanged>() == 1, "재화 구독 누적");
        UnityEngine.Object.FindAnyObjectByType<CatFieldController>().SetGameplayBlocked(true);
    }

    private static async void CheckFlow()
    {
        int errors = 0;
        Application.LogCallback onLog = (_, __, type) => { if (type == LogType.Exception || type == LogType.Error) errors++; };
        Application.logMessageReceived += onLog;
        try
        {
            CheckAttackDeduplication();
            await CheckSessionNotifications();
            BackendGameData.userData = null;
            var data = BackendUserData.CreateNew();
            data.progress.accountLevel = 30;
            foreach (string id in new[] { "cat_spinner", "cat_knight", "cat_moon" })
                if (!data.characters.Exists(cat => cat.characterId == id)) data.characters.Add(new OwnedCharacter { characterId = id });
            var runtime = CatGameRuntime.Initialize(data, null, true);
            var session = runtime.Session;
            long gems = data.wallet.gems;
            await Bounded(runtime.ReturnToMainAsync());
            AssertMain(runtime, session, data);

            await CheckCachedCharacterUI(data);
            await CheckSettings();

            var dungeonTeam = data.characters.Take(3).ToList();
            await Bounded(runtime.EnterEncounterAsync(true, 1, dungeonTeam));
            Require(SceneManager.GetActiveScene().name == CatGameRuntime.DungeonScene, "던전 씬 전환 실패");
            Require(UnityEngine.Object.FindAnyObjectByType<DungeonDefenseBattle>() != null, "던전 전투 생성 실패");
            Require(UnityEngine.Object.FindAnyObjectByType<CatFieldController>() == null, "던전에 메인 필드가 생성됨");
            await CheckEncounterPause();
            await SimulateResult(true);
            Require(data.wallet.gems == gems + 10 && data.progress.dungeonClearedStage == 1, "던전 보상/진행도 오류");
            await SimulateResult(true);
            Require(data.wallet.gems == gems + 10, "던전 보상 중복 지급");
            await Bounded(runtime.ReturnToMainAsync());
            AssertMain(runtime, session, data);

            await Bounded(runtime.EnterEncounterAsync(false, 0, data.characters.Take(4).ToList()));
            Require(SceneManager.GetActiveScene().name == CatGameRuntime.RaidScene, "레이드 씬 전환 실패");
            var field = UnityEngine.Object.FindAnyObjectByType<CatFieldController>();
            Require(field != null && field.RaidRunning && field.Combat == null, "레이드 전용 필드 생성 실패");
            Require(UnityEngine.Object.FindObjectsByType<FieldCombatController>().Length == 4, "레이드 고양이 수 오류");
            await CheckEncounterPause();
            await SimulateResult(true);
            await SimulateResult(true);
            Require(data.wallet.gems == gems + 20, "레이드 보상/중복 방지 오류");
            await Bounded(runtime.ReturnToMainAsync());
            AssertMain(runtime, session, data);

            await Bounded(runtime.EnterEncounterAsync(true, 2, dungeonTeam));
            await SimulateResult(false);
            Require(data.wallet.gems == gems + 20 && data.progress.dungeonClearedStage == 1, "패배 시 보상이 지급됨");
            await Bounded(runtime.ReturnToMainAsync());
            AssertMain(runtime, session, data);
            await CheckPauseExit(runtime, true);
            AssertMain(runtime, session, data);
            await CheckPauseExit(runtime, false);
            AssertMain(runtime, session, data);
            Require(errors == 0, "플레이 중 오류 로그 발생: " + errors);
            Debug.Log("PASS EncounterSceneChecks: dungeon/raid transitions, persistent data, no duplicate rewards, defeat, event cleanup");
            EditorApplication.UnlockReloadAssemblies();
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.UnlockReloadAssemblies();
            EditorApplication.Exit(1);
        }
        finally { Application.logMessageReceived -= onLog; }
    }

    private static void ClickThroughRaycast(UnityEngine.UI.Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var canvas = button.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
            button = UnityEngine.EventSystems.PointerEventData.InputButton.Left
        };
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
        Require(hits.Count > 0, "버튼에 클릭이 도달하지 않음: " + button.name);
        var target = UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject);
        Require(target == button.gameObject, "다른 UI가 버튼 클릭을 가림: " + hits[0].gameObject.name);
        UnityEngine.EventSystems.ExecuteEvents.Execute(target, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(target, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(target, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
    }

    private static async Task CheckPauseExit(CatGameRuntime runtime, bool dungeon)
    {
        var team = runtime.Data.characters.Take(dungeon ? 3 : 4).ToList();
        await Bounded(runtime.EnterEncounterAsync(dungeon, dungeon ? 1 : 0, team));
        long gems = runtime.Data.wallet.gems;
        float previousScale = Time.timeScale;
        var view = UnityEngine.Object.FindAnyObjectByType<CatGameSceneUI>();
        ClickThroughRaycast(GameObject.Find("Encounter Pause Button").GetComponent<UnityEngine.UI.Button>());
        var overlay = GameObject.Find("Encounter Pause");
        var exit = overlay.GetComponentsInChildren<UnityEngine.UI.Button>()
            .Single(button => button.GetComponentInChildren<UnityEngine.UI.Text>().text == "나가기");
        ClickThroughRaycast(exit);
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (SceneManager.GetActiveScene().name != CatGameRuntime.MainScene)
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("일시정지 메뉴에서 복귀 실패");
            await Task.Delay(50);
        }
        await Task.Delay(250);
        Require(Time.timeScale == previousScale, "메인 복귀 후 정지가 풀리지 않음");
        Require(GameObject.Find("Encounter Pause") == null, "메인 복귀 후 정지 팝업 잔류");
        Require(runtime.Data.wallet.gems == gems, "포기에 클리어 보상이 지급됨");
        Debug.Log("PASS paused exit to main: " + (dungeon ? "DungeonScene" : "RaidScene"));
    }

    private static async Task CheckEncounterPause()
    {
        var view = UnityEngine.Object.FindAnyObjectByType<CatGameSceneUI>();
        var field = UnityEngine.Object.FindAnyObjectByType<CatFieldController>();
        float previousScale = Time.timeScale;
        ClickThroughRaycast(GameObject.Find("Encounter Pause Button").GetComponent<UnityEngine.UI.Button>());
        Require(Time.timeScale == 0, "메뉴를 열어도 전투가 정지되지 않음");
        float seconds = field != null ? field.RaidSeconds : 0;
        double hp = field != null ? field.RaidBoss.Health.Current : 0;
        await Task.Delay(200);
        if (field != null)
        {
            Require(field.RaidSeconds == seconds, "일시정지 중 레이드 시간 감소");
            Require(field.RaidBoss.Health.Current == hp, "일시정지 중 공격 발생");
        }
        var overlay = GameObject.Find("Encounter Pause");
        Require(overlay != null, "일시정지 메뉴가 없음");
        var resume = overlay.GetComponentsInChildren<UnityEngine.UI.Button>()
            .Single(button => button.GetComponentInChildren<UnityEngine.UI.Text>().text == "계속하기");
        ClickThroughRaycast(resume);
        Require(Time.timeScale == previousScale, "계속하기 후 시간 배율 복원 실패");
        await Task.Delay(50);
        Require(GameObject.Find("Encounter Pause") == null, "계속하기 후 팝업 잔류");
        Debug.Log("PASS encounter pause/resume: " + SceneManager.GetActiveScene().name);
    }

    private static void CheckAttackDeduplication()
    {
        var root = new GameObject("Attack Regression Check");
        try
        {
            var data = BackendUserData.CreateNew();
            var catalog = Resources.Load<CatGameCatalog>("DefaultCatCatalog");
            var enemies = new FieldEnemy[3];
            for (int i = 0; i < enemies.Length; i++)
            {
                var obj = new GameObject("Test Enemy " + i);
                obj.transform.SetParent(root.transform);
                obj.transform.localPosition = new Vector3(0, 0, 1 + i * 1.3f);
                enemies[i] = obj.AddComponent<FieldEnemy>();
                var art = obj.AddComponent<SpriteRenderer>();
                var bar = new GameObject("HP").transform;
                bar.SetParent(obj.transform);
                enemies[i].Initialize(art, bar, 100);
            }
            var fighter = root.AddComponent<FieldCombatController>();
            fighter.Initialize(root.transform, enemies, data, catalog);
            var type = typeof(FieldCombatController);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var resolve = type.GetMethod("ResolveStats", flags);
            resolve.Invoke(fighter, null);
            var cooldown = type.GetField("cooldown", flags);
            cooldown.SetValue(fighter, 1.2f);
            data.characters.Find(cat => cat.characterId == data.loadout.characterId).level++;
            resolve.Invoke(fighter, null);
            Require((float)cooldown.GetValue(fighter) == 1.2f, "레벨업이 공격 대기시간을 초기화함");
            type.GetField("attackBehavior", flags).SetValue(fighter, new TargetAreaAttack());
            var arrowType = type.GetNestedType("SkillArrow", BindingFlags.NonPublic);
            object arrow = Activator.CreateInstance(arrowType, true);
            arrowType.GetField("damage").SetValue(arrow, 10d);
            var hit = type.GetMethod("HitArrow", flags);
            hit.Invoke(fighter, new[] { arrow, enemies[0] });
            hit.Invoke(fighter, new[] { arrow, enemies[1] });
            hit.Invoke(fighter, new[] { arrow, enemies[2] });
            foreach (var enemy in enemies) Require(enemy.Health.Current == 90, "관통 범위 공격 중복 피해");
            object secondArrow = Activator.CreateInstance(arrowType, true);
            arrowType.GetField("damage").SetValue(secondArrow, 10d);
            hit.Invoke(fighter, new[] { secondArrow, enemies[1] });
            foreach (var enemy in enemies) Require(enemy.Health.Current == 80, "별도 투사체의 정상 피해가 차단됨");
            Debug.Log("PASS attacks: growth cooldown preserved, piercing area deduplicated, separate projectile damage retained");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static async Task CheckSessionNotifications()
    {
        var data = BackendUserData.CreateNew();
        data.characters.Add(new OwnedCharacter { characterId = "cat_spinner", fragments = 50 });
        string original = data.loadout.characterId;
        bool saveSucceeds = false;
        var session = new CatGameSession(data, false, () => Task.FromResult(saveSucceeds));
        int selections = 0, changes = 0, wallets = 0;
        session.Events.Subscribe<SelectedCharacterChanged>(_ => selections++);
        session.Events.Subscribe<CharacterProgressChanged>(_ => changes++);
        session.Events.Subscribe<WalletChanged>(_ => wallets++);
        try
        {
            var cat = data.characters[1];
            Require(!await session.SelectCharacterAsync(cat), "실패 저장이 성공으로 반환됨");
            Require(data.loadout.characterId == original && selections == 0, "실패한 선택 변경 이벤트/롤백 오류");
            saveSucceeds = true;
            Require(await session.SelectCharacterAsync(cat), "성공 저장 반환 오류");
            Require(selections == 1, "선택 변경 알림 누락/중복");
            int before = changes;
            await session.LevelUpCharacterAsync(cat);
            Require(changes == before + 1 && cat.ascension == 1, "강화 알림 누락");
            saveSucceeds = false;
            before = changes;
            await session.LevelUpCharacterAsync(cat);
            Require(changes == before && cat.ascension == 1, "롤백한 강화가 완료로 통지됨");
            await session.AwardGoldAsync(10);
            Require(wallets == 1, "골드 변경 알림 오류");
            long gems = data.wallet.gems;
            await session.CompleteEncounterAsync(0);
            Require(data.wallet.gems == gems + 10 && session.HasPendingReward && wallets == 2, "저장 실패 보상 보류 알림 오류");
            Debug.Log("PASS session notifications: save success, rollback, upgrade, gold, pending reward");
        }
        finally { session.Events.Dispose(); }
    }

    private static async Task SimulateResult(bool won)
    {
        // 전투 결과 전달 이후의 실제 저장/결과 UI 경로를 검사합니다.
        var scene = UnityEngine.Object.FindAnyObjectByType<CatEncounterScene>();
        Require(scene != null, "전투 진입점 누락");
        typeof(CatEncounterScene).GetMethod("OnFinished", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(scene, new object[] { won });
        await Task.Delay(250);
    }

    private static async Task CheckCachedCharacterUI(BackendUserData data)
    {
        var ui = UnityEngine.Object.FindAnyObjectByType<CatGameSceneUI>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Call(string method, params object[] args) => typeof(CatGameSceneUI).GetMethod(method, flags).Invoke(ui, args);
        object Read(string field) => typeof(CatGameSceneUI).GetField(field, flags).GetValue(ui);
        Call("Show", "캐릭터");
        var root = (RectTransform)Read("characterListRoot");
        var grid = (Transform)Read("characterListGrid");
        var first = grid.GetChild(0);
        for (int i = 0; i < 5; i++)
        {
            Call("Show", "모험");
            Call("Show", "캐릭터");
            Require(ReferenceEquals(root, Read("characterListRoot")) && first == grid.GetChild(0), "캐릭터 목록 재생성");
            Call("CharacterDetail", data.characters[0]);
            var overlay = (RectTransform)Read("detailOverlay");
            ui.ShowSkillTree(data.characters[0]);
            Call("CharacterDetail", data.characters[0]);
            Require(ReferenceEquals(overlay, Read("detailOverlay")), "상세 팝업 배경 재생성");
            var frame = (RectTransform)Read("cachedDetailFrame");
            Require(frame.anchorMin == new Vector2(.035f, .15f), "스킬 창 복귀 크기 오류");
            Call("CloseDetailPopup");
            await Task.Delay(50);
        }
        Call("Show", "모험");
        Debug.Log("PASS cached character list / popup reuse and skill return");
    }

    private static async Task CheckSettings()
    {
        var ui = UnityEngine.Object.FindAnyObjectByType<CatGameSceneUI>();
        float volume = CatLocalSettings.GetVolume(CatAudioChannel.Bgm);
        float combatVolume = CatLocalSettings.GetVolume(CatAudioChannel.Combat);
        var music = new GameObject("Test BGM", typeof(AudioSource), typeof(CatAudioChannelSource));
        var effects = new GameObject("Test Combat", typeof(AudioSource), typeof(CatAudioChannelSource));
        music.GetComponent<CatAudioChannelSource>().Channel = CatAudioChannel.Bgm;
        bool muted = CatLocalSettings.Muted;
        int rate = CatLocalSettings.FrameRate;
        try
        {
            typeof(CatGameSceneUI).GetMethod("ShowSettings", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
            var slider = ui.GetComponentInChildren<UnityEngine.UI.Slider>();
            Require(slider != null, "설정 소리 슬라이더 누락");
            CatLocalSettings.SetMuted(false);
            slider.value = .35f;
            Require(Mathf.Approximately(music.GetComponent<AudioSource>().volume, .35f), "BGM 적용 실패");
            Require(Mathf.Approximately(effects.GetComponent<AudioSource>().volume, combatVolume), "BGM 조절이 효과음에 영향을 줌");
            CatLocalSettings.SetVolume(CatAudioChannel.Combat, .7f);
            Require(Mathf.Approximately(effects.GetComponent<AudioSource>().volume, .7f) && Mathf.Approximately(music.GetComponent<AudioSource>().volume, .35f), "효과음 분리 실패");
            CatLocalSettings.SetMuted(true);
            Require(AudioListener.volume == 0f && Mathf.Approximately(CatLocalSettings.GetVolume(CatAudioChannel.Bgm), .35f), "음소거/원래 음량 보존 실패");
            CatLocalSettings.SetMuted(false);
            CatLocalSettings.SetFrameRate(30);
            Require(Application.targetFrameRate == 30, "절전 프레임 적용 실패");
            CatLocalSettings.SetFrameRate(60);
            Require(Application.targetFrameRate == 60, "60 FPS 적용 실패");
            await Task.Delay(300);
            ScreenCapture.CaptureScreenshot("C:/Users/sp079/AppData/Local/Temp/CatRaising-settings-preview.png");
            await Task.Delay(300);
            typeof(CatGameSceneUI).GetMethod("ShowLogoutConfirmation", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
            Require(ui.GetComponentsInChildren<UnityEngine.UI.Text>().Any(text => text.text.Contains("계정과 고양이들은 삭제되지")), "로그아웃 확인창 누락");
            typeof(CatGameSceneUI).GetMethod("CloseSettings", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, null);
            Debug.Log("PASS Settings: popup, volume/mute, frame rate, logout confirmation (no server logout)");
        }
        finally
        {
            CatLocalSettings.SetVolume(CatAudioChannel.Bgm, volume);
            CatLocalSettings.SetVolume(CatAudioChannel.Combat, combatVolume);
            UnityEngine.Object.Destroy(music);
            UnityEngine.Object.Destroy(effects);
            CatLocalSettings.SetMuted(muted);
            CatLocalSettings.SetFrameRate(rate);
        }
    }
}
