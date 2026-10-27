using System;
using System.Collections.Generic;

// 로그인 세션이 소유합니다. 명령/저장 요청은 전달하지 않고 확정된 상태만 알립니다.
// 메인 스레드 전용. 발행 중에는 배열을 복사하지 않습니다.
public sealed class SessionEventBus : IDisposable
{
    private interface IChannel { void Clear(); }
    private sealed class Channel<T> : IChannel
    {
        public Subscription<T>[] Items = Array.Empty<Subscription<T>>();
        public void Clear()
        {
            foreach (var item in Items) item.Active = false;
            Items = Array.Empty<Subscription<T>>();
        }
    }
    private sealed class Subscription<T> : IDisposable
    {
        public bool Active = true;
        public Action<T> Handler;
        public Channel<T> Channel;
        public void Dispose()
        {
            if (!Active) return;
            Active = false;
            var items = new List<Subscription<T>>(Channel.Items);
            items.Remove(this);
            Channel.Items = items.ToArray();
        }
    }
    private readonly Dictionary<Type, IChannel> channels = new Dictionary<Type, IChannel>();
    private readonly Action<Exception> reportError;
    private bool disposed;
    public SessionEventBus(Action<Exception> reportError = null) { this.reportError = reportError; }
    public IDisposable Subscribe<T>(Action<T> handler)
    {
        if (disposed) throw new ObjectDisposedException(nameof(SessionEventBus));
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        if (!channels.TryGetValue(typeof(T), out var value)) channels[typeof(T)] = value = new Channel<T>();
        var channel = (Channel<T>)value;
        var subscription = new Subscription<T> { Handler = handler, Channel = channel };
        var items = new List<Subscription<T>>(channel.Items) { subscription };
        channel.Items = items.ToArray();
        return subscription;
    }
    public void Publish<T>(T message)
    {
        if (disposed || !channels.TryGetValue(typeof(T), out var value)) return;
        var snapshot = ((Channel<T>)value).Items;
        foreach (var item in snapshot)
        {
            if (!item.Active) continue;
            try { item.Handler(message); }
            catch (Exception error)
            {
                // UI 오류가 이미 성공한 저장을 실패/롤백으로 바꾸지 않게 격리합니다.
                try { if (reportError != null) reportError(error); else System.Diagnostics.Trace.TraceError(error.ToString()); }
                catch { /* 오류 보고 실패도 저장 결과에 영향을 주지 않습니다. */ }
            }
        }
    }
    public int SubscriberCount<T>() => channels.TryGetValue(typeof(T), out var value) ? ((Channel<T>)value).Items.Length : 0;
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var channel in channels.Values) channel.Clear();
        channels.Clear();
    }
}

public readonly struct SessionBusyChanged { public readonly bool Busy; public SessionBusyChanged(bool value) { Busy = value; } }
public readonly struct GameplayBlockChanged { public readonly bool Blocked; public GameplayBlockChanged(bool value) { Blocked = value; } }
public readonly struct SessionMessage { public readonly string Text; public SessionMessage(string value) { Text = value; } }
public readonly struct WalletChanged
{
    public readonly long Gold, Gems;
    public WalletChanged(long gold, long gems) { Gold = gold; Gems = gems; }
}
public readonly struct SelectedCharacterChanged { public readonly string Id; public SelectedCharacterChanged(string id) { Id = id; } }
public readonly struct CharacterProgressChanged { public readonly string Id; public CharacterProgressChanged(string id) { Id = id; } }
public readonly struct SessionSaveFinished
{
    public readonly bool Success, PendingReward;
    public SessionSaveFinished(bool success, bool pendingReward) { Success = success; PendingReward = pendingReward; }
}
