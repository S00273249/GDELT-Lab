import { TestBed } from '@angular/core/testing';

import { NewsArticle } from './news-article';

describe('NewsArticle', () => {
  let service: NewsArticle;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(NewsArticle);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
