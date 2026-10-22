using ChessAnalyzer.Application.DTOs;
using ChessAnalyzer.Application.Services;
using ChessAnalyzer.Infrastructure.Chess;
using Xunit;

namespace ChessAnalyzer.UnitTests;

public class ChessGameReviewAnalyzerTests
{
    // ==========================================
    // 1. Expected Points & Win Percent Tests
    // ==========================================

    [Fact]
    public void CentipawnsToExpectedPoints_ZeroCentipawns_ReturnsHalf()
    {
        double ep = ChessGameReviewAnalyzer.CentipawnsToExpectedPoints(0);
        Assert.Equal(0.50, ep, 2);
    }

    [Fact]
    public void CentipawnsToExpectedPoints_LargeAdvantage_ReturnsNearOne()
    {
        double ep = ChessGameReviewAnalyzer.CentipawnsToExpectedPoints(1500); // +15 pawns
        Assert.True(ep > 0.99);
    }

    [Fact]
    public void CentipawnsToExpectedPoints_LargeDisadvantage_ReturnsNearZero()
    {
        double ep = ChessGameReviewAnalyzer.CentipawnsToExpectedPoints(-1500); // -15 pawns
        Assert.True(ep < 0.01);
    }

    // ==========================================
    // 2. White vs Black Perspective Tests
    // ==========================================

    [Fact]
    public void EvaluationToExpectedPoints_NormalizesPerspectiveCorrectly()
    {
        // An evaluation of +200 cp (from White's perspective)
        var eval = new EvaluationDto { Centipawns = 200, ScoreText = "+2.00" };

        double epWhite = ChessGameReviewAnalyzer.EvaluationToExpectedPoints(eval, isWhite: true);
        double epBlack = ChessGameReviewAnalyzer.EvaluationToExpectedPoints(eval, isWhite: false);

        // White is winning, so White's expected points > 0.5 and Black's expected points < 0.5
        Assert.True(epWhite > 0.65);
        Assert.True(epBlack < 0.35);
        Assert.Equal(1.0, epWhite + epBlack, 2);
    }

    [Fact]
    public void EvaluationToExpectedPoints_MateScore_NormalizesCorrectly()
    {
        // White has mate in 1 (+M1 from White's perspective)
        var mateEval = new EvaluationDto { Type = "mate", MateInMoves = 1, ScoreText = "+M1" };

        double epWhite = ChessGameReviewAnalyzer.EvaluationToExpectedPoints(mateEval, isWhite: true);
        double epBlack = ChessGameReviewAnalyzer.EvaluationToExpectedPoints(mateEval, isWhite: false);

        Assert.True(epWhite >= 0.99);
        Assert.True(epBlack <= 0.01);

        // Black has mate in 1 (-M1 from White's perspective)
        var blackMateEval = new EvaluationDto { Type = "mate", MateInMoves = -1, ScoreText = "-M1" };

        double epWhite2 = ChessGameReviewAnalyzer.EvaluationToExpectedPoints(blackMateEval, isWhite: true);
        double epBlack2 = ChessGameReviewAnalyzer.EvaluationToExpectedPoints(blackMateEval, isWhite: false);

        Assert.True(epWhite2 <= 0.01);
        Assert.True(epBlack2 >= 0.99);
    }

    // ==========================================
    // 3. Move Classifications Tests
    // ==========================================

    [Fact]
    public void AnalyzeGameReview_DetectsBookMoves()
    {
        var initialEval = new EvaluationDto
        {
            Centipawns = 15,
            ScoreText = "+0.15",
            BestMoveUci = "e2e4"
        };

        var moves = new List<MoveAnalysisDto>
        {
            new()
            {
                Ply = 1,
                MoveNumber = 1,
                Color = "White",
                San = "e4",
                Uci = "e2e4",
                Evaluation = new EvaluationDto { Centipawns = 20, ScoreText = "+0.20", BestMoveUci = "e7e5" },
                BestMoveUci = "e7e5"
            },
            new()
            {
                Ply = 2,
                MoveNumber = 1,
                Color = "Black",
                San = "e5",
                Uci = "e7e5",
                Evaluation = new EvaluationDto { Centipawns = 25, ScoreText = "+0.25", BestMoveUci = "g1f3" },
                BestMoveUci = "g1f3"
            }
        };

        var review = ChessGameReviewAnalyzer.AnalyzeGameReview(moves, initialEval, "1. e4 e5");

        Assert.NotNull(review);
        Assert.Equal("Book", moves[0].Classification);
        Assert.Equal("Book", moves[1].Classification);
        Assert.Equal("📖", moves[0].ClassificationSymbol);
        Assert.Equal(1, review.WhiteClassifications.Book);
        Assert.Equal(1, review.BlackClassifications.Book);
        Assert.Equal(100.0, review.WhiteAccuracy);
        Assert.Equal(100.0, review.BlackAccuracy);
    }

    [Fact]
    public void AnalyzeGameReview_TopEngineMove_ClassifiedAsBest()
    {
        var initialEval = new EvaluationDto
        {
            Centipawns = 40,
            ScoreText = "+0.40",
            BestMoveUci = "g1f3"
        };

        var moves = new List<MoveAnalysisDto>
        {
            new()
            {
                Ply = 1,
                MoveNumber = 1,
                Color = "White",
                San = "Nf3",
                Uci = "g1f3",
                Evaluation = new EvaluationDto { Centipawns = 40, ScoreText = "+0.40", BestMoveUci = "d7d5" },
                BestMoveUci = "d7d5"
            }
        };

        var review = ChessGameReviewAnalyzer.AnalyzeGameReview(moves, initialEval);

        // Even though Nf3 is also in opening book, if it matches best move and is book it's Book, but if out of book it's Best
        Assert.True(moves[0].Classification == "Best" || moves[0].Classification == "Book");
        Assert.True(moves[0].AccuracyScore >= 99.0);
    }

    [Fact]
    public void AnalyzeGameReview_CatastrophicDrop_ClassifiedAsBlunder()
    {
        var initialEval = new EvaluationDto
        {
            Centipawns = 50,
            ScoreText = "+0.50",
            BestMoveUci = "d2d4"
        };

        var moves = new List<MoveAnalysisDto>
        {
            new()
            {
                Ply = 1,
                MoveNumber = 1,
                Color = "White",
                San = "f3",
                Uci = "f2f3",
                // Evaluation drops from +0.50 to -4.00 (-400 cp) for White
                Evaluation = new EvaluationDto { Centipawns = -400, ScoreText = "-4.00", BestMoveUci = "e7e5" },
                BestMoveUci = "e7e5"
            }
        };

        var review = ChessGameReviewAnalyzer.AnalyzeGameReview(moves, initialEval);

        Assert.Equal("Blunder", moves[0].Classification);
        Assert.Equal("??", moves[0].ClassificationSymbol);
        Assert.Equal(1, review.WhiteClassifications.Blunder);
        Assert.True(moves[0].AccuracyScore < 30.0);
        Assert.True(review.WhiteAccuracy < 30.0);
    }

    [Fact]
    public void AnalyzeGameReview_MissedWin_ClassifiedAsMiss()
    {
        // White was winning comfortably (+3.50, ep ~0.78), but played a move where evaluation drops to 0.00 (ep 0.50)
        var initialEval = new EvaluationDto
        {
            Centipawns = 350,
            ScoreText = "+3.50",
            BestMoveUci = "d2d4"
        };

        var moves = new List<MoveAnalysisDto>
        {
            new()
            {
                Ply = 1,
                MoveNumber = 1,
                Color = "White",
                San = "a3",
                Uci = "a2a3",
                Evaluation = new EvaluationDto { Centipawns = 0, ScoreText = "0.00", BestMoveUci = "e7e5" },
                BestMoveUci = "e7e5"
            }
        };

        var review = ChessGameReviewAnalyzer.AnalyzeGameReview(moves, initialEval);

        Assert.Equal("Miss", moves[0].Classification);
        Assert.Equal("✕", moves[0].ClassificationSymbol);
        Assert.Equal(1, review.WhiteClassifications.Miss);
    }

    [Fact]
    public void AnalyzeGameReview_BrilliantSacrifice_DetectedCorrectly()
    {
        // Bishop takes h7+ sacrifice (Bxh7+), sacrificing bishop for pawn (+100 cp -> +350 cp winning)
        var initialEval = new EvaluationDto
        {
            Centipawns = 150,
            ScoreText = "+1.50",
            BestMoveUci = "d3h7"
        };

        var moves = new List<MoveAnalysisDto>
        {
            new()
            {
                Ply = 1,
                MoveNumber = 1,
                Color = "White",
                San = "Bxh7+",
                Uci = "d3h7",
                FenBefore = "r1bqk2r/pp1n1ppp/2p1pn2/3p4/2PP4/2NBPN2/PP3PPP/R1BQK2R w KQkq - 0 1",
                FenAfter = "r1bqk2r/pp1n1ppB/2p1pn2/3p4/2PP4/2N1PN2/PP3PPP/R1BQK2R b KQkq - 0 1",
                Evaluation = new EvaluationDto { Centipawns = 350, ScoreText = "+3.50", BestMoveUci = "f6h7" },
                BestMoveUci = "f6h7"
            },
            new()
            {
                Ply = 2,
                MoveNumber = 1,
                Color = "Black",
                San = "Nxh7",
                Uci = "f6h7",
                FenBefore = "r1bqk2r/pp1n1ppB/2p1pn2/3p4/2PP4/2N1PN2/PP3PPP/R1BQK2R b KQkq - 0 1",
                FenAfter = "r1bqk2r/pp1n1ppn/2p1p3/3p4/2PP4/2N1PN2/PP3PPP/R1BQK2R w KQkq - 0 2",
                Evaluation = new EvaluationDto { Centipawns = 350, ScoreText = "+3.50", BestMoveUci = "e2e4" },
                BestMoveUci = "e2e4"
            }
        };

        var review = ChessGameReviewAnalyzer.AnalyzeGameReview(moves, initialEval);

        Assert.Equal("Brilliant", moves[0].Classification);
        Assert.Equal("!!", moves[0].ClassificationSymbol);
        Assert.Equal(1, review.WhiteClassifications.Brilliant);
    }

    [Fact]
    public void AnalyzeGameReview_RecaptureIsNotBrilliant()
    {
        // Opponent plays Bxf6, then White plays Qxf6 (recapture). This must NOT be Brilliant!
        var initialEval = new EvaluationDto { Centipawns = 0, ScoreText = "0.00", BestMoveUci = "c1g5" };

        var moves = new List<MoveAnalysisDto>
        {
            new()
            {
                Ply = 1,
                MoveNumber = 1,
                Color = "Black",
                San = "Bxf6",
                Uci = "g4f6",
                FenBefore = "r2qkb1r/ppp1pppp/2n2n2/8/2B3b1/2N2N2/PPPP1PPP/R1BQK2R b KQkq - 0 1",
                FenAfter = "r2qkb1r/ppp1pppp/2n2b2/8/2B5/2N2N2/PPPP1PPP/R1BQK2R w KQkq - 0 2",
                Evaluation = new EvaluationDto { Centipawns = 0, ScoreText = "0.00", BestMoveUci = "d1f6" },
                BestMoveUci = "d1f6"
            },
            new()
            {
                Ply = 2,
                MoveNumber = 1,
                Color = "White",
                San = "Qxf6",
                Uci = "d1f6",
                FenBefore = "r2qkb1r/ppp1pppp/2n2b2/8/2B5/2N2N2/PPPP1PPP/R1BQK2R w KQkq - 0 2",
                FenAfter = "r2qkb1r/ppp1pppp/2n2Q2/8/2B5/2N2N2/PPPP1PPP/R1B1K2R b KQkq - 0 2",
                Evaluation = new EvaluationDto { Centipawns = 250, ScoreText = "+2.50", BestMoveUci = "e7f6" },
                BestMoveUci = "e7f6"
            }
        };

        var review = ChessGameReviewAnalyzer.AnalyzeGameReview(moves, initialEval);

        // Move 1 was a recapture of Bishop on f6, so it must NOT be Brilliant!
        Assert.NotEqual("Brilliant", moves[1].Classification);
    }

    [Fact]
    public void AnalyzeGameReview_SlowerForcedMate_NeverClassifiedAsBlunder()
    {
        // White has forced mate in 2 (+M2), but plays a move that leads to forced mate in 4 (+M4)
        var initialEval = new EvaluationDto
        {
            Type = "mate",
            MateInMoves = 2,
            ScoreText = "+M2",
            BestMoveUci = "d1h5"
        };

        var moves = new List<MoveAnalysisDto>
        {
            new()
            {
                Ply = 1,
                MoveNumber = 1,
                Color = "White",
                San = "Bc4",
                Uci = "f1c4",
                Evaluation = new EvaluationDto { Type = "mate", MateInMoves = 4, ScoreText = "+M4", BestMoveUci = "e7e6" },
                BestMoveUci = "e7e6"
            }
        };

        var review = ChessGameReviewAnalyzer.AnalyzeGameReview(moves, initialEval);

        Assert.NotEqual("Blunder", moves[0].Classification);
        Assert.NotEqual("Mistake", moves[0].Classification);
        Assert.NotEqual("Miss", moves[0].Classification);
        Assert.True(moves[0].Classification == "Excellent" || moves[0].Classification == "Good" || moves[0].Classification == "Best");
    }

    [Fact]
    public void AnalyzeGameReview_DecisiveWinningAdvantageMaintained_NeverClassifiedAsBlunder()
    {
        // White is +14.00, plays a move that drops to +9.00
        var initialEval = new EvaluationDto
        {
            Centipawns = 1400,
            ScoreText = "+14.00",
            BestMoveUci = "a1a8"
        };

        var moves = new List<MoveAnalysisDto>
        {
            new()
            {
                Ply = 1,
                MoveNumber = 1,
                Color = "White",
                San = "Qe4",
                Uci = "d4e4",
                Evaluation = new EvaluationDto { Centipawns = 900, ScoreText = "+9.00", BestMoveUci = "c8d7" },
                BestMoveUci = "c8d7"
            }
        };

        var review = ChessGameReviewAnalyzer.AnalyzeGameReview(moves, initialEval);

        Assert.NotEqual("Blunder", moves[0].Classification);
        Assert.NotEqual("Mistake", moves[0].Classification);
        Assert.NotEqual("Miss", moves[0].Classification);
        Assert.True(moves[0].Classification == "Excellent" || moves[0].Classification == "Good");
    }

    // ==========================================
    // 4. SAN Converter Tests
    // ==========================================

    [Fact]
    public void ChessSanConverter_StandardMoves_ConvertsCorrectly()
    {
        string startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        string san1 = ChessSanConverter.ConvertUciToSan(startFen, "e2e4");
        Assert.Equal("e4", san1);

        string san2 = ChessSanConverter.ConvertUciToSan(startFen, "g1f3");
        Assert.Equal("Nf3", san2);
    }

    [Fact]
    public void ChessSanConverter_Castling_ConvertsCorrectly()
    {
        string castleFen = "r1bqk2r/pppp1ppp/2n2n2/2b1p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4";
        string san = ChessSanConverter.ConvertUciToSan(castleFen, "e1g1");
        Assert.Equal("O-O", san);
    }
}
