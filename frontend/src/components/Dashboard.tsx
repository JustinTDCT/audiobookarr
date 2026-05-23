import type { IntegrationState, LibraryState, SystemStatus } from '../api/client';

type DashboardProps = {
  library?: LibraryState;
  status?: SystemStatus;
  integrations?: IntegrationState;
};

export function Dashboard({ library, status, integrations }: DashboardProps) {
  const monitored = library?.books.filter((book) => book.monitored).length ?? 0;
  const missing = library?.books.filter((book) => book.monitored).length ?? 0;
  const warningCount = status?.warnings.length ?? 0;

  return (
    <section>
      <div className="pageHeader">
        <div>
          <h1>Dashboard</h1>
          <p>Monitor audiobook releases, imports, metadata, and Docker health.</p>
        </div>
        <button className="primaryButton" type="button">RSS Sync</button>
      </div>

      <div className="statGrid">
        <div className="statCard">
          <span>Monitored Books</span>
          <strong>{monitored}</strong>
        </div>
        <div className="statCard">
          <span>Wanted</span>
          <strong>{missing}</strong>
        </div>
        <div className="statCard">
          <span>Download Clients</span>
          <strong>{integrations?.downloadClients.length ?? 0}</strong>
        </div>
        <div className="statCard warning">
          <span>Health Warnings</span>
          <strong>{warningCount}</strong>
        </div>
      </div>

      <div className="panel">
        <h2>Recent Activity</h2>
        <p className="emptyState">No downloads or imports have run yet.</p>
      </div>
    </section>
  );
}
