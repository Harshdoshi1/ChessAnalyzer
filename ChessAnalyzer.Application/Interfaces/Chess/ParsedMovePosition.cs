namespace ChessAnalyzer.Application.Interfaces.Chess;

public class ParsedMovePosition
{
    public int Ply { get; set; }
    public int MoveNumber { get; set; }
    public string Color { get; set; } = string.Empty;
    public string San { get; set; } = string.Empty;
    public string Uci { get; set; } = string.Empty;
    public string FenBefore { get; set; } = string.Empty;
    public string FenAfter { get; set; } = string.Empty;
    public string SideToMove { get; set; } = string.Empty;
    public bool IsCheck { get; set; }
    public bool IsCheckmate { get; set; }
    public bool IsStalemate { get; set; }
}
