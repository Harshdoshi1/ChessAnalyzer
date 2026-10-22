namespace ChessAnalyzer.Infrastructure.Chess.Stockfish;

public class StockfishOptions
{
    public string ExecutablePath { get; set; } = string.Empty;
    public int DefaultDepth { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 120;
}