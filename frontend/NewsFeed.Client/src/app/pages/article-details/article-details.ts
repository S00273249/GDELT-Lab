
import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NewsArticle } from '../../models/news-article';
import { NewsArticleService } from '../../services/news-article';

@Component({
  selector: 'app-article-details',
  imports: [RouterLink],
  templateUrl: './article-details.html',
  styleUrl: './article-details.css'
})
export class ArticleDetails implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly articleService = inject(NewsArticleService);

  article?: NewsArticle;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.article = this.articleService.getById(id);
    }
  }
}
