// Explicit legal transitions; engine/UI side effects stay with their owners.
public sealed class DungeonWaveState
{
    public enum Phase { Preparing, Fighting, BetweenWaves, Finished }
    public Phase Current { get; private set; } = Phase.Preparing;
    public bool CanPrepare => Current == Phase.Preparing || Current == Phase.BetweenWaves;
    public bool IsFighting => Current == Phase.Fighting;
    public bool IsFinished => Current == Phase.Finished;
    public bool StartWave()
    {
        if (!CanPrepare) return false;
        Current = Phase.Fighting;
        return true;
    }
    public bool ClearWave()
    {
        if (!IsFighting) return false;
        Current = Phase.BetweenWaves;
        return true;
    }
    public bool Finish()
    {
        if (IsFinished) return false;
        Current = Phase.Finished;
        return true;
    }
}

public sealed class EncounterFlowState
{
    public enum Phase { Initializing, Active, Paused, ResolvingResult, Result, Error, Returning }
    public Phase Current { get; private set; } = Phase.Initializing;
    private Phase beforeReturn;
    public void Ready() { if (Current == Phase.Initializing) Current = Phase.Active; }
    public void Fail() { if (Current != Phase.Returning) Current = Phase.Error; }
    public bool Pause()
    {
        if (Current != Phase.Active) return false;
        Current = Phase.Paused; return true;
    }
    public bool Resume()
    {
        if (Current != Phase.Paused) return false;
        Current = Phase.Active; return true;
    }
    public bool BeginResult()
    {
        if (Current != Phase.Active && Current != Phase.Paused) return false;
        Current = Phase.ResolvingResult; return true;
    }
    public void CompleteResult() { if (Current == Phase.ResolvingResult) Current = Phase.Result; }
    public bool BeginReturn()
    {
        if (Current == Phase.Returning || Current == Phase.ResolvingResult) return false;
        beforeReturn = Current; Current = Phase.Returning; return true;
    }
    public void CancelReturn() { if (Current == Phase.Returning) Current = beforeReturn; }
}
