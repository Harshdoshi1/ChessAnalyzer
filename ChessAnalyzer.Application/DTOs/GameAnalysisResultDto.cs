namespace ChessAnalyzer.Application.DTOs;

public class GameAnalysisResultDto
{
    /// <summary>
    /// Text summary of the analysis (preserved for backward compatibility).
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Evaluation of the starting position before move 1 (ply 0).
    /// </summary>
    public EvaluationDto? InitialEvaluation { get; set; }

    /// <summary>
    /// Evaluation of the final board position.
    /// </summary>
    public EvaluationDto? FinalEvaluation { get; set; }

    /// <summary>
    /// Move-by-move analysis results for every played move in the game.
    /// </summary>
    public List<MoveAnalysisDto> Moves { get; set; } = new();

    /// <summary>
    /// Total number of half-moves analyzed.
    /// </summary>
    public int TotalMoves => Moves.Count;

    /// <summary>
    /// Recommended next move for the final position (or starting position if no moves).
    /// </summary>
    public string? SuggestedNextMove => Moves.Count > 0
        ? Moves.Last().BestMoveUci
        : InitialEvaluation?.BestMoveUci;

    /// <summary>
    /// Chess.com style game review containing player accuracy scores, classification breakdown, and verdict.
    /// </summary>
    public GameReviewDto? Review { get; set; }
}