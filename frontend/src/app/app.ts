
import { Component, signal, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Api, HealthResponse } from './services/api';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})

export class App {
  protected readonly title = signal('GDELT-Lab');

  private readonly api = inject(Api);

  protected readonly health = signal<HealthResponse | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly loading = signal(true);

  constructor() {
    this.api.getHealth().subscribe({
      next: (response) => {
        this.health.set(response);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('API request failed:', err);
        this.error.set(
          'Could not connect to the API. Check that the backend is running.'
        );
        this.loading.set(false);
      }
    });
  }
}
