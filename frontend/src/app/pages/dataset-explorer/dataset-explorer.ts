
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { Api, LocalDatasetResponse, LocalRecord } from '../../services/api';

interface GkgThemeDictionaryEntry {
  code: string;
  label: string;
  occurrenceCount: number;
  labelSource: 'embedded-name' | 'explicit-label' | 'rule-generated';
  verificationStatus: 'verified' | 'unverified';
}

@Component({
  selector: 'app-dataset-explorer',
  imports: [CommonModule],
  templateUrl: './dataset-explorer.html',
  styleUrl: './dataset-explorer.css'
})
export class DatasetExplorer {
  private readonly api = inject(Api);
  private readonly http = inject(HttpClient);

  protected readonly datasets = [
    {
      id: 'gkg',
      name: 'GKG',
      description: 'Global Knowledge Graph'
    },
    {
      id: 'events',
      name: 'Events',
      description: 'Structured event records'
    },
    {
      id: 'mentions',
      name: 'Mentions',
      description: 'Event mentions in source articles'
    }
  ];

  protected readonly selectedDataset = signal('gkg');
  protected readonly searchText = signal('');
  protected readonly data = signal<LocalDatasetResponse | null>(null);
  protected readonly selectedRecord = signal<LocalRecord | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly themeDictionary =
    signal<Map<string, GkgThemeDictionaryEntry> | null>(null);

  protected readonly themeDictionaryLoading = signal(false);
  protected readonly themeDictionaryError = signal<string | null>(null);

  constructor() {
    this.loadRecords();
  }

  protected loadRecords(): void {
    this.loading.set(true);
    this.error.set(null);
    this.selectedRecord.set(null);

    this.api.getLocalDataset(
      this.selectedDataset(),
      this.searchText(),
      25
    ).subscribe({
      next: response => {
        this.data.set(response);
        this.loading.set(false);
      },
      error: err => {
        console.error('Dataset request failed:', err);

        this.error.set(
          err.error?.detail ?? 'Could not load this dataset.'
        );

        this.data.set(null);
        this.loading.set(false);
      }
    });
  }

  protected selectDataset(dataset: string): void {
    this.selectedDataset.set(dataset);
    this.loadRecords();
  }

  protected selectRecord(record: LocalRecord): void {
    this.selectedRecord.set(record);

    if (this.selectedDataset() === 'gkg') {
      this.loadThemeDictionary();
    }
  }

  private loadThemeDictionary(): void {
    if (
      this.themeDictionary() !== null ||
      this.themeDictionaryLoading()
    ) {
      return;
    }

    this.themeDictionaryLoading.set(true);
    this.themeDictionaryError.set(null);

    this.http
      .get<GkgThemeDictionaryEntry[]>('/data/gkg-theme-dictionary.json')
      .subscribe({
        next: entries => {
          const dictionary = new Map(
            entries.map(entry => [entry.code, entry])
          );

          this.themeDictionary.set(dictionary);
          this.themeDictionaryLoading.set(false);

          console.log(
            `Loaded ${dictionary.size} GKG theme dictionary entries.`
          );
        },
        error: err => {
          console.error('Could not load GKG theme dictionary:', err);

          this.themeDictionaryError.set(
            'The GKG theme dictionary could not be loaded.'
          );

          this.themeDictionaryLoading.set(false);
        }
      });
  }

  protected recordSummary(record: LocalRecord): string {
    if (record.source) {
      return record.source;
    }

    return record.fields?.[4] ?? 'Raw record';
  }

  protected fieldName(index: number): string {
    const names: Record<string, string[]> = {
      gkg: [
        'GKG Record ID',
        'Record Date',
        'Source Collection',
        'Source Name',
        'Article URL',
        'Counts',
        'Version 2 Counts',
        'Themes',
        'Version 2 Themes',
        'Locations',
        'Version 2 Locations',
        'People',
        'Version 2 People',
        'Organisations',
        'Version 2 Organisations',
        'Tone Measurements',
        'Dates Mentioned',
        'GCAM Measurements',
        'Sharing Image',
        'Related Images',
        'Social Image Embeds',
        'Social Video Embeds',
        'Quotations',
        'All Names',
        'Amounts',
        'Translation Information',
        'Additional Data'
      ],

      events: [
        'Global Event ID',
        'Event Date',
        'Month and Year',
        'Year',
        'Fractional Date',
        'Actor 1 Code',
        'Actor 1 Name',
        'Actor 1 Country Code',
        'Actor 1 Known Group Code',
        'Actor 1 Ethnic Code',
        'Actor 1 Religion 1 Code',
        'Actor 1 Religion 2 Code',
        'Actor 1 Type 1 Code',
        'Actor 1 Type 2 Code',
        'Actor 1 Type 3 Code',
        'Actor 2 Code',
        'Actor 2 Name',
        'Actor 2 Country Code',
        'Actor 2 Known Group Code',
        'Actor 2 Ethnic Code',
        'Actor 2 Religion 1 Code',
        'Actor 2 Religion 2 Code',
        'Actor 2 Type 1 Code',
        'Actor 2 Type 2 Code',
        'Actor 2 Type 3 Code',
        'Root Event Flag',
        'Event Code',
        'Base Event Code',
        'Root Event Code',
        'Quad Class',
        'Goldstein Scale',
        'Number of Mentions',
        'Number of Sources',
        'Number of Articles',
        'Average Tone',
        'Actor 1 Location Type',
        'Actor 1 Location Name',
        'Actor 1 Location Country Code',
        'Actor 1 Location ADM1 Code',
        'Actor 1 Location ADM2 Code',
        'Actor 1 Latitude',
        'Actor 1 Longitude',
        'Actor 1 Geographic Feature ID',
        'Actor 2 Location Type',
        'Actor 2 Location Name',
        'Actor 2 Location Country Code',
        'Actor 2 Location ADM1 Code',
        'Actor 2 Location ADM2 Code',
        'Actor 2 Latitude',
        'Actor 2 Longitude',
        'Actor 2 Geographic Feature ID',
        'Action Location Type',
        'Action Location Name',
        'Action Location Country Code',
        'Action Location ADM1 Code',
        'Action Location ADM2 Code',
        'Action Location Latitude',
        'Action Location Longitude',
        'Action Geographic Feature ID',
        'Date Added to GDELT',
        'Source Article URL'
      ],

      mentions: [
        'Global Event ID',
        'Event Date and Time',
        'Mention Date and Time',
        'Mention Type',
        'Mention Source Name',
        'Mention Article URL',
        'Sentence ID',
        'Actor 1 Character Offset',
        'Actor 2 Character Offset',
        'Action Character Offset',
        'Present in Raw Text',
        'Extraction Confidence',
        'Article Length',
        'Article Tone',
        'Reserved Field 14',
        'Reserved Field 15'
      ]
    };

    return names[this.selectedDataset()]?.[index]
      ?? `Unknown Field ${index + 1}`;
  }

  protected fieldDescription(index: number): string {
    const descriptions: Record<string, string[]> = {
      gkg: [
        'Unique identifier for this GKG record.',
        'Timestamp associated with the record, normally formatted as YYYYMMDDHHMMSS.',
        'Identifies the GDELT collection that supplied this record.',
        'Recognised name of the source website or publisher.',
        'URL identifying the document associated with this record.',
        'Extracted count information. The internal format depends on the count type.',
        'Version 2 count records, which may include additional categories and locations.',
        'Themes and concepts detected in the document. These are machine-extracted labels, not proof that a topic is the article’s main subject.',
        'Expanded version 2 theme information, potentially including additional metadata.',
        'Locations detected in the document. The raw format can contain several components per location.',
        'Version 2 location records with structured geographic information.',
        'People or person-related references detected in the document.',
        'Version 2 person records with structured information where available.',
        'Organisations detected in the document, such as companies, agencies or institutions.',
        'Version 2 organisation records with structured information where available.',
        'Comma-separated tone measurements. When seven values are present, each position has a distinct meaning.',
        'Dates extracted from the document. These may refer to events described in the text rather than publication time.',
        'Global Content Analysis Measures: numerical measurements of text features and concepts.',
        'Image URL associated with social sharing, if supplied.',
        'Other image URLs associated with the document.',
        'Social-media image embed information.',
        'Social-media video embed information.',
        'Quotation text extracted from the document.',
        'Names detected across the document. This may combine people, organisations and other named entities.',
        'Amounts and quantities extracted from the document.',
        'Language or translation processing metadata, when available.',
        'Additional metadata whose structure depends on the source record.'
      ],

      events: [
        'Unique identifier for the event record. Related mention records can reference this ID.',
        'Event date as YYYYMMDD.',
        'Event month and year as YYYYMM.',
        'Calendar year in which the event is recorded.',
        'Fractional-year representation of the event date.',
        'CAMEO actor classification code for the first actor. This describes the actor category, not necessarily a unique person.',
        'Extracted name of the first actor, such as a person, group or institution.',
        'Country classification code associated with actor 1.',
        'Code identifying a known group associated with actor 1, where classified.',
        'Ethnic classification code associated with actor 1, where classified.',
        'First religion classification for actor 1, where classified.',
        'Second religion classification for actor 1, where classified.',
        'First actor-type classification for actor 1.',
        'Second actor-type classification for actor 1.',
        'Third actor-type classification for actor 1.',
        'CAMEO actor classification code for the second actor.',
        'Extracted name of the second actor.',
        'Country classification code associated with actor 2.',
        'Code identifying a known group associated with actor 2, where classified.',
        'Ethnic classification code associated with actor 2, where classified.',
        'First religion classification for actor 2, where classified.',
        'Second religion classification for actor 2, where classified.',
        'First actor-type classification for actor 2.',
        'Second actor-type classification for actor 2.',
        'Third actor-type classification for actor 2.',
        'Indicates whether GDELT marks this as a root event.',
        'Specific CAMEO event code describing the reported action.',
        'Broader base event code grouping related event types.',
        'Root event code grouping events into a broad action category.',
        'Broad event category: verbal cooperation, material cooperation, verbal conflict or material conflict.',
        'Goldstein scale associated with the event type. It estimates the typical impact associated with that type of event; it is not article sentiment.',
        'Number of event mentions recorded in the dataset.',
        'Number of distinct sources associated with the event.',
        'Number of articles associated with the event.',
        'Average tone of the associated coverage. This is distinct from the Goldstein scale.',
        'Geographic feature type for actor 1’s location.',
        'Readable geographic name associated with actor 1.',
        'Country code for actor 1’s geographic location.',
        'First-level administrative area code for actor 1’s location.',
        'Second-level administrative area code for actor 1’s location, when available.',
        'Latitude of actor 1’s geographic location.',
        'Longitude of actor 1’s geographic location.',
        'Geographic database feature identifier for actor 1’s location.',
        'Geographic feature type for actor 2’s location.',
        'Readable geographic name associated with actor 2.',
        'Country code for actor 2’s geographic location.',
        'First-level administrative area code for actor 2’s location.',
        'Second-level administrative area code for actor 2’s location, when available.',
        'Latitude of actor 2’s geographic location.',
        'Longitude of actor 2’s geographic location.',
        'Geographic database feature identifier for actor 2’s location.',
        'Geographic feature type for the location associated with the action.',
        'Readable geographic name associated with the action.',
        'Country code for the action location.',
        'First-level administrative area code for the action location.',
        'Second-level administrative area code for the action location, when available.',
        'Latitude of the action location.',
        'Longitude of the action location.',
        'Geographic database feature identifier for the action location.',
        'Date and time when this event record was added to GDELT, commonly YYYYMMDDHHMMSS.',
        'URL of an article associated with the event.'
      ],

      mentions: [
        'Identifier linking this mention to its associated event record.',
        'Timestamp associated with the event, formatted as YYYYMMDDHHMMSS.',
        'Timestamp associated with the article mention, formatted as YYYYMMDDHHMMSS.',
        'Type of mention record. Known codes include WEB, citation-only and core-document mentions.',
        'Domain or source name associated with the article.',
        'Identifier of the source article, commonly its URL.',
        'Sentence number associated with the mention in the source document.',
        'Character offset for actor 1 in the document; -1 commonly indicates that no offset was identified.',
        'Character offset for actor 2 in the document; -1 commonly indicates that no offset was identified.',
        'Character offset for the action in the document; -1 commonly indicates that no offset was identified.',
        'Indicates whether the event was found in the raw text.',
        'GDELT extraction confidence score. Interpret it according to the dataset specification rather than as a probability unless explicitly defined that way.',
        'Length of the source document as measured by GDELT.',
        'Tone measurement associated with the source document.',
        'Reserved field in the source format; currently not interpreted here.',
        'Reserved field in the source format; currently not interpreted here.'
      ]
    };

    return descriptions[this.selectedDataset()]?.[index]
      ?? 'No field-specific explanation is available yet.';
  }

  protected formatFieldValue(index: number, value: string): string {
    if (!value) {
      return '(empty)';
    }

    const dataset = this.selectedDataset();

    if (dataset === 'gkg') {
      if (index === 15) {
        const parts = value.split(',');

        const labels = [
          'Average tone',
          'Positive score',
          'Negative score',
          'Polarity',
          'Activity reference density',
          'Self/group reference density',
          'Word count'
        ];

        if (parts.length === labels.length) {
          return parts
            .map((part, i) => `${labels[i]}: ${part.trim()}`)
            .join('\n');
        }
      }

      if ([7, 8, 11, 12, 13, 14, 23].includes(index)) {
        return value
          .split(';')
          .map(item => item.trim())
          .filter(Boolean)
          .map((item, i) => `${i + 1}. ${item}`)
          .join('\n');
      }

      return value;
    }

    if (dataset === 'events') {
      if (index === 25) {
        if (value === '1') return 'Yes (1)';
        if (value === '0') return 'No (0)';
      }

      if (index === 29) {
        const quadClasses: Record<string, string> = {
          '1': 'Verbal cooperation (1)',
          '2': 'Material cooperation (2)',
          '3': 'Verbal conflict (3)',
          '4': 'Material conflict (4)'
        };

        return quadClasses[value] ?? value;
      }

      if (index === 30) {
        const score = Number(value);

        if (Number.isFinite(score)) {
          const interpretation =
            score > 0 ? 'typically cooperative' :
            score < 0 ? 'typically conflictual' :
            'neutral on this scale';

          return `${value} — ${interpretation}`;
        }
      }

      return value;
    }

    if (dataset === 'mentions') {
      if (index === 3) {
        const mentionTypes: Record<string, string> = {
          '1': 'Web mention (1)',
          '2': 'Citation-only mention (2)',
          '3': 'Core-document mention (3)'
        };

        return mentionTypes[value] ?? `Unknown mention type (${value})`;
      }

      if ([7, 8, 9].includes(index) && value === '-1') {
        return '-1 — offset not identified';
      }

      if (index === 10) {
        if (value === '1') return 'Yes (1)';
        if (value === '0') return 'No (0)';
      }

      return value;
    }

    return value;
  }

  protected recordEntries(
    record: LocalRecord
  ): { name: string; value: string; description: string }[] {
    if (record.fields) {
      return record.fields.map((value, index) => ({
        name: this.fieldName(index),
        value: this.formatFieldValue(index, value),
        description: this.fieldDescription(index)
      }));
    }

    if (this.selectedDataset() === 'gkg') {
      const gkgFields: {
        key: keyof LocalRecord;
        name: string;
        description: string;
        formatIndex?: number;
      }[] = [
        {
          key: 'recordId',
          name: 'GKG Record ID',
          description: 'Unique identifier for this GKG record.'
        },
        {
          key: 'timestamp',
          name: 'Record Date',
          description: 'Timestamp associated with the record, normally in UTC.'
        },
        {
          key: 'source',
          name: 'Source',
          description: 'Recognised name of the source website or publisher.'
        },
        {
          key: 'url',
          name: 'Article URL',
          description: 'URL of the document associated with this record.'
        },
        {
          key: 'themes',
          name: 'Themes',
          description: 'Machine-extracted topics and concepts. These do not necessarily represent the article’s main subject.',
          formatIndex: 7
        },
        {
          key: 'locations',
          name: 'Locations',
          description: 'Geographic locations extracted from the article. Each entry contains a location type, name, geographic codes and coordinates where available.'
        },
        {
          key: 'people',
          name: 'People',
          description: 'People detected or referenced in the article.',
          formatIndex: 11
        },
        {
          key: 'organisations',
          name: 'Organisations',
          description: 'Organisations detected in the article.',
          formatIndex: 13
        },
        {
          key: 'tone',
          name: 'Tone Measurements',
          description: 'Seven measurements describing textual tone and reference patterns.',
          formatIndex: 15
        }
      ];

      return gkgFields.map(field => {
        const rawValue = record[field.key];
        const value = rawValue == null ? '' : String(rawValue);

        return {
          name: field.name,
          value: field.key === 'locations'
            ? this.formatGkgLocations(value)
            : field.key === 'themes'
              ? this.formatGkgThemes(value)
              : field.formatIndex === undefined
                ? value || '(empty)'
                : this.formatFieldValue(field.formatIndex, value),
          description: field.description
        };
      });
    }

    return Object.entries(record)
      .filter(([key]) => key !== 'dataset')
      .map(([name, value]) => ({
        name,
        value: value == null || value === ''
          ? '(empty)'
          : String(value),
        description: 'No field-specific explanation is available yet.'
      }));
  }

  protected formatGkgLocations(value: string): string {
    if (!value.trim()) {
      return '(empty)';
    }

    const entries = value
      .split(';')
      .map(entry => entry.trim())
      .filter(Boolean);

    return entries.map((entry, index) => {
      const parts = entry.split('#');

      if (parts.length !== 7 && parts.length !== 8) {
        return `Location ${index + 1}\n${entry}`;
      }

      const [
        type,
        name,
        country,
        adm1,
        ...remaining
      ] = parts;

      let adm2 = '';
      let latitude: string;
      let longitude: string;
      let featureId: string;

      if (remaining.length === 3) {
        [latitude, longitude, featureId] = remaining;
      } else {
        [adm2, latitude, longitude, featureId] = remaining;
      }

      const details = [
        `Location type: ${type || 'Unknown'}`,
        `Name: ${name || 'Unknown'}`,
        `Country code: ${country || 'Not supplied'}`,
        `Administrative area 1: ${adm1 || 'Not supplied'}`
      ];

      if (adm2) {
        details.push(`Administrative area 2: ${adm2}`);
      }

      if (latitude && longitude) {
        details.push(`Coordinates: ${latitude}, ${longitude}`);
      }

      if (featureId) {
        details.push(`Geographic feature ID: ${featureId}`);
      }

      return `Location ${index + 1}\n${details.join('\n')}`;
    }).join('\n\n');
  }

  protected formatGkgThemes(value: string): string {
    if (!value.trim()) {
      return '(empty)';
    }

    const dictionary = this.themeDictionary();

    if (!dictionary) {
      if (this.themeDictionaryLoading()) {
        return 'Loading GKG theme dictionary...';
      }

      if (this.themeDictionaryError()) {
        return this.themeDictionaryError()!;
      }

      return 'GKG theme dictionary has not been loaded.';
    }

    const labelSourceNames: Record<
      GkgThemeDictionaryEntry['labelSource'],
      string
    > = {
      'embedded-name': 'Name embedded in code',
      'explicit-label': 'Explicit mapping',
      'rule-generated': 'Rule-generated label'
    };

    return value
      .split(';')
      .map(theme => theme.trim())
      .filter(Boolean)
      .map((theme, index) => {
        const entry = dictionary.get(theme);

        if (!entry) {
          return [
            `${index + 1}. Uncatalogued GDELT theme`,
            `   Original code: ${theme}`,
            '   Label source: No dictionary entry',
            '   Status: No catalogue match'
          ].join('\n');
        }

        const verification =
          entry.verificationStatus === 'verified'
            ? 'Verified'
            : 'Unverified';

        return [
          `${index + 1}. ${entry.label}`,
          `   Original code: ${theme}`,
          `   Label source: ${labelSourceNames[entry.labelSource]}`,
          `   Status: ${verification}`,
          `   Occurrences in reference catalogue: ${entry.occurrenceCount.toLocaleString()}`
        ].join('\n');
      })
      .join('\n\n');
  }
}
