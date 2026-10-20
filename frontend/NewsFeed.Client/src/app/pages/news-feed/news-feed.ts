
import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NewsArticle } from '../../models/news-article';
import { NewsArticleService } from '../../services/news-article';

@Component({
  selector: 'app-news-feed',
  imports: [RouterLink],
  templateUrl: './news-feed.html',
  styleUrl: './news-feed.css'
})
export class NewsFeed implements OnInit {
  private readonly articleService = inject(NewsArticleService);
  private readonly cdr = inject(ChangeDetectorRef);

  selectedSource = 'All sources';

  sources = [
    'All sources',
    'BBC News',
    'RTÉ News',
    'The Guardian',
    'The Irish Times'
  ];

  articles: NewsArticle[] = [];
  loading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.articleService.getAll().subscribe({
      next: articles => {
        this.articles = articles.filter(article =>
          article.url &&
          article.headline &&
          article.source
        );

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
  }
}
