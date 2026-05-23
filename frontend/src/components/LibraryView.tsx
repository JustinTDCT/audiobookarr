import { useEffect, useRef, useState } from 'react';
import { refreshBookMetadata, setBookMonitoring, type Book, type LibraryState } from '../api/client';

type LibraryViewProps = {
  library?: LibraryState;
  onLibraryChanged: () => void;
};

export function LibraryView({ library, onLibraryChanged }: LibraryViewProps) {
  const [selectedBook, setSelectedBook] = useState<Book>();
  const [isRefreshingMetadata, setIsRefreshingMetadata] = useState(false);
  const refreshAttempts = useRef(new Set<string>());

  useEffect(() => {
    if (!selectedBook || selectedBook.coverUrl || refreshAttempts.current.has(selectedBook.id)) {
      return;
    }

    refreshAttempts.current.add(selectedBook.id);
    setIsRefreshingMetadata(true);
    refreshBookMetadata(selectedBook.id)
      .then((updated) => {
        setSelectedBook(updated);
        onLibraryChanged();
      })
      .finally(() => setIsRefreshingMetadata(false));
  }, [onLibraryChanged, selectedBook]);

  async function onToggleMonitoring(book: Book) {
    const updated = await setBookMonitoring(book.id, !book.monitored);
    setSelectedBook(updated);
    onLibraryChanged();
  }

  return (
    <section>
      <div className="pageHeader">
        <div>
          <h1>Library</h1>
          <p>Your monitored audiobook library.</p>
        </div>
        <button className="primaryButton" type="button">Mass Editor</button>
      </div>

      <div className="panel">
        <table className="dataTable">
          <thead>
            <tr>
              <th>Title</th>
              <th>Author</th>
              <th>Narrator</th>
              <th>Profile</th>
              <th>Monitored</th>
              <th>Source</th>
            </tr>
          </thead>
          <tbody>
            {library?.books.map((book) => (
              <tr className="clickableRow" key={book.id} onClick={() => setSelectedBook(book)}>
                <td>{book.title}</td>
                <td>{book.authors.map((author) => author.name).join(', ') || 'Unknown'}</td>
                <td>{book.editions[0]?.narrators?.join(', ') || 'Unknown'}</td>
                <td>{book.qualityProfile}</td>
                <td>
                  <span className={book.monitored ? 'statusBadge monitored' : 'statusBadge unmonitored'}>
                    {book.monitored ? 'Monitored' : 'Unmonitored'}
                  </span>
                </td>
                <td>{book.metadataSource}</td>
              </tr>
            ))}
          </tbody>
        </table>

        {library?.books.length === 0 && (
          <p className="emptyState">No books yet. Use Add Book to search Audible and Open Library.</p>
        )}
      </div>

      {selectedBook && (
        <div className="modalBackdrop" onClick={() => setSelectedBook(undefined)}>
          <article className="detailModal" onClick={(event) => event.stopPropagation()}>
            <button className="modalClose" onClick={() => setSelectedBook(undefined)} type="button">
              Close
            </button>

            <div className="modalHero">
              {selectedBook.coverUrl ? (
                <img alt="" src={selectedBook.coverUrl} />
              ) : (
                <div className="coverPlaceholder large" />
              )}
              <div>
                <span className="providerBadge">{selectedBook.metadataSource}</span>
                <h2>{selectedBook.title}</h2>
                {selectedBook.subtitle && <p className="subtitle">{selectedBook.subtitle}</p>}
                {isRefreshingMetadata && <p className="subtitle">Refreshing metadata...</p>}
                <div className="modalActions">
                  <button className="primaryButton" onClick={() => void onToggleMonitoring(selectedBook)} type="button">
                    {selectedBook.monitored ? 'Stop Monitoring' : 'Monitor'}
                  </button>
                  <button className="secondaryButton" onClick={() => setSelectedBook(undefined)} type="button">
                    Close
                  </button>
                </div>
              </div>
            </div>

            <dl className="definitionList modalDetails">
              <dt>Author</dt>
              <dd>{selectedBook.authors.map((author) => author.name).join(', ') || 'Unknown'}</dd>
              <dt>Narrator</dt>
              <dd>{selectedBook.editions[0]?.narrators?.join(', ') || 'Unknown'}</dd>
              <dt>Series</dt>
              <dd>
                {selectedBook.series
                  .map((series) => `${series.name}${series.sequence ? ` #${series.sequence}` : ''}`)
                  .join(', ') || 'None'}
              </dd>
              <dt>Publisher</dt>
              <dd>{selectedBook.editions[0]?.publisher || 'Unknown'}</dd>
              <dt>Release Date</dt>
              <dd>{selectedBook.editions[0]?.publishedDate || 'Unknown'}</dd>
              <dt>Duration</dt>
              <dd>{formatDuration(selectedBook.editions[0]?.durationMinutes)}</dd>
              <dt>Root Folder</dt>
              <dd>{selectedBook.rootFolder || 'Not set'}</dd>
              <dt>Quality Profile</dt>
              <dd>{selectedBook.qualityProfile}</dd>
              <dt>ASIN</dt>
              <dd>{selectedBook.editions[0]?.asin || 'None'}</dd>
              <dt>ISBN</dt>
              <dd>{selectedBook.editions[0]?.isbn || 'None'}</dd>
              <dt>Added</dt>
              <dd>{new Date(selectedBook.addedAt).toLocaleString()}</dd>
            </dl>

            <div className="panel embeddedPanel">
              <h3>History</h3>
              <p className="emptyState">No import or download history yet.</p>
            </div>

            {selectedBook.description && (
              <div className="descriptionBlock">
                <h3>Description</h3>
                <div dangerouslySetInnerHTML={{ __html: selectedBook.description }} />
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
