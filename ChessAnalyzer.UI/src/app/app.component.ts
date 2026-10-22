import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GameAnalysisComponent } from './features/game-analysis/game-analysis.component';
import { ThemeService } from './services/theme.service';
import { SafeHtmlPipe } from './shared/safe-html.pipe';
import { UI_ICONS } from './shared/ui-icons';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, GameAnalysisComponent, SafeHtmlPipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.sass'
})
export class AppComponent {
  title = 'ChessAnalyzer.UI';
  readonly themeService = inject(ThemeService);
  readonly icons = UI_ICONS;
  activeNavTab: 'analyze' | 'games' | 'explorer' | 'insights' = 'analyze';

  setActiveNav(tab: 'analyze' | 'games' | 'explorer' | 'insights'): void {
    this.activeNavTab = tab;
  }
}
