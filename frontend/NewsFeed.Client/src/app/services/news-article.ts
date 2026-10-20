
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { map, Observable, catchError, of, throwError } from 'rxjs';
import { NewsArticle, NewsArticleApiResponse, NewsFeedApiPage } from '../models/news-article';

export interface NewsArticlePage {
  items: NewsArticle[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class NewsArticleService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'http://localhost:5074/api/news';

  getPage(page = 1, pageSize = 12): Observable<NewsArticlePage> {
    return this.http
      .get<NewsFeedApiPage>(this.apiUrl, {
        params: {
          page: page.toString(),
          pageSize: pageSize.toString()
        }
      })
      .pipe(
        map(response => ({
          items: response.items.map(article => this.mapArticle(article)),
          page: response.page,
          pageSize: response.pageSize,
          totalCount: response.totalCount,
          totalPages: response.totalPages
        }))
      );
  }

  // Keeps existing callers working while returning the first page.
  getAll(pageSize = 12): Observable<NewsArticle[]> {
    return this.getPage(1, pageSize).pipe(
      map(response => response.items)
    );
  }


  getById(id: string): Observable<NewsArticle | undefined> {
    return this.http
      .get<NewsArticleApiResponse>(`${this.apiUrl}/${id}`)
      .pipe(
        map(article => this.mapArticle(article)),
        catchError((error: HttpErrorResponse) => {
          if (error.status === 404) {
            return of(undefined);
          }

          return throwError(() => error);
        })
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
