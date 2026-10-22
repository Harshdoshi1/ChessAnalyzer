import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GameAnalysisResult } from '../models/game-analysis-result';

@Injectable({
  providedIn: 'root'
})
export class GameAnalysisService {

  get apiUrl(): string {
    if (typeof window !== 'undefined') {
      const hostname = window.location.hostname;
      const isLocalhost = hostname === 'localhost' || hostname === '127.0.0.1';

      if (isLocalhost) {
        // If frontend is directly hosted on API port, use relative path
        if (window.location.port === '7155' || window.location.port === '5240') {
          return '/api/GameAnalysis';
        }
        // In local development, target the API service directly
        if (window.location.protocol === 'http:') {
          return 'http://localhost:5240/api/GameAnalysis';
        }
        return 'https://localhost:7155/api/GameAnalysis';
      }

      // In production / deployed environment, use relative URL for reverse-proxy / same-origin routing
      return '/api/GameAnalysis';
    }
    return 'https://localhost:7155/api/GameAnalysis';
  }

  constructor(private http: HttpClient) {}

  analyzeGame(pgn: string): Observable<GameAnalysisResult> {
    console.log('PGN being sent:', pgn);

    return this.http.post<GameAnalysisResult>(this.apiUrl, {
      pgn: pgn
    });
  }
}
