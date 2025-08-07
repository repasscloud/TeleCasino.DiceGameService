using Microsoft.AspNetCore.Mvc;
using TeleCasino.DiceGameService.Models;
using TeleCasino.DiceGameService.Services.Interface;

namespace TeleCasino.DiceGameApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiceController : ControllerBase
{
    private readonly IDiceGameService _diceGameService;

    public DiceController(IDiceGameService diceGameService)
    {
        _diceGameService = diceGameService;
    }

    /// <summary>
    /// Plays a Dice game and returns the result with a generated video file path.
    /// </summary>
    /// <param name="wager">Amount wagered (must match allowed values).</param>
    /// <param name="betArg">Type of bet (e.g. "Odd", "Even", "Over7", "Pair3", "12").</param>
    /// <param name="gameSessionId">Game session identifier.</param>
    [HttpPost("play")]
    [ProducesResponseType(typeof(DiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlayGame(
        [FromQuery] decimal wager,
        [FromQuery] string betArg,
        [FromQuery] int gameSessionId)
    {
        if (wager <= 0)
            return BadRequest("Wager must be a positive number.");

        if (string.IsNullOrWhiteSpace(betArg))
            return BadRequest("A bet argument must be provided.");

        // Only allow known wager amounts
        var allowedWagers = new[] { 0.05m, 0.10m, 0.50m, 1.0m, 2.0m, 5.0m, 10m, 25m, 50m };
        if (!allowedWagers.Contains(wager))
            return BadRequest("Invalid wager amount. Allowed: 0.05, 0.10, 0.50, 1.0, 2.0, 5.0, 10.0, 25.0, 50.0");

        // Validate betArg maps to DiceBetType enum and has a payout
        if (!Enum.TryParse<DiceBetType>(betArg, ignoreCase: true, out var betType))
            return BadRequest($"Invalid bet type '{betArg}'. Must be one of: {string.Join(", ", DiceProperties.HouseOdds.Keys)}");

        if (!DiceProperties.HouseOdds.ContainsKey(betType))
            return BadRequest($"No house odds defined for bet type '{betType}'.");

        try
        {
            var result = await _diceGameService.PlayGameAsync(wager, betArg, gameSessionId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest($"Error occurred while playing the game: {ex.Message}");
        }
    }
}
