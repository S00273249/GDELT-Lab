
import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NewsArticle } from '../../models/news-article';
import { NewsArticleService } from '../../services/news-article';
import { DesignSwitcher } from '../../components/design-switcher';
import { GkgThemeService } from '../../services/gkg-theme.service';

@Component({
  selector: 'app-news-feed',
  imports: [RouterLink, DesignSwitcher],
  templateUrl: './news-feed.html',
  styleUrl: './news-feed.css'
})
export class NewsFeed implements OnInit {
  private readonly articleService = inject(NewsArticleService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly gkgThemeService = inject(GkgThemeService);

  selectedSource = 'All sources';

  sources = [
    'All sources',
    'BBC News',
    'RTÉ News',
    'The Guardian',
    'The Irish Times'
  ];

  articles: NewsArticle[] = [];

  currentPage = 1;
  pageSize = 12;
  totalCount = 0;
  totalPages = 0;

  loading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.loadPage(1);
  }

  formatTheme(theme: string): string {
    return this.gkgThemeService.formatTheme(theme);
  }

  loadPage(page: number): void {
    this.loading = true;
    this.errorMessage = '';

    this.articleService.getPage(page, this.pageSize, this.selectedSource).subscribe({
      next: response => {
        this.articles = response.items.filter(article =>
          article.url &&
          article.headline &&
          article.source
        );

        this.currentPage = response.page;
        this.totalCount = response.totalCount;
        this.totalPages = response.totalPages;
        this.loading = false;

        this.cdr.markForCheck();
      },
      error: error => {
        console.error('Failed to load news articles:', error);

        this.errorMessage =
          'Unable to load articles. Check that the News Feed API is running.';

        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  get filteredArticles(): NewsArticle[] {
    if (this.selectedSource === 'All sources') {
      return this.articles;
    }

    return this.articles.filter(
      article => article.source === this.selectedSource
    );
  }

  selectSource(source: string): void {
    this.selectedSource = source;
    this.loadPage(1);
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.loadPage(this.currentPage - 1);
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.loadPage(this.currentPage + 1);
    }
  }

  onImageError(event: Event): void {
    const image = event.target as HTMLImageElement;

    image.style.display = 'none';
    image.parentElement?.classList.add('image-fallback');
  }
}
