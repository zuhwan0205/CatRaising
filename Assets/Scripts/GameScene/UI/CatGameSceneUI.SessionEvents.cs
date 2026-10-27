using System;
using System.Collections.Generic;

public partial class CatGameSceneUI
{
    private CatGameSession boundSession;
    private readonly List<IDisposable> sessionSubscriptions = new List<IDisposable>();
    private bool walletRefreshPending, characterRefreshPending;

    public void BindSessionEvents(CatGameSession session)
    {
        ReleaseSessionEvents();
        boundSession = session;
        if (isActiveAndEnabled) SubscribeSessionEvents();
    }
    private void OnEnable() => SubscribeSessionEvents();
    private void OnDisable() => ReleaseSessionEvents();

    private void SubscribeSessionEvents()
    {
        if (boundSession == null || sessionSubscriptions.Count != 0) return;
        var events = boundSession.Events;
        sessionSubscriptions.Add(events.Subscribe<SessionBusyChanged>(change => SetBusy(change.Busy)));
        sessionSubscriptions.Add(events.Subscribe<SessionMessage>(message => ShowStatus(message.Text)));
        sessionSubscriptions.Add(events.Subscribe<WalletChanged>(_ => walletRefreshPending = true));
        sessionSubscriptions.Add(events.Subscribe<SelectedCharacterChanged>(_ => MarkCharacterRefresh()));
        sessionSubscriptions.Add(events.Subscribe<CharacterProgressChanged>(_ => MarkCharacterRefresh()));
        sessionSubscriptions.Add(events.Subscribe<SessionSaveFinished>(result =>
        {
            PendingAdSave = result.PendingReward;
            MarkCharacterRefresh(); // 저장 중 보았던 임시 값도 롤백 후 실제 상태로 복구합니다.
        }));
        SetBusy(boundSession.IsUiBusy);
        PendingAdSave = boundSession.HasPendingReward;
        MarkCharacterRefresh();
    }
    private void MarkCharacterRefresh() { walletRefreshPending = characterRefreshPending = true; }
    private void ReleaseSessionEvents()
    {
        foreach (var subscription in sessionSubscriptions) subscription.Dispose();
        sessionSubscriptions.Clear();
        walletRefreshPending = characterRefreshPending = false;
    }
    private void LateUpdate()
    {
        // 한 프레임에 여러 보상이 들어와도 화면 갱신은 한 번만 합니다.
        if (walletRefreshPending && data != null)
        {
            walletRefreshPending = false;
            RefreshWallet();
        }
        if (characterRefreshPending)
        {
            characterRefreshPending = false;
            if (characterListRoot != null && characterListRoot.gameObject.activeInHierarchy && detailOverlay == null) Characters();
        }
    }
}
