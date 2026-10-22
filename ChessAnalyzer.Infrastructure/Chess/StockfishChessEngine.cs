using ChessAnalyzer.Application.DTOs;
using ChessAnalyzer.Application.Interfaces.Chess;
using ChessAnalyzer.Application.Services;
using ChessAnalyzer.Infrastructure.Chess.Stockfish;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace ChessAnalyzer.Infrastructure.Chess;

public class StockfishChessEngine : IChessEngine
{
    private readonly StockfishOptions _options;
    private readonly IPgnParser _pgnParser;

    public StockfishChessEngine(
        IOptions<StockfishOptions> options,
        IPgnParser pgnParser)
    {
        _options = options.Value;
        _pgnParser = pgnParser;
    }

    public async Task<string> AnalyzeAsync(string pgn, CancellationToken cancellationToken = default)
    {
        var result = await AnalyzeGameAsync(pgn, cancellationToken);
        return result.Message;
    }

    public async Task<GameAnalysisResultDto> AnalyzeGameAsync(string pgn, CancellationToken cancellationToken = default)
    {
        _pgnParser.Validate(pgn);

        var positions = _pgnParser.ParseGamePositions(pgn);
        if (positions == null || positions.Count == 0)
        {
            throw new ArgumentException("The PGN contains no playable moves.", nameof(pgn));
        }

        var startingFen = _pgnParser.ExtractStartingFen(pgn);
        var initialFen = !string.IsNullOrWhiteSpace(startingFen)
            ? startingFen
            : "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        var executablePath = ResolveStockfishExecutable();
        var targetDepth = _options.DefaultDepth > 0 ? _options.DefaultDepth : 10;
        var baseTimeout = _options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 120;
        var timeoutSeconds = Math.Max(baseTimeout, positions.Count * 2);

        var workingDir = Path.GetDirectoryName(executablePath);
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = string.IsNullOrEmpty(workingDir) ? AppContext.BaseDirectory : workingDir,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var token = linkedCts.Token;

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start Stockfish process.");
            }

            process.ErrorDataReceived += (_, _) => { };
            process.BeginErrorReadLine();

            var input = process.StandardInput;
            var output = process.StandardOutput;

            // 1. Initialize UCI engine
            await input.WriteLineAsync("uci".AsMemory(), token);
            await input.FlushAsync(token);

            string? line;
            var uciReady = false;
            while ((line = await output.ReadLineAsync(token)) != null)
            {
                if (line == "uciok")
                {
                    uciReady = true;
                    break;
                }
            }

            if (!uciReady)
            {
                throw new InvalidOperationException("Stockfish did not complete UCI initialization.");
            }

            // 2. Ready check
            await input.WriteLineAsync("isready".AsMemory(), token);
            await input.FlushAsync(token);

            var engineReady = false;
            while ((line = await output.ReadLineAsync(token)) != null)
            {
                if (line == "readyok")
                {
                    engineReady = true;
                    break;
                }
            }

            if (!engineReady)
            {
                throw new InvalidOperationException("Stockfish did not become ready.");
            }

            // Reset engine game state
            await input.WriteLineAsync("ucinewgame".AsMemory(), token);
            await input.WriteLineAsync("isready".AsMemory(), token);
            await input.FlushAsync(token);
            while ((line = await output.ReadLineAsync(token)) != null)
            {
                if (line == "readyok") break;
            }

            // 3. Evaluate starting position (Ply 0)
            var initialSideToMove = initialFen.Split(' ').Length > 1 && initialFen.Split(' ')[1] == "b" ? "Black" : "White";
            var (initialEval, initialBestMove, _) = await QueryPositionAsync(input, output, initialFen, initialSideToMove, targetDepth, token);
            initialEval.BestMoveUci = initialBestMove;
            initialEval.BestMoveSan = !string.IsNullOrWhiteSpace(initialBestMove)
                ? ChessSanConverter.ConvertUciToSan(initialFen, initialBestMove)
                : null;

            // 4. Evaluate each move position
            var moveAnalyses = new List<MoveAnalysisDto>(positions.Count);

            foreach (var pos in positions)
            {
                EvaluationDto eval;
                string bestMove;
                string? pv;

                if (pos.IsCheckmate)
                {
                    // The side whose turn it is now (SideToMove) is checkmated
                    var isBlack = pos.SideToMove.Equals("Black", StringComparison.OrdinalIgnoreCase);
                    var scoreText = isBlack ? "# 1-0" : "# 0-1";

                    eval = new EvaluationDto
                    {
                        Type = "mate",
                        MateInMoves = 0,
                        ScoreText = scoreText,
                        Depth = targetDepth
                    };
                    bestMove = string.Empty;
                    pv = null;
                }
                else if (pos.IsStalemate)
                {
                    eval = new EvaluationDto
                    {
                        Type = "cp",
                        Centipawns = 0,
                        Value = 0.0,
                        ScoreText = "0.00",
                        Depth = targetDepth
                    };
                    bestMove = string.Empty;
                    pv = "Stalemate";
                }
                else
                {
                    var queryResult = await QueryPositionAsync(input, output, pos.FenAfter, pos.SideToMove, targetDepth, token);
                    eval = queryResult.Evaluation;
                    bestMove = queryResult.BestMove;
                    pv = queryResult.PrincipalVariation;
                }

                string? bestMoveSan = null;
                if (!string.IsNullOrWhiteSpace(bestMove))
                {
                    bestMoveSan = ChessSanConverter.ConvertUciToSan(pos.FenAfter, bestMove);
                    eval.BestMoveSan = bestMoveSan;
                }

                moveAnalyses.Add(new MoveAnalysisDto
                {
                    Ply = pos.Ply,
                    MoveNumber = pos.MoveNumber,
                    Color = pos.Color,
                    San = pos.San,
                    Uci = pos.Uci,
                    FenBefore = pos.FenBefore,
                    FenAfter = pos.FenAfter,
                    SideToMove = pos.SideToMove,
                    Evaluation = eval,
                    BestMoveUci = bestMove,
                    BestMoveSan = bestMoveSan,
                    PrincipalVariation = pv
                });
            }

            // 5. Gracefully terminate engine
            try
            {
                await input.WriteLineAsync("quit".AsMemory(), token);
                await input.FlushAsync(token);
                await process.WaitForExitAsync(token);
            }
            catch
            {
                // Ignore exit exception
            }

            var finalEval = moveAnalyses.Count > 0 ? moveAnalyses.Last().Evaluation : initialEval;
            var gameReview = ChessGameReviewAnalyzer.AnalyzeGameReview(moveAnalyses, initialEval, pgn);

            var summary = $"Stockfish analysis completed.{Environment.NewLine}" +
                          $"Moves analyzed: {moveAnalyses.Count}{Environment.NewLine}" +
                          $"White accuracy: {gameReview.WhiteAccuracy}% | Black accuracy: {gameReview.BlackAccuracy}%{Environment.NewLine}" +
                          $"Opening: {gameReview.OpeningName} ({gameReview.OpeningEco}){Environment.NewLine}" +
                          $"Initial evaluation: {initialEval.ScoreText}{Environment.NewLine}" +
                          $"Final evaluation: {finalEval.ScoreText}{Environment.NewLine}" +
                          $"Engine depth: {targetDepth}";

            return new GameAnalysisResultDto
            {
                Message = summary,
                InitialEvaluation = initialEval,
                FinalEvaluation = finalEval,
                Moves = moveAnalyses,
                Review = gameReview
            };
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore kill errors during disposal
                }
            }
        }
    }

    private static async Task<(EvaluationDto Evaluation, string BestMove, string? PrincipalVariation)> QueryPositionAsync(
        StreamWriter input,
        StreamReader output,
        string fen,
        string sideToMove,
        int targetDepth,
        CancellationToken token)
    {
        await input.WriteLineAsync($"position fen {fen}".AsMemory(), token);
        await input.WriteLineAsync($"go depth {targetDepth}".AsMemory(), token);
        await input.FlushAsync(token);

        string? line;
        string? latestInfoLine = null;
        string? bestMoveLine = null;

        while ((line = await output.ReadLineAsync(token)) != null)
        {
            if (line.StartsWith("info ") && line.Contains(" score "))
            {
                latestInfoLine = line;
            }

            if (line.StartsWith("bestmove "))
            {
                bestMoveLine = line;
                break;
            }
        }

        var eval = StockfishEvaluationHelper.ParseEvaluation(latestInfoLine, sideToMove, targetDepth);
        var bestMove = StockfishEvaluationHelper.ParseBestMove(bestMoveLine);
        var pv = StockfishEvaluationHelper.ParsePrincipalVariation(latestInfoLine);

        eval.BestMoveUci = bestMove;

        return (eval, bestMove, pv);
    }

    private string ResolveStockfishExecutable()
    {
        // 1. If configured path is specified and exists, use it
        if (!string.IsNullOrWhiteSpace(_options.ExecutablePath))
        {
            var executablePath = Path.IsPathRooted(_options.ExecutablePath)
                ? _options.ExecutablePath
                : Path.Combine(AppContext.BaseDirectory, _options.ExecutablePath);

            executablePath = Path.GetFullPath(executablePath);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }

            var fallbackPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..", "ChessAnalyzer.Infrastructure", _options.ExecutablePath));

            if (File.Exists(fallbackPath))
            {
                return fallbackPath;
            }
        }

        // 2. Standard Linux / Docker / Render binary locations
        string[] linuxCandidates = ["/usr/games/stockfish", "/usr/bin/stockfish", "/usr/local/bin/stockfish"];
        foreach (var candidate in linuxCandidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // 3. Search in system PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var binaryNames = OperatingSystem.IsWindows()
                ? new[] { "stockfish-windows-x86-64-universal.exe", "stockfish.exe" }
                : new[] { "stockfish" };

            foreach (var pathDir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var binaryName in binaryNames)
                {
                    try
                    {
                        var candidate = Path.Combine(pathDir, binaryName);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                    catch
                    {
                        // Ignore invalid paths in PATH env
                    }
                }
            }
        }

        // 4. On Linux/Render, stockfish may be runnable directly via PATH invocation
        if (!OperatingSystem.IsWindows())
        {
            return "stockfish";
        }

        throw new FileNotFoundException(
            $"Stockfish executable not found at: '{_options.ExecutablePath}'. " +
            "Please ensure Stockfish is configured in appsettings.json or installed on the system PATH.");
    }
}
