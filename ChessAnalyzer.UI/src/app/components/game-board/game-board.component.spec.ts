import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SimpleChange } from '@angular/core';
import { GameBoardComponent } from './game-board.component';

describe('GameBoardComponent', () => {
  let component: GameBoardComponent;
  let fixture: ComponentFixture<GameBoardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GameBoardComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(GameBoardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with starting position when PGN is empty', () => {
    component.loadGame('');
    expect(component.board.length).toBe(8);
    expect(component.board[0].length).toBe(8);
    expect(component.currentStepIndex).toBe(0);
    expect(component.totalMoves).toBe(0);
    expect(component.errorMessage).toBe('');

    // Check rank 8 has black pieces, rank 1 has white pieces
    const a8 = component.board[0][0];
    const e1 = component.board[7][4];
    expect(a8.pieceGlyph).toBe('♜');
    expect(a8.pieceColor).toBe('b');
    expect(e1.pieceGlyph).toBe('♔');
    expect(e1.pieceColor).toBe('w');
  });

  it('should parse valid moves and navigate history', () => {
    component.pgn = '1. e4 e5 2. Nf3 Nc6';
    component.ngOnChanges({
      pgn: new SimpleChange('', component.pgn, true)
    });
    fixture.detectChanges();

    expect(component.totalMoves).toBe(4);
    expect(component.currentStepIndex).toBe(4);
    expect(component.currentMoveLabel).toContain('2... Nc6');
    expect(component.movePairs.length).toBe(2);

    // Test navigation: previous
    component.previousMove();
    expect(component.currentStepIndex).toBe(3);
    expect(component.currentMoveLabel).toContain('2. Nf3');

    // Test navigation: start
    component.goToStart();
    expect(component.currentStepIndex).toBe(0);
    expect(component.currentMoveLabel).toBe('Starting position');

    // Test navigation: next
    component.nextMove();
    expect(component.currentStepIndex).toBe(1);
    expect(component.currentMoveLabel).toContain('1. e4');

    // Test navigation: end
    component.goToEnd();
    expect(component.currentStepIndex).toBe(4);
  });

  it('should highlight last move squares correctly', () => {
    component.loadGame('1. e4 e5');
    component.showStep(1); // 1. e4
    fixture.detectChanges();

    let e2Square = null;
    let e4Square = null;

    for (const row of component.board) {
      for (const sq of row) {
        if (sq.square === 'e2') e2Square = sq;
        if (sq.square === 'e4') e4Square = sq;
      }
    }

    expect(e2Square?.isLastMoveFrom).toBeTrue();
    expect(e4Square?.isLastMoveTo).toBeTrue();
  });

  it('should flip board orientation', () => {
    expect(component.isFlipped).toBeFalse();
    // Default top-left is a8
    expect(component.board[0][0].square).toBe('a8');

    component.flipBoard();
    expect(component.isFlipped).toBeTrue();
    // When flipped, top-left is h1
    expect(component.board[0][0].square).toBe('h1');
  });

  it('should handle invalid PGN gracefully with error message', () => {
    component.loadGame('invalid moves syntax 1234');
    expect(component.errorMessage).toContain('Could not parse PGN');
    expect(component.board.length).toBe(8);
  });

  it('should parse PGN headers when provided', () => {
    const pgnWithHeaders = `[Event "World Championship"]
[White "Magnus Carlsen"]
[Black "Ian Nepomniachtchi"]
[Result "1-0"]

1. e4 e5 2. Nf3 1-0`;

    component.loadGame(pgnWithHeaders);
    expect(component.whitePlayer).toBe('Magnus Carlsen');
    expect(component.blackPlayer).toBe('Ian Nepomniachtchi');
    expect(component.eventName).toBe('World Championship');
    expect(component.gameResult).toBe('1-0');
  });

  it('should display evaluations and best moves when analysisMoves are provided', () => {
    component.pgn = '1. e4 e5';
    component.analysisMoves = [
      {
        ply: 1,
        moveNumber: 1,
        color: 'White',
        san: 'e4',
        uci: 'e2e4',
        fenBefore: 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1',
        fenAfter: 'rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1',
        sideToMove: 'Black',
        evaluation: { type: 'cp', centipawns: 30, value: 0.3, scoreText: '+0.30', isMate: false, depth: 10 },
        bestMoveUci: 'e7e5',
        principalVariation: 'e7e5 g1f3'
      },
      {
        ply: 2,
        moveNumber: 1,
        color: 'Black',
        san: 'e5',
        uci: 'e7e5',
        fenBefore: 'rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1',
        fenAfter: 'rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq - 0 2',
        sideToMove: 'White',
        evaluation: { type: 'cp', centipawns: 20, value: 0.2, scoreText: '+0.20', isMate: false, depth: 10 },
        bestMoveUci: 'g1f3',
        principalVariation: 'g1f3 b8c6'
      }
    ];

    component.initialEvaluation = {
      type: 'cp',
      centipawns: 15,
      value: 0.15,
      scoreText: '+0.15',
      isMate: false,
      depth: 10
    };

    component.ngOnChanges({
      pgn: new SimpleChange('', component.pgn, true),
      analysisMoves: new SimpleChange([], component.analysisMoves, true)
    });
    fixture.detectChanges();

    // At end (step 2: 1... e5)
    expect(component.currentStepIndex).toBe(2);
    expect(component.currentEvaluation?.scoreText).toBe('+0.20');
    expect(component.currentBestMove).toBe('g1f3');
    expect(component.currentPrincipalVariation).toBe('g1f3 b8c6');

    // Check step 1: 1. e4
    component.showStep(1);
    expect(component.currentEvaluation?.scoreText).toBe('+0.30');
    expect(component.currentBestMove).toBe('e7e5');

    // Check step 0: initial position
    component.showStep(0);
    expect(component.currentEvaluation?.scoreText).toBe('+0.15');
    expect(component.currentBestMove).toBeNull();

    // Check movePairs have evalText populated
    expect(component.movePairs[0].white?.evalText).toBe('+0.30');
    expect(component.movePairs[0].black?.evalText).toBe('+0.20');
  });

  it('should return correct getEvalClass for different scores', () => {
    expect(component.getEvalClass('+0.45')).toBe('eval-positive');
    expect(component.getEvalClass('-1.20')).toBe('eval-negative');
    expect(component.getEvalClass('+M2')).toBe('eval-mate');
    expect(component.getEvalClass('-M1')).toBe('eval-mate');
    expect(component.getEvalClass('# 1-0')).toBe('eval-mate');
    expect(component.getEvalClass('0.00')).toBe('eval-neutral');
    expect(component.getEvalClass('')).toBe('');
  });

  it('should allow clicking a piece to select it and show legal moves', () => {
    component.loadGame(''); // Starting position, White to move
    fixture.detectChanges();

    // Find e2 square (rank 2, file e)
    const e2Square = component.board.flat().find(sq => sq.square === 'e2')!;
    expect(e2Square).toBeTruthy();
    expect(e2Square.pieceColor).toBe('w');

    // Click e2
    component.onSquareClick(e2Square);
    fixture.detectChanges();

    expect(component.selectedSquare).toBe('e2');
    expect(component.legalTargetSquares).toContain('e3');
    expect(component.legalTargetSquares).toContain('e4');
  });

  it('should execute move when clicking a legal target square and emit moveMade and pgnChange', () => {
    component.loadGame('');
    fixture.detectChanges();

    let emittedPgn = '';
    component.moveMade.subscribe(pgn => emittedPgn = pgn);

    const e2Square = component.board.flat().find(sq => sq.square === 'e2')!;
    const e4Square = component.board.flat().find(sq => sq.square === 'e4')!;

    // Select e2
    component.onSquareClick(e2Square);
    // Click destination e4
    component.onSquareClick(e4Square);
    fixture.detectChanges();

    expect(component.totalMoves).toBe(1);
    expect(component.currentStepIndex).toBe(1);
    expect(component.currentMoveLabel).toContain('1. e4');
    expect(emittedPgn).toBe('1. e4');
    expect(component.selectedSquare).toBeNull();
  });

  it('should execute playBestMove when currentBestMove is available', () => {
    component.loadGame('1. e4');
    component.analysisMoves = [
      {
        ply: 1,
        moveNumber: 1,
        color: 'White',
        san: 'e4',
        uci: 'e2e4',
        fenBefore: '',
        fenAfter: '',
        sideToMove: 'Black',
        evaluation: { type: 'cp', centipawns: 25, scoreText: '+0.25', isMate: false, depth: 10 },
        bestMoveUci: 'e7e5'
      }
    ];
    fixture.detectChanges();

    expect(component.currentBestMove).toBe('e7e5');

    let emittedPgn = '';
    component.moveMade.subscribe(pgn => emittedPgn = pgn);

    component.playBestMove();
    fixture.detectChanges();

    expect(component.totalMoves).toBe(2);
    expect(component.currentStepIndex).toBe(2);
    expect(component.currentMoveLabel).toContain('1... e5');
    expect(emittedPgn).toBe('1. e4 e5');
  });

  it('should detect checkmate and set isCheckmate and isMatedKing on the board', () => {
    // Scholar's mate: 1. e4 e5 2. Bc4 Nc6 3. Qh5 Nf6 4. Qxf7#
    component.loadGame('1. e4 e5 2. Bc4 Nc6 3. Qh5 Nf6 4. Qxf7#');
    fixture.detectChanges();

    expect(component.isCheckmate).toBeTrue();
    expect(component.checkmateWinner).toBe('White');

    // Black king on e8 should be marked as isMatedKing
    const e8Square = component.board.flat().find(sq => sq.square === 'e8');
    expect(e8Square?.isMatedKing).toBeTrue();
    expect(e8Square?.isCheck).toBeTrue();

    // White king on e1 should be marked as isWinningKing
    const e1Square = component.board.flat().find(sq => sq.square === 'e1');
    expect(e1Square?.isWinningKing).toBeTrue();

    // Dismiss checkmate banner
    component.dismissCheckmateBanner();
    expect(component.checkmateBannerDismissed).toBeTrue();
  });
});


