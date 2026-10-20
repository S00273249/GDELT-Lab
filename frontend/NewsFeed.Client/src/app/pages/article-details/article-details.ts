
import {
  Component,
  inject,
  OnInit,
  ChangeDetectorRef
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
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
  private readonly cdr = inject(ChangeDetectorRef);

  article?: NewsArticle;
  loading = true;
  errorMessage = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.loading = false;
      this.errorMessage = 'No article ID was provided.';
      return;
    }

    this.articleService.getById(id)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: article => {
          this.article = article;

          if (!article) {
            this.errorMessage =
              'This article does not exist in the current prototype dataset.';
          }
        },
        error: error => {
          console.error('Failed to load article:', error);
          this.errorMessage =
            'Unable to load this article. Check that the News Feed API is running.';
        }
      });
  }
}
