export type Author = {
  id: string;
  name: string;
  sortName?: string;
};

export type BookEdition = {
  id: string;
  isbn?: string;
  asin?: string;
  publisher?: string;
  publishedDate?: string;
  language?: string;
  narrators?: string[];
  durationMinutes?: number;
};

export type MetadataSearchResult = {
  provider: string;
  providerId: string;
  title: string;
  subtitle?: string;
  authors: Author[];
  editions: BookEdition[];
  series: { name: string; sequence?: string }[];
  description?: string;
  coverUrl?: string;
  genres: string[];
  score: number;
  fieldSources: { field: string; provider: string }[];
};

export type Book = MetadataSearchResult & {
  id: string;
  monitored: boolean;
  rootFolder?: string;
  qualityProfile: string;
  metadataSource: string;
  addedAt: string;
};

export type LibraryState = {
  books: Book[];
  rootFolders: { path: string; isDefault: boolean }[];
  qualityProfiles: {
    id: string;
    name: string;
    preferUnabridged: boolean;
    minimumBitrate?: number;
    preferredFormats: string[];
  }[];
};

export type SystemStatus = {
  appName: string;
  version: string;
  paths: {
    configPath: string;
    audiobooksPath: string;
    downloadsPath: string;
  };
  pathHealth: {
    path: string;
    exists: boolean;
    writable: boolean;
    message?: string;
  }[];
  warnings: string[];
};

export type IntegrationState = {
  downloadClients: unknown[];
  indexers: unknown[];
};

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    headers: {
      'Content-Type': 'application/json',
      ...init?.headers
    },
    ...init
  });

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export function getSystemStatus() {
  return request<SystemStatus>('/api/v1/system/status');
}

export function getLibrary() {
  return request<LibraryState>('/api/v1/library');
}

export function getIntegrations() {
  return request<IntegrationState>('/api/v1/integrations');
}

export function searchMetadata(query: string, limit = 0) {
  return request<MetadataSearchResult[]>(
    `/api/v1/metadata/search?q=${encodeURIComponent(query)}&limit=${limit}`
  );
}

export function addBook(metadata: MetadataSearchResult, rootFolder?: string) {
  return request<Book>('/api/v1/library/books', {
    method: 'POST',
    body: JSON.stringify({
      metadata,
      monitored: true,
      rootFolder,
      qualityProfile: 'standard'
    })
  });
}
