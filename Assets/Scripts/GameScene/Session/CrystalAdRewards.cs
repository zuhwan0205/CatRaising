using System;

public static class CrystalAdRewards
{
    public const int Amount = 10;
    public const int DailyLimit = 5;
    public static string Today => DateTime.UtcNow.ToString("yyyy-MM-dd");

    public static int Remaining(PlayerWallet wallet)
    {
        return wallet.crystalAdDay == Today
            ? Math.Max(0, DailyLimit - Math.Max(0, wallet.crystalAdCount)) : DailyLimit;
    }
}
