using ChessAnalyzer.Application.DTOs;

namespace ChessAnalyzer.Application.Interfaces.Chess;

public interface IChessEngine
{
    Task<string> AnalyzeAsync(string pgn, CancellationToken cancellationToken = default);

    Task<GameAnalysisResultDto> AnalyzeGameAsync(string pgn, CancellationToken cancellationToken = default);
}