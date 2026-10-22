using ChessDotNetCore;
using ChessDotNetCore.Pieces;

namespace ChessAnalyzer.Infrastructure.Chess;

public static class ChessSanConverter
{
    public static string ConvertUciToSan(string fen, string uci)
    {
        if (string.IsNullOrWhiteSpace(uci) || uci.Length < 4) return uci;

        try
        {
            var game = string.IsNullOrWhiteSpace(fen) ? new ChessGame() : new ChessGame(fen);
            var fromStr = uci.Substring(0, 2);
            var toStr = uci.Substring(2, 2);
            var promoChar = uci.Length > 4 ? (char?)char.ToUpperInvariant(uci[4]) : null;

            var fromPos = new Position(fromStr);
            var toPos = new Position(toStr);

            var piece = game.GetPieceAt(fromPos);
            if (piece == null) return uci;

            var player = piece.Owner;

            // Check castling
            if (piece is King)
            {
                if ((fromStr == "e1" && toStr == "g1") || (fromStr == "e8" && toStr == "g8")) return "O-O";
                if ((fromStr == "e1" && toStr == "c1") || (fromStr == "e8" && toStr == "c8")) return "O-O-O";
            }

            var isCapture = game.GetPieceAt(toPos) != null;
            // En passant: pawn moves diagonally to an empty square
            if (piece is Pawn && fromStr[0] != toStr[0] && !isCapture)
            {
                isCapture = true;
            }

            string piecePrefix = piece switch
            {
                Knight => "N",
                Bishop => "B",
                Rook => "R",
                Queen => "Q",
                King => "K",
                _ => ""
            };

            string disambiguation = "";
            if (piece is not Pawn && piece is not King)
            {
                var validMoves = game.GetValidMoves(player);
                var others = validMoves
                    .Where(m => m.NewPosition == toPos && m.OriginalPosition != fromPos &&
                                game.GetPieceAt(m.OriginalPosition)?.GetType() == piece.GetType())
                    .ToList();

                if (others.Count > 0)
                {
                    bool sameFile = others.Any(m => m.OriginalPosition.ToString().ToLowerInvariant()[0] == fromStr[0]);
                    bool sameRank = others.Any(m => m.OriginalPosition.ToString().ToLowerInvariant()[1] == fromStr[1]);

                    if (!sameFile)
                    {
                        disambiguation = fromStr[0].ToString();
                    }
                    else if (!sameRank)
                    {
                        disambiguation = fromStr[1].ToString();
                    }
                    else
                    {
                        disambiguation = fromStr;
                    }
                }
            }

            string san;
            if (piece is Pawn)
            {
                if (isCapture)
                {
                    san = $"{fromStr[0]}x{toStr}";
                }
                else
                {
                    san = toStr;
                }

                if (promoChar.HasValue)
                {
                    san += $"={promoChar.Value}";
                }
            }
            else
            {
                san = $"{piecePrefix}{disambiguation}{(isCapture ? "x" : "")}{toStr}";
            }

            // Test move for check or checkmate
            var move = new Move(fromPos, toPos, player, promoChar);
            game.MakeMove(move, true);

            var opponent = player == Player.White ? Player.Black : Player.White;
            if (game.IsCheckmated(opponent))
            {
                san += "#";
            }
            else if (game.IsInCheck(opponent))
            {
                san += "+";
            }

            return san;
        }
        catch
        {
            return uci;
        }
    }
}
