export interface PositionEvaluation {
  type: 'cp' | 'mate';
  centipawns?: number | null;
  mateInMoves?: number | null;
  value?: number | null;
  scoreText: string;
  isMate: boolean;
  depth: number;
  bestMoveUci?: string | null;
  bestMoveSan?: string | null;
}

export interface MoveClassificationCounts {
  brilliant: number;
  great: number;
  best: number;
  excellent: number;
  good: number;
  book: number;
  inaccuracy: number;
  mistake: number;
  miss: number;
  blunder: number;
}

export interface GameReview {
  whiteAccuracy: number;
  blackAccuracy: number;
  openingName: string;
  openingEco: string;
  summaryVerdict: string;
  whiteClassifications: MoveClassificationCounts;
  blackClassifications: MoveClassificationCounts;
}

export interface MoveAnalysis {
  ply: number;
  moveNumber: number;
  color: string;
  san: string;
  uci: string;
  fenBefore: string;
  fenAfter: string;
  sideToMove: string;
  evaluation: PositionEvaluation;
  bestMoveUci: string;
  bestMoveSan?: string;
  principalVariation?: string;
  classification?: string;
  classificationSymbol?: string;
  accuracyScore?: number;
  winPercentBefore?: number;
  winPercentAfter?: number;
  winPercentLoss?: number;
  expectedPointsBefore?: number;
  expectedPointsAfter?: number;
  expectedPointsLoss?: number;
  explanation?: string;
}

export interface GameAnalysisResult {
  message: string;
  initialEvaluation?: PositionEvaluation;
  finalEvaluation?: PositionEvaluation;
  moves: MoveAnalysis[];
  totalMoves?: number;
  suggestedNextMove?: string | null;
  review?: GameReview;
}