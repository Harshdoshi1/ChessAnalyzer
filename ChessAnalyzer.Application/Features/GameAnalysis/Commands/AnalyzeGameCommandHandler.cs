using ChessAnalyzer.Application.DTOs;
using ChessAnalyzer.Application.Interfaces.Chess;
using MediatR;

namespace ChessAnalyzer.Application.Features.GameAnalysis.Commands;

public class AnalyzeGameCommandHandler
    : IRequestHandler<AnalyzeGameCommand, GameAnalysisResultDto>
{
    private readonly IChessEngine _chessEngine;

    public AnalyzeGameCommandHandler(IChessEngine chessEngine)
    {
        _chessEngine = chessEngine;
    }

    public async Task<GameAnalysisResultDto> Handle(
        AnalyzeGameCommand request,
        CancellationToken cancellationToken)
    {
        return await _chessEngine.AnalyzeGameAsync(request.Pgn, cancellationToken);
    }
}