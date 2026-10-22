using ChessAnalyzer.Application.Features.GameAnalysis.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChessAnalyzer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameAnalysisController : ControllerBase
{
    private readonly IMediator _mediator;

    public GameAnalysisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze(
        [FromBody] AnalyzeGameCommand command,
        CancellationToken cancellationToken)
    {
        if (command == null || string.IsNullOrWhiteSpace(command.Pgn))
        {
            return BadRequest(new { message = "PGN cannot be empty." });
        }

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An unexpected error occurred during game analysis: {ex.Message}" });
        }
    }
}