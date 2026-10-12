using System;
using System.Threading.Tasks;
using GoogleMobileAds.Api;

// 설치된 Google Mobile Ads SDK를 사용합니다. 개발 중에는 공식 테스트 광고만 사용합니다.
public static class CrystalRewardedAd
{
    private static Task initialization;

    public static async Task<bool> ShowAsync()
    {
        if (initialization == null)
        {
            var initialized = new TaskCompletionSource<bool>();
            MobileAds.Initialize(_ => initialized.TrySetResult(true));
            initialization = initialized.Task;
        }
        if (await Task.WhenAny(initialization, Task.Delay(30000)) != initialization)
            throw new Exception("광고 초기화 시간이 초과되었습니다. 다시 시도해주세요.");
        await initialization;
#if UNITY_IOS
        const string id = "ca-app-pub-3940256099942544/4411468910";
#else
        const string id = "ca-app-pub-3940256099942544/1033173712";
#endif
        var loaded = new TaskCompletionSource<RewardedAd>();
        RewardedAd.Load(id, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                ad?.Destroy();
                loaded.TrySetException(new Exception("광고를 불러오지 못했습니다. 잠시 후 다시 시도해주세요."));
            }
            else if (!loaded.TrySetResult(ad))
                ad.Destroy();
        });
        if (await Task.WhenAny(loaded.Task, Task.Delay(30000)) != loaded.Task)
        {
            loaded.TrySetCanceled();
            throw new Exception("광고 로딩 시간이 초과되었습니다.");
        }
        var current = await loaded.Task;
        try
        {
            if (!current.CanShowAd())
                throw new Exception("광고를 재생할 수 없습니다.");
            var closed = new TaskCompletionSource<bool>();
            int earned = 0;
            current.OnAdFullScreenContentClosed += () => closed.TrySetResult(true);
            current.OnAdFullScreenContentFailed += _ => closed.TrySetResult(false);
            current.Show(_ => System.Threading.Interlocked.Exchange(ref earned, 1));
            await closed.Task;
            return System.Threading.Volatile.Read(ref earned) == 1;
        }
        finally { current.Destroy(); }
    }
}
