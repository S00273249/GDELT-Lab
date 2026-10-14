
import { Routes } from '@angular/router';

import { Overview } from './pages/overview/overview';
import { DatasetExplorer } from './pages/dataset-explorer/dataset-explorer';
import { ResearchResults } from './pages/research-results/research-results';
import { About } from './pages/about/about';

export const routes: Routes = [
  {
    path: '',
    component: Overview
  },
  {
    path: 'datasets',
    component: DatasetExplorer
  },
  {
    path: 'research',
    component: ResearchResults
  },
  {
    path: 'about',
    component: About
  },
  {
    path: '**',
    redirectTo: ''
  }
];
