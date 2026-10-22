namespace ChessAnalyzer.Application.Services;

public static class ChessOpeningBook
{
    private class OpeningEntry
    {
        public string Name { get; }
        public string Eco { get; }
        public string[] Moves { get; }

        public OpeningEntry(string eco, string name, params string[] moves)
        {
            Eco = eco;
            Name = name;
            Moves = moves;
        }
    }

    private static readonly List<OpeningEntry> Openings = new()
    {
        // King's Pawn Openings (e4 e5)
        new("C20", "King's Pawn Game", "e4", "e5"),
        new("C40", "King's Knight Opening", "e4", "e5", "Nf3"),
        new("C44", "King's Pawn Game: Normal Variation", "e4", "e5", "Nf3", "Nc6"),
        new("C50", "Italian Game", "e4", "e5", "Nf3", "Nc6", "Bc4"),
        new("C50", "Italian Game: Giuoco Piano", "e4", "e5", "Nf3", "Nc6", "Bc4", "Bc5"),
        new("C53", "Italian Game: Giuoco Piano Main Line", "e4", "e5", "Nf3", "Nc6", "Bc4", "Bc5", "c3"),
        new("C51", "Evans Gambit", "e4", "e5", "Nf3", "Nc6", "Bc4", "Bc5", "b4"),
        new("C55", "Two Knights Defense", "e4", "e5", "Nf3", "Nc6", "Bc4", "Nf6"),
        new("C57", "Two Knights: Fried Liver Attack", "e4", "e5", "Nf3", "Nc6", "Bc4", "Nf6", "Ng5", "d5", "exd5", "Nxd5", "Nxf7"),
        new("C50", "Hungarian Opening", "e4", "e5", "Nf3", "Nc6", "Bc4", "Be7"),
        new("C60", "Ruy Lopez", "e4", "e5", "Nf3", "Nc6", "Bb5"),
        new("C70", "Ruy Lopez: Morphy Defense", "e4", "e5", "Nf3", "Nc6", "Bb5", "a6"),
        new("C70", "Ruy Lopez: Columbus Variation", "e4", "e5", "Nf3", "Nc6", "Bb5", "a6", "Ba4"),
        new("C78", "Ruy Lopez: Morphy Defense, Modern", "e4", "e5", "Nf3", "Nc6", "Bb5", "a6", "Ba4", "Nf6"),
        new("C84", "Ruy Lopez: Closed", "e4", "e5", "Nf3", "Nc6", "Bb5", "a6", "Ba4", "Nf6", "O-O", "Be7"),
        new("C68", "Ruy Lopez: Exchange Variation", "e4", "e5", "Nf3", "Nc6", "Bb5", "a6", "Bxc6"),
        new("C65", "Ruy Lopez: Berlin Defense", "e4", "e5", "Nf3", "Nc6", "Bb5", "Nf6"),
        new("C45", "Scotch Game", "e4", "e5", "Nf3", "Nc6", "d4"),
        new("C45", "Scotch Game: Classical", "e4", "e5", "Nf3", "Nc6", "d4", "exd4", "Nxd4"),
        new("C46", "Three Knights Opening", "e4", "e5", "Nf3", "Nc6", "Nc3"),
        new("C47", "Four Knights Game", "e4", "e5", "Nf3", "Nc6", "Nc3", "Nf6"),
        new("C48", "Four Knights: Spanish Variation", "e4", "e5", "Nf3", "Nc6", "Nc3", "Nf6", "Bb5"),
        new("C42", "Petrov's Defense", "e4", "e5", "Nf3", "Nf6"),
        new("C41", "Philidor Defense", "e4", "e5", "Nf3", "d6"),
        new("C30", "King's Gambit", "e4", "e5", "f4"),
        new("C33", "King's Gambit Accepted", "e4", "e5", "f4", "exf4"),
        new("C30", "King's Gambit Declined", "e4", "e5", "f4", "Bc5"),
        new("C25", "Vienna Game", "e4", "e5", "Nc3"),
        new("C26", "Vienna Game: Falkbeer", "e4", "e5", "Nc3", "Nf6"),
        new("C21", "Center Game", "e4", "e5", "d4"),
        new("C23", "Bishop's Opening", "e4", "e5", "Bc4"),

        // Sicilian Defense (e4 c5)
        new("B20", "Sicilian Defense", "e4", "c5"),
        new("B27", "Sicilian: Open Preparation", "e4", "c5", "Nf3"),
        new("B50", "Sicilian: Modern Line", "e4", "c5", "Nf3", "d6"),
        new("B53", "Sicilian: Open", "e4", "c5", "Nf3", "d6", "d4"),
        new("B54", "Sicilian: Open Main Line", "e4", "c5", "Nf3", "d6", "d4", "cxd4", "Nxd4", "Nf6", "Nc3"),
        new("B90", "Sicilian Defense: Najdorf Variation", "e4", "c5", "Nf3", "d6", "d4", "cxd4", "Nxd4", "Nf6", "Nc3", "a6"),
        new("B70", "Sicilian Defense: Dragon Variation", "e4", "c5", "Nf3", "d6", "d4", "cxd4", "Nxd4", "Nf6", "Nc3", "g6"),
        new("B80", "Sicilian Defense: Scheveningen", "e4", "c5", "Nf3", "d6", "d4", "cxd4", "Nxd4", "Nf6", "Nc3", "e6"),
        new("B30", "Sicilian Defense: Old Sicilian", "e4", "c5", "Nf3", "Nc6"),
        new("B32", "Sicilian Defense: Open, Kalashnikov", "e4", "c5", "Nf3", "Nc6", "d4", "cxd4", "Nxd4"),
        new("B51", "Sicilian: Rossolimo Variation", "e4", "c5", "Nf3", "Nc6", "Bb5"),
        new("B40", "Sicilian: French Variation", "e4", "c5", "Nf3", "e6"),
        new("B41", "Sicilian: Kan Variation", "e4", "c5", "Nf3", "e6", "d4", "cxd4", "Nxd4", "a6"),
        new("B44", "Sicilian: Taimanov Variation", "e4", "c5", "Nf3", "e6", "d4", "cxd4", "Nxd4", "Nc6"),
        new("B22", "Sicilian: Alapin Variation", "e4", "c5", "c3"),
        new("B23", "Sicilian: Closed", "e4", "c5", "Nc3"),
        new("B21", "Sicilian: Grand Prix Attack", "e4", "c5", "f4"),
        new("B21", "Sicilian: Smith-Morra Gambit", "e4", "c5", "d4", "cxd4", "c3"),

        // French Defense (e4 e6)
        new("C00", "French Defense", "e4", "e6"),
        new("C01", "French Defense: Normal", "e4", "e6", "d4", "d5"),
        new("C02", "French Defense: Advance Variation", "e4", "e6", "d4", "d5", "e5"),
        new("C10", "French Defense: Paulsen", "e4", "e6", "d4", "d5", "Nc3"),
        new("C15", "French Defense: Winawer Variation", "e4", "e6", "d4", "d5", "Nc3", "Bb4"),
        new("C11", "French Defense: Classical", "e4", "e6", "d4", "d5", "Nc3", "Nf6"),
        new("C03", "French Defense: Tarrasch Variation", "e4", "e6", "d4", "d5", "Nd2"),
        new("C01", "French Defense: Exchange Variation", "e4", "e6", "d4", "d5", "exd5", "exd5"),

        // Caro-Kann Defense (e4 c6)
        new("B10", "Caro-Kann Defense", "e4", "c6"),
        new("B12", "Caro-Kann Defense: Main Line", "e4", "c6", "d4", "d5"),
        new("B15", "Caro-Kann Defense: Classical", "e4", "c6", "d4", "d5", "Nc3", "dxe4", "Nxe4"),
        new("B12", "Caro-Kann Defense: Advance Variation", "e4", "c6", "d4", "d5", "e5"),
        new("B13", "Caro-Kann Defense: Exchange Variation", "e4", "c6", "d4", "d5", "exd5", "cxd5"),
        new("B13", "Panov-Botvinnik Attack", "e4", "c6", "d4", "d5", "exd5", "cxd5", "c4"),
        new("B10", "Caro-Kann: Two Knights", "e4", "c6", "Nc3", "d5", "Nf3"),

        // Other 1. e4 Responses
        new("B01", "Scandinavian Defense", "e4", "d5"),
        new("B01", "Scandinavian: Mieses-Kotroc", "e4", "d5", "exd5", "Qxd5", "Nc3"),
        new("B01", "Scandinavian: Modern Variation", "e4", "d5", "exd5", "Nf6"),
        new("B02", "Alekhine's Defense", "e4", "Nf6"),
        new("B03", "Alekhine's Defense: Four Pawns Attack", "e4", "Nf6", "e5", "Nd5", "d4", "d6", "c4"),
        new("B04", "Alekhine's Defense: Modern", "e4", "Nf6", "e5", "Nd5", "d4", "d6", "Nf3"),
        new("B07", "Pirc Defense", "e4", "d6"),
        new("B07", "Pirc Defense: Main Line", "e4", "d6", "d4", "Nf6", "Nc3"),
        new("B06", "Modern Defense", "e4", "g6"),

        // Queen's Pawn Openings (d4 d5)
        new("D00", "Queen's Pawn Game", "d4", "d5"),
        new("D06", "Queen's Gambit", "d4", "d5", "c4"),
        new("D30", "Queen's Gambit Declined", "d4", "d5", "c4", "e6"),
        new("D35", "QGD: Orthodox Defense", "d4", "d5", "c4", "e6", "Nc3", "Nf6"),
        new("D53", "QGD: Traditional Line", "d4", "d5", "c4", "e6", "Nc3", "Nf6", "Bg5", "Be7"),
        new("D20", "Queen's Gambit Accepted", "d4", "d5", "c4", "dxc4"),
        new("D10", "Slav Defense", "d4", "d5", "c4", "c6"),
        new("D15", "Slav Defense: Three Knights", "d4", "d5", "c4", "c6", "Nf3", "Nf6", "Nc3"),
        new("D43", "Semi-Slav Defense", "d4", "d5", "c4", "c6", "Nf3", "Nf6", "Nc3", "e6"),
        new("D02", "Queen's Pawn: London System", "d4", "d5", "Nf3", "Nf6", "Bf4"),
        new("D00", "London System", "d4", "d5", "Bf4"),
        new("D00", "Levitsky Attack", "d4", "d5", "Bg5"),
        new("D05", "Colle System", "d4", "d5", "Nf3", "Nf6", "e3"),

        // Indian Defenses (d4 Nf6)
        new("A45", "Indian Defense", "d4", "Nf6"),
        new("A50", "Indian Defense: Normal", "d4", "Nf6", "c4"),
        new("E20", "Nimzo-Indian Defense", "d4", "Nf6", "c4", "e6", "Nc3", "Bb4"),
        new("E12", "Queen's Indian Defense", "d4", "Nf6", "c4", "e6", "Nf3", "b6"),
        new("E11", "Bogo-Indian Defense", "d4", "Nf6", "c4", "e6", "Nf3", "Bb4+"),
        new("E60", "King's Indian Defense", "d4", "Nf6", "c4", "g6", "Nc3", "Bg7"),
        new("D80", "Grünfeld Defense", "d4", "Nf6", "c4", "g6", "Nc3", "d5"),
        new("A56", "Benoni Defense", "d4", "Nf6", "c4", "c5", "d5"),
        new("A45", "Trompowsky Attack", "d4", "Nf6", "Bg5"),
        new("A46", "Torre Attack", "d4", "Nf6", "Nf3", "e6", "Bg5"),
        new("A48", "East Indian Defense", "d4", "Nf6", "Nf3", "g6"),

        // Dutch & Other d4
        new("A80", "Dutch Defense", "d4", "f5"),
        new("A84", "Dutch: Classical", "d4", "f5", "c4", "Nf6", "g3"),

        // Flank Openings
        new("A10", "English Opening", "c4"),
        new("A20", "English Opening: King's English", "c4", "e5"),
        new("A30", "English Opening: Symmetrical", "c4", "c5"),
        new("A15", "English Opening: Anglo-Indian", "c4", "Nf6"),
        new("A04", "Réti Opening", "Nf3"),
        new("A06", "Réti Opening: King's Indian Attack", "Nf3", "d5"),
        new("A05", "Réti: Nimzowitsch-Larsen", "Nf3", "Nf6"),
        new("A02", "Bird's Opening", "f4"),
        new("A01", "Nimzo-Larsen Attack", "b3"),
        new("A00", "King's Fianchetto Opening", "g3"),
        new("A00", "Polish Opening", "b4"),

        // Custom & Unusual Lines
        new("A00", "Van't Kruijs Opening", "e3"),
        new("A00", "Nimzo-Larsen / Van't Kruijs", "e3", "e5", "b3"),
        new("A00", "Nimzo-Larsen / Van't Kruijs Line", "e3", "e5", "b3", "Nc6", "Bb2"),
        new("A00", "Nimzo-Larsen / Van't Kruijs Extended", "e3", "e5", "b3", "Nc6", "Bb2", "Nf6", "g3", "d5", "Bg2", "e4"),
    };

    public static bool IsBookMove(IReadOnlyList<string> playedSans)
    {
        if (playedSans == null || playedSans.Count == 0 || playedSans.Count > 24)
            return false;

        foreach (var op in Openings)
        {
            if (op.Moves.Length >= playedSans.Count)
            {
                bool matches = true;
                for (int i = 0; i < playedSans.Count; i++)
                {
                    if (!NormalizeSan(op.Moves[i]).Equals(NormalizeSan(playedSans[i]), StringComparison.OrdinalIgnoreCase))
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches) return true;
            }
        }

        return false;
    }

    public static (string Name, string Eco) IdentifyOpening(IReadOnlyList<string> playedSans)
    {
        if (playedSans == null || playedSans.Count == 0)
            return ("Starting Position", "A00");

        OpeningEntry? bestMatch = null;
        int maxMatched = 0;

        foreach (var op in Openings)
        {
            int matched = 0;
            for (int i = 0; i < Math.Min(op.Moves.Length, playedSans.Count); i++)
            {
                if (NormalizeSan(op.Moves[i]).Equals(NormalizeSan(playedSans[i]), StringComparison.OrdinalIgnoreCase))
                {
                    matched++;
                }
                else
                {
                    break;
                }
            }

            if (matched > maxMatched)
            {
                maxMatched = matched;
                bestMatch = op;
            }
            else if (matched == maxMatched && matched > 0 && bestMatch != null)
            {
                // Prefer more specific opening description
                if (op.Moves.Length > bestMatch.Moves.Length)
                {
                    bestMatch = op;
                }
            }
        }

        if (bestMatch != null && maxMatched >= 1)
        {
            return (bestMatch.Name, bestMatch.Eco);
        }

        // Generic fallback by first move
        string firstSan = NormalizeSan(playedSans[0]);
        return firstSan switch
        {
            "e4" => ("King's Pawn Opening", "B00"),
            "d4" => ("Queen's Pawn Opening", "A40"),
            "c4" => ("English Opening", "A10"),
            "Nf3" => ("Réti Opening", "A04"),
            "f4" => ("Bird's Opening", "A02"),
            "b3" => ("Nimzo-Larsen Attack", "A01"),
            "g3" => ("King's Fianchetto Opening", "A00"),
            _ => ("Custom Opening", "A00")
        };
    }

    private static string NormalizeSan(string san)
    {
        if (string.IsNullOrWhiteSpace(san)) return "";
        return san.TrimEnd('+', '#', '!', '?');
    }
}
