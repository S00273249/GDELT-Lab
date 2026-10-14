import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface HealthResponse {
  status: string;
  message: string;
}

export interface LocalRecord {
  dataset: string;
  recordId: string;
  timestamp?: string;
  source?: string;
  url?: string;
  themes?: string;
  locations?: string;
  people?: string;
  organisations?: string;
  tone?: string;
  fields?: string[];
}

export interface LocalDatasetResponse {
  dataset: string;
  query: string | null;
  filesAvailable: number;
  count: number;
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  recordsScanned: number;
  results: LocalRecord[];
}

export interface IngestionDatasetSummary {
  datasetType: string;
  status: string;
  fileCount: number;
  recordsScanned: number;
  recordsImported: number;
  recordsSkipped: number;
}

export interface IngestionSummaryResponse {
  value: IngestionDatasetSummary[];
  Count: number;
}

export interface IngestionDatasetMeasurement {
  datasetType: string;
  fileCount: number;
  completedFiles: number;
  failedFiles: number;
  recordsScanned: number;
  recordsImported: number;
  recordsSkipped: number;
  totalProcessingSeconds: number;
  averageSecondsPerFile: number;
}

export interface IngestionMeasurementsResponse {
  totalFiles: number;
  totalCompletedFiles: number;
  totalFailedFiles: number;
  totalRecordsImported: number;
  datasets: IngestionDatasetMeasurement[];
}

@Injectable({
  providedIn: 'root'
})
export class Api {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5218/api';

  getHealth(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(
      `${this.apiUrl}/health`
    );
  }

  getLocalDataset(
    dataset: string,
    query: string,
    pageSize = 25,
    page = 1
  ): Observable<LocalDatasetResponse> {
    let params = new HttpParams()
      .set('limit', pageSize)
      .set('page', page);

    if (query.trim()) {
      params = params.set('query', query.trim());
    }

    return this.http.get<LocalDatasetResponse>(
      `${this.apiUrl}/gdelt/local/${dataset}`,
      { params }
    );
  }

  getIngestionSummary(): Observable<IngestionSummaryResponse> {
    return this.http.get<IngestionSummaryResponse>(
      `${this.apiUrl}/gdelt/ingest/summary`
    );
  }

  getIngestionMeasurements(): Observable<IngestionMeasurementsResponse> {
    return this.http.get<IngestionMeasurementsResponse>(
      `${this.apiUrl}/gdelt/ingest/measurements`
    );
  }
}