
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NewsArticle } from '../../models/news-article';
import { NewsArticleService } from '../../services/news-article';

@Component({
  selector: 'app-news-feed',
  imports: [RouterLink],
  templateUrl: './news-feed.html',
  styleUrl: './news-feed.css'
})
export class NewsFeed {
  private readonly articleService = inject(NewsArticleService);

  selectedSource = 'All sources';

  sources = ['All sources', 'BBC', 'RTÉ', 'The Guardian'];

  articles: NewsArticle[] = this.articleService.getAll();

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
