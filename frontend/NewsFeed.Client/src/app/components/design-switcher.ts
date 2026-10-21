
import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-design-switcher',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './design-switcher.html',
  styleUrl: './design-switcher.css'
})
export class DesignSwitcher {
  private readonly router = inject(Router);

  isActive(design: string): boolean {
    const url = this.router.url.split(/[?#]/)[0];

    return url.startsWith(`/feed/${design}`) ||
      url.startsWith(`/article/${design}/`);
  }

  getTargetUrl(design: string): string {
    const url = this.router.url.split(/[?#]/)[0];
    const segments = url.split('/').filter(Boolean);

    if (segments[0] === 'article' && segments.length >= 3) {
      const articleId = segments[2];
      return `/article/${design}/${articleId}`;
    }

    return `/feed/${design}`;
  }
}
