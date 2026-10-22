import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { Chess } from 'chess.js';
import { MoveAnalysis, PositionEvaluation } from '../../models/game-analysis-result';
import { CHESS_PIECE_SVGS } from '../../shared/chess-piece-svgs';
import { UI_ICONS } from '../../shared/ui-icons';
import { SafeHtmlPipe } from '../../shared/safe-html.pipe';

export interface MoveStep {
  index: number;
  moveNumber: number;
  san: string;
  from?: string;
  to?: string;
  color?: 'w' | 'b';
  piece?: string;
  captured?: string;
  fen: string;
  evalText?: string;
  classification?: string;
  classificationSymbol?: string;
}

export interface MovePairView {
  moveNumber: number;
  white?: {
    stepIndex: number;
    san: string;
    evalText?: string;
    classification?: string;
    classificationSymbol?: string;
  };
  black?: {
    stepIndex: number;
    san: string;
    evalText?: string;
    classification?: string;
    classificationSymbol?: string;
  };
}

export interface SquareInfo {
  square: string;
  file: string;
  rank: number;
  isLight: boolean;
  pieceKey: string;
  pieceGlyph: string;
  pieceColor: 'w' | 'b' | null;
  pieceType: string | null;
  isLastMoveFrom: boolean;
  isLastMoveTo: boolean;
  isSelected: boolean;
  isLegalTarget: boolean;
  isLegalCapture: boolean;
  isBestMoveFrom: boolean;
  isBestMoveTo: boolean;
  isCheck: boolean;
  isCheckmate: boolean;
  isMatedKing: boolean;
  isWinningKing: boolean;
}

export interface PieceAnimationState {
  square: string;
  deltaX: number;
  deltaY: number;
  isReverse?: boolean;
  isAnimating?: boolean;
  isTransitioning?: boolean;
  isFadeIn?: boolean;
  durationMs?: number;
  easing?: string;
}

@Component({
  selector: 'app-game-board',
  standalone: true,
  imports: [CommonModule, SafeHtmlPipe],
  templateUrl: './game-board.component.html',
  styleUrl: './game-board.component.css'
})
export class GameBoardComponent implements OnInit, OnChanges, OnDestroy {
  @Input() pgn = '';
  @Input() analysisMoves: MoveAnalysis[] = [];
  @Input() initialEvaluation?: PositionEvaluation | null = null;
  @Input() openingName = '';
  @Input() openingEco = '';
  @Input() isAnalyzing = false;
  @Input() suggestedBestMove?: string;

  @Output() pgnChange = new EventEmitter<string>();
  @Output() moveMade = new EventEmitter<string>();
  @Output() stepSelected = new EventEmitter<number>();
  @Output() runFullAnalysisClicked = new EventEmitter<void>();

  moveFxKey = 0;

  board: SquareInfo[][] = [];
  moveSteps: MoveStep[] = [];
  movePairs: MovePairView[] = [];
  currentStepIndex = 0;
  errorMessage = '';
  isFlipped = false;
  isPlaying = false;
  isRewinding = false;

  selectedSquare: string | null = null;
  legalTargetSquares: string[] = [];
  legalCaptureSquares: string[] = [];

  gameHeaders: Record<string, string> = {};
  gameResult = '';

  isCheckmate = false;
  checkmateWinner: 'White' | 'Black' | null = null;
  checkmateBannerDismissed = false;

  activeAnimation: PieceAnimationState | null = null;
  pieceAnimations: Record<string, PieceAnimationState> = {};
  capturedSquareAnimating: string | null = null;

  getPieceTransform(square: string): string {
    const anim = this.pieceAnimations[square];
    if (anim && anim.isAnimating) {
      return `translate3d(${anim.deltaX}%, ${anim.deltaY}%, 0)`;
    }
    if (this.activeAnimation && this.activeAnimation.square === square) {
      return `translate3d(${this.activeAnimation.deltaX}%, ${this.activeAnimation.deltaY}%, 0)`;
    }
    return '';
  }

  getPieceTransition(square: string): string {
    const anim = this.pieceAnimations[square];
    if (anim && anim.isTransitioning) {
      const dur = anim.durationMs ?? 200;
      const ease = anim.easing ?? 'cubic-bezier(0.25, 1, 0.5, 1)';
      return `transform ${dur}ms ${ease}`;
    }
    if (this.activeAnimation && this.activeAnimation.square === square) {
      return 'transform 200ms cubic-bezier(0.25, 1, 0.5, 1)';
    }
    return 'none';
  }

  isPieceAnimating(square: string): boolean {
    if (this.pieceAnimations[square]?.isAnimating) return true;
    return this.activeAnimation?.square === square;
  }

  isPieceFadeIn(square: string): boolean {
    return !!this.pieceAnimations[square]?.isFadeIn;
  }

  readonly icons = UI_ICONS;
  readonly pieceSvgs = CHESS_PIECE_SVGS;

  private lastLoadedPgn = '';
  private playTimer?: any;
  private readonly defaultFiles = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];
  private readonly defaultRanks = ['8', '7', '6', '5', '4', '3', '2', '1'];

  ngOnInit(): void {
    if (this.board.length === 0) {
      this.loadGame(this.pgn);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['pgn']) {
      const curr = (changes['pgn'].currentValue ?? '').trim();
      if (curr !== this.lastLoadedPgn || this.board.length === 0) {
        this.clearSelection();
        this.loadGame(changes['pgn'].currentValue ?? '');
      }
    }
    if (changes['analysisMoves']) {
      this.updateMovePairEvaluations();
      this.renderCurrentPosition();
    }
  }

  ngOnDestroy(): void {
    this.stopPlayback();
  }

  get rankLabels(): string[] {
    return this.isFlipped ? ['1', '2', '3', '4', '5', '6', '7', '8'] : this.defaultRanks;
  }

  get fileLabels(): string[] {
    return this.isFlipped ? ['h', 'g', 'f', 'e', 'd', 'c', 'b', 'a'] : this.defaultFiles;
  }

  get currentTurn(): 'w' | 'b' {
    const currentFen = this.moveSteps[this.currentStepIndex]?.fen;
    if (!currentFen) return 'w';
    const tokens = currentFen.split(' ');
    return (tokens.length > 1 && tokens[1] === 'b') ? 'b' : 'w';
  }

  get currentStep(): MoveStep | null {
    return this.moveSteps[this.currentStepIndex] ?? null;
  }

  get whitePlayer(): string {
    return this.gameHeaders['White'] || 'White Player';
  }

  get blackPlayer(): string {
    return this.gameHeaders['Black'] || 'Black Player';
  }

  get topPlayerName(): string {
    return this.isFlipped ? this.whitePlayer : this.blackPlayer;
  }

  get bottomPlayerName(): string {
    return this.isFlipped ? this.blackPlayer : this.whitePlayer;
  }

  get topPlayerSubtext(): string {
    const isBlackTurn = this.currentTurn === 'b';
    const isTopTurn = this.isFlipped ? !isBlackTurn : isBlackTurn;
    return isTopTurn ? (this.isFlipped ? 'White to move' : 'Black to move') : '';
  }

  get bottomPlayerSubtext(): string {
    const isWhiteTurn = this.currentTurn === 'w';
    const isBottomTurn = this.isFlipped ? !isWhiteTurn : isWhiteTurn;
    return isBottomTurn ? (this.isFlipped ? 'Black to move' : 'White to move') : '';
  }

  get currentMoveNavigationLabel(): string {
    if (this.currentStepIndex === 0) {
      return 'Starting position';
    }
    const step = this.currentStep;
    if (!step) return '';
    return `Move ${step.moveNumber} ... ${step.san}`;
  }

  get currentMoveLabel(): string {
    if (this.currentStepIndex === 0) {
      return 'Starting position';
    }
    const step = this.currentStep;
    if (!step) return '';
    const prefix = step.color === 'w' ? `${step.moveNumber}. ` : `${step.moveNumber}... `;
    return `${prefix}${step.san}`;
  }

  get eventName(): string {
    return this.gameHeaders['Event'] || '';
  }

  get currentPrincipalVariation(): string {
    const moveIdx = this.currentStepIndex - 1;
    return (this.analysisMoves && moveIdx >= 0 && moveIdx < this.analysisMoves.length)
      ? (this.analysisMoves[moveIdx].principalVariation || '')
      : '';
  }

  getEvalClass(scoreText?: string): string {
    if (!scoreText) return '';
    if (scoreText.startsWith('+M') || scoreText.startsWith('-M') || scoreText.startsWith('#')) return 'eval-mate';
    if (scoreText === '0.00') return 'eval-neutral';
    if (scoreText.startsWith('+')) return 'eval-positive';
    if (scoreText.startsWith('-')) return 'eval-negative';
    return 'eval-neutral';
  }

  get currentEvaluation(): PositionEvaluation | null {
    if (this.currentStepIndex === 0) {
      return this.initialEvaluation ?? null;
    }
    const moveIdx = this.currentStepIndex - 1;
    if (this.analysisMoves && moveIdx >= 0 && moveIdx < this.analysisMoves.length) {
      return this.analysisMoves[moveIdx].evaluation;
    }
    if (this.analysisMoves && this.analysisMoves.length > 0 && moveIdx >= this.analysisMoves.length) {
      return this.analysisMoves[this.analysisMoves.length - 1].evaluation;
    }
    return this.initialEvaluation ?? null;
  }

  get hasFullAnalysis(): boolean {
    return !!(this.analysisMoves && this.analysisMoves.length > 0);
  }

  get currentStepClassification(): string | null {
    if (!this.hasFullAnalysis || this.currentStepIndex <= 0) return null;
    const moveIdx = this.currentStepIndex - 1;
    if (moveIdx >= 0 && moveIdx < this.analysisMoves.length) {
      return this.analysisMoves[moveIdx].classification || null;
    }
    return this.currentStep?.classification || null;
  }

  get currentClassificationNormalized(): string {
    const c = (this.currentStepClassification || '').toLowerCase();
    if (c.includes('brilliant')) return 'brilliant';
    if (c.includes('best') || c.includes('great')) return 'best';
    if (c.includes('excellent')) return 'excellent';
    if (c.includes('good')) return 'good';
    if (c.includes('book')) return 'book';
    if (c.includes('inaccuracy')) return 'inaccuracy';
    if (c.includes('mistake')) return 'mistake';
    if (c.includes('blunder')) return 'blunder';
    return '';
  }

  getClassificationBadgeSvg(classification: string | null): string {
    if (!classification) return '';
    const lower = classification.toLowerCase();
    if (lower.includes('brilliant')) return this.icons['badgeBrilliant'];
    if (lower.includes('best') || lower.includes('great')) return this.icons['badgeBest'];
    if (lower.includes('excellent')) return this.icons['badgeExcellent'];
    if (lower.includes('good')) return this.icons['badgeGood'];
    if (lower.includes('book')) return this.icons['badgeBook'];
    if (lower.includes('inaccuracy')) return this.icons['badgeInaccuracy'];
    if (lower.includes('mistake')) return this.icons['badgeMistake'];
    if (lower.includes('blunder')) return this.icons['badgeBlunder'];
    return this.icons['badgeGood'];
  }

  get currentBestMove(): string | null {
    if (this.suggestedBestMove && this.suggestedBestMove.length >= 4) {
      return this.suggestedBestMove.toLowerCase();
    }
    if (this.currentStepIndex === 0) {
      return this.initialEvaluation?.bestMoveUci?.toLowerCase() ?? null;
    }
    const moveIdx = this.currentStepIndex - 1;
    if (this.analysisMoves && moveIdx >= 0 && moveIdx < this.analysisMoves.length && this.analysisMoves[moveIdx].bestMoveUci) {
      return this.analysisMoves[moveIdx].bestMoveUci.toLowerCase();
    }
    if (this.initialEvaluation?.bestMoveUci) {
      return this.initialEvaluation.bestMoveUci.toLowerCase();
    }
    return this.getFallbackBestMove();
  }

  private getFallbackBestMove(): string | null {
    const currentFen = this.moveSteps[this.currentStepIndex]?.fen;
    if (!currentFen) return null;
    try {
      const chess = new Chess(currentFen);
      const moves = chess.moves({ verbose: true });
      if (!moves || moves.length === 0) return null;

      const highValue = moves.find(m =>
        m.captured ||
        m.san.includes('+') ||
        ['e4', 'e5', 'd4', 'd5', 'c4', 'c5', 'Nf3', 'Nf6', 'Nc3', 'Nc6'].includes(m.san)
      );
      const chosen = highValue || moves[0];
      return chosen ? (chosen.from + chosen.to).toLowerCase() : null;
    } catch {
      return null;
    }
  }

  get totalMoves(): number {
    return Math.max(0, this.moveSteps.length - 1);
  }

  getPieceSvg(sq: SquareInfo): string {
    return sq.pieceKey ? (this.pieceSvgs[sq.pieceKey] || '') : '';
  }

  isPieceDraggable(sq: SquareInfo): boolean {
    if (this.isPlaying || this.isRewinding) return false;
    return !!sq.pieceColor && sq.pieceColor === this.currentTurn;
  }

  loadGame(rawPgn: string, animateLast = false): void {
    this.errorMessage = '';
    this.lastLoadedPgn = (rawPgn ?? '').trim();
    const trimmed = (rawPgn ?? '').trim();

    if (!trimmed) {
      this.initStartingPosition();
      return;
    }

    try {
      const chess = new Chess();
      chess.loadPgn(trimmed);

      this.gameHeaders = chess.getHeaders();
      this.gameResult = this.gameHeaders['Result'] ?? '';

      const verboseHistory = chess.history({ verbose: true });

      const initialFen = (verboseHistory.length > 0 && verboseHistory[0].before)
        ? verboseHistory[0].before
        : new Chess().fen();

      const newSteps: MoveStep[] = [
        {
          index: 0,
          moveNumber: 0,
          san: 'Start',
          fen: initialFen
        }
      ];

      const pairs: MovePairView[] = [];
      let currentPair: MovePairView | null = null;

      verboseHistory.forEach((move, i) => {
        const stepIndex = i + 1;
        const moveNumber = Math.floor(i / 2) + 1;
        const isWhite = move.color === 'w';

        newSteps.push({
          index: stepIndex,
          moveNumber,
          san: move.san,
          from: move.from,
          to: move.to,
          color: move.color,
          piece: move.piece,
          captured: move.captured,
          fen: move.after
        });

        if (isWhite) {
          currentPair = {
            moveNumber,
            white: { stepIndex, san: move.san }
          };
          pairs.push(currentPair);
        } else {
          if (currentPair && currentPair.moveNumber === moveNumber) {
            currentPair.black = { stepIndex, san: move.san };
          } else {
            pairs.push({
              moveNumber,
              black: { stepIndex, san: move.san }
            });
          }
        }
      });

      this.moveSteps = newSteps;
      this.movePairs = pairs;
      this.updateMovePairEvaluations();
      this.showStep(this.moveSteps.length - 1, animateLast);
    } catch (err: unknown) {
      const details = err instanceof Error ? err.message : 'Invalid notation';
      this.errorMessage = `Could not parse PGN: ${details}`;
      this.initStartingPosition();
    }
  }

  private updateMovePairEvaluations(): void {
    if (!this.analysisMoves || this.analysisMoves.length === 0) return;

    for (let i = 1; i < this.moveSteps.length; i++) {
      const moveIdx = i - 1;
      if (moveIdx >= 0 && moveIdx < this.analysisMoves.length) {
        const m = this.analysisMoves[moveIdx];
        this.moveSteps[i].evalText = m.evaluation?.scoreText;
        this.moveSteps[i].classification = m.classification;
        this.moveSteps[i].classificationSymbol = m.classificationSymbol;
      }
    }

    for (const pair of this.movePairs) {
      if (pair.white) {
        const whiteIdx = pair.white.stepIndex - 1;
        if (whiteIdx >= 0 && whiteIdx < this.analysisMoves.length) {
          const m = this.analysisMoves[whiteIdx];
          pair.white.evalText = m.evaluation?.scoreText;
          pair.white.classification = m.classification;
          pair.white.classificationSymbol = m.classificationSymbol;
        }
      }
      if (pair.black) {
        const blackIdx = pair.black.stepIndex - 1;
        if (blackIdx >= 0 && blackIdx < this.analysisMoves.length) {
          const m = this.analysisMoves[blackIdx];
          pair.black.evalText = m.evaluation?.scoreText;
          pair.black.classification = m.classification;
          pair.black.classificationSymbol = m.classificationSymbol;
        }
      }
    }
  }

  private initStartingPosition(): void {
    const startChess = new Chess();
    this.moveSteps = [
      {
        index: 0,
        moveNumber: 0,
        san: 'Start',
        fen: startChess.fen()
      }
    ];
    this.movePairs = [];
    this.currentStepIndex = 0;
    this.renderFen(startChess.fen(), null, null);
  }

  showStep(index: number, animate = true): void {
    if (index < 0 || index >= this.moveSteps.length) return;

    const fromIndex = this.currentStepIndex;
    this.clearSelection();
    this.currentStepIndex = index;
    this.moveFxKey++;
    const step = this.moveSteps[index];

    this.updateLastMoveHighlights(step.from ?? null, step.to ?? null);

    if (animate && Math.abs(index - fromIndex) === 1) {
      this.triggerPieceAnimation(fromIndex, index, step);
    } else {
      this.renderFen(step.fen, step.from ?? null, step.to ?? null);
    }

    this.stepSelected.emit(index);
    this.scrollTimelineToActive(index);
  }

  private updateLastMoveHighlights(from: string | null, to: string | null): void {
    if (!this.board || this.board.length === 0) return;
    for (const row of this.board) {
      for (const sq of row) {
        sq.isLastMoveFrom = !!from && sq.square === from;
        sq.isLastMoveTo = !!to && sq.square === to;
      }
    }
  }

  private triggerPieceAnimation(fromIndex: number, toIndex: number, targetStep: MoveStep, onComplete?: () => void): void {
    const isForward = toIndex > fromIndex;
    const stepToAnimate = isForward ? targetStep : this.moveSteps[fromIndex];

    if (!stepToAnimate.from || !stepToAnimate.to) {
      this.renderFen(targetStep.fen, targetStep.from ?? null, targetStep.to ?? null);
      onComplete?.();
      return;
    }

    const fromSq = stepToAnimate.from;
    const toSq = stepToAnimate.to;

    const f1 = fromSq.charCodeAt(0) - 97;
    const r1 = parseInt(fromSq[1], 10) - 1;
    const f2 = toSq.charCodeAt(0) - 97;
    const r2 = parseInt(toSq[1], 10) - 1;

    // Render target board state in DOM immediately (FLIP technique)
    this.renderFen(targetStep.fen, targetStep.from ?? null, targetStep.to ?? null);

    const targetSquare = isForward ? toSq : fromSq;
    let deltaCol = isForward ? (f1 - f2) : (f2 - f1);
    let deltaRow = isForward ? (r2 - r1) : (r1 - r2);

    if (this.isFlipped) {
      deltaCol = -deltaCol;
      deltaRow = -deltaRow;
    }

    const animDuration = 200;
    const ease = 'cubic-bezier(0.25, 1, 0.5, 1)';

    // Castling support: animate Rook alongside the King
    let castlingRookTargetSq: string | null = null;
    let rookDeltaCol = 0;
    if (stepToAnimate.piece === 'k' && Math.abs(f2 - f1) === 2) {
      const rank = fromSq[1];
      if (toSq[0] === 'g') {
        // Kingside castling
        castlingRookTargetSq = isForward ? `f${rank}` : `h${rank}`;
        rookDeltaCol = isForward ? 2 : -2;
      } else if (toSq[0] === 'c') {
        // Queenside castling
        castlingRookTargetSq = isForward ? `d${rank}` : `a${rank}`;
        rookDeltaCol = isForward ? -3 : 3;
      }
      if (this.isFlipped) {
        rookDeltaCol = -rookDeltaCol;
      }
    }

    // Frame 0: Invert position without transition
    const animState: PieceAnimationState = {
      square: targetSquare,
      deltaX: deltaCol * 100,
      deltaY: deltaRow * 100,
      isAnimating: true,
      isTransitioning: false,
      durationMs: animDuration,
      easing: ease
    };

    this.pieceAnimations = {
      [targetSquare]: animState
    };
    this.activeAnimation = animState;

    if (castlingRookTargetSq) {
      this.pieceAnimations[castlingRookTargetSq] = {
        square: castlingRookTargetSq,
        deltaX: rookDeltaCol * 100,
        deltaY: 0,
        isAnimating: true,
        isTransitioning: false,
        durationMs: animDuration,
        easing: ease
      };
    }

    if (stepToAnimate.captured && isForward) {
      this.capturedSquareAnimating = toSq;
    }

    // Frame 1: requestAnimationFrame triggers 60fps GPU transition to translate3d(0, 0, 0)
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        if (this.pieceAnimations[targetSquare]) {
          this.pieceAnimations[targetSquare].isTransitioning = true;
          this.pieceAnimations[targetSquare].deltaX = 0;
          this.pieceAnimations[targetSquare].deltaY = 0;
        }
        if (castlingRookTargetSq && this.pieceAnimations[castlingRookTargetSq]) {
          this.pieceAnimations[castlingRookTargetSq].isTransitioning = true;
          this.pieceAnimations[castlingRookTargetSq].deltaX = 0;
          this.pieceAnimations[castlingRookTargetSq].deltaY = 0;
        }
      });
    });

    setTimeout(() => {
      delete this.pieceAnimations[targetSquare];
      if (castlingRookTargetSq) {
        delete this.pieceAnimations[castlingRookTargetSq];
      }
      this.activeAnimation = null;
      this.capturedSquareAnimating = null;
      onComplete?.();
    }, animDuration + 20);
  }

  private scrollTimelineToActive(stepIndex: number): void {
    if (typeof requestAnimationFrame !== 'undefined') {
      requestAnimationFrame(() => {
        const activeElem = document.querySelector(`.timeline-pill[data-step="${stepIndex}"]`);
        activeElem?.scrollIntoView({ inline: 'center', block: 'nearest', behavior: 'smooth' });
      });
    }
  }

  // --- Replay Controls ---

  togglePlayPause(): void {
    if (this.isPlaying) {
      this.stopPlayback();
    } else {
      this.startPlayback();
    }
  }

  startPlayback(): void {
    if (this.currentStepIndex >= this.totalMoves) {
      this.showStep(0, false);
    }
    this.isPlaying = true;
    this.playTimer = setInterval(() => {
      if (this.currentStepIndex < this.totalMoves) {
        this.nextMove();
      } else {
        this.stopPlayback();
      }
    }, 600);
  }

  stopPlayback(): void {
    this.isPlaying = false;
    if (this.playTimer) {
      clearInterval(this.playTimer);
      this.playTimer = null;
    }
  }

  goToStart(): void {
    this.stopPlayback();
    if (this.currentStepIndex > 0) {
      this.startReverseReplayToStart(() => {});
    }
  }

  previousMove(): void {
    this.stopPlayback();
    if (this.currentStepIndex > 0) {
      this.showStep(this.currentStepIndex - 1, true);
    }
  }

  nextMove(): void {
    if (this.currentStepIndex < this.moveSteps.length - 1) {
      this.showStep(this.currentStepIndex + 1, true);
    }
  }

  goToEnd(): void {
    this.stopPlayback();
    this.showStep(this.moveSteps.length - 1, false);
  }

  flipBoard(): void {
    this.clearSelection();
    this.isFlipped = !this.isFlipped;
    this.renderCurrentPosition();
  }

  dismissCheckmateBanner(): void {
    this.checkmateBannerDismissed = true;
  }

  /**
   * Reverse Replay back to starting position:
   * Silky smooth 60 FPS GPU-accelerated transition gliding all pieces back to starting squares.
   */
  startReverseReplayToStart(onComplete: () => void): void {
    this.stopPlayback();
    if (this.currentStepIndex === 0) {
      onComplete();
      return;
    }

    this.isRewinding = true;

    // 1. Snapshot all pieces currently on the board
    const endPieces: { square: string; color: 'w' | 'b'; type: string; used?: boolean }[] = [];
    for (const row of this.board) {
      for (const sq of row) {
        if (sq.pieceKey && sq.pieceColor && sq.pieceType) {
          endPieces.push({ square: sq.square, color: sq.pieceColor, type: sq.pieceType.toLowerCase(), used: false });
        }
      }
    }

    // 2. Default home squares in starting chess position
    const homeSquares: { square: string; color: 'w' | 'b'; type: string }[] = [
      { square: 'a1', color: 'w', type: 'r' }, { square: 'b1', color: 'w', type: 'n' },
      { square: 'c1', color: 'w', type: 'b' }, { square: 'd1', color: 'w', type: 'q' },
      { square: 'e1', color: 'w', type: 'k' }, { square: 'f1', color: 'w', type: 'b' },
      { square: 'g1', color: 'w', type: 'n' }, { square: 'h1', color: 'w', type: 'r' },
      { square: 'a2', color: 'w', type: 'p' }, { square: 'b2', color: 'w', type: 'p' },
      { square: 'c2', color: 'w', type: 'p' }, { square: 'd2', color: 'w', type: 'p' },
      { square: 'e2', color: 'w', type: 'p' }, { square: 'f2', color: 'w', type: 'p' },
      { square: 'g2', color: 'w', type: 'p' }, { square: 'h2', color: 'w', type: 'p' },
      { square: 'a7', color: 'b', type: 'p' }, { square: 'b7', color: 'b', type: 'p' },
      { square: 'c7', color: 'b', type: 'p' }, { square: 'd7', color: 'b', type: 'p' },
      { square: 'e7', color: 'b', type: 'p' }, { square: 'f7', color: 'b', type: 'p' },
      { square: 'g7', color: 'b', type: 'p' }, { square: 'h7', color: 'b', type: 'p' },
      { square: 'a8', color: 'b', type: 'r' }, { square: 'b8', color: 'b', type: 'n' },
      { square: 'c8', color: 'b', type: 'b' }, { square: 'd8', color: 'b', type: 'q' },
      { square: 'e8', color: 'b', type: 'k' }, { square: 'f8', color: 'b', type: 'b' },
      { square: 'g8', color: 'b', type: 'n' }, { square: 'h8', color: 'b', type: 'r' },
    ];

    // 3. Render starting position FEN immediately
    const startStep = this.moveSteps[0];
    this.currentStepIndex = 0;
    this.moveFxKey++;
    this.clearSelection();
    this.renderFen(startStep.fen, null, null);
    this.updateLastMoveHighlights(null, null);

    const animDuration = 520;
    const ease = 'cubic-bezier(0.22, 1, 0.36, 1)';
    const newAnims: Record<string, PieceAnimationState> = {};

    // 4. Pair starting home square with corresponding end piece
    for (const home of homeSquares) {
      let match = endPieces.find(p => !p.used && p.color === home.color && p.type === home.type && p.square === home.square);
      if (!match) {
        match = endPieces.find(p => !p.used && p.color === home.color && p.type === home.type);
      }

      if (match) {
        match.used = true;
        const fEnd = match.square.charCodeAt(0) - 97;
        const rEnd = parseInt(match.square[1], 10) - 1;
        const fHome = home.square.charCodeAt(0) - 97;
        const rHome = parseInt(home.square[1], 10) - 1;

        let deltaCol = (fEnd - fHome);
        let deltaRow = (rHome - rEnd);

        if (this.isFlipped) {
          deltaCol = -deltaCol;
          deltaRow = -deltaRow;
        }

        newAnims[home.square] = {
          square: home.square,
          deltaX: deltaCol * 100,
          deltaY: deltaRow * 100,
          isAnimating: true,
          isTransitioning: false,
          durationMs: animDuration,
          easing: ease
        };
      } else {
        newAnims[home.square] = {
          square: home.square,
          deltaX: 0,
          deltaY: 0,
          isAnimating: true,
          isTransitioning: false,
          isFadeIn: true,
          durationMs: animDuration
        };
      }
    }

    this.pieceAnimations = newAnims;

    // 5. In next RAF, trigger transition to delta(0, 0)
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        for (const sq of Object.keys(this.pieceAnimations)) {
          if (!this.pieceAnimations[sq].isFadeIn) {
            this.pieceAnimations[sq].isTransitioning = true;
            this.pieceAnimations[sq].deltaX = 0;
            this.pieceAnimations[sq].deltaY = 0;
          }
        }
      });
    });

    // 6. Complete animation cleanly
    setTimeout(() => {
      this.pieceAnimations = {};
      this.activeAnimation = null;
      this.isRewinding = false;
      this.stepSelected.emit(0);
      this.scrollTimelineToActive(0);
      onComplete();
    }, animDuration + 30);
  }

  // --- Interactive Move Handling ---

  onSquareClick(sq: SquareInfo): void {
    if (this.isPlaying || this.isRewinding) return;

    if (this.selectedSquare) {
      if (this.selectedSquare === sq.square) {
        this.clearSelection();
        this.renderCurrentPosition();
        return;
      }

      if (this.legalTargetSquares.includes(sq.square)) {
        const from = this.selectedSquare;
        this.clearSelection();
        this.executeMove(from, sq.square);
        return;
      }

      if (sq.pieceColor === this.currentTurn) {
        this.selectSquare(sq.square);
        return;
      }

      this.clearSelection();
      this.renderCurrentPosition();
      return;
    }

    if (sq.pieceColor === this.currentTurn) {
      this.selectSquare(sq.square);
    }
  }

  onDragStart(event: DragEvent, sq: SquareInfo): void {
    if (!this.isPieceDraggable(sq)) {
      event.preventDefault();
      return;
    }
    this.selectSquare(sq.square);
    if (event.dataTransfer) {
      event.dataTransfer.setData('text/plain', sq.square);
      event.dataTransfer.effectAllowed = 'move';
    }
  }

  onDragOver(event: DragEvent, sq: SquareInfo): void {
    if (this.selectedSquare) {
      event.preventDefault();
      if (event.dataTransfer) {
        event.dataTransfer.dropEffect = 'move';
      }
    }
  }

  onDrop(event: DragEvent, sq: SquareInfo): void {
    event.preventDefault();
    if (this.selectedSquare && this.legalTargetSquares.includes(sq.square)) {
      const from = this.selectedSquare;
      this.clearSelection();
      this.executeMove(from, sq.square, false);
    } else {
      this.clearSelection();
      this.renderCurrentPosition();
    }
  }

  onDragEnd(): void {
    this.clearSelection();
    this.renderCurrentPosition();
  }

  private selectSquare(square: string): void {
    this.selectedSquare = square;
    const currentFen = this.moveSteps[this.currentStepIndex]?.fen;
    if (currentFen) {
      try {
        const chess = new Chess(currentFen);
        const moves = chess.moves({ square: square as any, verbose: true });
        this.legalTargetSquares = moves.map(m => m.to);
        this.legalCaptureSquares = moves.filter(m => !!m.captured).map(m => m.to);
      } catch {
        this.legalTargetSquares = [];
        this.legalCaptureSquares = [];
      }
    } else {
      this.legalTargetSquares = [];
      this.legalCaptureSquares = [];
    }
    this.renderCurrentPosition();
  }

  clearSelection(): void {
    this.selectedSquare = null;
    this.legalTargetSquares = [];
    this.legalCaptureSquares = [];
  }

  executeMove(from: string, to: string, animate = true): void {
    this.errorMessage = '';
    const currentFen = this.moveSteps[this.currentStepIndex]?.fen;
    if (!currentFen) return;

    try {
      const validator = new Chess(currentFen);
      const moveResult = validator.move({ from, to, promotion: 'q' });
      if (!moveResult) return;

      const gameChess = new Chess();
      for (let i = 1; i <= this.currentStepIndex; i++) {
        gameChess.move(this.moveSteps[i].san);
      }
      gameChess.move({ from, to, promotion: 'q' });

      let updatedPgn: string;
      const originalHasHeaders = (this.pgn || '').trim().startsWith('[');
      if (originalHasHeaders) {
        for (const [key, value] of Object.entries(this.gameHeaders)) {
          if (key !== 'Result') {
            gameChess.setHeader(key, value);
          }
        }
        updatedPgn = gameChess.pgn();
      } else {
        updatedPgn = this.formatMoveOnlyPgn(gameChess.history());
      }

      this.clearSelection();
      this.lastLoadedPgn = updatedPgn.trim();
      this.loadGame(updatedPgn, animate);
      this.pgn = updatedPgn;
      this.pgnChange.emit(updatedPgn);
      this.moveMade.emit(updatedPgn);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Invalid move';
      this.errorMessage = `Cannot make move: ${msg}`;
    }
  }

  playMoveFromEngine(moveUci?: string): void {
    const move = moveUci || this.currentBestMove;
    if (!move || move.length < 4) return;
    const from = move.substring(0, 2);
    const to = move.substring(2, 4);
    this.executeMove(from, to);
  }

  playBestMove(): void {
    this.playMoveFromEngine(this.currentBestMove ?? undefined);
  }

  formatMoveOnlyPgn(history: string[]): string {
    let result = '';
    for (let i = 0; i < history.length; i++) {
      if (i % 2 === 0) {
        const moveNum = Math.floor(i / 2) + 1;
        result += `${moveNum}. ${history[i]} `;
      } else {
        result += `${history[i]} `;
      }
    }
    return result.trim();
  }

  private renderCurrentPosition(): void {
    const step = this.moveSteps[this.currentStepIndex];
    if (step) {
      this.renderFen(step.fen, step.from ?? null, step.to ?? null);
    }
  }

  private renderFen(fen: string, lastFrom: string | null, lastTo: string | null): void {
    const positionChess = new Chess(fen);
    const boardArray = positionChess.board();
    const inCheck = positionChess.inCheck();
    const isCheckmate = positionChess.isCheckmate();
    const turn = positionChess.turn();

    this.isCheckmate = isCheckmate;
    if (isCheckmate) {
      this.checkmateWinner = turn === 'w' ? 'Black' : 'White';
      this.checkmateBannerDismissed = false;
    } else {
      this.checkmateWinner = null;
    }

    const bestMove = this.currentBestMove;
    const bestMoveFrom = (bestMove && bestMove.length >= 4) ? bestMove.substring(0, 2) : null;
    const bestMoveTo = (bestMove && bestMove.length >= 4) ? bestMove.substring(2, 4) : null;

    const rankIndices = this.isFlipped ? [7, 6, 5, 4, 3, 2, 1, 0] : [0, 1, 2, 3, 4, 5, 6, 7];
    const fileIndices = this.isFlipped ? [7, 6, 5, 4, 3, 2, 1, 0] : [0, 1, 2, 3, 4, 5, 6, 7];

    const rendered: SquareInfo[][] = [];

    for (let rIdx = 0; rIdx < 8; rIdx++) {
      const r = rankIndices[rIdx];
      const rankNum = 8 - r;
      const rowSquares: SquareInfo[] = [];

      for (let cIdx = 0; cIdx < 8; cIdx++) {
        const c = fileIndices[cIdx];
        const fileChar = this.defaultFiles[c];
        const squareName = `${fileChar}${rankNum}`;

        const piece = boardArray[r][c];
        const pieceKey = piece ? (piece.color === 'w' ? piece.type.toUpperCase() : piece.type) : '';
        const glyphMap: Record<string, string> = {
          'R': '♖', 'N': '♘', 'B': '♗', 'Q': '♕', 'K': '♔', 'P': '♙',
          'r': '♜', 'n': '♞', 'b': '♝', 'q': '♛', 'k': '♚', 'p': '♟'
        };
        const pieceGlyph = pieceKey ? (glyphMap[pieceKey] || '') : '';
        const isKingInCheck = inCheck && piece && piece.type === 'k' && piece.color === turn;
        const isMatedKing = isCheckmate && piece && piece.type === 'k' && piece.color === turn;
        const isWinningKing = isCheckmate && piece && piece.type === 'k' && piece.color !== turn;

        const isLight = (r + c) % 2 === 0;

        const isBestMoveFrom = !!bestMoveFrom && bestMoveFrom === squareName;
        const isBestMoveTo = !!bestMoveTo && bestMoveTo === squareName;

        rowSquares.push({
          square: squareName,
          file: fileChar,
          rank: rankNum,
          isLight,
          pieceKey,
          pieceGlyph,
          pieceColor: piece ? piece.color : null,
          pieceType: piece ? piece.type : null,
          isLastMoveFrom: lastFrom === squareName,
          isLastMoveTo: lastTo === squareName,
          isSelected: this.selectedSquare === squareName,
          isLegalTarget: this.legalTargetSquares.includes(squareName),
          isLegalCapture: this.legalCaptureSquares.includes(squareName),
          isBestMoveFrom,
          isBestMoveTo,
          isCheck: !!isKingInCheck,
          isCheckmate: isCheckmate,
          isMatedKing: !!isMatedKing,
          isWinningKing: !!isWinningKing
        });
      }

      rendered.push(rowSquares);
    }

    this.board = rendered;
  }

  @HostListener('window:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent): void {
    const targetTag = (event.target as HTMLElement)?.tagName?.toLowerCase();
    if (targetTag === 'input' || targetTag === 'textarea') return;

    if (event.key === 'ArrowLeft') {
      this.previousMove();
      event.preventDefault();
    } else if (event.key === 'ArrowRight') {
      this.nextMove();
      event.preventDefault();
    } else if (event.key === 'Home') {
      this.goToStart();
      event.preventDefault();
    } else if (event.key === 'End') {
      this.goToEnd();
      event.preventDefault();
    } else if (event.key === ' ') {
      this.togglePlayPause();
      event.preventDefault();
    }
  }
}
