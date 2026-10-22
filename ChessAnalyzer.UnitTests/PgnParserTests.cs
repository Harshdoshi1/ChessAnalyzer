using ChessAnalyzer.Infrastructure.Chess;
using Xunit;

namespace ChessAnalyzer.UnitTests;

public class PgnParserTests
{
    private readonly PgnParser _parser = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullPgn_ThrowsArgumentException(string? pgn)
    {
        Assert.Throws<ArgumentException>(() => _parser.Validate(pgn!));
    }

    [Fact]
    public void ParseToUciMoves_WithMoveOnlyPgn_ReturnsCorrectUciMoves()
    {
        var pgn = "1. e4 e5 2. Nf3 Nc6";

        var uciMoves = _parser.ParseToUciMoves(pgn);

        Assert.NotNull(uciMoves);
        Assert.Equal(4, uciMoves.Count);
        Assert.Equal("e2e4", uciMoves[0]);
        Assert.Equal("e7e5", uciMoves[1]);
        Assert.Equal("g1f3", uciMoves[2]);
        Assert.Equal("b8c6", uciMoves[3]);
    }

    [Fact]
    public void ParseToUciMoves_WithFullPgnHeaders_ReturnsCorrectMoves()
    {
        var pgn = @"[Event ""Test Event""]
[Site ""Local""]
[Date ""2026.10.15""]
[Round ""1""]
[White ""Player 1""]
[Black ""Player 2""]
[Result ""1-0""]

1. d4 d5 2. c4 e6 1-0";

        var uciMoves = _parser.ParseToUciMoves(pgn);

        Assert.NotNull(uciMoves);
        Assert.Equal(4, uciMoves.Count);
        Assert.Equal("d2d4", uciMoves[0]);
        Assert.Equal("d7d5", uciMoves[1]);
        Assert.Equal("c2c4", uciMoves[2]);
        Assert.Equal("e7e6", uciMoves[3]);
    }

    [Fact]
    public void ParseToUciMoves_WithKingsideCastling_ConvertsToCorrectKingSquares()
    {
        // 1. e4 e5 2. Nf3 Nf6 3. Bc4 Bc5 4. O-O
        var pgn = "1. e4 e5 2. Nf3 Nf6 3. Bc4 Bc5 4. O-O";

        var uciMoves = _parser.ParseToUciMoves(pgn);

        Assert.Equal(7, uciMoves.Count);
        Assert.Equal("e1g1", uciMoves[6]);
    }

    [Fact]
    public void ExtractStartingFen_WithFenHeader_ReturnsExtractedFen()
    {
        var pgn = @"[Event ""Puzzle""]
[FEN ""8/8/8/8/8/8/4K3/4k3 w - - 0 1""]

1. Ke3 Kd1";

        var fen = _parser.ExtractStartingFen(pgn);

        Assert.Equal("8/8/8/8/8/8/4K3/4k3 w - - 0 1", fen);
    }

    [Fact]
    public void ExtractStartingFen_WithoutFenHeader_ReturnsNull()
    {
        var pgn = "1. e4 e5 2. Nf3 Nc6";

        var fen = _parser.ExtractStartingFen(pgn);

        Assert.Null(fen);
    }

    [Fact]
    public void ParseToUciMoves_WithInvalidMoves_ThrowsArgumentException()
    {
        var pgn = "1. z9 z1";

        Assert.Throws<ArgumentException>(() => _parser.ParseToUciMoves(pgn));
    }

    [Fact]
    public void ParseGamePositions_WithFourMoves_ReturnsDetailedPositionSteps()
    {
        var pgn = "1. e4 e5 2. Nf3 Nc6";
        var positions = _parser.ParseGamePositions(pgn);

        Assert.Equal(4, positions.Count);

        // Move 1: 1. e4
        Assert.Equal(1, positions[0].Ply);
        Assert.Equal(1, positions[0].MoveNumber);
        Assert.Equal("White", positions[0].Color);
        Assert.Equal("e4", positions[0].San);
        Assert.Equal("e2e4", positions[0].Uci);
        Assert.Equal("Black", positions[0].SideToMove);
        Assert.StartsWith("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR", positions[0].FenBefore);
        Assert.StartsWith("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR", positions[0].FenAfter);
        Assert.False(positions[0].IsCheck);
        Assert.False(positions[0].IsCheckmate);

        // Move 2: 1... e5
        Assert.Equal(2, positions[1].Ply);
        Assert.Equal(1, positions[1].MoveNumber);
        Assert.Equal("Black", positions[1].Color);
        Assert.Equal("e5", positions[1].San);
        Assert.Equal("e7e5", positions[1].Uci);
        Assert.Equal("White", positions[1].SideToMove);

        // Move 3: 2. Nf3
        Assert.Equal(3, positions[2].Ply);
        Assert.Equal(2, positions[2].MoveNumber);
        Assert.Equal("White", positions[2].Color);
        Assert.Equal("Nf3", positions[2].San);
        Assert.Equal("g1f3", positions[2].Uci);
        Assert.Equal("Black", positions[2].SideToMove);

        // Move 4: 2... Nc6
        Assert.Equal(4, positions[3].Ply);
        Assert.Equal(2, positions[3].MoveNumber);
        Assert.Equal("Black", positions[3].Color);
        Assert.Equal("Nc6", positions[3].San);
        Assert.Equal("b8c6", positions[3].Uci);
        Assert.Equal("White", positions[3].SideToMove);
    }

    [Fact]
    public void ParseGamePositions_WithCastling_TracksPositionAccurately()
    {
        var pgn = "1. e4 e5 2. Nf3 Nf6 3. Bc4 Bc5 4. O-O";
        var positions = _parser.ParseGamePositions(pgn);

        Assert.Equal(7, positions.Count);
        var castlingStep = positions[6];
        Assert.Equal("O-O", castlingStep.San);
        Assert.Equal("e1g1", castlingStep.Uci);
        Assert.Equal("White", castlingStep.Color);
        Assert.Equal("Black", castlingStep.SideToMove);
    }

    [Fact]
    public void ParseGamePositions_WithFoolsMate_DetectsCheckmateOnFinalMove()
    {
        // Fool's Mate: 1. f3 e5 2. g4 Qh4#
        var pgn = "1. f3 e5 2. g4 Qh4#";
        var positions = _parser.ParseGamePositions(pgn);

        Assert.Equal(4, positions.Count);
        var mateMove = positions[3];
        Assert.Equal(4, mateMove.Ply);
        Assert.Equal("Black", mateMove.Color);
        Assert.Equal("Qh4#", mateMove.San);
        Assert.True(mateMove.IsCheck);
        Assert.True(mateMove.IsCheckmate);
    }

    [Fact]
    public void ParseGamePositions_WithUserGame_ParsesSuccessfully()
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

        var positions = _parser.ParseGamePositions(pgn);
        Assert.NotNull(positions);
        Assert.Equal(94, positions.Count);
    }
}







