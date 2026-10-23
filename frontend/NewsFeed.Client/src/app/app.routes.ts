
import { Routes } from '@angular/router';

import { NewsFeed } from './pages/news-feed/news-feed';
import { ArticleDetails } from './pages/article-details/article-details';

import { NewsFeedB } from './pages/news-feed-b/news-feed';
import { ArticleDetailsB } from './pages/article-details-b/article-details';

import { NewsFeedC } from './pages/news-feed-c/news-feed';
import { ArticleDetailsC } from './pages/article-details-c/article-details';

import { NewsFeedD } from './pages/news-feed-d/news-feed';
import { ArticleDetailsD } from './pages/article-details-d/article-details';

import { NewsFeedE } from './pages/news-feed-e/news-feed';
import { ArticleDetailsE } from './pages/article-details-e/article-details';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'feed/a',
    pathMatch: 'full'
  },
  {
    path: 'feed/a',
    component: NewsFeed
  },
  {
    path: 'article/a/:id',
    component: ArticleDetails
  },
  {
    path: 'feed/b',
    component: NewsFeedB
  },
  {
    path: 'article/b/:id',
    component: ArticleDetailsB
  },
  {
    path: 'feed/c',
    component: NewsFeedC
  },
  {
    path: 'article/c/:id',
    component: ArticleDetailsC
  },
  {
    path: 'feed/d',
    component: NewsFeedD
  },
  {
    path: 'article/d/:id',
    component: ArticleDetailsD
  },
  {
    path: 'feed/e',
    component: NewsFeedE
  },
  {
    path: 'article/e/:id',
    component: ArticleDetailsE
  },
  {
    path: 'article/:id',
    redirectTo: 'article/a/:id',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: 'feed/a'
  }
];
