import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ApiError } from '../api/client';
import { loadTemplatesApi } from '../api/loadTemplates';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { SearchIcon } from '../components/Icons';
import { Layout } from '../components/Layout';
import { AddProductsModal } from '../features/AddProductsModal';
import { DAY_FIELDS, DAY_LABELS, type DayField, type SkuListItem, type Uom } from '../types';

/** A grid row while it is being edited — quantities stay as strings so the inputs can be cleared. */
interface GridRow {
  /** Stable key: the line UID from the server, or a generated one for new rows. */
  key: string;
  skuCode: string;
  skuName: string;
  uomCode: string;
  availableUoms: Uom[];
  quantities: Record<DayField, string>;
}

function emptyQuantities(): Record<DayField, string> {
  return DAY_FIELDS.reduce(
    (acc, field) => ({ ...acc, [field]: '0' }),
    {} as Record<DayField, string>,
  );
}

/** Trims trailing zeros so 5.00 shows as 5, matching the screenshots. */
function formatQty(value: number): string {
  return Number.isInteger(value) ? String(value) : String(value);
}

export function LoadTemplateFormPage() {
  const { uid } = useParams<{ uid: string }>();
  const navigate = useNavigate();
  const isEdit = Boolean(uid);

  const [templateName, setTemplateName] = useState('');
  const [description, setDescription] = useState('');
  const [rows, setRows] = useState<GridRow[]>([]);
  const [selectedKeys, setSelectedKeys] = useState<Set<string>>(new Set());

  const [gridSearch, setGridSearch] = useState('');
  const [showAddProducts, setShowAddProducts] = useState(false);
  const [confirmRemove, setConfirmRemove] = useState(false);

  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<string[]>([]);
  const [notice, setNotice] = useState<string | null>(null);

  // Load the existing template when editing.
  useEffect(() => {
    if (!uid) return;

    const controller = new AbortController();

    const run = async () => {
      setLoading(true);
      setError(null);

      try {
        const detail = await loadTemplatesApi.getByUid(uid, controller.signal);

        setTemplateName(detail.templateName);
        setDescription(detail.templateDescription ?? '');
        setRows(
          detail.lines.map((line) => ({
            key: line.uid,
            skuCode: line.skuCode,
            skuName: line.skuName,
            uomCode: line.uomCode,
            availableUoms: line.availableUoms,
            quantities: DAY_FIELDS.reduce(
              (acc, field) => ({ ...acc, [field]: formatQty(line[field]) }),
              {} as Record<DayField, string>,
            ),
          })),
        );
      } catch (err) {
        if ((err as Error).name === 'AbortError') return;
        setError(err instanceof ApiError ? err.message : 'Failed to load the template.');
      } finally {
        setLoading(false);
      }
    };

    void run();
    return () => controller.abort();
  }, [uid]);

  // Filters the grid as the user types — search on key press, no button needed.
  const visibleRows = useMemo(() => {
    const term = gridSearch.trim().toLowerCase();
    if (!term) return rows;

    return rows.filter(
      (row) =>
        row.skuCode.toLowerCase().includes(term) || row.skuName.toLowerCase().includes(term),
    );
  }, [rows, gridSearch]);

  const existingSkuCodes = useMemo(() => rows.map((row) => row.skuCode), [rows]);

  const allVisibleSelected =
    visibleRows.length > 0 && visibleRows.every((row) => selectedKeys.has(row.key));

  const toggleRow = (key: string, checked: boolean) => {
    setSelectedKeys((current) => {
      const next = new Set(current);
      if (checked) next.add(key);
      else next.delete(key);
      return next;
    });
  };

  const toggleAllVisible = (checked: boolean) => {
    setSelectedKeys((current) => {
      const next = new Set(current);
      for (const row of visibleRows) {
        if (checked) next.add(row.key);
        else next.delete(row.key);
      }
      return next;
    });
  };

  const updateRow = (key: string, patch: Partial<GridRow>) => {
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  };

  const updateQuantity = (key: string, field: DayField, value: string) => {
    // Digits and one decimal point only.
    if (value !== '' && !/^\d*\.?\d{0,2}$/.test(value)) return;

    setRows((current) =>
      current.map((row) =>
        row.key === key ? { ...row, quantities: { ...row.quantities, [field]: value } } : row,
      ),
    );
  };

  const handleAddProducts = (skus: SkuListItem[]) => {
    // The modal already excludes what is in the grid; this guards against a stale list.
    const present = new Set(rows.map((row) => row.skuCode));
    const additions = skus
      .filter((sku) => !present.has(sku.code))
      .map<GridRow>((sku) => ({
        key: `new-${sku.code}-${crypto.randomUUID()}`,
        skuCode: sku.code,
        skuName: sku.name,
        uomCode: sku.uoms[0]?.code ?? '',
        availableUoms: sku.uoms,
        quantities: emptyQuantities(),
      }));

    const skipped = skus.length - additions.length;

    setRows((current) => [...current, ...additions]);
    setShowAddProducts(false);
    setNotice(
      skipped > 0
        ? `${additions.length} product(s) added. ${skipped} already in the list and ignored.`
        : `${additions.length} product(s) added.`,
    );
  };

  const removeSelected = () => {
    setRows((current) => current.filter((row) => !selectedKeys.has(row.key)));
    setSelectedKeys(new Set());
    setConfirmRemove(false);
    setNotice('Selected products removed from the list. Click Save to persist the change.');
  };

  const handleSave = async () => {
    setSaving(true);
    setError(null);
    setFieldErrors([]);
    setNotice(null);

    const payload = {
      templateName: templateName.trim(),
      templateDescription: description.trim() ? description.trim() : null,
      lines: rows.map((row) => ({
        skuCode: row.skuCode,
        uomCode: row.uomCode,
        ...DAY_FIELDS.reduce(
          (acc, field) => ({ ...acc, [field]: Number(row.quantities[field] || 0) }),
          {} as Record<DayField, number>,
        ),
      })),
    };

    try {
      if (uid) {
        await loadTemplatesApi.update(uid, payload);
      } else {
        await loadTemplatesApi.create(payload);
      }

      navigate('/load-templates');
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
        setFieldErrors(err.fieldErrors.length > 1 ? err.fieldErrors : []);
      } else {
        setError('Failed to save the template.');
      }
    } finally {
      setSaving(false);
    }
  };

  const title = isEdit ? 'Edit Load Template' : 'Add Load Template';

  return (
    <Layout
      title={title}
      crumbs={[
        { label: 'Home', to: '/' },
        { label: 'Maintain Load Request Template', to: '/load-templates' },
        { label: title },
      ]}
    >
      {error && (
        <div className="alert alert--error">
          {error}
          {fieldErrors.length > 0 && (
            <ul>
              {fieldErrors.map((message) => (
                <li key={message}>{message}</li>
              ))}
            </ul>
          )}
        </div>
      )}
      {notice && <div className="alert alert--success">{notice}</div>}

      {loading ? (
        <div className="card loading">Loading template…</div>
      ) : (
        <>
          <div className="card">
            <div className="form-grid">
              <div className="field">
                <label className="field__label" htmlFor="name">
                  Template Name
                </label>
                <input
                  id="name"
                  className="input"
                  placeholder="Enter Name"
                  maxLength={250}
                  value={templateName}
                  onChange={(event) => setTemplateName(event.target.value)}
                />
              </div>

              <div className="field">
                <label className="field__label" htmlFor="description">
                  Description
                </label>
                <textarea
                  id="description"
                  className="textarea"
                  placeholder="Enter Template Description"
                  maxLength={250}
                  value={description}
                  onChange={(event) => setDescription(event.target.value)}
                />
              </div>
            </div>
          </div>

          <h2 className="section-title">Model Quantity for Load Template Items</h2>

          <div className="grid-search">
            <div className="search-box">
              <input
                className="input"
                placeholder="Search by SKU Code/Name"
                value={gridSearch}
                onChange={(event) => setGridSearch(event.target.value)}
              />
              <span className="search-box__icon">
                <SearchIcon />
              </span>
            </div>

            <button
              type="button"
              className="btn btn--primary btn--wide"
              onClick={() => {
                setNotice(null);
                setShowAddProducts(true);
              }}
            >
              Add Product
            </button>
          </div>

          <div className="table-wrap">
            <div className="table-scroll">
              <table className="table">
                <thead>
                  <tr>
                    <th className="col-check" rowSpan={2}>
                      <input
                        type="checkbox"
                        checked={allVisibleSelected}
                        onChange={(event) => toggleAllVisible(event.target.checked)}
                        aria-label="Select all rows"
                      />
                    </th>
                    <th rowSpan={2} className="col-sku-code">
                      SKU Code
                    </th>
                    <th rowSpan={2} className="col-sku-name">
                      SKU Name
                    </th>
                    <th rowSpan={2}>UOM</th>
                    <th className="table__group-head" colSpan={DAY_FIELDS.length}>
                      Load
                    </th>
                  </tr>
                  <tr>
                    {DAY_FIELDS.map((field) => (
                      <th key={field}>{DAY_LABELS[field]}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {visibleRows.length === 0 && (
                    <tr>
                      <td colSpan={4 + DAY_FIELDS.length} className="table__empty">
                        {rows.length === 0
                          ? 'No products yet. Use "Add Product" to add items to this template.'
                          : 'No products match your search.'}
                      </td>
                    </tr>
                  )}

                  {visibleRows.map((row) => (
                    <tr key={row.key}>
                      <td className="col-check">
                        <input
                          type="checkbox"
                          checked={selectedKeys.has(row.key)}
                          onChange={(event) => toggleRow(row.key, event.target.checked)}
                          aria-label={`Select ${row.skuCode}`}
                        />
                      </td>
                      <td className="col-sku-code">{row.skuCode}</td>
                      <td className="col-sku-name">{row.skuName}</td>
                      <td>
                        {/* Shows the UOM Name, saves the UOM Code — per the task note. */}
                        <select
                          className="uom-select"
                          value={row.uomCode}
                          onChange={(event) => updateRow(row.key, { uomCode: event.target.value })}
                          aria-label={`UOM for ${row.skuCode}`}
                        >
                          {row.availableUoms.map((uom) => (
                            <option key={uom.code} value={uom.code}>
                              {uom.code}
                            </option>
                          ))}
                        </select>
                      </td>

                      {DAY_FIELDS.map((field) => (
                        <td key={field}>
                          <input
                            className="qty-input"
                            inputMode="decimal"
                            value={row.quantities[field]}
                            onChange={(event) => updateQuantity(row.key, field, event.target.value)}
                            onBlur={(event) => {
                              if (event.target.value === '') {
                                updateQuantity(row.key, field, '0');
                              }
                            }}
                            aria-label={`${DAY_LABELS[field]} for ${row.skuCode}`}
                          />
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="form-actions">
            <button
              type="button"
              className="btn btn--ghost btn--wide"
              onClick={() => navigate('/load-templates')}
              disabled={saving}
            >
              Back
            </button>

            <button
              type="button"
              className="btn btn--ghost btn--wide"
              disabled={selectedKeys.size === 0 || saving}
              onClick={() => setConfirmRemove(true)}
            >
              Delete Selected From List
            </button>

            <button
              type="button"
              className="btn btn--primary btn--wide"
              onClick={handleSave}
              disabled={saving}
            >
              {saving ? 'Saving…' : 'Save'}
            </button>
          </div>
        </>
      )}

      {showAddProducts && (
        <AddProductsModal
          existingSkuCodes={existingSkuCodes}
          onClose={() => setShowAddProducts(false)}
          onAdd={handleAddProducts}
        />
      )}

      {confirmRemove && (
        <ConfirmDialog
          title="Remove Products"
          message={`Are you sure you want to delete ${selectedKeys.size} selected product(s) from the list?`}
          onConfirm={removeSelected}
          onCancel={() => setConfirmRemove(false)}
        />
      )}
    </Layout>
  );
}
