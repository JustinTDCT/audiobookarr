import {
  Activity,
  BookOpen,
  CalendarClock,
  Download,
  Gauge,
  Library,
  Search,
  Settings,
  Stethoscope
} from 'lucide-react';
import type { ReactNode } from 'react';

type NavItem = {
  id: string;
  label: string;
  icon: ReactNode;
};

const navItems: NavItem[] = [
  { id: 'dashboard', label: 'Dashboard', icon: <Gauge size={18} /> },
  { id: 'library', label: 'Library', icon: <Library size={18} /> },
  { id: 'add', label: 'Add Book', icon: <Search size={18} /> },
  { id: 'wanted', label: 'Wanted', icon: <BookOpen size={18} /> },
  { id: 'activity', label: 'Activity', icon: <Activity size={18} /> },
  { id: 'calendar', label: 'Calendar', icon: <CalendarClock size={18} /> },
  { id: 'settings', label: 'Settings', icon: <Settings size={18} /> },
  { id: 'system', label: 'System', icon: <Stethoscope size={18} /> }
];

type AppShellProps = {
  activeView: string;
  onNavigate: (view: string) => void;
  children: ReactNode;
};

export function AppShell({ activeView, onNavigate, children }: AppShellProps) {
  return (
    <div className="appShell">
      <aside className="sidebar">
        <div className="brand">
          <Download size={26} />
          <div>
            <strong>AudioBookarr</strong>
            <span>Readarr for listeners</span>
          </div>
        </div>

        <nav>
          {navItems.map((item) => (
            <button
              className={item.id === activeView ? 'navItem active' : 'navItem'}
              key={item.id}
              onClick={() => onNavigate(item.id)}
              type="button"
            >
              {item.icon}
              {item.label}
            </button>
          ))}
        </nav>
      </aside>

      <main className="mainContent">{children}</main>
    </div>
  );
}
