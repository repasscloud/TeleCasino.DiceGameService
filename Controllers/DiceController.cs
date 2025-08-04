using Microsoft.AspNetCore.Mvc;
using TeleCasino.DiceGameService.Models;
using TeleCasino.DiceGameService.Services.Interface;

namespace TeleCasino.DiceGameApi.Controllers
{
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
        /// <param name="wager">Amount wagered.</param>
        /// <param name="betArg">Type of bet placed (e.g., "Odd", "Even", "Over7", "Under7", "Pair3", "7").</param>
        /// <param name="gameSessionId">Game session identifier.</param>
        [HttpPost("play")]
        [ProducesResponseType(typeof(DiceResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PlayGame(
            [FromQuery] int wager,
            [FromQuery] string betArg,
            [FromQuery] int gameSessionId)
        {
            if (wager <= 0)
                return BadRequest("Wager must be a positive integer.");

            if (string.IsNullOrWhiteSpace(betArg))
                return BadRequest("A bet argument must be provided.");

            // Validate betArg
            var validBets = DiceProperties.HouseOdds.Keys;
            if (!validBets.Contains(betArg, StringComparer.OrdinalIgnoreCase))
                return BadRequest($"Invalid bet type '{betArg}'. Valid options are: {string.Join(", ", validBets)}");

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
}
