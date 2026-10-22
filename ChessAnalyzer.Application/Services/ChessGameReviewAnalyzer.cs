using ChessAnalyzer.Application.DTOs;
using System.Text.RegularExpressions;

namespace ChessAnalyzer.Application.Services;

public static class ChessGameReviewAnalyzer
{
    public static GameReviewDto AnalyzeGameReview(
        List<MoveAnalysisDto> moves,
        EvaluationDto? initialEval,
        string? pgn = null)
    {
        var review = new GameReviewDto();

        if (moves == null || moves.Count == 0)
        {
            review.WhiteAccuracy = 100.0;
            review.BlackAccuracy = 100.0;
            review.SummaryVerdict = "No moves to analyze.";
            return review;
        }

        // 1. Extract PGN metadata or detect opening from moves
        var (whiteName, blackName, opening, eco) = ParsePgnHeaders(pgn);
        var playedSans = moves.Select(m => m.San).ToList();
        var (detectedName, detectedEco) = ChessOpeningBook.IdentifyOpening(playedSans);

        review.OpeningName = !string.IsNullOrWhiteSpace(opening) ? opening : detectedName;
        review.OpeningEco = !string.IsNullOrWhiteSpace(eco) ? eco : detectedEco;

        // 2. Analyze each move using Expected-Points model
        var whiteAccuracies = new List<double>();
        var blackAccuracies = new List<double>();
        var progressiveSans = new List<string>(moves.Count);

        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            progressiveSans.Add(move.San);
            var isWhite = move.Color.Equals("White", StringComparison.OrdinalIgnoreCase);

            // Pre-move position evaluation
            EvaluationDto priorEval = (i == 0)
                ? (initialEval ?? new EvaluationDto { ScoreText = "0.00", Centipawns = 0, Value = 0.0 })
                : moves[i - 1].Evaluation;

            string priorBestUci = (i == 0)
                ? (initialEval?.BestMoveUci ?? string.Empty)
                : moves[i - 1].BestMoveUci;

            string priorBestSan = (i == 0)
                ? (initialEval?.BestMoveSan ?? priorBestUci)
                : (moves[i - 1].BestMoveSan ?? priorBestUci);

            if (string.IsNullOrWhiteSpace(priorBestSan))
            {
                priorBestSan = FormatUciToSanFallback(priorBestUci, move.FenBefore);
            }

            bool isEngineTopMove = !string.IsNullOrWhiteSpace(priorBestUci) &&
                                   move.Uci.Equals(priorBestUci, StringComparison.OrdinalIgnoreCase);

            // Calculate Expected Points (0.00 to 1.00) from perspective of moving player
            double expectedBefore = EvaluationToExpectedPoints(priorEval, isWhite);
            double expectedAfter = EvaluationToExpectedPoints(move.Evaluation, isWhite);

            // Expected points loss:
            // If the player played the engine's top choice, any small difference in eval is horizon noise
            double expectedLoss;
            if (isEngineTopMove)
            {
                expectedLoss = 0.0;
            }
            else
            {
                expectedLoss = Math.Max(0.0, expectedBefore - expectedAfter);
            }

            // Forced mate adjustments
            bool hadForcedMate = IsWinningForcedMate(priorEval, isWhite);
            bool hasForcedMate = IsWinningForcedMate(move.Evaluation, isWhite);
            bool opponentHasForcedMate = IsWinningForcedMate(move.Evaluation, !isWhite);
            bool opponentHadForcedMate = IsWinningForcedMate(priorEval, !isWhite);

            // Slower forced mate (e.g. mate in 4 instead of mate in 2):
            // The player still has a forced checkmate, so loss is negligible
            if (hadForcedMate && hasForcedMate)
            {
                expectedLoss = Math.Min(expectedLoss, 0.005);
            }

            // Completely winning position maintained (e.g. +14 down to +9):
            // Expected points loss is tiny, must never be called Mistake or Blunder
            if (expectedBefore >= 0.95 && expectedAfter >= 0.90)
            {
                expectedLoss = Math.Min(expectedLoss, 0.02);
            }

            // Opening Book Detection
            bool isBookMove = ChessOpeningBook.IsBookMove(progressiveSans) && expectedLoss <= 0.03;

            // Brilliant Piece Sacrifice Detection
            var priorMove = i > 0 ? moves[i - 1] : null;
            var nextMove = i + 1 < moves.Count ? moves[i + 1] : null;
            bool isBrilliant = !isBookMove && ChessSacrificeDetector.IsBrilliantPieceSacrifice(
                move,
                priorMove,
                nextMove,
                isEngineTopMove,
                expectedLoss,
                expectedBefore,
                expectedAfter);

            // Classify Move
            var (classification, symbol, explanation) = ClassifyMove(
                move,
                priorBestSan,
                priorBestUci,
                isEngineTopMove,
                isBookMove,
                isBrilliant,
                expectedBefore,
                expectedAfter,
                expectedLoss,
                hadForcedMate,
                hasForcedMate,
                opponentHasForcedMate,
                opponentHadForcedMate,
                review.OpeningName);

            // CAPS2 Accuracy Formula: 103.1668 * exp(-0.04354 * winLossPct) - 3.1669
            double winLossPct = expectedLoss * 100.0;
            double moveAccuracy = isBookMove
                ? 100.0
                : Math.Clamp(103.1668 * Math.Exp(-0.04354 * winLossPct) - 3.1669, 0.0, 100.0);

            move.ExpectedPointsBefore = Math.Round(expectedBefore, 3);
            move.ExpectedPointsAfter = Math.Round(expectedAfter, 3);
            move.ExpectedPointsLoss = Math.Round(expectedLoss, 3);
            move.WinPercentBefore = Math.Round(expectedBefore * 100.0, 1);
            move.WinPercentAfter = Math.Round(expectedAfter * 100.0, 1);
            move.WinPercentLoss = Math.Round(winLossPct, 1);
            move.AccuracyScore = Math.Round(moveAccuracy, 1);
            move.Classification = classification;
            move.ClassificationSymbol = symbol;
            move.Explanation = explanation;

            if (isWhite)
            {
                whiteAccuracies.Add(moveAccuracy);
            }
            else
            {
                blackAccuracies.Add(moveAccuracy);
            }

            // Increment count in review
            var targetCounts = isWhite ? review.WhiteClassifications : review.BlackClassifications;
            IncrementClassification(targetCounts, classification);
        }

        // 3. Overall Accuracies
        review.WhiteAccuracy = CalculatePlayerAccuracy(moves, "White");
        review.BlackAccuracy = CalculatePlayerAccuracy(moves, "Black");

        // 4. Summary Verdict
        review.SummaryVerdict = GenerateSummaryVerdict(review, whiteName, blackName, moves);

        return review;
    }

    private static (string classification, string symbol, string explanation) ClassifyMove(
        MoveAnalysisDto move,
        string priorBestSan,
        string priorBestUci,
        bool isEngineTopMove,
        bool isBookMove,
        bool isBrilliant,
        double expectedBefore,
        double expectedAfter,
        double expectedLoss,
        bool hadForcedMate,
        bool hasForcedMate,
        bool opponentHasForcedMate,
        bool opponentHadForcedMate,
        string openingName)
    {
        string bestMoveDisplay = !string.IsNullOrWhiteSpace(priorBestSan)
            ? priorBestSan
            : (!string.IsNullOrWhiteSpace(priorBestUci) ? priorBestUci : "the top engine move");

        // 1. Book Move: Recognized opening theory with negligible loss
        if (isBookMove)
        {
            return ("Book", "📖", $"Book move. {move.San} is established opening theory ({openingName}).");
        }

        // 2. Brilliant (!!): Spectacular piece sacrifice maintaining decisive advantage
        if (isBrilliant)
        {
            return ("Brilliant", "!!", $"Brilliant! {move.San} is a spectacular sacrifice that maintains a decisive advantage.");
        }

        // 3. Great Move (!): Crucial resource swinging the game in player's favor
        // (Only winning move or only saving resource)
        bool isGreat = (isEngineTopMove || expectedLoss <= 0.005) &&
                       ((expectedBefore >= 0.35 && expectedBefore <= 0.60 && expectedAfter >= 0.68) ||
                        (expectedBefore <= 0.35 && expectedAfter >= 0.50));

        if (isGreat)
        {
            return ("Great", "!", $"Great find! {move.San} is a critical resource that turns the game in your favor.");
        }

        // 4. Best Move (★)
        if (isEngineTopMove || expectedLoss <= 0.005)
        {
            return ("Best", "★", $"Best move. {move.San} was the engine's top recommendation.");
        }

        // 5. Excellent (✓): very small expected points loss (<= 0.02)
        if (expectedLoss <= 0.02)
        {
            if (hadForcedMate && hasForcedMate)
            {
                return ("Excellent", "✓", $"Excellent move. {move.San} maintains a forced checkmate.");
            }
            return ("Excellent", "✓", $"Excellent move. {move.San} maintains a very strong position.");
        }

        // 6. Good (👍): minor expected points loss (<= 0.05)
        if (expectedLoss <= 0.05)
        {
            if (hadForcedMate && hasForcedMate)
            {
                return ("Good", "👍", $"Good move. {move.San} maintains a forced checkmate.");
            }
            return ("Good", "👍", $"Good move. {move.San} is a sound choice, though {bestMoveDisplay} was slightly sharper.");
        }

        // 7. Miss (✕): Missed tactical win or squandered clear winning advantage
        // Player was clearly winning (>= 0.68) and dropped to equal or worse (<= 0.52)
        if (expectedBefore >= 0.68 && expectedAfter <= 0.52 && expectedLoss >= 0.15)
        {
            return ("Miss", "✕", $"Missed win. {move.San} squanders a winning tactical advantage. Best was {bestMoveDisplay}.");
        }

        // 8. Inaccuracy (?!): noticeable loss (<= 0.10)
        if (expectedLoss <= 0.10)
        {
            return ("Inaccuracy", "?!", $"Inaccuracy. {move.San} relinquishes some advantage. Better was {bestMoveDisplay}.");
        }

        // 9. Mistake (?): significant loss (<= 0.20)
        if (expectedLoss <= 0.20 && !(opponentHasForcedMate && !opponentHadForcedMate))
        {
            return ("Mistake", "?", $"Mistake. {move.San} turns a favorable position into an uphill battle. Best was {bestMoveDisplay}.");
        }

        // 10. Blunder (??): severe loss (> 0.20) or blundering into forced mate
        if (opponentHasForcedMate && !opponentHadForcedMate)
        {
            return ("Blunder", "??", $"Blunder! {move.San} allows forced checkmate. Best was {bestMoveDisplay}.");
        }

        return ("Blunder", "??", $"Blunder! {move.San} is a critical error that severely damages your position. Best was {bestMoveDisplay}.");
    }

    public static double EvaluationToExpectedPoints(EvaluationDto eval, bool isWhite)
    {
        if (eval.Type == "mate" || eval.MateInMoves.HasValue)
        {
            int mate = eval.MateInMoves ?? (eval.ScoreText.Contains("+M") ? 1 : (eval.ScoreText.Contains("-M") ? -1 : 0));
            // Invert mate for Black so positive is always winning for moving player
            int movingPlayerMate = isWhite ? mate : -mate;

            if (movingPlayerMate > 0)
            {
                // Moving player has forced mate in N moves (e.g. M1 -> 0.999, M2 -> 0.998)
                return Math.Clamp(1.0 - Math.Min(movingPlayerMate, 50) * 0.001, 0.95, 1.0);
            }
            else if (movingPlayerMate < 0)
            {
                // Moving player is being mated in N moves (e.g. -M1 -> 0.001, -M2 -> 0.002)
                int absMate = Math.Abs(movingPlayerMate);
                return Math.Clamp(0.0 + Math.Min(absMate, 50) * 0.001, 0.0, 0.05);
            }
            else
            {
                // Mate on board (# 1-0 or # 0-1)
                bool whiteWins = eval.ScoreText.Contains("1-0");
                bool movingWon = isWhite ? whiteWins : !whiteWins;
                return movingWon ? 1.0 : 0.0;
            }
        }

        double cpWhite = eval.Centipawns ?? (int)Math.Round((eval.Value ?? 0.0) * 100.0);
        double cpMoving = isWhite ? cpWhite : -cpWhite;

        return CentipawnsToExpectedPoints(cpMoving);
    }

    public static double CentipawnsToExpectedPoints(double cp)
    {
        // CAPS2 sigmoid model: Expected Points in [0.0, 1.0]
        // E(cp) = 1.0 / (1.0 + exp(-0.00368208 * cp))
        double exponent = -0.00368208 * cp;
        if (exponent > 50.0) exponent = 50.0;
        if (exponent < -50.0) exponent = -50.0;

        double ep = 1.0 / (1.0 + Math.Exp(exponent));
        return Math.Clamp(ep, 0.0, 1.0);
    }

    public static double CentipawnsToWinPercent(double cp)
    {
        return CentipawnsToExpectedPoints(cp) * 100.0;
    }

    public static double GetCentipawnsForMovingPlayer(EvaluationDto eval, bool isWhite)
    {
        double cpWhite;
        if (eval.Type == "mate" || eval.MateInMoves.HasValue)
        {
            int mate = eval.MateInMoves ?? (eval.ScoreText.Contains("+M") ? 1 : (eval.ScoreText.Contains("-M") ? -1 : 0));
            if (mate > 0)
            {
                cpWhite = 10000.0 - mate * 100.0;
            }
            else if (mate < 0)
            {
                cpWhite = -10000.0 - mate * 100.0;
            }
            else
            {
                cpWhite = eval.ScoreText.Contains("1-0") ? 10000.0 : -10000.0;
            }
        }
        else
        {
            cpWhite = eval.Centipawns ?? (int)Math.Round((eval.Value ?? 0.0) * 100.0);
        }

        return isWhite ? cpWhite : -cpWhite;
    }

    public static bool IsWinningForcedMate(EvaluationDto eval, bool forWhite)
    {
        if (eval.Type != "mate" && !eval.MateInMoves.HasValue) return false;
        int mate = eval.MateInMoves ?? (eval.ScoreText.Contains("+M") ? 1 : (eval.ScoreText.Contains("-M") ? -1 : 0));
        return forWhite ? mate > 0 : mate < 0;
    }

    private static double CalculatePlayerAccuracy(List<MoveAnalysisDto> moves, string color)
    {
        var playerMoves = moves.Where(m => m.Color.Equals(color, StringComparison.OrdinalIgnoreCase)).ToList();
        if (playerMoves.Count == 0) return 100.0;

        var nonBookMoves = playerMoves.Where(m => m.Classification != "Book").ToList();
        if (nonBookMoves.Count == 0)
        {
            return 100.0;
        }

        // Weighted accuracy: non-book moves count with full weight, book moves count with 0.3 weight
        // so book moves don't artificially mask bad play, but reward solid opening knowledge
        double weightedSum = 0.0;
        double totalWeight = 0.0;

        foreach (var m in playerMoves)
        {
            double weight = m.Classification == "Book" ? 0.35 : 1.0;
            weightedSum += m.AccuracyScore * weight;
            totalWeight += weight;
        }

        double accuracy = totalWeight > 0 ? (weightedSum / totalWeight) : 100.0;
        return Math.Clamp(Math.Round(accuracy, 1), 0.0, 100.0);
    }

    private static void IncrementClassification(MoveClassificationCountsDto counts, string classification)
    {
        switch (classification)
        {
            case "Brilliant": counts.Brilliant++; break;
            case "Great": counts.Great++; break;
            case "Best": counts.Best++; break;
            case "Excellent": counts.Excellent++; break;
            case "Good": counts.Good++; break;
            case "Book": counts.Book++; break;
            case "Inaccuracy": counts.Inaccuracy++; break;
            case "Mistake": counts.Mistake++; break;
            case "Miss": counts.Miss++; break;
            case "Blunder": counts.Blunder++; break;
            default: counts.Good++; break;
        }
    }

    private static (string white, string black, string opening, string eco) ParsePgnHeaders(string? pgn)
    {
        if (string.IsNullOrWhiteSpace(pgn))
            return ("White", "Black", "", "");

        string white = "White";
        string black = "Black";
        string opening = "";
        string eco = "";

        var wMatch = Regex.Match(pgn, @"\[White\s+""([^""]+)""\]", RegexOptions.IgnoreCase);
        if (wMatch.Success) white = wMatch.Groups[1].Value.Trim();

        var bMatch = Regex.Match(pgn, @"\[Black\s+""([^""]+)""\]", RegexOptions.IgnoreCase);
        if (bMatch.Success) black = bMatch.Groups[1].Value.Trim();

        var opMatch = Regex.Match(pgn, @"\[Opening\s+""([^""]+)""\]", RegexOptions.IgnoreCase);
        if (opMatch.Success) opening = opMatch.Groups[1].Value.Trim();

        var ecoMatch = Regex.Match(pgn, @"\[ECO\s+""([^""]+)""\]", RegexOptions.IgnoreCase);
        if (ecoMatch.Success) eco = ecoMatch.Groups[1].Value.Trim();

        return (white, black, opening, eco);
    }

    private static string FormatUciToSanFallback(string uci, string fenBefore)
    {
        if (string.IsNullOrWhiteSpace(uci) || uci.Length < 4) return uci;
        string from = uci.Substring(0, 2);
        string to = uci.Substring(2, 2);

        // Castling
        if ((from == "e1" && to == "g1") || (from == "e8" && to == "g8")) return "O-O";
        if ((from == "e1" && to == "c1") || (from == "e8" && to == "c8")) return "O-O-O";

        char? pieceChar = ChessSacrificeDetector.GetPieceAtSquare(fenBefore, from);
        if (pieceChar == null) return uci;

        char upperPiece = char.ToUpperInvariant(pieceChar.Value);
        if (upperPiece == 'P')
        {
            if (from[0] != to[0])
            {
                return $"{from[0]}x{to}";
            }
            return to;
        }

        char? captured = ChessSacrificeDetector.GetPieceAtSquare(fenBefore, to);
        bool isCapture = captured != null;

        return $"{upperPiece}{(isCapture ? "x" : "")}{to}";
    }

    private static string GenerateSummaryVerdict(
        GameReviewDto review,
        string whiteName,
        string blackName,
        List<MoveAnalysisDto> moves)
    {
        double diff = review.WhiteAccuracy - review.BlackAccuracy;
        string leader = diff > 3.0 ? whiteName : (diff < -3.0 ? blackName : "Both players");

        if (Math.Abs(diff) <= 3.0)
        {
            if (review.WhiteAccuracy >= 88.0 && review.BlackAccuracy >= 88.0)
            {
                return $"A masterclass performance! Both {whiteName} ({review.WhiteAccuracy}%) and {blackName} ({review.BlackAccuracy}%) played with extraordinary precision.";
            }
            return $"A fiercely contested battle! Both {whiteName} and {blackName} traded blows with closely matched accuracy ({review.WhiteAccuracy}% vs {review.BlackAccuracy}%).";
        }

        if (diff > 0)
        {
            int blunders = review.BlackClassifications.Blunder;
            if (blunders > 0)
            {
                return $"{whiteName} capitalized on key tactical slips, securing control while {blackName} faced mounting pressure.";
            }
            return $"{whiteName} displayed superior strategic precision ({review.WhiteAccuracy}%), consistently finding top engine moves.";
        }
        else
        {
            int blunders = review.WhiteClassifications.Blunder;
            if (blunders > 0)
            {
                return $"{blackName} seized decisive counter-attacking chances, punishing tactical oversights to clinch the advantage.";
            }
            return $"{blackName} demonstrated outstanding accuracy ({review.BlackAccuracy}%), outplaying {whiteName} across the board.";
        }
    }
}
