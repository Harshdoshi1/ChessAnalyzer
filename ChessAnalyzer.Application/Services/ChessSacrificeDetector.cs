using ChessAnalyzer.Application.DTOs;

namespace ChessAnalyzer.Application.Services;

public static class ChessSacrificeDetector
{
    public static bool IsBrilliantPieceSacrifice(
        MoveAnalysisDto move,
        MoveAnalysisDto? priorMove,
        MoveAnalysisDto? nextMove,
        bool isEngineTopMove,
        double expectedLoss,
        double expectedBefore,
        double expectedAfter)
    {
        // Must be top or near-top engine move
        if (!isEngineTopMove && expectedLoss > 0.015)
            return false;

        // Must lead to a winning or strongly favorable position
        if (expectedAfter < 0.60)
            return false;

        // Must not be played from a hopeless / lost position
        if (expectedBefore < 0.35)
            return false;

        if (string.IsNullOrWhiteSpace(move.San) || string.IsNullOrWhiteSpace(move.Uci) || move.Uci.Length < 4)
            return false;

        // Piece must be Knight, Bishop, Rook, or Queen
        char pieceChar = move.San[0];
        if (pieceChar != 'N' && pieceChar != 'B' && pieceChar != 'R' && pieceChar != 'Q')
        {
            return false;
        }

        int movingPieceValue = GetPieceValue(pieceChar);
        string destSquare = move.Uci.Substring(2, 2);

        // 1. Must NOT be an obvious recapture
        if (priorMove != null && priorMove.Uci.Length >= 4)
        {
            string priorDest = priorMove.Uci.Substring(2, 2);
            if (move.San.Contains("x") && destSquare.Equals(priorDest, StringComparison.OrdinalIgnoreCase))
            {
                // Current move captures on the exact square the opponent just moved to / captured on
                return false;
            }
        }

        // 2. Inspect target piece on destSquare from FenBefore
        char? capturedPieceChar = GetPieceAtSquare(move.FenBefore, destSquare);
        int capturedPieceValue = capturedPieceChar.HasValue ? GetPieceValue(char.ToUpperInvariant(capturedPieceChar.Value)) : 0;

        // Equal trades are NOT sacrifices (e.g. QxQ, RxR, minor x minor)
        if (capturedPieceValue > 0)
        {
            if (movingPieceValue == capturedPieceValue)
                return false;

            // Minor piece trading for minor piece (Bxc6, Nxd5)
            if (movingPieceValue == 3 && capturedPieceValue == 3)
                return false;

            // Capturing a piece of lesser value:
            // Bishop/Knight takes pawn: gives 3, takes 1 (net -2)
            // Rook takes minor: gives 5, takes 3 (net -2)
            // Rook takes pawn: gives 5, takes 1 (net -4)
            // Queen takes minor/rook/pawn: gives 9, takes 1/3/5 (net -4 or more)
            int netLossOnCapture = movingPieceValue - capturedPieceValue;
            if (netLossOnCapture >= 2)
            {
                // This is a direct material sacrifice
                return true;
            }
        }

        // 3. Piece sacrificed and taken by opponent on next ply
        if (nextMove != null && nextMove.Uci.Length >= 4)
        {
            string nextDest = nextMove.Uci.Substring(2, 2);
            if (nextDest.Equals(destSquare, StringComparison.OrdinalIgnoreCase) && nextMove.San.Contains("x"))
            {
                // Opponent captured the piece on destSquare!
                int netMaterialLost = movingPieceValue - capturedPieceValue;
                if (netMaterialLost >= 2)
                {
                    return true;
                }
            }
        }

        // 4. Quiet piece sacrifice: piece moves into attack by opponent pawn or lesser piece
        if (IsSquareAttackedByLesserPiece(move.FenAfter, destSquare, move.Color, movingPieceValue))
        {
            return true;
        }

        return false;
    }

    public static int GetPieceValue(char piece)
    {
        return char.ToUpperInvariant(piece) switch
        {
            'P' => 1,
            'N' => 3,
            'B' => 3,
            'R' => 5,
            'Q' => 9,
            _ => 0
        };
    }

    public static char? GetPieceAtSquare(string fen, string square)
    {
        if (string.IsNullOrWhiteSpace(fen) || square.Length < 2) return null;
        int file = square[0] - 'a'; // 0 to 7
        int rank = square[1] - '1'; // 0 to 7 (0 = 1st rank, 7 = 8th rank)
        if (file < 0 || file > 7 || rank < 0 || rank > 7) return null;

        var parts = fen.Split(' ');
        if (parts.Length == 0) return null;

        var ranks = parts[0].Split('/');
        if (ranks.Length != 8) return null;

        // FEN ranks are 8 down to 1 (index 0 is rank 8, index 7 is rank 1)
        int fenRankIndex = 7 - rank;
        var rankStr = ranks[fenRankIndex];

        int currentFile = 0;
        foreach (char c in rankStr)
        {
            if (char.IsDigit(c))
            {
                currentFile += (c - '0');
            }
            else
            {
                if (currentFile == file)
                {
                    return c;
                }
                currentFile++;
            }
        }

        return null;
    }

    private static bool IsSquareAttackedByLesserPiece(string fenAfter, string square, string movingColor, int movingPieceValue)
    {
        // Simple pawn attack detection on destination square:
        // If Black pawn can capture White piece on square, or White pawn can capture Black piece
        if (square.Length < 2) return false;
        int file = square[0] - 'a';
        int rank = square[1] - '1';
        bool isWhite = movingColor.Equals("White", StringComparison.OrdinalIgnoreCase);

        // For White piece on (file, rank), attacked by Black pawns from (file-1, rank+1) or (file+1, rank+1)
        // For Black piece on (file, rank), attacked by White pawns from (file-1, rank-1) or (file+1, rank-1)
        int pawnRank = isWhite ? rank + 1 : rank - 1;
        char targetPawn = isWhite ? 'p' : 'P';

        if (pawnRank >= 0 && pawnRank <= 7 && movingPieceValue >= 3)
        {
            if (file > 0)
            {
                string pSq = $"{(char)('a' + file - 1)}{(char)('1' + pawnRank)}";
                if (GetPieceAtSquare(fenAfter, pSq) == targetPawn) return true;
            }
            if (file < 7)
            {
                string pSq = $"{(char)('a' + file + 1)}{(char)('1' + pawnRank)}";
                if (GetPieceAtSquare(fenAfter, pSq) == targetPawn) return true;
            }
        }

        return false;
    }
}
