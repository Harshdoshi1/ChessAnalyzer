namespace ChessAnalyzer.Domain.Entities;

public class Game
{
    public Guid Id { get; private set; }

    public string WhitePlayer { get; private set; } = string.Empty;

    public string BlackPlayer { get; private set; } = string.Empty;

    public string Pgn { get; private set; } = string.Empty;

    public string Result { get; private set; } = string.Empty;

    public Game(
        string whitePlayer,
        string blackPlayer,
        string pgn,
        string result)
    {
        Id = Guid.NewGuid();
        WhitePlayer = whitePlayer;
        BlackPlayer = blackPlayer;
        Pgn = pgn;
        Result = result;
    }
}