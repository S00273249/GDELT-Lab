import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface HealthResponse {
  status: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class Api {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = 'https://localhost:7099/api';

  getHealth(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(`${this.apiUrl}/health`);
  }
}
