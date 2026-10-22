namespace ChessAnalyzer.Application.DTOs;

public class MoveAnalysisDto
{
    /// <summary>
    /// 1-based half-move index (1 for White's move 1, 2 for Black's move 1, etc.).
    /// </summary>
    public int Ply { get; set; }

    /// <summary>
    /// Full move number (1, 1, 2, 2, 3, 3...).
    /// </summary>
    public int MoveNumber { get; set; }

    /// <summary>
    /// "White" or "Black".
    /// </summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>
    /// Standard Algebraic Notation for the move played (e.g. "e4", "Nf3", "O-O", "Qh5#").
    /// </summary>
    public string San { get; set; } = string.Empty;

    /// <summary>
    /// UCI notation for the move played (e.g. "e2e4", "g1f3").
    /// </summary>
    public string Uci { get; set; } = string.Empty;

    /// <summary>
    /// FEN of the board position immediately before this move was played.
    /// </summary>
    public string FenBefore { get; set; } = string.Empty;

    /// <summary>
    /// FEN of the board position immediately after this move was played.
    /// </summary>
    public string FenAfter { get; set; } = string.Empty;

    /// <summary>
    /// Side whose turn it is to move in FenAfter ("White" or "Black").
    /// </summary>
    public string SideToMove { get; set; } = string.Empty;

    /// <summary>
    /// Stockfish evaluation of the resulting position (FenAfter).
    /// </summary>
    public EvaluationDto Evaluation { get; set; } = new();

    /// <summary>
    /// Stockfish recommended best move from FenAfter in UCI notation (e.g. "e7e5").
    /// </summary>
    public string BestMoveUci { get; set; } = string.Empty;

    /// <summary>
    /// Stockfish recommended best move in SAN if available.
    /// </summary>
    public string? BestMoveSan { get; set; }

    /// <summary>
    /// Principal variation line returned by the engine (e.g. "e7e5 g1f3 b8c6").
    /// </summary>
    public string? PrincipalVariation { get; set; }

    /// <summary>
    /// Chess.com style classification: Brilliant, Great, Best, Excellent, Good, Book, Inaccuracy, Mistake, Miss, Blunder.
    /// </summary>
    public string Classification { get; set; } = "Good";

    /// <summary>
    /// Chess.com icon symbol: !!, !, ★, ✓, 👍, 📖, ?!, ?, ✕, ??
    /// </summary>
    public string ClassificationSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Move accuracy score from 0.0 to 100.0 based on CAPS2 win expectancy model.
    /// </summary>
    public double AccuracyScore { get; set; } = 100.0;

    /// <summary>
    /// Win percentage for the moving player before this move was played (0 to 100).
    /// </summary>
    public double WinPercentBefore { get; set; } = 50.0;

    /// <summary>
    /// Win percentage for the moving player after this move was played (0 to 100).
    /// </summary>
    public double WinPercentAfter { get; set; } = 50.0;

    /// <summary>
    /// Loss in win percentage caused by this move (0.0 to 100.0).
    /// </summary>
    public double WinPercentLoss { get; set; }

    /// <summary>
    /// Expected points for the moving player before this move (0.00 to 1.00).
    /// </summary>
    public double ExpectedPointsBefore { get; set; } = 0.50;

    /// <summary>
    /// Expected points for the moving player after this move (0.00 to 1.00).
    /// </summary>
    public double ExpectedPointsAfter { get; set; } = 0.50;

    /// <summary>
    /// Loss in expected points caused by this move (0.00 to 1.00).
    /// </summary>
    public double ExpectedPointsLoss { get; set; } = 0.0;

    /// <summary>
    /// Human-friendly coach explanation of the move quality and suggested alternative.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
}
