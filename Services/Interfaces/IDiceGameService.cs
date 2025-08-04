using TeleCasino.DiceGameService.Models;

namespace TeleCasino.DiceGameService.Services.Interface;

public interface IDiceGameService
{
    Task<DiceResult> PlayGameAsync(int wager, string betArg, int gameSessionId);
}