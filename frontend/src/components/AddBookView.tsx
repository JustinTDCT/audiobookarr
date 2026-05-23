import { useState } from 'react';
import { addBook, searchMetadata, type LibraryState, type MetadataSearchResult } from '../api/client';

type PageSize = 25 | 50 | 'all';

type AddBookViewProps = {
  library?: LibraryState;
  onBookAdded: () => void;
};

export function AddBookView({ library, onBookAdded }: AddBookViewProps) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<MetadataSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState<string>();
  const [selectedResult, setSelectedResult] = useState<MetadataSearchResult>();
  const [pageSize, setPageSize] = useState<PageSize>(25);
  const [currentPage, setCurrentPage] = useState(1);
  const totalPages = pageSize === 'all' ? 1 : Math.max(1, Math.ceil(results.length / pageSize));
  const visibleResults = pageSize === 'all'
    ? results
    : results.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  async function onSearch() {
    setIsSearching(true);
    setError(undefined);

    try {
      setResults(await searchMetadata(query));
      setCurrentPage(1);
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

  async function onModalAdd(result: MetadataSearchResult) {
    await onAdd(result);
    setSelectedResult(undefined);
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

      {results.length > 0 && (
        <div className="resultsToolbar">
          <span>
            Showing {pageSize === 'all' ? results.length : visibleResults.length} of {results.length} results
          </span>
          <label>
            Results per page
            <select
              onChange={(event) => {
                const value = event.target.value;
                setPageSize(value === 'all' ? 'all' : Number(value) as 25 | 50);
                setCurrentPage(1);
              }}
              value={pageSize}
            >
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value="all">All</option>
            </select>
          </label>
        </div>
      )}

      <div className="resultGrid">
        {visibleResults.map((result) => {
          const existingBook = findExistingBook(result, library);

          return (
            <article
              className={existingBook ? 'resultCard alreadyAdded' : 'resultCard'}
              key={`${result.provider}-${result.providerId}`}
              onClick={() => setSelectedResult(result)}
            >
              {result.coverUrl ? <img alt="" src={result.coverUrl} /> : <div className="coverPlaceholder" />}
              <div>
                <span className="providerBadge">{result.provider}</span>
                {existingBook && <span className="statusBadge monitored inlineStatus">In Library</span>}
                <h2>{result.title}</h2>
                <p>{result.authors.map((author) => author.name).join(', ') || 'Unknown author'}</p>
                <p>{result.editions[0]?.narrators?.join(', ') || 'Narrator unknown'}</p>
                <button
                  className="secondaryButton"
                  disabled={Boolean(existingBook)}
                  onClick={(event) => {
                    event.stopPropagation();

                    if (!existingBook) {
                      void onAdd(result);
                    }
                  }}
                  type="button"
                >
                  {existingBook ? 'Already in Library' : 'Add and Monitor'}
                </button>
              </div>
            </article>
          );
        })}
      </div>

      {results.length > 0 && pageSize !== 'all' && totalPages > 1 && (
        <div className="paginationBar">
          <button
            className="secondaryButton"
            disabled={currentPage === 1}
            onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
            type="button"
          >
            Previous
          </button>
          <span>
            Page {currentPage} of {totalPages}
          </span>
          <button
            className="secondaryButton"
            disabled={currentPage === totalPages}
            onClick={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
            type="button"
          >
            Next
          </button>
        </div>
      )}

      {selectedResult && (
        <div className="modalBackdrop" onClick={() => setSelectedResult(undefined)}>
          <article className="detailModal" onClick={(event) => event.stopPropagation()}>
            <button className="modalClose" onClick={() => setSelectedResult(undefined)} type="button">
              Close
            </button>

            <div className="modalHero">
              {selectedResult.coverUrl ? (
                <img alt="" src={selectedResult.coverUrl} />
              ) : (
                <div className="coverPlaceholder large" />
              )}
              <div>
                <span className="providerBadge">{selectedResult.provider}</span>
                {findExistingBook(selectedResult, library) && (
                  <span className="statusBadge monitored inlineStatus">In Library</span>
                )}
                <h2>{selectedResult.title}</h2>
                {selectedResult.subtitle && <p className="subtitle">{selectedResult.subtitle}</p>}
                <div className="modalActions">
                  <button
                    className="primaryButton"
                    disabled={Boolean(findExistingBook(selectedResult, library))}
                    onClick={() => void onModalAdd(selectedResult)}
                    type="button"
                  >
                    {findExistingBook(selectedResult, library) ? 'Already in Library' : 'Add and Monitor'}
                  </button>
                  <button className="secondaryButton" onClick={() => setSelectedResult(undefined)} type="button">
                    Cancel
                  </button>
                </div>
              </div>
            </div>

            <dl className="definitionList modalDetails">
              <dt>Author</dt>
              <dd>{selectedResult.authors.map((author) => author.name).join(', ') || 'Unknown'}</dd>
              <dt>Narrator</dt>
              <dd>{selectedResult.editions[0]?.narrators?.join(', ') || 'Unknown'}</dd>
              <dt>Series</dt>
              <dd>
                {selectedResult.series
                  .map((series) => `${series.name}${series.sequence ? ` #${series.sequence}` : ''}`)
                  .join(', ') || 'None'}
              </dd>
              <dt>Publisher</dt>
              <dd>{selectedResult.editions[0]?.publisher || 'Unknown'}</dd>
              <dt>Release Date</dt>
              <dd>{selectedResult.editions[0]?.publishedDate || 'Unknown'}</dd>
              <dt>Duration</dt>
              <dd>{formatDuration(selectedResult.editions[0]?.durationMinutes)}</dd>
              <dt>ASIN</dt>
              <dd>{selectedResult.editions[0]?.asin || 'None'}</dd>
              <dt>ISBN</dt>
              <dd>{selectedResult.editions[0]?.isbn || 'None'}</dd>
              <dt>Genres</dt>
              <dd>{selectedResult.genres.join(', ') || 'None'}</dd>
            </dl>

            {selectedResult.description && (
              <div className="descriptionBlock">
                <h3>Description</h3>
                <div dangerouslySetInnerHTML={{ __html: selectedResult.description }} />
              </div>
            )}
          </article>
        </div>
      )}
    </section>
  );
}

function formatDuration(minutes?: number) {
  if (!minutes) {
    return 'Unknown';
  }

  const hours = Math.floor(minutes / 60);
  const remainder = minutes % 60;
  return `${hours}h ${remainder}m`;
}

function findExistingBook(result: MetadataSearchResult, library?: LibraryState) {
  const edition = result.editions[0];
  const normalizedTitle = normalize(result.title);

  return library?.books.find((book) => {
    const bookEdition = book.editions[0];

    return Boolean(
      (edition?.asin && bookEdition?.asin === edition.asin) ||
      (edition?.isbn && bookEdition?.isbn === edition.isbn) ||
      normalize(book.title) === normalizedTitle
    );
  });
}

function normalize(value?: string) {
  return (value ?? '').trim().toLowerCase();
}
