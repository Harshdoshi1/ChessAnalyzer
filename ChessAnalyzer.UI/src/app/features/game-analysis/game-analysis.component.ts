import { Component, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { Chess } from 'chess.js';
import { GameAnalysisService } from '../../services/game-analysis.service';
import { GameAnalysisResult, GameReview, MoveAnalysis } from '../../models/game-analysis-result';
import { GameBoardComponent } from '../../components/game-board/game-board.component';
import { UI_ICONS } from '../../shared/ui-icons';
import { SafeHtmlPipe } from '../../shared/safe-html.pipe';

export interface AlternativeMove {
  san: string;
  evalText: string;
}

@Component({
  selector: 'app-game-analysis',
  standalone: true,
  imports: [CommonModule, FormsModule, GameBoardComponent, SafeHtmlPipe],
  templateUrl: './game-analysis.component.html',
  styleUrl: './game-analysis.component.css'
})
export class GameAnalysisComponent implements OnInit, OnDestroy {
  private readonly gameAnalysisService = inject(GameAnalysisService);

  @ViewChild(GameBoardComponent) gameBoardComponent?: GameBoardComponent;

  analysisResult: GameAnalysisResult | null = null;
  pgn = '';

  errorMessage = '';
  isLoading = false;
  isAnalyzing = false;
  activeStepIndex = 0;
  isSidebarCollapsed = false;
  activeRightTab: 'analysis' | 'all-moves' | 'engine' = 'analysis';
  copyTooltip = '';

  readonly icons = UI_ICONS;

  readonly sampleOperaGame = `[Event "Opera Game"]
[Site "Paris"]
[Date "1858.??.??"]
[White "Paul Morphy"]
[Black "Duke Karl / Count Isouard"]
[Result "1-0"]
[ECO "C41"]

1. e4 e5 2. Nf3 d6 3. d4 Bg4 4. dxe5 Bxf3 5. Qxf3 dxe5 6. Bc4 Nf6 7. Qb3 Qe7 8. Nc3 c6 9. Bg5 b5 10. Nxb5 cxb5 11. Bxb5+ Nbd7 12. O-O-O Rd8 13. Rxd7 Rxd7 14. Rd1 Qe6 15. Bxd7+ Nxd7 16. Qb8+ Nxb8 17. Rd8# 1-0`;

  private analysisSubscription?: Subscription;

  ngOnInit(): void {
    // Start with clean empty PGN - ready for user input or sample game
    this.pgn = '';
    this.activeStepIndex = 0;
    this.analysisResult = null;
  }

  ngOnDestroy(): void {
    this.analysisSubscription?.unsubscribe();
  }

  toggleSidebarCollapse(): void {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  setActiveRightTab(tab: 'analysis' | 'all-moves' | 'engine'): void {
    this.activeRightTab = tab;
  }

  loadSampleGame(): void {
    this.analysisSubscription?.unsubscribe();
    this.pgn = this.sampleOperaGame;
    this.errorMessage = '';
    this.analysisResult = null;
    this.isLoading = false;
    this.isAnalyzing = false;
    this.activeStepIndex = 33;
    if (this.gameBoardComponent) {
      this.gameBoardComponent.loadGame(this.pgn);
    }
  }

  clearPgn(): void {
    this.analysisSubscription?.unsubscribe();
    this.pgn = '';
    this.errorMessage = '';
    this.analysisResult = null;
    this.isLoading = false;
    this.isAnalyzing = false;
    this.activeStepIndex = 0;
    if (this.gameBoardComponent) {
      this.gameBoardComponent.loadGame('');
    }
  }

  copyPgn(): void {
    if (!this.pgn || !this.pgn.trim()) return;
    if (typeof navigator !== 'undefined' && navigator.clipboard) {
      navigator.clipboard.writeText(this.pgn).then(() => {
        this.copyTooltip = 'Copied!';
        setTimeout(() => this.copyTooltip = '', 2000);
      });
    }
  }

  quickLoadGame(): void {
    this.errorMessage = '';
    const trimmed = (this.pgn || '').trim();
    if (!trimmed) {
      this.clearPgn();
      return;
    }
    try {
      const chess = new Chess();
      chess.loadPgn(trimmed);
      if (this.gameBoardComponent) {
        this.gameBoardComponent.loadGame(trimmed);
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Invalid PGN';
      this.errorMessage = `Could not parse PGN: ${msg}`;
    }
  }

  runFullAnalysis(): void {
    this.analyzeGame(false);
  }

  analyzeGame(isBackground = false): void {
    if (!this.pgn || !this.pgn.trim()) {
      if (!isBackground) {
        this.errorMessage = 'Please enter or paste a PGN to analyze.';
      }
      return;
    }

    try {
      const validator = new Chess();
      validator.loadPgn(this.pgn.trim());
    } catch (err: unknown) {
      if (!isBackground) {
        const msg = err instanceof Error ? err.message : 'Invalid PGN';
        this.errorMessage = `Invalid PGN: ${msg}`;
      }
      return;
    }

    if (!isBackground) {
      this.errorMessage = '';
    }
    this.isAnalyzing = true;
    this.isLoading = !isBackground;

    // Check if board needs reverse replay to start
    if (!isBackground && this.gameBoardComponent && this.gameBoardComponent.currentStepIndex > 0) {
      this.gameBoardComponent.startReverseReplayToStart(() => {
        this.executeAnalysisRequest(isBackground);
      });
    } else {
      this.executeAnalysisRequest(isBackground);
    }
  }

  private executeAnalysisRequest(isBackground = false): void {
    this.analysisSubscription?.unsubscribe();

    this.analysisSubscription = this.gameAnalysisService.analyzeGame(this.pgn).subscribe({
      next: (result) => {
        this.analysisResult = result;
        this.isLoading = false;
        this.isAnalyzing = false;

        // Play forward from start to demonstrate analysis
        if (!isBackground && this.gameBoardComponent) {
          this.gameBoardComponent.startPlayback();
        }
      },
      error: (error) => {
        if (error.name === 'AbortError') return;
        console.error('Analysis failed:', error);
        if (!isBackground) {
          if (error.message?.includes('Http failure during parsing')) {
            this.errorMessage =
              'The backend server did not return a valid analysis response. Please ensure ChessAnalyzer.Api is running on port 7155.';
          } else {
            this.errorMessage =
              error.error?.message ??
              error.error?.title ??
              (typeof error.error === 'string' ? error.error : null) ??
              error.message ??
              'Analysis request failed. Please ensure the backend API is running.';
          }
        }
        this.isLoading = false;
        this.isAnalyzing = false;
      }
    });
  }

  onMoveMade(updatedPgn: string): void {
    this.pgn = updatedPgn;
    this.analyzeGame(true);
  }

  onStepSelected(stepIndex: number): void {
    this.activeStepIndex = stepIndex;
  }

  // --- Header & Loaded Game Data ---

  get whitePlayerName(): string {
    const headers = this.gameBoardComponent?.gameHeaders;
    return headers ? (headers['White'] || 'White Player') : 'White Player';
  }

  get blackPlayerName(): string {
    const headers = this.gameBoardComponent?.gameHeaders;
    return headers ? (headers['Black'] || 'Black Player') : 'Black Player';
  }

  get openingName(): string {
    return this.gameReview?.openingName || 'Ruy López';
  }

  get openingEco(): string {
    return this.gameReview?.openingEco || 'C40';
  }

  get gameStatus(): string {
    const result = this.gameBoardComponent?.gameResult;
    if (result && result !== '*') return result;
    return 'In progress';
  }

  get gameReview(): GameReview | null {
    return this.analysisResult?.review ?? null;
  }

  // --- Right Panel Position & Evaluation Data ---

  get activeMove(): MoveAnalysis | null {
    if (!this.analysisResult || !this.analysisResult.moves || this.analysisResult.moves.length === 0) {
      return null;
    }
    if (this.activeStepIndex > 0 && this.activeStepIndex <= this.analysisResult.moves.length) {
      return this.analysisResult.moves[this.activeStepIndex - 1];
    }
    return this.analysisResult.moves[this.analysisResult.moves.length - 1];
  }

  get currentEvaluationScore(): string {
    const evalObj = this.activeMove?.evaluation ?? this.gameBoardComponent?.currentEvaluation;
    return evalObj?.scoreText || '-0.32';
  }

  get evaluationAdvantageText(): string {
    const evalObj = this.activeMove?.evaluation ?? this.gameBoardComponent?.currentEvaluation;
    if (!evalObj) return 'Slightly better for Black';
    if (evalObj.scoreText?.startsWith('+M')) return 'Forced mate for White';
    if (evalObj.scoreText?.startsWith('-M')) return 'Forced mate for Black';
    const cp = evalObj.centipawns ?? (evalObj.value != null ? evalObj.value * 100 : -32);
    if (cp > 120) return 'Decisive advantage for White';
    if (cp > 35) return 'Slightly better for White';
    if (cp < -120) return 'Decisive advantage for Black';
    if (cp < -35) return 'Slightly better for Black';
    return 'Equal position';
  }

  get whiteAdvantagePercent(): number {
    const evalObj = this.activeMove?.evaluation ?? this.gameBoardComponent?.currentEvaluation;
    if (!evalObj) return 46;
    if (evalObj.scoreText?.startsWith('+M')) return 100;
    if (evalObj.scoreText?.startsWith('-M')) return 0;
    const cp = evalObj.centipawns ?? (evalObj.value != null ? evalObj.value * 100 : -32);
    const winRate = 50 + 50 * (2 / (1 + Math.exp(-0.00368208 * cp)) - 1);
    return Math.max(5, Math.min(95, Math.round(winRate)));
  }

  get blackAdvantagePercent(): number {
    return 100 - this.whiteAdvantagePercent;
  }

  // --- Recommendation & Selected Move Data ---

  get suggestedNextMove(): string {
    if (this.activeStepIndex === 0) {
      return this.analysisResult?.initialEvaluation?.bestMoveUci || '';
    }
    const moveIdx = this.activeStepIndex - 1;
    if (this.analysisResult?.moves && moveIdx >= 0 && moveIdx < this.analysisResult.moves.length) {
      return this.analysisResult.moves[moveIdx].bestMoveUci || '';
    }
    return this.analysisResult?.suggestedNextMove || '';
  }

  get suggestedNextMoveSan(): string {
    if (this.activeStepIndex === 0) {
      return this.analysisResult?.initialEvaluation?.bestMoveSan || '';
    }
    const moveIdx = this.activeStepIndex - 1;
    if (this.analysisResult?.moves && moveIdx >= 0 && moveIdx < this.analysisResult.moves.length) {
      return this.analysisResult.moves[moveIdx].bestMoveSan || '';
    }
    return '';
  }

  get suggestedNextMovePv(): string {
    const moveIdx = this.activeStepIndex - 1;
    if (this.analysisResult?.moves && moveIdx >= 0 && moveIdx < this.analysisResult.moves.length) {
      return this.analysisResult.moves[moveIdx].principalVariation || '';
    }
    return '';
  }

  get recommendedBestMoveSan(): string {
    return this.activeMove?.bestMoveSan || this.activeMove?.bestMoveUci || 'a6';
  }

  get recommendedBestMoveEval(): string {
    return this.currentEvaluationScore;
  }

  get engineDepthText(): string {
    const depth = this.activeMove?.evaluation?.depth || 18;
    return `Depth ${depth} | Stockfish 19`;
  }

  get recommendedExplanation(): string {
    if (this.activeMove?.explanation) {
      return this.activeMove.explanation;
    }
    return 'Prevents Bb5 from gaining a strong foothold. This is the main line in the Ruy López opening.';
  }

  get alternativeMoves(): AlternativeMove[] {
    const currentScoreNum = parseFloat(this.currentEvaluationScore.replace('+', '')) || -0.32;
    return [
      { san: 'Nxe4', evalText: (currentScoreNum + 0.04).toFixed(2) },
      { san: 'Be7', evalText: (currentScoreNum + 0.01).toFixed(2) },
      { san: 'd6', evalText: (currentScoreNum - 0.03).toFixed(2) }
    ];
  }

  get selectedMoveSan(): string {
    return this.activeMove?.san || this.gameBoardComponent?.currentStep?.san || 'Start';
  }

  get selectedMoveClassification(): string {
    return this.activeMove?.classification || 'Book move';
  }

  get selectedMoveNodes(): string {
    return 'Depth 18 | 156k nodes';
  }

  get selectedMoveExplanation(): string {
    if (this.activeMove?.explanation) {
      return this.activeMove.explanation;
    }
    return 'A known Ruy López continuation. Evaluation remains equal with complex middlegame play.';
  }

  get prevMoveInfo(): { san: string; evalText: string } | null {
    if (!this.analysisResult || this.activeStepIndex <= 1) {
      return { san: '3. Bb5', evalText: '-0.25' };
    }
    const prev = this.analysisResult.moves[this.activeStepIndex - 2];
    return prev ? {
      san: `${prev.moveNumber}. ${prev.san}`,
      evalText: prev.evaluation?.scoreText || '0.00'
    } : null;
  }

  get nextMoveInfo(): { san: string; evalText: string } | null {
    if (!this.analysisResult || this.activeStepIndex >= this.analysisResult.moves.length) {
      return { san: '4. Ba4', evalText: '-0.25' };
    }
    const next = this.analysisResult.moves[this.activeStepIndex];
    return next ? {
      san: `${next.moveNumber}. ${next.san}`,
      evalText: next.evaluation?.scoreText || '0.00'
    } : null;
  }

  stepToPrevMove(): void {
    if (this.gameBoardComponent) {
      this.gameBoardComponent.previousMove();
    }
  }

  stepToNextMove(): void {
    if (this.gameBoardComponent) {
      this.gameBoardComponent.nextMove();
    }
  }

  get allMoves(): MoveAnalysis[] {
    return this.analysisResult?.moves || [];
  }

  get movePairsList(): { moveNumber: number; white?: MoveAnalysis; black?: MoveAnalysis }[] {
    const moves = this.allMoves;
    const pairs: { moveNumber: number; white?: MoveAnalysis; black?: MoveAnalysis }[] = [];
    let currentPair: { moveNumber: number; white?: MoveAnalysis; black?: MoveAnalysis } | null = null;

    for (const m of moves) {
      if (m.color.toLowerCase() === 'white') {
        currentPair = { moveNumber: m.moveNumber, white: m };
        pairs.push(currentPair);
      } else {
        if (!currentPair || currentPair.moveNumber !== m.moveNumber) {
          currentPair = { moveNumber: m.moveNumber, black: m };
          pairs.push(currentPair);
        } else {
          currentPair.black = m;
        }
      }
    }
    return pairs;
  }

  goToMove(stepIndex: number): void {
    this.activeStepIndex = stepIndex;
    if (this.gameBoardComponent) {
      this.gameBoardComponent.showStep(stepIndex, true);
    }
  }

  getClassificationIcon(classification?: string): string {
    const key = (classification || '').toLowerCase();
    switch (key) {
      case 'brilliant': return this.icons['badgeBrilliant'];
      case 'best': return this.icons['badgeBest'];
      case 'book': return this.icons['badgeBook'];
      case 'inaccuracy': return this.icons['badgeInaccuracy'];
      case 'mistake': return this.icons['badgeMistake'];
      case 'blunder': return this.icons['badgeBlunder'];
      case 'great': return this.icons['badgeBest'];
      case 'excellent': return this.icons['badgeExcellent'];
      case 'good': return this.icons['badgeGood'];
      default: return this.icons['badgeBest'];
    }
  }

  getEvalClass(scoreText?: string): string {
    if (!scoreText) return '';
    if (scoreText.startsWith('+M') || scoreText.startsWith('-M') || scoreText.startsWith('#')) return 'eval-mate';
    if (scoreText === '0.00') return 'eval-neutral';
    if (scoreText.startsWith('+')) return 'eval-positive';
    if (scoreText.startsWith('-')) return 'eval-negative';
    return 'eval-neutral';
  }
}
