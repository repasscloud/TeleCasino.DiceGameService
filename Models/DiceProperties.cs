namespace TeleCasino.DiceGameService.Models;

public static class DiceProperties
{
    // Precomputed house odds (×0.95) from your table
    public static readonly Dictionary<string, decimal> HouseOdds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["2"]      = 33.25m,
        ["12"]     = 33.25m,
        ["3"]      = 16.15m,
        ["11"]     = 16.15m,
        ["4"]      = 10.45m,
        ["10"]     = 10.45m,
        ["5"]      = 7.60m,
        ["9"]      = 7.60m,
        ["6"]      = 5.89m,
        ["8"]      = 5.89m,
        ["7"]      = 4.75m,
        // optional bets
        ["Odd"]    = 1.90m,
        ["Even"]   = 1.90m,
        ["Under7"] = 1.33m,
        ["Over7"]  = 1.33m
    };
}