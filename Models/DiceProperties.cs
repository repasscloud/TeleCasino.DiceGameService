namespace TeleCasino.DiceGameService.Models;

public static class DiceProperties
{
    // Precomputed house odds (×0.95) from your table
    public static readonly Dictionary<DiceBetType, decimal> HouseOdds = new()
    {
        [DiceBetType.Two]    = 33.25m,
        [DiceBetType.Three]  = 16.15m,
        [DiceBetType.Four]   = 10.45m,
        [DiceBetType.Five]   = 7.60m,
        [DiceBetType.Six]    = 5.89m,
        [DiceBetType.Seven]  = 4.75m,
        [DiceBetType.Eight]  = 5.89m,
        [DiceBetType.Nine]   = 7.60m,
        [DiceBetType.Ten]    = 10.45m,
        [DiceBetType.Eleven] = 16.15m,
        [DiceBetType.Twelve] = 33.25m,

        [DiceBetType.Odd]    = 1.90m,
        [DiceBetType.Even]   = 1.90m,
        [DiceBetType.Under7] = 1.33m,
        [DiceBetType.Over7]  = 1.33m,

        [DiceBetType.Pair1]  = 35.00m,
        [DiceBetType.Pair2]  = 35.00m,
        [DiceBetType.Pair3]  = 35.00m,
        [DiceBetType.Pair4]  = 35.00m,
        [DiceBetType.Pair5]  = 35.00m,
        [DiceBetType.Pair6]  = 35.00m,
    };
}
