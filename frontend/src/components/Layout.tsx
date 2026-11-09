import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { ChevronDownIcon } from './Icons';

export interface Crumb {
  label: string;
  to?: string;
}

interface LayoutProps {
  title: string;
  crumbs: Crumb[];
  /** Optional "Search Criteria" strip shown under the breadcrumb. */
  criteria?: ReactNode;
  children: ReactNode;
}

export function Layout({ title, crumbs, criteria, children }: LayoutProps) {
  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <span className="brand__mark">WINIT</span>
          <span className="brand__tag">
            thinking
            <br />
            mobile
          </span>
        </div>
        <div className="topbar__user">
          {/* <strong>Musfiq Ali Khan</strong> */}
          {/* <span>Admin</span> */}
        </div>
        <ChevronDownIcon />
      </header>

      <main className="page">
        <h1 className="page__title">{title}</h1>

        <nav className="breadcrumb">
          {crumbs.map((crumb, index) => (
            <span key={`${crumb.label}-${index}`}>
              {index > 0 && <span className="breadcrumb__sep">&gt;</span>}
              {crumb.to ? <Link to={crumb.to}>{crumb.label}</Link> : crumb.label}
            </span>
          ))}
        </nav>

        {criteria && <div className="criteria">{criteria}</div>}

        {children}
      </main>
    </div>
  );
}
