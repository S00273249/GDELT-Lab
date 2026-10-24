
import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs';
import { NewsArticle } from '../../models/news-article';
import { NewsArticleService } from '../../services/news-article';
import { DesignSwitcher } from '../../components/design-switcher';
import { GkgThemeService } from '../../services/gkg-theme.service';

interface GdeltLocation {
  name: string;
  countryCode: string;
  latitude: number | null;
  longitude: number | null;
}

interface GdeltEntity {
  name: string;
  count: number | null;
}

interface GdeltToneMetric {
  label: string;
  value: number;
  displayValue: string;
}

@Component({
  selector: 'app-article-details',
  imports: [RouterLink, DatePipe, DesignSwitcher],
  templateUrl: './article-details.html',
  styleUrl: './article-details.css'
})
export class ArticleDetailsC implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly articleService = inject(NewsArticleService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly gkgThemeService = inject(GkgThemeService);

  article?: NewsArticle;
  loading = true;
  errorMessage = '';

  relatedArticles: NewsArticle[] = [];
  relatedLoading = false;
  relatedErrorMessage = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.loading = false;
      this.errorMessage = 'No article ID was provided.';
      return;
    }

    // Wait for the GDELT theme dictionary before loading the article.
    this.gkgThemeService.loadDictionary()
      .pipe(
        switchMap(() => this.articleService.getById(id)),
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
            return;
          }

          this.loadRelatedArticles(article.id);
        },
        error: error => {
          console.error('Failed to load article:', error);
          this.errorMessage =
            'Unable to load this article. Check that the News Feed API is running.';
        }
      });
  }

  private loadRelatedArticles(articleId: NewsArticle['id']): void {
    this.relatedLoading = true;
    this.relatedErrorMessage = '';

    this.articleService
      .getRelated(articleId, 6)
      .pipe(
        finalize(() => {
          this.relatedLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: articles => {
          this.relatedArticles = articles;
        },
        error: error => {
          console.error('Failed to load related articles:', error);
          this.relatedErrorMessage = 'Unable to load related articles.';
        }
      });
  }

  formatTheme(theme: string): string {
    return this.gkgThemeService.formatTheme(theme);
  }

  get parsedLocations(): GdeltLocation[] {
    const locations = this.parseLocations(this.article?.locations);
    const seen = new Set<string>();

    const uniqueLocations = locations.filter(location => {
      const key = [
        this.normaliseText(location.name),
        this.normaliseText(location.countryCode)
      ].join('|');

      if (seen.has(key)) {
        return false;
      }

      seen.add(key);
      return true;
    });

    // Broad aliases add clutter when more specific locations are available.
    const genericLocationNames = new Set([
      'uk',
      'united kingdom',
      'great britain',
      'britain',
      'british',
      'united kingdom of great britain and northern ireland'
    ]);

    const specificLocations = uniqueLocations.filter(
      location => !genericLocationNames.has(this.normaliseText(location.name))
    );

    return specificLocations.length > 0
      ? specificLocations
      : uniqueLocations;
  }

  get visibleLocations(): GdeltLocation[] {
    return this.parsedLocations.slice(0, 5);
  }

  get hiddenLocationCount(): number {
    return Math.max(0, this.parsedLocations.length - this.visibleLocations.length);
  }

  get parsedPeople(): GdeltEntity[] {
    return this.getUniqueEntities(this.parseEntities(this.article?.people));
  }

  get parsedOrganisations(): GdeltEntity[] {
    return this.getUniqueEntities(
      this.parseEntities(this.article?.organisations)
    );
  }

  get parsedTone(): GdeltToneMetric[] {
    const raw = this.article?.tone;

    if (!raw) {
      return [];
    }

    const values = raw
      .split(',')
      .map(value => value.trim())
      .map(value => Number(value));

    if (
      values.length < 7 ||
      values.some(value => !Number.isFinite(value))
    ) {
      return [];
    }

    const definitions = [
      { label: 'Average tone', value: values[0] },
      { label: 'Positive score', value: values[1] },
      { label: 'Negative score', value: values[2] },
      { label: 'Polarity', value: values[3] },
      { label: 'Activity reference density', value: values[4] },
      { label: 'Self/group reference density', value: values[5] },
      { label: 'Word count', value: values[6] }
    ];

    return definitions.map(metric => ({
      ...metric,
      displayValue: metric.label === 'Word count'
        ? Math.round(metric.value).toLocaleString()
        : metric.value.toFixed(2)
    }));
  }

  private parseLocations(raw: string | null | undefined): GdeltLocation[] {
    if (!raw) {
      return [];
    }

    const countryNames: Record<string, string> = {
      EI: 'Ireland',
      IE: 'Ireland',
      UK: 'United Kingdom',
      GB: 'United Kingdom',
      EG: 'Egypt',
      IT: 'Italy'
    };

    const locations = raw
      .split(';')
      .map(entry => entry.trim())
      .filter(Boolean)
      .map(entry => {
        const fields = entry.split('#');

        if (fields.length < 7) {
          return null;
        }

        const type = fields[0]?.trim() ?? '';
        let name = fields[1]?.trim() ?? '';
        const countryCode = fields[2]?.trim() ?? '';
        const latitude = this.parseOptionalNumber(fields[5]);
        const longitude = this.parseOptionalNumber(fields[6]);

        if (!name) {
          return null;
        }

        // Type 1 entries in this data represent country-level locations.
        if (type === '1') {
          const canonicalCountry = countryNames[countryCode.toUpperCase()];

          if (canonicalCountry) {
            name = canonicalCountry;
          }
        }

        // Remove repeated adjacent name parts.
        const parts = name
          .split(',')
          .map(part => part.trim())
          .filter(Boolean);

        const cleanedParts = parts.filter((part, index) =>
          index === 0 ||
          part.toLowerCase() !== parts[index - 1].toLowerCase()
        );

        name = cleanedParts
          .filter(part => part.toLowerCase() !== 'city of')
          .join(', ');

        return {
          name,
          countryCode,
          latitude,
          longitude
        };
      })
      .filter((location): location is GdeltLocation => location !== null);

    // Deduplicate equivalent labels, keeping the first occurrence.
    const seen = new Set<string>();

    return locations.filter(location => {
      const key = location.name.trim().toLowerCase();

      if (seen.has(key)) {
        return false;
      }

      seen.add(key);
      return true;
    });
  }

  private getUniqueEntities(entities: GdeltEntity[]): GdeltEntity[] {
    const seen = new Set<string>();

    return entities.filter(entity => {
      const key = this.normaliseText(entity.name);

      if (!key || seen.has(key)) {
        return false;
      }

      seen.add(key);
      return true;
    });
  }

  private parseEntities(raw: string | null | undefined): GdeltEntity[] {
    if (!raw) {
      return [];
    }

    return raw
      .split(';')
      .map(entry => entry.trim())
      .filter(Boolean)
      .map(entry => {
        // The final comma separates the entity name from its GDELT count.
        const separatorIndex = entry.lastIndexOf(',');

        if (separatorIndex === -1) {
          return {
            name: entry,
            count: null
          };
        }

        const name = entry.slice(0, separatorIndex).trim();
        const countText = entry.slice(separatorIndex + 1).trim();
        const count = Number(countText);

        if (
          !name ||
          countText.length === 0 ||
          !Number.isFinite(count)
        ) {
          return {
            name: entry,
            count: null
          };
        }

        return {
          name,
          count
        };
      });
  }

  private parseOptionalNumber(value: string | undefined): number | null {
    if (!value?.trim()) {
      return null;
    }

    const parsed = Number(value);

    return Number.isFinite(parsed) ? parsed : null;
  }

  private normaliseText(value: string): string {
    return value.trim().toLowerCase().replace(/\s+/g, ' ');
  }
}
