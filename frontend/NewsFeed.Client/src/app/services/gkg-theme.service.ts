
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, shareReplay, tap } from 'rxjs/operators';

interface GkgThemeEntry {
  code: string;
  label: string;
  labelSource?: string;
  verificationStatus?: string;
}

@Injectable({
  providedIn: 'root'
})
export class GkgThemeService {
  private readonly http = inject(HttpClient);

  private readonly dictionaryUrl = '/data/gkg-theme-labels.json';

  private readonly labels = new Map<string, string>();

  private readonly dictionary$: Observable<void> = this.http
    .get<unknown>(this.dictionaryUrl)
    .pipe(
      map(data => this.extractEntries(data)),
      tap(entries => {
        this.labels.clear();

        for (const entry of entries) {
          if (entry.code && entry.label) {
            this.labels.set(
              this.normaliseCode(entry.code),
              entry.label.trim()
            );
          }
        }

        console.info(
          `Loaded ${this.labels.size} GDELT theme labels.`
        );
      }),
      map(() => undefined),
      catchError(error => {
        console.error('Failed to load the GDELT theme dictionary:', error);
        return of(undefined);
      }),
      shareReplay(1)
    );

  constructor() {
    this.loadDictionary().subscribe();
  }

  loadDictionary(): Observable<void> {
    return this.dictionary$;
  }

  formatTheme(code: string): string {
    if (!code?.trim()) {
      return '';
    }

    const normalisedCode = this.normaliseCode(code);
    const dictionaryLabel = this.labels.get(normalisedCode);

    if (dictionaryLabel) {
      return this.cleanLabel(dictionaryLabel);
    }

    return this.humaniseCode(code);
  }

  private extractEntries(data: unknown): GkgThemeEntry[] {
    if (Array.isArray(data)) {
      return data as GkgThemeEntry[];
    }

    if (data && typeof data === 'object') {
      const dictionary = data as Record<string, unknown>;

      // Support the compact code-to-label dictionary.
      const values = Object.values(dictionary);

      if (
        values.length > 0 &&
        values.every(value => typeof value === 'string')
      ) {
        return Object.entries(dictionary).map(([code, label]) => ({
          code,
          label: label as string
        }));
      }

      // Support dictionaries containing arrays of entries.
      for (const property of ['entries', 'themes', 'items', 'records']) {
        const value = dictionary[property];

        if (Array.isArray(value)) {
          return value as GkgThemeEntry[];
        }
      }

      // Support dictionaries keyed by code with entry objects.
      if (
        values.length > 0 &&
        values.every(
          value =>
            value !== null &&
            typeof value === 'object' &&
            'code' in value &&
            'label' in value
        )
      ) {
        return values as GkgThemeEntry[];
      }
    }

    console.warn(
      'The GDELT theme dictionary has an unexpected structure.'
    );

    return [];
  }

  private normaliseCode(code: string): string {
    return code
      .trim()
      .replace(/[\s-]+/g, '_')
      .replace(/_+/g, '_')
      .toUpperCase();
  }

  private cleanLabel(label: string): string {
    return label
      .replace(/^Functional Role:\s*/i, '')
      .replace(/[_]+/g, ' ')
      .trim();
  }

  private humaniseCode(code: string): string {
    return code
      .replace(/[_-]+/g, ' ')
      .replace(/\s+/g, ' ')
      .trim()
      .toLowerCase()
      .replace(/\b[a-z]/g, character => character.toUpperCase());
  }
}
