import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { GameAnalysisService } from './game-analysis.service';

describe('GameAnalysisService', () => {
  let service: GameAnalysisService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(GameAnalysisService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
