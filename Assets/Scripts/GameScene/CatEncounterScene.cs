using System;
using UnityEngine;

// RaidScene / DungeonScene의 공통 진입점. 전투 구현과 UI는 기존 코드를 재사용합니다.
public sealed class CatEncounterScene : MonoBehaviour
{
    private CatGameRuntime runtime;
    private CatGameSceneUI view;
    private CatFieldController field;
    private readonly EncounterFlowState flow = new EncounterFlowState();
    private bool menuPaused;
    private float resumeTimeScale;

    private void Start()
    {
        runtime = CatGameRuntime.Current;
        view = gameObject.AddComponent<CatGameSceneUI>();
        if (runtime == null || runtime.Session == null)
        {
            // 전투 씬만 직접 실행했을 때 임의 보유 고양이나 보상을 만들지 않습니다.
            runtime = CatGameRuntime.Initialize(BackendGameData.userData ?? BackendUserData.CreateNew(), null,
                BackendGameData.userData == null);
        }
        var entry = runtime.Encounter;
        bool dungeonScene = gameObject.scene.name == CatGameRuntime.DungeonScene;
        try
        {
            if (entry == null || entry.ResultHandled || entry.IsDungeon != dungeonScene)
                throw new InvalidOperationException("메인 화면에서 고양이를 편성한 뒤 입장해주세요.");
            if (!dungeonScene)
            {
                field = gameObject.AddComponent<CatFieldController>();
                field.Build(runtime.Data, runtime.Catalog, raidOnly: true);
            }
            view.Build(runtime.Data, runtime.Catalog, null, runtime.IsPreview, field != null ? field.Output : null, encounterOnly: true);
            BindEvents();
            var team = entry.ResolveTeam(runtime.Data);
            flow.Ready();
            if (dungeonScene) view.BeginDungeonDefense(entry.Index, team);
            else
            {
                field.RaidFinished += OnFinished;
                view.StickChanged += field.SetStick;
                view.RaidField = field;
                field.BeginRaid(entry.Index, team, runtime.Data, runtime.Catalog);
                view.ShowRaidBattle(entry.Index);
            }
        }
        catch (Exception error)
        {
            flow.Fail();
            // 초기화 실패 시에도 메인으로 복귀할 수 있는 화면을 제공합니다.
            if (!viewReady)
            {
                view.Build(runtime.Data, runtime.Catalog, null, runtime.IsPreview, null, encounterOnly: true);
                BindEvents();
            }
            if (field != null) field.EndRaid();
            view.ShowEncounterError(error.Message);
            Debug.LogWarning(error.Message);
        }
    }

    private bool viewReady;
    private void BindEvents()
    {
        viewReady = true;
        view.DungeonDefenseFinished += OnFinished;
        view.EncounterExitRequested += ReturnToMain;
        view.EncounterPauseChanged += SetMenuPaused;
        view.BindSessionEvents(runtime.Session);
    }

    private async void OnFinished(bool won)
    {
        if (!flow.BeginResult()) return;
        RestorePause();
        view.CloseEncounterPause();
        try
        {
            bool rewarded = await runtime.FinishEncounterAsync(won);
            flow.CompleteResult();
            if (view != null) view.ShowRaidResult(won, rewarded, runtime.Session.HasPendingReward);
        }
        catch (Exception error)
        {
            flow.Fail();
            Debug.LogException(error);
            if (view != null) view.ShowEncounterError("결과 처리 중 오류가 발생했습니다. 메인에서 저장 상태를 확인해주세요.");
        }
    }

    private async void ReturnToMain()
    {
        if (runtime.Session.IsSaving || runtime.IsTransitioning || !flow.BeginReturn()) return;
        view.SetBusy(true);
        view.SetSceneTransition(true);
        // 포기는 보상을 지급하지 않으며 화면 전환 중 전투 종료를 다시 처리하지 않습니다.
        try { await runtime.ReturnToMainAsync(); }
        catch (Exception error)
        {
            if (view != null) { view.SetSceneTransition(false); view.SetBusy(false); view.ShowStatus(error.Message); }
        }
        finally { if (this != null) flow.CancelReturn(); }
    }

    private void SetMenuPaused(bool value)
    {
        if (menuPaused == value) return;
        if (value)
        {
            if (!flow.Pause()) return;
            resumeTimeScale = Time.timeScale;
            menuPaused = true;
            Time.timeScale = 0;
            if (field != null) field.SetStick(Vector2.zero);
        }
        else if (flow.Resume()) RestorePause();
    }
    private void RestorePause()
    {
        if (!menuPaused) return;
        menuPaused = false;
        flow.Resume();
        Time.timeScale = resumeTimeScale;
    }
    private void OnDisable()
    {
        RestorePause();
        if (view != null) view.CloseEncounterPause();
    }

    private void OnDestroy()
    {
        if (field != null)
        {
            field.RaidFinished -= OnFinished;
            if (view != null) view.StickChanged -= field.SetStick;
        }
        if (ReferenceEquals(view, null)) return;
        view.DungeonDefenseFinished -= OnFinished;
        view.EncounterExitRequested -= ReturnToMain;
        view.EncounterPauseChanged -= SetMenuPaused;
    }
}
