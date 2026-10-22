using ChessAnalyzer.Domain.Entities;
using Xunit;

namespace ChessAnalyzer.UnitTests;

public class GameEntityTests
{
    [Fact]
    public void Game_Constructor_InitializesAllPropertiesAndGuid()
    {
        var white = "Magnus Carlsen";
        var black = "Hikaru Nakamura";
        var pgn = "1. e4 e5";
        var result = "1/2-1/2";

        var game = new Game(white, black, pgn, result);

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal(white, game.WhitePlayer);
        Assert.Equal(black, game.BlackPlayer);
        Assert.Equal(pgn, game.Pgn);
        Assert.Equal(result, game.Result);
    }
}
