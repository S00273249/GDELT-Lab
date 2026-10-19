
import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import {
  Api,
  IngestionSummaryResponse,
  IngestionMeasurementsResponse
} from '../../services/api';

@Component({
  selector: 'app-overview',
  imports: [CommonModule],
  templateUrl: './overview.html',
  styleUrl: './overview.css'
})
export class Overview {
  protected readonly title = signal('GDELT-Lab');

  private readonly api = inject(Api);

  protected readonly ingestionSummary =
    signal<IngestionSummaryResponse | null>(null);

  protected readonly ingestionMeasurements =
    signal<IngestionMeasurementsResponse | null>(null);

  protected readonly loading = signal(true);

  protected readonly error = signal<string | null>(null);

  constructor() {
    this.loadOverview();
  }

  protected loadOverview(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getIngestionSummary().subscribe({
      next: summary => {
        this.ingestionSummary.set(summary);

        this.api.getIngestionMeasurements().subscribe({
          next: measurements => {
            this.ingestionMeasurements.set(measurements);
            this.loading.set(false);
          },
          error: err => {
            console.error('Could not load ingestion measurements:', err);
            this.error.set(
              'The ingestion summary loaded, but the measurements could not be loaded.'
            );
            this.loading.set(false);
          }
        });
      },
      error: err => {
        console.error('Could not load ingestion summary:', err);
        this.error.set(
          'Could not load the research statistics. Check that the backend is running.'
        );
        this.loading.set(false);
      }
    });
  }
}
