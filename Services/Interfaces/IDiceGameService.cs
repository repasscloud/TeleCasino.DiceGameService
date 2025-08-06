using TeleCasino.DiceGameService.Models;

namespace TeleCasino.DiceGameService.Services.Interface;

public interface IDiceGameService
{
    Task<DiceResult> PlayGameAsync(decimal wager, string betArg, int gameSessionId);
}