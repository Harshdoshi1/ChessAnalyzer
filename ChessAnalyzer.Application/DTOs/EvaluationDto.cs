namespace ChessAnalyzer.Application.DTOs;

public class EvaluationDto
{
    /// <summary>
    /// Type of evaluation: "cp" for centipawns or "mate" for mate in N moves.
    /// </summary>
    public string Type { get; set; } = "cp";

    /// <summary>
    /// Evaluation in centipawns, normalized to White's perspective (+ for White advantage, - for Black advantage).
    /// </summary>
    public int? Centipawns { get; set; }

    /// <summary>
    /// Moves to checkmate, normalized to White's perspective (+N for White mate, -N for Black mate, 0 for mate on board).
    /// </summary>
    public int? MateInMoves { get; set; }

    /// <summary>
    /// Evaluation in pawn units, e.g. +0.35, -1.20 (null if mate).
    /// </summary>
    public double? Value { get; set; }

    /// <summary>
    /// Formatted display score: e.g. "+0.35", "-1.20", "+M2", "-M1", "# Checkmate", "0.00".
    /// </summary>
    public string ScoreText { get; set; } = "0.00";

    /// <summary>
    /// Whether this position indicates a forced checkmate or checkmate on board.
    /// </summary>
    public bool IsMate => Type == "mate" || MateInMoves.HasValue;

    /// <summary>
    /// Search depth reached by the engine.
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Stockfish recommended best move from this position in UCI format (e.g. "e2e4").
    /// </summary>
    public string? BestMoveUci { get; set; }

    /// <summary>
    /// Stockfish recommended best move in SAN if available.
    /// </summary>
    public string? BestMoveSan { get; set; }
}
