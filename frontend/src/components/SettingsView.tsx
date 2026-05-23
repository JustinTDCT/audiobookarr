import type { IntegrationState, LibraryState } from '../api/client';

type SettingsViewProps = {
  library?: LibraryState;
  integrations?: IntegrationState;
};

const settingsSections = [
  'Media Management',
  'Metadata Providers',
  'Indexers',
  'Download Clients',
  'Profiles',
  'Root Folders',
  'General / Auth / UI'
];

export function SettingsView({ library, integrations }: SettingsViewProps) {
  return (
    <section>
      <div className="pageHeader">
        <div>
          <h1>Settings</h1>
          <p>Familiar provider, profile, path, and integration configuration.</p>
        </div>
      </div>

      <div className="settingsGrid">
        {settingsSections.map((section) => (
          <div className="panel settingsCard" key={section}>
            <h2>{section}</h2>
            {section === 'Metadata Providers' && <p>Audible priority 10, Open Library priority 20, Goodreads reserved for plugin support.</p>}
            {section === 'Download Clients' && <p>{integrations?.downloadClients.length ?? 0} configured clients.</p>}
            {section === 'Indexers' && <p>{integrations?.indexers.length ?? 0} configured indexers.</p>}
            {section === 'Root Folders' && <p>{library?.rootFolders.map((folder) => folder.path).join(', ')}</p>}
            {section === 'Profiles' && <p>{library?.qualityProfiles.map((profile) => profile.name).join(', ')}</p>}
            {!['Metadata Providers', 'Download Clients', 'Indexers', 'Root Folders', 'Profiles'].includes(section) && (
              <p>Ready for detailed configuration forms.</p>
            )}
          </div>
        ))}
      </div>
    </section>
  );
}
