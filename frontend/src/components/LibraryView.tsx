import type { LibraryState } from '../api/client';

type LibraryViewProps = {
  library?: LibraryState;
};

export function LibraryView({ library }: LibraryViewProps) {
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
              <tr key={book.id}>
                <td>{book.title}</td>
                <td>{book.authors.map((author) => author.name).join(', ') || 'Unknown'}</td>
                <td>{book.editions[0]?.narrators?.join(', ') || 'Unknown'}</td>
                <td>{book.qualityProfile}</td>
                <td>{book.monitored ? 'Yes' : 'No'}</td>
                <td>{book.metadataSource}</td>
              </tr>
            ))}
          </tbody>
        </table>

        {library?.books.length === 0 && (
          <p className="emptyState">No books yet. Use Add Book to search Audible and Open Library.</p>
        )}
      </div>
    </section>
  );
}
