
export interface NewsArticle {
  id: string;
  source: string;
  headline: string;
  summary: string;
  publishedAt: string;
  url: string;
  imageUrl: string | null;
  themes: string[];

  // Original GDELT intelligence metadata for article details.
  gkgRecordId: string;
  locations: string | null;
  people: string | null;
  organisations: string | null;
  tone: string | null;
}

export interface NewsArticleApiResponse {
  id: number;
  gkgRecordId: string;
  headline: string | null;
  description: string | null;
  imageUrl: string | null;
  publisher: string | null;
  publishedAtUtc: string;
  articleUrl: string | null;
  themes: string | null;
  locations: string | null;
  people: string | null;
  organisations: string | null;
  tone: string | null;
}

export interface NewsFeedApiPage {
  items: NewsArticleApiResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}