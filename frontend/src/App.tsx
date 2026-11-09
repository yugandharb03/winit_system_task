import { Navigate, Route, Routes } from 'react-router-dom';
import { LoadTemplateFormPage } from './pages/LoadTemplateFormPage';
import { LoadTemplateListPage } from './pages/LoadTemplateListPage';

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/load-templates" replace />} />
      <Route path="/load-templates" element={<LoadTemplateListPage />} />
      <Route path="/load-templates/new" element={<LoadTemplateFormPage />} />
      {/* Keyed so switching between templates remounts with fresh state. */}
      <Route path="/load-templates/:uid/edit" element={<LoadTemplateFormPage />} />
      <Route path="*" element={<Navigate to="/load-templates" replace />} />
    </Routes>
  );
}
