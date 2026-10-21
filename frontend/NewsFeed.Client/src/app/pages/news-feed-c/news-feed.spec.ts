import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NewsFeed } from './news-feed';

describe('NewsFeed', () => {
  let component: NewsFeed;
  let fixture: ComponentFixture<NewsFeed>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NewsFeed],
    }).compileComponents();

    fixture = TestBed.createComponent(NewsFeed);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
