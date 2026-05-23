import type { SystemStatus } from '../api/client';

type SystemViewProps = {
  status?: SystemStatus;
};

export function SystemView({ status }: SystemViewProps) {
  return (
    <section>
      <div className="pageHeader">
        <div>
          <h1>System</h1>
          <p>Runtime health, Docker paths, logs, backups, and tasks.</p>
        </div>
      </div>

      <div className="panel">
        <h2>Status</h2>
        <dl className="definitionList">
          <dt>Version</dt>
          <dd>{status?.version ?? 'Unknown'}</dd>
          <dt>Config</dt>
          <dd>{status?.paths.configPath}</dd>
          <dt>Audiobooks</dt>
          <dd>{status?.paths.audiobooksPath}</dd>
          <dt>Downloads</dt>
          <dd>{status?.paths.downloadsPath}</dd>
        </dl>
      </div>

      <div className="panel">
        <h2>Path Health</h2>
        {status?.pathHealth.map((path) => (
          <div className={path.exists && path.writable ? 'healthRow ok' : 'healthRow warning'} key={path.path}>
            <strong>{path.path}</strong>
            <span>{path.exists && path.writable ? 'Healthy' : path.message ?? 'Warning'}</span>
          </div>
        ))}
      </div>
    </section>
  );
}
