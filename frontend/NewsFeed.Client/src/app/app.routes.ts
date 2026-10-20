
import { Routes } from '@angular/router';
import { NewsFeed } from './pages/news-feed/news-feed';
import { ArticleDetails } from './pages/article-details/article-details';

export const routes: Routes = [
  {
    path: '',
    component: NewsFeed
  },
  {
    path: 'article/:id',
    component: ArticleDetails
  },
  {
    path: '**',
    redirectTo: ''
  }
];
