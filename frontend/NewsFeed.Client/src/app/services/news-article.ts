
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';

import {
  NewsArticle,
  NewsArticleApiResponse
} from '../models/news-article';

@Injectable({
  providedIn: 'root'
})
export class NewsArticleService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'http://localhost:5074/api/news';

  getAll(limit = 100): Observable<NewsArticle[]> {
    return this.http
      .get<NewsArticleApiResponse[]>(this.apiUrl, {
        params: { limit: limit.toString() }
      })
      .pipe(
        map(articles => articles.map(article => this.mapArticle(article)))
      );
  }

  getById(id: string): Observable<NewsArticle | undefined> {
    return this.getAll().pipe(
      map(articles => articles.find(article => article.id === id))
    );
  }

  private mapArticle(article: NewsArticleApiResponse): NewsArticle {
    const sourceNames: Record<string, string> = {
      'bbc.co.uk': 'BBC News',
      'bbc.com': 'BBC News',
      'rte.ie': 'RTÉ News',
      'theguardian.com': 'The Guardian',
      'irishtimes.com': 'The Irish Times'
    };

    const themes = article.themes
      ? [...new Set(
          article.themes
            .split(';')
            .filter(Boolean)
            .map(theme => theme.split(',')[0].replaceAll('_', ' '))
        )].slice(0, 3)
      : [];

    return {
      id: article.id.toString(),
      gkgRecordId: article.gkgRecordId,
      source: sourceNames[article.publisher ?? '']
        ?? article.publisher
        ?? 'Unknown publisher',
      headline: article.headline ?? 'Untitled article',
      summary: article.description ?? '',
      publishedAt: article.publishedAtUtc,
      url: article.articleUrl ?? '',
      imageUrl: article.imageUrl,
      themes,
      locations: article.locations,
      people: article.people,
      organisations: article.organisations,
      tone: article.tone
    };
  }
}
