/**
 * Standard Vector SVG Chess Pieces with authentic Staunton styling and drop shadows
 */
export const CHESS_PIECE_SVGS: Record<string, string> = {
  // White Pieces
  P: `<svg viewBox="0 0 45 45" class="chess-piece-svg white-piece-svg">
        <path d="M22.5 9c-2.21 0-4 1.79-4 4 0 .89.29 1.71.78 2.38C17.33 16.5 16 18.59 16 21c0 2.03.94 3.84 2.41 5.03-3 1.06-7.41 5.55-7.41 13.47h23c0-7.92-4.41-12.41-7.41-13.47 1.47-1.19 2.41-3 2.41-5.03 0-2.41-1.33-4.5-3.28-5.62.49-.67.78-1.49.78-2.38 0-2.21-1.79-4-4-4z" fill="#FFFFFF" stroke="#17251E" stroke-width="1.5" stroke-linecap="round"/>
      </svg>`,
  N: `<svg viewBox="0 0 45 45" class="chess-piece-svg white-piece-svg">
        <path d="M22 10c10.5 1 16.5 8 16 29H15c0-9 10-6.5 8-21" fill="#FFFFFF" stroke="#17251E" stroke-width="1.5"/>
        <path d="M24 18c.38 2.91-5.55 7.37-8 9-3 2-2.82 4.34-5 4-1.042-.94 1.41-3.04 0-3-1 0 .19 1.23-1 2-1 0-4.003 1-4-4 0-2 6-12 6-12s1.89-1.9 2-3.5c-.73-.994-.5-2-.5-3 1-1 3 2.5 3 2.5h2s.78-1.992 2.5-3c1 0 1 3 1 3" fill="#FFFFFF" stroke="#17251E" stroke-width="1.5"/>
        <circle cx="9.5" cy="25.5" r="1" fill="#17251E"/>
        <path d="M15 15.5c1.5.5 3 0 3-1" stroke="#17251E" stroke-width="1.5" fill="none"/>
      </svg>`,
  B: `<svg viewBox="0 0 45 45" class="chess-piece-svg white-piece-svg">
        <g fill="none" fill-rule="evenodd" stroke="#17251E" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <g fill="#FFFFFF" stroke-linecap="butt">
            <path d="M9 36c3.39-.97 10.11.43 13.5-2 3.39 2.43 10.11 1.03 13.5 2 0 0 1.65.54 3 2-.68.97-1.65.99-3 .5-3.39-.97-10.11.46-13.5-1-3.39 1.46-10.11.03-13.5 1-1.354.49-2.323.47-3-.5 1.354-1.94 3-2 3-2z"/>
            <path d="M15 32c2.5 2.5 12.5 2.5 15 0 .5-1.5 0-2 0-2 0-2.5-2.5-4-2.5-4 5.5-1.5 6-11.5-5-15.5-11 4-10.5 14-5 15.5 0 0-2.5 1.5-2.5 4 0 0-.5.5 0 2z"/>
            <path d="M25 8a2.5 2.5 0 1 1-5 0 2.5 2.5 0 1 1 5 0z"/>
          </g>
          <path d="M17.5 26h10M15 30h15M22.5 15.5v5M20 18h5"/>
        </g>
      </svg>`,
  R: `<svg viewBox="0 0 45 45" class="chess-piece-svg white-piece-svg">
        <g fill="#FFFFFF" fill-rule="evenodd" stroke="#17251E" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M9 39h27v-3H9v3zM12 36v-4h21v4H12zM11 14V9h4v2h5V9h5v2h5V9h4v5" stroke-linecap="butt"/>
          <path d="M34 14l-3 3H14l-3-3"/>
          <path d="M31 17v12.5H14V17" stroke-linecap="butt" stroke-linejoin="miter"/>
          <path d="M31 29.5l1.5 2.5h-20l1.5-2.5"/>
          <path d="M11 14h23"/>
        </g>
      </svg>`,
  Q: `<svg viewBox="0 0 45 45" class="chess-piece-svg white-piece-svg">
        <g fill="#FFFFFF" fill-rule="evenodd" stroke="#17251E" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M9 26c8.5-1.5 21-1.5 27 0l2-12-7 11-4-15-4.5 15-4.5-15-4 15-7-11 2 12z"/>
          <path d="M9 26c0 2 1.5 2 2.5 4 1 1.5 1 1 .5 3.5-1.5 1-1.5 2.5-1.5 2.5-1.5 1.5.5 2.5.5 2.5 6.5 1 16.5 1 23 0 0 0 2-1 .5-2.5 0 0 0-1.5-1.5-2.5-.5-2.5-.5-2 .5-3.5 1-2 2.5-2 2.5-4-8.5-1.5-18.5-1.5-27 0z"/>
          <circle cx="6" cy="12" r="2"/>
          <circle cx="14" cy="9" r="2"/>
          <circle cx="22.5" cy="8" r="2"/>
          <circle cx="31" cy="9" r="2"/>
          <circle cx="39" cy="12" r="2"/>
        </g>
      </svg>`,
  K: `<svg viewBox="0 0 45 45" class="chess-piece-svg white-piece-svg">
        <g fill="none" fill-rule="evenodd" stroke="#17251E" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M22.5 11.63V6M20 8h5" stroke-linejoin="miter"/>
          <path d="M22.5 25s4.5-7.5 3-10.5c0 0-1-2.5-3-2.5s-3 2.5-3 2.5c-1.5 3 3 10.5 3 10.5" fill="#FFFFFF" stroke-linecap="butt" stroke-linejoin="miter"/>
          <path d="M11.5 37c5.5 3.5 15.5 3.5 21 0v-7s9-4.5 6-10.5c-4-6.5-13.5-3.5-16 4V23v-2.5c-2.5-7.5-12-10.5-16-4-3 6 6 10.5 6 10.5v7z" fill="#FFFFFF"/>
          <path d="M11.5 30c5.5-3 15.5-3 21 0M11.5 33.5c5.5-3 15.5-3 21 0M11.5 37c5.5-3 15.5-3 21 0"/>
        </g>
      </svg>`,

  // Black Pieces
  p: `<svg viewBox="0 0 45 45" class="chess-piece-svg black-piece-svg">
        <path d="M22.5 9c-2.21 0-4 1.79-4 4 0 .89.29 1.71.78 2.38C17.33 16.5 16 18.59 16 21c0 2.03.94 3.84 2.41 5.03-3 1.06-7.41 5.55-7.41 13.47h23c0-7.92-4.41-12.41-7.41-13.47 1.47-1.19 2.41-3 2.41-5.03 0-2.41-1.33-4.5-3.28-5.62.49-.67.78-1.49.78-2.38 0-2.21-1.79-4-4-4z" fill="#171C1A" stroke="#000000" stroke-width="1.5" stroke-linecap="round"/>
        <path d="M22.5 10c-1.65 0-3 1.35-3 3 0 .7.24 1.34.64 1.85.3.38.71.68 1.18.86-.4.43-.72.95-.92 1.54C19.78 18.9 20.9 20 22.5 20s2.72-1.1 2.1-2.75c-.2-.59-.52-1.11-.92-1.54.47-.18.88-.48 1.18-.86.4-.51.64-1.15.64-1.85 0-1.65-1.35-3-3-3z" fill="#2E3B35" opacity="0.35"/>
      </svg>`,
  n: `<svg viewBox="0 0 45 45" class="chess-piece-svg black-piece-svg">
        <path d="M22 10c10.5 1 16.5 8 16 29H15c0-9 10-6.5 8-21" fill="#171C1A" stroke="#000000" stroke-width="1.5"/>
        <path d="M24 18c.38 2.91-5.55 7.37-8 9-3 2-2.82 4.34-5 4-1.042-.94 1.41-3.04 0-3-1 0 .19 1.23-1 2-1 0-4.003 1-4-4 0-2 6-12 6-12s1.89-1.9 2-3.5c-.73-.994-.5-2-.5-3 1-1 3 2.5 3 2.5h2s.78-1.992 2.5-3c1 0 1 3 1 3" fill="#171C1A" stroke="#000000" stroke-width="1.5"/>
        <circle cx="9.5" cy="25.5" r="1" fill="#D7D9BC"/>
        <path d="M15 15.5c1.5.5 3 0 3-1" stroke="#D7D9BC" stroke-width="1.2" fill="none"/>
      </svg>`,
  b: `<svg viewBox="0 0 45 45" class="chess-piece-svg black-piece-svg">
        <g fill="none" fill-rule="evenodd" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <g fill="#171C1A" stroke-linecap="butt">
            <path d="M9 36c3.39-.97 10.11.43 13.5-2 3.39 2.43 10.11 1.03 13.5 2 0 0 1.65.54 3 2-.68.97-1.65.99-3 .5-3.39-.97-10.11.46-13.5-1-3.39 1.46-10.11.03-13.5 1-1.354.49-2.323.47-3-.5 1.354-1.94 3-2 3-2z"/>
            <path d="M15 32c2.5 2.5 12.5 2.5 15 0 .5-1.5 0-2 0-2 0-2.5-2.5-4-2.5-4 5.5-1.5 6-11.5-5-15.5-11 4-10.5 14-5 15.5 0 0-2.5 1.5-2.5 4 0 0-.5.5 0 2z"/>
            <path d="M25 8a2.5 2.5 0 1 1-5 0 2.5 2.5 0 1 1 5 0z"/>
          </g>
          <path d="M17.5 26h10M15 30h15M22.5 15.5v5M20 18h5" stroke="#D7D9BC" stroke-width="1.2"/>
        </g>
      </svg>`,
  r: `<svg viewBox="0 0 45 45" class="chess-piece-svg black-piece-svg">
        <g fill="#171C1A" fill-rule="evenodd" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M9 39h27v-3H9v3zM12 36v-4h21v4H12zM11 14V9h4v2h5V9h5v2h5V9h4v5" stroke-linecap="butt"/>
          <path d="M34 14l-3 3H14l-3-3"/>
          <path d="M31 17v12.5H14V17" stroke-linecap="butt" stroke-linejoin="miter"/>
          <path d="M31 29.5l1.5 2.5h-20l1.5-2.5"/>
          <path d="M11 14h23"/>
          <path d="M14 20h17M14 25h17" stroke="#365D4B" stroke-width="1"/>
        </g>
      </svg>`,
  q: `<svg viewBox="0 0 45 45" class="chess-piece-svg black-piece-svg">
        <g fill="#171C1A" fill-rule="evenodd" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M9 26c8.5-1.5 21-1.5 27 0l2-12-7 11-4-15-4.5 15-4.5-15-4 15-7-11 2 12z"/>
          <path d="M9 26c0 2 1.5 2 2.5 4 1 1.5 1 1 .5 3.5-1.5 1-1.5 2.5-1.5 2.5-1.5 1.5.5 2.5.5 2.5 6.5 1 16.5 1 23 0 0 0 2-1 .5-2.5 0 0 0-1.5-1.5-2.5-.5-2.5-.5-2 .5-3.5 1-2 2.5-2 2.5-4-8.5-1.5-18.5-1.5-27 0z"/>
          <circle cx="6" cy="12" r="2"/>
          <circle cx="14" cy="9" r="2"/>
          <circle cx="22.5" cy="8" r="2"/>
          <circle cx="31" cy="9" r="2"/>
          <circle cx="39" cy="12" r="2"/>
          <path d="M11.5 30c5.5-1.5 16.5-1.5 22 0" stroke="#D7D9BC" stroke-width="1.2" fill="none"/>
        </g>
      </svg>`,
  k: `<svg viewBox="0 0 45 45" class="chess-piece-svg black-piece-svg">
        <g fill="none" fill-rule="evenodd" stroke="#000000" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M22.5 11.63V6M20 8h5" stroke-linejoin="miter"/>
          <path d="M22.5 25s4.5-7.5 3-10.5c0 0-1-2.5-3-2.5s-3 2.5-3 2.5c-1.5 3 3 10.5 3 10.5" fill="#171C1A" stroke-linecap="butt" stroke-linejoin="miter"/>
          <path d="M11.5 37c5.5 3.5 15.5 3.5 21 0v-7s9-4.5 6-10.5c-4-6.5-13.5-3.5-16 4V23v-2.5c-2.5-7.5-12-10.5-16-4-3 6 6 10.5 6 10.5v7z" fill="#171C1A"/>
          <path d="M11.5 30c5.5-3 15.5-3 21 0M11.5 33.5c5.5-3 15.5-3 21 0M11.5 37c5.5-3 15.5-3 21 0" stroke="#D7D9BC" stroke-width="1.2"/>
        </g>
      </svg>`
};
