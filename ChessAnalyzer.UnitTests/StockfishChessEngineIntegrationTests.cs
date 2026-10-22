using ChessAnalyzer.Infrastructure.Chess;
using ChessAnalyzer.Infrastructure.Chess.Stockfish;
using Microsoft.Extensions.Options;
using Xunit;

namespace ChessAnalyzer.UnitTests;

public class StockfishChessEngineIntegrationTests
{
    private readonly StockfishChessEngine _engine;

    public StockfishChessEngineIntegrationTests()
    {
        var options = Options.Create(new StockfishOptions
        {
            ExecutablePath = "Chess/Stockfish/stockfish-windows-x86-64-universal.exe",
            DefaultDepth = 8,
            TimeoutSeconds = 20
        });

        _engine = new StockfishChessEngine(options, new PgnParser());
    }

    [Fact]
    public async Task AnalyzeGameAsync_FourMoves_ReturnsAllEvaluationsAndBestMoves()
    {
        var pgn = "1. e4 e5 2. Nf3 Nc6";
        var result = await _engine.AnalyzeGameAsync(pgn);

        Assert.NotNull(result);
        Assert.Equal(4, result.TotalMoves);
        Assert.Equal(4, result.Moves.Count);

        // Initial evaluation
        Assert.NotNull(result.InitialEvaluation);
        Assert.False(string.IsNullOrWhiteSpace(result.InitialEvaluation.ScoreText));

        // Move 1: 1. e4
        var move1 = result.Moves[0];
        Assert.Equal(1, move1.Ply);
        Assert.Equal(1, move1.MoveNumber);
        Assert.Equal("White", move1.Color);
        Assert.Equal("e4", move1.San);
        Assert.Equal("e2e4", move1.Uci);
        Assert.Equal("Black", move1.SideToMove);
        Assert.NotNull(move1.Evaluation);
        Assert.False(string.IsNullOrWhiteSpace(move1.Evaluation.ScoreText));
        Assert.False(string.IsNullOrWhiteSpace(move1.BestMoveUci));

        // Move 2: 1... e5
        var move2 = result.Moves[1];
        Assert.Equal(2, move2.Ply);
        Assert.Equal("Black", move2.Color);
        Assert.Equal("e5", move2.San);
        Assert.Equal("e7e5", move2.Uci);
        Assert.Equal("White", move2.SideToMove);
        Assert.NotNull(move2.Evaluation);

        // Move 3: 2. Nf3
        var move3 = result.Moves[2];
        Assert.Equal(3, move3.Ply);
        Assert.Equal("White", move3.Color);
        Assert.Equal("Nf3", move3.San);

        // Move 4: 2... Nc6
        var move4 = result.Moves[3];
        Assert.Equal(4, move4.Ply);
        Assert.Equal("Black", move4.Color);
        Assert.Equal("Nc6", move4.San);

        // Final evaluation matches last move evaluation
        Assert.NotNull(result.FinalEvaluation);
        Assert.Equal(move4.Evaluation.ScoreText, result.FinalEvaluation.ScoreText);

        // Backward compatibility Message string
        Assert.Contains("Stockfish analysis completed", result.Message);
        Assert.Contains("Moves analyzed: 4", result.Message);
    }

    [Fact]
    public async Task AnalyzeGameAsync_FoolsMate_HandlesCheckmateCorrectly()
    {
        // 1. f3 e5 2. g4 Qh4#
        var pgn = "1. f3 e5 2. g4 Qh4#";
        var result = await _engine.AnalyzeGameAsync(pgn);

        Assert.Equal(4, result.Moves.Count);
        var finalMove = result.Moves[3];
        Assert.Equal("Qh4#", finalMove.San);
        Assert.Equal("mate", finalMove.Evaluation.Type);
        Assert.True(finalMove.Evaluation.IsMate);
        Assert.Contains("#", finalMove.Evaluation.ScoreText);
    }

    [Fact]
    public async Task AnalyzeGameAsync_UserGame_CompletesSuccessfully()
    {
        var pgn = @"[Event ""Live Chess""]
[Site ""Chess.com""]
[Date ""2026.10.15""]
[Round ""?""]
[White ""kiran1682""]
[Black ""Harsh_Spcy""]
[Result ""0-1""]
[TimeControl ""600""]
[WhiteElo ""922""]
[BlackElo ""934""]
[Termination ""Harsh_Spcy won - game abandoned""]
[ECO ""A00""]
[EndTime ""11:24:18 GMT+0000""]
[Link ""https://www.chess.com/game/live/185363220460?username=harsh_spcy&move=0""]

1. e3 e5 2. b3 Nc6 3. Bb2 Nf6 4. g3 d5 5. Bg2 e4 6. Ne2 Bd6 7. Nd4 Nxd4 8. Bxd4
Bg4 9. f3 exf3 10. Bxf3 Bxf3 11. Qxf3 c5 12. Bxf6 Qxf6 13. Qxf6 gxf6 14. Nc3 Be5
15. O-O O-O 16. d4 cxd4 17. Nxd5 dxe3 18. Rae1 Bd4 19. Ne7+ Kg7 20. Nf5+ Kg6 21.
Nxd4 Rfe8 22. Nb5 e2 23. Rf2 Rac8 24. Nxa7 Rxc2 25. Nb5 Rd2 26. Nc7 Re7 27. Kg2
Rxc7 28. Rfxe2 Rxe2+ 29. Rxe2 h5 30. Re4 f5 31. Re8 Rc2+ 32. Kf3 Rxa2 33. Rg8+
Kf6 34. h3 Rb2 35. Kf4 Rxb3 36. Rg5 Rb4+ 37. Kf3 Kxg5 38. h4+ Kf6 39. g4 fxg4+
40. Kg3 Rc4 41. Kf2 Ke5 42. Ke3 Kd5 43. Kd3 b6 44. Kd2 b5 45. Kd3 b4 46. Kd2 b3
47. Kd3 b2 0-1";

        var options = Options.Create(new StockfishOptions
        {
            ExecutablePath = "Chess/Stockfish/stockfish-windows-x86-64-universal.exe",
            DefaultDepth = 8,
            TimeoutSeconds = 60
        });
        var engine = new StockfishChessEngine(options, new PgnParser());
        var result = await engine.AnalyzeGameAsync(pgn);

        Assert.NotNull(result);
        Assert.Equal(94, result.Moves.Count);
        Assert.NotNull(result.Review);
        Assert.InRange(result.Review.WhiteAccuracy, 0.0, 100.0);
        Assert.InRange(result.Review.BlackAccuracy, 0.0, 100.0);
        Assert.False(string.IsNullOrWhiteSpace(result.Review.OpeningName));
        Assert.All(result.Moves, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Classification));
            Assert.False(string.IsNullOrWhiteSpace(m.ClassificationSymbol));
            Assert.False(string.IsNullOrWhiteSpace(m.Explanation));
            Assert.InRange(m.ExpectedPointsBefore, 0.0, 1.0);
            Assert.InRange(m.ExpectedPointsAfter, 0.0, 1.0);
            Assert.InRange(m.AccuracyScore, 0.0, 100.0);
        });
    }
}
