import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiError } from '../api/client';
import { loadTemplatesApi } from '../api/loadTemplates';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { PencilIcon, TrashIcon } from '../components/Icons';
import { Layout } from '../components/Layout';
import { Pagination } from '../components/Pagination';
import type { LoadTemplateListItem, SortDirection, SortableColumn } from '../types';

const COLUMNS: { key: SortableColumn; label: string }[] = [
  { key: 'templateName', label: 'Template Name' },
  { key: 'templateDescription', label: 'Template Description' },
  { key: 'itemCount', label: 'No of Items' },
];

export function LoadTemplateListPage() {
  const navigate = useNavigate();

  // What the user is typing, versus the filter actually applied by Search.
  const [nameInput, setNameInput] = useState('');
  const [appliedName, setAppliedName] = useState('');

  const [sortBy, setSortBy] = useState<SortableColumn | ''>('');
  const [sortDir, setSortDir] = useState<SortDirection>('asc');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(5);

  const [items, setItems] = useState<LoadTemplateListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const [pendingDelete, setPendingDelete] = useState<LoadTemplateListItem | null>(null);
  const [deleting, setDeleting] = useState(false);

  const fetchPage = useCallback(
    async (signal?: AbortSignal) => {
      setLoading(true);
      setError(null);

      try {
        const result = await loadTemplatesApi.list(
          { name: appliedName, sortBy, sortDir, page, pageSize },
          signal,
        );

        setItems(result.items);
        setTotalCount(result.totalCount);
        setTotalPages(result.totalPages);
      } catch (err) {
        if ((err as Error).name === 'AbortError') return;
        setError(err instanceof ApiError ? err.message : 'Failed to load templates.');
        setItems([]);
        setTotalCount(0);
        setTotalPages(0);
      } finally {
        setLoading(false);
      }
    },
    [appliedName, sortBy, sortDir, page, pageSize],
  );

  useEffect(() => {
    const controller = new AbortController();
    void fetchPage(controller.signal);
    return () => controller.abort();
  }, [fetchPage]);

  const handleSearch = () => {
    setAppliedName(nameInput.trim());
    setPage(1);
  };

  const handleReset = () => {
    setNameInput('');
    setAppliedName('');
    setSortBy('');
    setSortDir('asc');
    setPage(1);
  };

  const handleSort = (column: SortableColumn) => {
    if (sortBy === column) {
      setSortDir((current) => (current === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortBy(column);
      setSortDir('asc');
    }
    setPage(1);
  };

  const confirmDelete = async () => {
    if (!pendingDelete) return;

    setDeleting(true);
    try {
      await loadTemplatesApi.remove(pendingDelete.uid);
      setNotice(`Template "${pendingDelete.templateName}" was deleted.`);
      setPendingDelete(null);

      // Step back a page if the last row on this page has just gone.
      if (items.length === 1 && page > 1) {
        setPage(page - 1);
      } else {
        await fetchPage();
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to delete the template.');
      setPendingDelete(null);
    } finally {
      setDeleting(false);
    }
  };

  return (
    <Layout
      title="Maintain Load Request Template"
      crumbs={[{ label: 'Home', to: '/' }, { label: 'Maintain Load Request Template' }]}
      criteria={
        <>
          Search Criteria: Name: <b>{appliedName || 'All'}</b>
        </>
      }
    >
      {notice && <div className="alert alert--success">{notice}</div>}
      {error && <div className="alert alert--error">{error}</div>}

      <div className="card filter-card">
        <div className="field">
          <label className="field__label" htmlFor="template-name">
            Name
          </label>
          <input
            id="template-name"
            className="input input--name"
            placeholder="Enter Name"
            value={nameInput}
            onChange={(event) => setNameInput(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') handleSearch();
            }}
          />
        </div>

        <button type="button" className="btn btn--primary btn--wide" onClick={handleSearch}>
          Search
        </button>
        <button type="button" className="btn btn--ghost btn--wide" onClick={handleReset}>
          Reset
        </button>
      </div>

      <div className="toolbar">
        <button
          type="button"
          className="btn btn--primary btn--wide"
          onClick={() => navigate('/load-templates/new')}
        >
          Add New
        </button>
      </div>

      <div className="table-wrap">
        <div className="table-scroll">
          <table className="table">
            <thead>
              <tr>
                <th className="col-sn">SN No</th>
                {COLUMNS.map((column) => (
                  <th
                    key={column.key}
                    className="is-sortable"
                    onClick={() => handleSort(column.key)}
                    aria-sort={
                      sortBy === column.key
                        ? sortDir === 'asc'
                          ? 'ascending'
                          : 'descending'
                        : 'none'
                    }
                  >
                    {column.label}
                    <span className={sortBy === column.key ? 'sort-arrow' : 'sort-arrow sort-arrow--idle'}>
                      {sortBy === column.key ? (sortDir === 'asc' ? '▲' : '▼') : '▲'}
                    </span>
                  </th>
                ))}
                <th className="col-actions">Action</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <tr>
                  <td colSpan={5} className="table__empty">
                    Loading…
                  </td>
                </tr>
              )}

              {!loading && items.length === 0 && (
                <tr>
                  <td colSpan={5} className="table__empty">
                    No load templates found.
                  </td>
                </tr>
              )}

              {!loading &&
                items.map((item, index) => (
                  <tr key={item.uid}>
                    <td className="col-sn">
                      {String((page - 1) * pageSize + index + 1).padStart(2, '0')}
                    </td>
                    <td>{item.templateName}</td>
                    <td>{item.templateDescription}</td>
                    {/* The brief shows an empty count as N/A. */}
                    <td>{item.itemCount > 0 ? item.itemCount : 'N/A'}</td>
                    <td className="col-actions">
                      <button
                        type="button"
                        className="icon-btn"
                        title="Edit"
                        aria-label={`Edit ${item.templateName}`}
                        onClick={() => navigate(`/load-templates/${encodeURIComponent(item.uid)}/edit`)}
                      >
                        <PencilIcon />
                      </button>
                      <button
                        type="button"
                        className="icon-btn icon-btn--danger"
                        title="Delete"
                        aria-label={`Delete ${item.templateName}`}
                        onClick={() => {
                          setNotice(null);
                          setPendingDelete(item);
                        }}
                      >
                        <TrashIcon />
                      </button>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>

        <Pagination
          page={page}
          pageSize={pageSize}
          totalCount={totalCount}
          totalPages={totalPages}
          onPageChange={setPage}
          onPageSizeChange={(size) => {
            setPageSize(size);
            setPage(1);
          }}
        />
      </div>

      {pendingDelete && (
        <ConfirmDialog
          title="Delete Load Template"
          message="Are you sure you want to delete?"
          busy={deleting}
          onConfirm={confirmDelete}
          onCancel={() => setPendingDelete(null)}
        />
      )}
    </Layout>
  );
}
