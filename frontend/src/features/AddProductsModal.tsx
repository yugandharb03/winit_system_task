import { useEffect, useMemo, useState } from 'react';
import { ApiError } from '../api/client';
import { skusApi } from '../api/skus';
import { Modal } from '../components/Modal';
import { useDebouncedValue } from '../hooks/useDebouncedValue';
import type { SkuListItem } from '../types';

interface AddProductsModalProps {
  /** SKU codes already in the grid — the API filters them out, so they can never be added twice. */
  existingSkuCodes: string[];
  onClose: () => void;
  onAdd: (skus: SkuListItem[]) => void;
}

export function AddProductsModal({ existingSkuCodes, onClose, onAdd }: AddProductsModalProps) {
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 300);

  const [results, setResults] = useState<SkuListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<Record<string, SkuListItem>>({});

  // Stable dependency so the effect does not re-run on every parent render.
  const excludeKey = useMemo(() => [...existingSkuCodes].sort().join(','), [existingSkuCodes]);

  // Search runs on key press (debounced), per the task brief.
  useEffect(() => {
    const controller = new AbortController();

    const run = async () => {
      setLoading(true);
      setError(null);

      try {
        const result = await skusApi.search(
          {
            search: debouncedSearch.trim(),
            excludeCodes: excludeKey ? excludeKey.split(',') : [],
            page: 1,
            pageSize: 100,
          },
          controller.signal,
        );
        setResults(result.items);
      } catch (err) {
        if ((err as Error).name === 'AbortError') return;
        setError(err instanceof ApiError ? err.message : 'Failed to load products.');
        setResults([]);
      } finally {
        setLoading(false);
      }
    };

    void run();
    return () => controller.abort();
  }, [debouncedSearch, excludeKey]);

  const selectedList = Object.values(selected);
  const allVisibleSelected = results.length > 0 && results.every((sku) => selected[sku.code]);

  const toggleOne = (sku: SkuListItem, checked: boolean) => {
    setSelected((current) => {
      const next = { ...current };
      if (checked) {
        next[sku.code] = sku;
      } else {
        delete next[sku.code];
      }
      return next;
    });
  };

  const toggleAll = (checked: boolean) => {
    setSelected((current) => {
      const next = { ...current };
      for (const sku of results) {
        if (checked) {
          next[sku.code] = sku;
        } else {
          delete next[sku.code];
        }
      }
      return next;
    });
  };

  return (
    <Modal
      title="Add Products"
      onClose={onClose}
      footer={
        <button
          type="button"
          className="btn btn--primary"
          disabled={selectedList.length === 0}
          onClick={() => onAdd(selectedList)}
        >
          Add Selected Product
        </button>
      }
    >
      <div className="modal__search">
        <input
          className="input"
          placeholder="Search By Product Code / Description"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          autoFocus
        />
        {/* The list already updates on key press; this is here to match the screenshot. */}
        <button type="button" className="btn btn--ghost" onClick={() => setSearch(search)}>
          Search
        </button>
      </div>

      {error && <div className="alert alert--error">{error}</div>}

      <table className="table">
        <thead>
          <tr>
            <th className="col-check">
              <input
                type="checkbox"
                checked={allVisibleSelected}
                onChange={(event) => toggleAll(event.target.checked)}
                aria-label="Select all products"
              />
            </th>
            <th>SKU Code</th>
            <th>SKU Name</th>
          </tr>
        </thead>
        <tbody>
          {loading && (
            <tr>
              <td colSpan={3} className="table__empty">
                Loading…
              </td>
            </tr>
          )}

          {!loading && results.length === 0 && (
            <tr>
              <td colSpan={3} className="table__empty">
                {search.trim()
                  ? 'No products match your search.'
                  : 'Every product is already in the list.'}
              </td>
            </tr>
          )}

          {!loading &&
            results.map((sku) => (
              <tr key={sku.code}>
                <td className="col-check">
                  <input
                    type="checkbox"
                    checked={Boolean(selected[sku.code])}
                    onChange={(event) => toggleOne(sku, event.target.checked)}
                    aria-label={`Select ${sku.code}`}
                  />
                </td>
                <td>{sku.code}</td>
                <td>{sku.name}</td>
              </tr>
            ))}
        </tbody>
      </table>

      <p className="modal__hint">
        {selectedList.length} selected · products already in the list are not shown.
      </p>
    </Modal>
  );
}
