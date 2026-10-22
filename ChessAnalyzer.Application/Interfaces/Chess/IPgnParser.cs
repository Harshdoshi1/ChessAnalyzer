namespace ChessAnalyzer.Application.Interfaces.Chess;

public interface IPgnParser
{
    void Validate(string pgn);

    IReadOnlyList<string> ParseToUciMoves(string pgn);

    string? ExtractStartingFen(string pgn);

    IReadOnlyList<ParsedMovePosition> ParseGamePositions(string pgn);
}
