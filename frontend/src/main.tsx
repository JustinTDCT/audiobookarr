import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import {
  getIntegrations,
  getLibrary,
  getSystemStatus,
  type IntegrationState,
  type LibraryState,
  type SystemStatus
} from './api/client';
import { AddBookView } from './components/AddBookView';
import { AppShell } from './components/AppShell';
import { Dashboard } from './components/Dashboard';
import { LibraryView } from './components/LibraryView';
import { PlaceholderView } from './components/PlaceholderView';
import { SettingsView } from './components/SettingsView';
import { SystemView } from './components/SystemView';
import './styles/app.css';

function App() {
  const [activeView, setActiveView] = useState('dashboard');
  const [library, setLibrary] = useState<LibraryState>();
  const [status, setStatus] = useState<SystemStatus>();
  const [integrations, setIntegrations] = useState<IntegrationState>();

  async function refresh() {
    const [nextLibrary, nextStatus, nextIntegrations] = await Promise.all([
      getLibrary(),
      getSystemStatus(),
      getIntegrations()
    ]);

    setLibrary(nextLibrary);
    setStatus(nextStatus);
    setIntegrations(nextIntegrations);
  }

  useEffect(() => {
    void refresh();
  }, []);

  return (
    <AppShell activeView={activeView} onNavigate={setActiveView}>
      {activeView === 'dashboard' && <Dashboard integrations={integrations} library={library} status={status} />}
      {activeView === 'library' && <LibraryView library={library} />}
      {activeView === 'add' && <AddBookView library={library} onBookAdded={() => void refresh()} />}
      {activeView === 'settings' && <SettingsView integrations={integrations} library={library} />}
      {activeView === 'system' && <SystemView status={status} />}
      {activeView === 'wanted' && <PlaceholderView description="Missing and cutoff-unmet audiobook monitoring." title="Wanted" />}
      {activeView === 'activity' && <PlaceholderView description="Download queue, imports, and failures." title="Activity" />}
      {activeView === 'calendar' && <PlaceholderView description="Upcoming releases and monitored author activity." title="Calendar" />}
    </AppShell>
  );
}

createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
