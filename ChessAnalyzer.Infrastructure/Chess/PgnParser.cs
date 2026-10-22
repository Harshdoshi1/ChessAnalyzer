using ChessAnalyzer.Application.Interfaces.Chess;
using System.Text.RegularExpressions;

namespace ChessAnalyzer.Infrastructure.Chess;

public class PgnParser : IPgnParser
{
    public void Validate(string pgn)
    {
        if (string.IsNullOrWhiteSpace(pgn))
        {
            throw new ArgumentException(
                "PGN cannot be empty.",
                nameof(pgn));
        }
    }

    public string? ExtractStartingFen(string pgn)
    {
        if (string.IsNullOrWhiteSpace(pgn))
        {
            return null;
        }

        var match = Regex.Match(
            pgn,
            @"\[FEN\s+""([^""]+)""\]",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    public IReadOnlyList<string> ParseToUciMoves(string pgn)
    {
        Validate(pgn);

        var normalizedPgn = NormalizePgn(pgn);

        var parser = new ChessDotNetCore.PgnParsing.PgnParser(
            isDiagnoseOn: true);

        parser.InitFromString(normalizedPgn);

        var success = parser.ParseSingle(
            noMoveList: false,
            out _,
            out _,
            out var parsedGame,
            out var errorMessage);

        if (!success || parsedGame == null)
        {
            throw new ArgumentException(
                errorMessage ?? "The supplied PGN could not be parsed.",
                nameof(pgn));
        }

        if (parsedGame.ChessMoves == null ||
            parsedGame.ChessMoves.Count == 0)
        {
            throw new ArgumentException(
                "The PGN contains no playable moves.",
                nameof(pgn));
        }

        var uciMoves = parsedGame.ChessMoves
            .Select(move =>
            {
                var from = move.OriginalPosition
                    .ToString()
                    .ToLowerInvariant();

                var to = move.NewPosition
                    .ToString()
                    .ToLowerInvariant();

                var uci = from + to;

                if (move.Promotion.HasValue)
                {
                    uci += char.ToLowerInvariant(
                        move.Promotion.Value);
                }

                return uci;
            })
            .ToList();

        return uciMoves;
    }

    public IReadOnlyList<ParsedMovePosition> ParseGamePositions(string pgn)
    {
        Validate(pgn);

        var normalizedPgn = NormalizePgn(pgn);

        var parser = new ChessDotNetCore.PgnParsing.PgnParser(
            isDiagnoseOn: true);

        parser.InitFromString(normalizedPgn);

        var success = parser.ParseSingle(
            noMoveList: false,
            out _,
            out _,
            out var parsedGame,
            out var errorMessage);

        if (!success || parsedGame == null)
        {
            throw new ArgumentException(
                errorMessage ?? "The supplied PGN could not be parsed.",
                nameof(pgn));
        }

        if (parsedGame.ChessMoves == null ||
            parsedGame.ChessMoves.Count == 0)
        {
            throw new ArgumentException(
                "The PGN contains no playable moves.",
                nameof(pgn));
        }

        var startingFen = ExtractStartingFen(pgn);
        var game = string.IsNullOrWhiteSpace(startingFen)
            ? new ChessDotNetCore.ChessGame()
            : new ChessDotNetCore.ChessGame(startingFen);

        var positions = new List<ParsedMovePosition>(parsedGame.ChessMoves.Count);

        for (int i = 0; i < parsedGame.ChessMoves.Count; i++)
        {
            var move = parsedGame.ChessMoves[i];
            var pgnMove = (parsedGame.PgnMoves != null && i < parsedGame.PgnMoves.Count)
                ? parsedGame.PgnMoves[i]
                : null;

            var ply = i + 1;
            var moveNumber = (i / 2) + 1;
            var color = move.Player == ChessDotNetCore.Player.White ? "White" : "Black";

            var from = move.OriginalPosition.ToString().ToLowerInvariant();
            var to = move.NewPosition.ToString().ToLowerInvariant();
            var uci = from + to;
            if (move.Promotion.HasValue)
            {
                uci += char.ToLowerInvariant(move.Promotion.Value);
            }

            var san = !string.IsNullOrWhiteSpace(pgnMove?.SanMove)
                ? pgnMove.SanMove
                : uci;

            var fenBefore = game.GetFen();

            game.MakeMove(move, true);

            var fenAfter = game.GetFen();
            var sideToMoveTokens = fenAfter.Split(' ');
            var sideToMove = (sideToMoveTokens.Length > 1 && sideToMoveTokens[1] == "b") ? "Black" : "White";

            var nextPlayer = sideToMove == "Black" ? ChessDotNetCore.Player.Black : ChessDotNetCore.Player.White;
            var isCheckmate = game.IsCheckmated(nextPlayer);
            var isStalemate = game.IsStalemated(nextPlayer);
            var isCheck = game.IsInCheck(nextPlayer);


            positions.Add(new ParsedMovePosition
            {
                Ply = ply,
                MoveNumber = moveNumber,
                Color = color,
                San = san,
                Uci = uci,
                FenBefore = fenBefore,
                FenAfter = fenAfter,
                SideToMove = sideToMove,
                IsCheck = isCheck,
                IsCheckmate = isCheckmate,
                IsStalemate = isStalemate
            });
        }

        return positions;
    }

    private static string NormalizePgn(string pgn)
    {
        var trimmedPgn = pgn.Trim();

        // Preserve full PGN input, including its original headers.
        if (trimmedPgn.StartsWith("["))
        {
            return trimmedPgn;
        }

        // Support move-only input from the Angular form.
        var hasResult = trimmedPgn.EndsWith("*") ||
                        trimmedPgn.EndsWith("1-0") ||
                        trimmedPgn.EndsWith("0-1") ||
                        trimmedPgn.EndsWith("1/2-1/2");
        var gameMoves = hasResult ? trimmedPgn : trimmedPgn + " *";

        return
            "[Event \"ChessAnalyzer Game\"]\n" +
            "[Site \"Local\"]\n" +
            "[Date \"????.??.??\"]\n" +
            "[Round \"-\"]\n" +
            "[White \"Player 1\"]\n" +
            "[Black \"Player 2\"]\n" +
            "[Result \"*\"]\n\n" +
            gameMoves;
    }
}
