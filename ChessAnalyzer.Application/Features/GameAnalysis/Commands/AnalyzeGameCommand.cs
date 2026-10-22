using ChessAnalyzer.Application.DTOs;
using MediatR;

namespace ChessAnalyzer.Application.Features.GameAnalysis.Commands;

public class AnalyzeGameCommand : IRequest<GameAnalysisResultDto>
{
    public string Pgn { get; set; } = string.Empty;

    public AnalyzeGameCommand()
    {
    }

    public AnalyzeGameCommand(string pgn)
    {
        Pgn = pgn;
    }
}