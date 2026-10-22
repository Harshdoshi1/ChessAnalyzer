using ChessAnalyzer.Infrastructure.Chess.Stockfish;
using Xunit;

namespace ChessAnalyzer.UnitTests;

public class ScoreNormalizationTests
{
    [Fact]
    public void ParseEvaluation_WhiteToMove_PositiveCp_ReturnsPositiveScore()
    {
        var line = "info depth 10 seldepth 12 multipv 1 score cp 45 nodes 1000 nps 500000 time 2 pv e2e4";
        var eval = StockfishEvaluationHelper.ParseEvaluation(line, "White", 10);

        Assert.Equal("cp", eval.Type);
        Assert.Equal(45, eval.Centipawns);
        Assert.Equal(0.45, eval.Value);
        Assert.Equal("+0.45", eval.ScoreText);
        Assert.False(eval.IsMate);
        Assert.Equal(10, eval.Depth);
    }

    [Fact]
    public void ParseEvaluation_BlackToMove_InvertsCpRelativeToWhite()
    {
        // When Black is to move and Stockfish reports cp 45 for Black, White is at -45 (-0.45)
        var line = "info depth 10 score cp 45 pv e7e5";
        var eval = StockfishEvaluationHelper.ParseEvaluation(line, "Black", 10);

        Assert.Equal("cp", eval.Type);
        Assert.Equal(-45, eval.Centipawns);
        Assert.Equal(-0.45, eval.Value);
        Assert.Equal("-0.45", eval.ScoreText);
        Assert.False(eval.IsMate);
    }

    [Fact]
    public void ParseEvaluation_BlackToMove_NegativeCp_InvertsToPositiveWhiteAdvantage()
    {
        // When Black is to move and Stockfish reports cp -150 (Black is down 1.5 pawns), White is +1.50
        var line = "info depth 12 score cp -150 pv d7d5";
        var eval = StockfishEvaluationHelper.ParseEvaluation(line, "Black", 12);

        Assert.Equal("cp", eval.Type);
        Assert.Equal(150, eval.Centipawns);
        Assert.Equal(1.50, eval.Value);
        Assert.Equal("+1.50", eval.ScoreText);
    }

    [Fact]
    public void ParseEvaluation_ZeroCp_ReturnsZeroString()
    {
        var line = "info depth 10 score cp 0 pv e2e4";
        var eval = StockfishEvaluationHelper.ParseEvaluation(line, "White", 10);

        Assert.Equal(0, eval.Centipawns);
        Assert.Equal(0.0, eval.Value);
        Assert.Equal("0.00", eval.ScoreText);
    }

    [Fact]
    public void ParseEvaluation_WhiteToMove_MateInTwo_ReturnsPositiveMate()
    {
        var line = "info depth 8 score mate 2 pv d1h5 g7g6 h5g6";
        var eval = StockfishEvaluationHelper.ParseEvaluation(line, "White", 8);

        Assert.Equal("mate", eval.Type);
        Assert.Equal(2, eval.MateInMoves);
        Assert.Null(eval.Centipawns);
        Assert.Equal("+M2", eval.ScoreText);
        Assert.True(eval.IsMate);
    }

    [Fact]
    public void ParseEvaluation_BlackToMove_MateInOne_InvertsToNegativeMateForWhite()
    {
        // Black can mate in 1 -> from White perspective: -M1
        var line = "info depth 6 score mate 1 pv d8h4";
        var eval = StockfishEvaluationHelper.ParseEvaluation(line, "Black", 6);

        Assert.Equal("mate", eval.Type);
        Assert.Equal(-1, eval.MateInMoves);
        Assert.Equal("-M1", eval.ScoreText);
        Assert.True(eval.IsMate);
    }

    [Fact]
    public void ParseEvaluation_BlackToMove_GettingMated_InvertsToPositiveMateForWhite()
    {
        // Black is getting mated in 1 (-1) -> from White perspective: +M1
        var line = "info depth 6 score mate -1 pv g8f6";
        var eval = StockfishEvaluationHelper.ParseEvaluation(line, "Black", 6);

        Assert.Equal("mate", eval.Type);
        Assert.Equal(1, eval.MateInMoves);
        Assert.Equal("+M1", eval.ScoreText);
        Assert.True(eval.IsMate);
    }

    [Fact]
    public void ParseBestMove_WithPonder_ExtractsFirstUciMove()
    {
        var line = "bestmove e2e4 ponder e7e5";
        var bestMove = StockfishEvaluationHelper.ParseBestMove(line);

        Assert.Equal("e2e4", bestMove);
    }

    [Fact]
    public void ParseBestMove_WithNone_ReturnsEmpty()
    {
        var line = "bestmove (none)";
        var bestMove = StockfishEvaluationHelper.ParseBestMove(line);

        Assert.Equal(string.Empty, bestMove);
    }

    [Fact]
    public void ParsePrincipalVariation_ExtractsFullPvLine()
    {
        var line = "info depth 10 score cp 20 time 5 pv e2e4 e7e5 g1f3 b8c6";
        var pv = StockfishEvaluationHelper.ParsePrincipalVariation(line);

        Assert.Equal("e2e4 e7e5 g1f3 b8c6", pv);
    }
}
