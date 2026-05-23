import { useState } from 'react';
import { addBook, searchMetadata, type LibraryState, type MetadataSearchResult } from '../api/client';

type AddBookViewProps = {
  library?: LibraryState;
  onBookAdded: () => void;
};

export function AddBookView({ library, onBookAdded }: AddBookViewProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<MetadataSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState<string>();

  async function onSearch() {
    setIsSearching(true);
    setError(undefined);

    try {
      setResults(await searchMetadata(query));
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Metadata search failed.');
    } finally {
      setIsSearching(false);
    }
  }

  async function onAdd(result: MetadataSearchResult) {
    await addBook(result, library?.rootFolders[0]?.path);
    onBookAdded();
  }

  return (
    <section>
      <div className="pageHeader">
        <div>
          <h1>Add Book</h1>
          <p>Search Audible first, then Open Library for fallback bibliographic metadata.</p>
        </div>
      </div>

      <div className="searchPanel">
        <input
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              void onSearch();
            }
          }}
          placeholder="Search title, author, narrator, ISBN, or ASIN"
          value={query}
        />
        <button className="primaryButton" disabled={isSearching || !query.trim()} onClick={onSearch} type="button">
          {isSearching ? 'Searching...' : 'Search'}
        </button>
      </div>

      {error && <div className="alert">{error}</div>}

      <div className="resultGrid">
        {results.map((result) => (
          <article className="resultCard" key={`${result.provider}-${result.providerId}`}>
            {result.coverUrl ? <img alt="" src={result.coverUrl} /> : <div className="coverPlaceholder" />}
            <div>
              <span className="providerBadge">{result.provider}</span>
              <h2>{result.title}</h2>
              <p>{result.authors.map((author) => author.name).join(', ') || 'Unknown author'}</p>
              <p>{result.editions[0]?.narrators?.join(', ') || 'Narrator unknown'}</p>
              <button className="secondaryButton" onClick={() => void onAdd(result)} type="button">
                Add and Monitor
              </button>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
