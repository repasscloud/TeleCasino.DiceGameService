namespace TeleCasino.DiceGameService.Models;

public class DiceResult
{
    public required string Id { get; set; }
    public decimal Wager { get; set; }
    public decimal Payout { get; set; }
    public decimal NetGain { get; set; }
    public required string VideoFile { get; set; }
    public bool Win { get; set; }

    // game mechanics
    public DiceBetType BetType { get; set; }
    public int DieSum { get; set; }
    public int GameSessionId { get; set; }
}