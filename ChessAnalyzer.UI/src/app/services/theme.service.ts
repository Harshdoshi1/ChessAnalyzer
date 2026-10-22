import { Injectable, signal } from '@angular/core';

export type Theme = 'dark' | 'light';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly storageKey = 'chess-analyzer-theme';
  readonly currentTheme = signal<Theme>(this.getInitialTheme());

  constructor() {
    this.applyTheme(this.currentTheme());
  }

  toggleTheme(): void {
    const nextTheme: Theme = this.currentTheme() === 'dark' ? 'light' : 'dark';
    this.setTheme(nextTheme);
  }

  setTheme(theme: Theme): void {
    this.currentTheme.set(theme);
    if (typeof localStorage !== 'undefined') {
      try {
        localStorage.setItem(this.storageKey, theme);
      } catch {
        // Ignore localStorage quota or privacy errors
      }
    }
    this.applyTheme(theme);
  }

  isDark(): boolean {
    return this.currentTheme() === 'dark';
  }

  private getInitialTheme(): Theme {
    if (typeof localStorage !== 'undefined') {
      try {
        const saved = localStorage.getItem(this.storageKey) as Theme | null;
        if (saved === 'dark' || saved === 'light') {
          return saved;
        }
      } catch {
        // Ignore localStorage errors
      }
    }
    return 'dark'; // Default to the reference dark forest green theme
  }

  private applyTheme(theme: Theme): void {
    if (typeof document !== 'undefined') {
      document.documentElement.setAttribute('data-theme', theme);
      if (theme === 'dark') {
        document.body.classList.add('dark-theme');
        document.body.classList.remove('light-theme');
      } else {
        document.body.classList.add('light-theme');
        document.body.classList.remove('dark-theme');
      }
    }
  }
}
