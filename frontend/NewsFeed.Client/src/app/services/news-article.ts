
import { Injectable } from '@angular/core';
import { NewsArticle } from '../models/news-article';

@Injectable({
  providedIn: 'root'
})
export class NewsArticleService {
  private readonly articles: NewsArticle[] = [
    {
      id: '1',
      source: 'BBC',
      headline: 'Global economic outlook faces renewed uncertainty',
      summary: 'Governments and financial institutions assess changing economic conditions and their potential international impact.',
      publishedAt: '20 October 2026',
      url: 'https://www.bbc.com/news',
      themes: ['ECONOMY', 'GOVERNMENT']
    },
    {
      id: '2',
      source: 'RTÉ',
      headline: 'European leaders discuss new policy priorities',
      summary: 'European officials consider shared policy challenges and possible responses across member states.',
      publishedAt: '20 October 2026',
      url: 'https://www.rte.ie/news/',
      themes: ['EUROPE', 'POLITICS']
    },
    {
      id: '3',
      source: 'The Guardian',
      headline: 'International climate talks focus on future commitments',
      summary: 'Negotiators debate environmental targets, international cooperation and the implementation of climate policies.',
      publishedAt: '19 October 2026',
      url: 'https://www.theguardian.com/international',
      themes: ['CLIMATE', 'ENVIRONMENT']
    },
    {
      id: '4',
      source: 'BBC',
      headline: 'Technology policy returns to the international agenda',
      summary: 'Policymakers examine the opportunities and challenges associated with emerging technologies.',
      publishedAt: '19 October 2026',
      url: 'https://www.bbc.com/news',
      themes: ['TECHNOLOGY', 'POLITICS']
    }
  ];

  getAll(): NewsArticle[] {
    return this.articles;
  }

  getById(id: string): NewsArticle | undefined {
    return this.articles.find(article => article.id === id);
  }
}
