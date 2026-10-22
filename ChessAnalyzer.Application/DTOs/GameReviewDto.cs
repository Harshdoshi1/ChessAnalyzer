namespace ChessAnalyzer.Application.DTOs;

public class GameReviewDto
{
    /// <summary>
    /// Overall accuracy score for White (0.0 to 100.0).
    /// </summary>
    public double WhiteAccuracy { get; set; }

    /// <summary>
    /// Overall accuracy score for Black (0.0 to 100.0).
    /// </summary>
    public double BlackAccuracy { get; set; }

    /// <summary>
    /// Name of the recognized opening (e.g. "Ruy Lopez: Morphy Defense").
    /// </summary>
    public string OpeningName { get; set; } = string.Empty;

    /// <summary>
    /// ECO code of the opening (e.g. "C78").
    /// </summary>
    public string OpeningEco { get; set; } = string.Empty;

    /// <summary>
    /// High-level verdict / game recap commentary.
    /// </summary>
    public string SummaryVerdict { get; set; } = string.Empty;

    /// <summary>
    /// Counts of each move category for White.
    /// </summary>
    public MoveClassificationCountsDto WhiteClassifications { get; set; } = new();

    /// <summary>
    /// Counts of each move category for Black.
    /// </summary>
    public MoveClassificationCountsDto BlackClassifications { get; set; } = new();
}

public class MoveClassificationCountsDto
{
    public int Brilliant { get; set; }
    public int Great { get; set; }
    public int Best { get; set; }
    public int Excellent { get; set; }
    public int Good { get; set; }
    public int Book { get; set; }
    public int Inaccuracy { get; set; }
    public int Mistake { get; set; }
    public int Miss { get; set; }
    public int Blunder { get; set; }
}
