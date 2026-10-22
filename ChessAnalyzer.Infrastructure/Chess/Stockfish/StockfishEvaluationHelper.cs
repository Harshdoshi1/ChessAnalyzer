using ChessAnalyzer.Application.DTOs;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChessAnalyzer.Infrastructure.Chess.Stockfish;

public static class StockfishEvaluationHelper
{
    private static readonly Regex DepthRegex = new(@"\bdepth\s+(\d+)\b", RegexOptions.Compiled);
    private static readonly Regex CpRegex = new(@"\bscore\s+cp\s+(-?\d+)\b", RegexOptions.Compiled);
    private static readonly Regex MateRegex = new(@"\bscore\s+mate\s+(-?\d+)\b", RegexOptions.Compiled);
    private static readonly Regex BestMoveRegex = new(@"^bestmove\s+([^\s]+)", RegexOptions.Compiled);
    private static readonly Regex PvRegex = new(@"\bpv\s+(.+)$", RegexOptions.Compiled);

    public static EvaluationDto ParseEvaluation(string? infoLine, string sideToMove, int fallbackDepth = 10)
    {
        var depth = fallbackDepth;
        if (!string.IsNullOrWhiteSpace(infoLine))
        {
            var depthMatch = DepthRegex.Match(infoLine);
            if (depthMatch.Success && int.TryParse(depthMatch.Groups[1].Value, out var parsedDepth))
            {
                depth = parsedDepth;
            }
        }

        var isBlack = sideToMove.Equals("Black", StringComparison.OrdinalIgnoreCase) ||
                      sideToMove.Equals("b", StringComparison.OrdinalIgnoreCase);

        // Check for mate score first
        if (!string.IsNullOrWhiteSpace(infoLine))
        {
            var mateMatch = MateRegex.Match(infoLine);
            if (mateMatch.Success && int.TryParse(mateMatch.Groups[1].Value, out var rawMate))
            {
                var normalizedMate = isBlack ? -rawMate : rawMate;
                string mateText;
                if (normalizedMate > 0)
                {
                    mateText = $"+M{normalizedMate}";
                }
                else if (normalizedMate < 0)
                {
                    mateText = $"-M{-normalizedMate}";
                }
                else
                {
                    mateText = isBlack ? "# 1-0" : "# 0-1";
                }

                return new EvaluationDto
                {
                    Type = "mate",
                    MateInMoves = normalizedMate,
                    Centipawns = null,
                    Value = null,
                    ScoreText = mateText,
                    Depth = depth
                };
            }

            var cpMatch = CpRegex.Match(infoLine);
            if (cpMatch.Success && int.TryParse(cpMatch.Groups[1].Value, out var rawCp))
            {
                var normalizedCp = isBlack ? -rawCp : rawCp;
                var pawns = Math.Round(normalizedCp / 100.0, 2);
                string scoreText;
                if (normalizedCp > 0)
                {
                    scoreText = $"+{pawns.ToString("0.00", CultureInfo.InvariantCulture)}";
                }
                else if (normalizedCp < 0)
                {
                    scoreText = pawns.ToString("0.00", CultureInfo.InvariantCulture);
                }
                else
                {
                    scoreText = "0.00";
                }

                return new EvaluationDto
                {
                    Type = "cp",
                    Centipawns = normalizedCp,
                    MateInMoves = null,
                    Value = pawns,
                    ScoreText = scoreText,
                    Depth = depth
                };
            }
        }

        // Default neutral evaluation if no score found
        return new EvaluationDto
        {
            Type = "cp",
            Centipawns = 0,
            MateInMoves = null,
            Value = 0.0,
            ScoreText = "0.00",
            Depth = depth
        };
    }

    public static string ParseBestMove(string? bestMoveLine)
    {
        if (string.IsNullOrWhiteSpace(bestMoveLine))
        {
            return string.Empty;
        }

        var match = BestMoveRegex.Match(bestMoveLine.Trim());
        if (match.Success)
        {
            var move = match.Groups[1].Value;
            return move.Equals("(none)", StringComparison.OrdinalIgnoreCase) ? string.Empty : move;
        }

        return string.Empty;
    }

    public static string? ParsePrincipalVariation(string? infoLine)
    {
        if (string.IsNullOrWhiteSpace(infoLine))
        {
            return null;
        }

        var match = PvRegex.Match(infoLine);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
