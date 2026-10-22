import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { GameAnalysisComponent } from './game-analysis.component';

describe('GameAnalysisComponent', () => {
  let component: GameAnalysisComponent;
  let fixture: ComponentFixture<GameAnalysisComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GameAnalysisComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GameAnalysisComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should update PGN and trigger analyzeGame when onMoveMade is called with background flag', () => {
    spyOn(component, 'analyzeGame');
    component.onMoveMade('1. e4 e5');

    expect(component.pgn).toBe('1. e4 e5');
    expect(component.analyzeGame).toHaveBeenCalledWith(true);
  });

  it('should calculate suggestedNextMove and principal variation correctly', () => {
    component.analysisResult = {
      message: 'Analysis done',
      initialEvaluation: {
        type: 'cp',
        scoreText: '+0.15',
        isMate: false,
        depth: 10,
        bestMoveUci: 'e2e4'
      },
      moves: [
        {
          ply: 1,
          moveNumber: 1,
          color: 'White',
          san: 'e4',
          uci: 'e2e4',
          fenBefore: '',
          fenAfter: '',
          sideToMove: 'Black',
          evaluation: { type: 'cp', scoreText: '+0.25', isMate: false, depth: 10 },
          bestMoveUci: 'e7e5',
          bestMoveSan: 'e5',
          principalVariation: 'e7e5 g1f3'
        },
        {
          ply: 2,
          moveNumber: 1,
          color: 'Black',
          san: 'e5',
          uci: 'e7e5',
          fenBefore: '',
          fenAfter: '',
          sideToMove: 'White',
          evaluation: { type: 'cp', scoreText: '+0.30', isMate: false, depth: 10 },
          bestMoveUci: 'g1f3',
          bestMoveSan: 'Nf3',
          principalVariation: 'g1f3 b8c6'
        }
      ]
    };

    // At step 2 (latest move: 1... e5)
    component.onStepSelected(2);
    expect(component.suggestedNextMove).toBe('g1f3');
    expect(component.suggestedNextMoveSan).toBe('Nf3');
    expect(component.suggestedNextMovePv).toBe('g1f3 b8c6');

    // Select step 1 (1. e4)
    component.onStepSelected(1);
    expect(component.suggestedNextMove).toBe('e7e5');
    expect(component.suggestedNextMoveSan).toBe('e5');
    expect(component.suggestedNextMovePv).toBe('e7e5 g1f3');

    // Select step 0 (initial position)
    component.onStepSelected(0);
    expect(component.suggestedNextMove).toBe('e2e4');
  });

  it('should preserve analysisResult during background updates to avoid page flicker', () => {
    const existingResult = {
      message: 'Previous analysis',
      moves: []
    };
    component.analysisResult = existingResult;
    component.pgn = '1. e4';

    // Background update should NOT wipe analysisResult
    component.analyzeGame(true);

    expect(component.analysisResult).toBe(existingResult);
    expect(component.isAnalyzing).toBeTrue();
    expect(component.isLoading).toBeFalse();
  });
});

