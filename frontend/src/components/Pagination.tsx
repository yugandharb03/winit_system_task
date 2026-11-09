interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

/** Builds a compact page list: 1 … 4 5 6 … 20 */
function buildPages(current: number, total: number): (number | 'gap')[] {
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const pages = new Set<number>([1, total, current, current - 1, current + 1]);
  const sorted = [...pages].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);

  const result: (number | 'gap')[] = [];
  let previous = 0;

  for (const page of sorted) {
    if (previous && page - previous > 1) {
      result.push('gap');
    }
    result.push(page);
    previous = page;
  }

  return result;
}

export function Pagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
}: PaginationProps) {
  const first = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, totalCount);

  return (
    <div className="pager">
      <span className="pager__info">
        {totalCount === 0 ? 'No records' : `Showing ${first}–${last} of ${totalCount}`}
      </span>

      <div className="pager__controls">
        <button
          type="button"
          className="pager__btn"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          aria-label="Previous page"
        >
          &lt;
        </button>

        {buildPages(page, Math.max(totalPages, 1)).map((entry, index) =>
          entry === 'gap' ? (
            <span key={`gap-${index}`} className="pager__info">
              …
            </span>
          ) : (
            <button
              key={entry}
              type="button"
              className={entry === page ? 'pager__btn is-active' : 'pager__btn'}
              onClick={() => onPageChange(entry)}
              aria-current={entry === page ? 'page' : undefined}
            >
              {entry}
            </button>
          ),
        )}

        <button
          type="button"
          className="pager__btn"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          aria-label="Next page"
        >
          &gt;
        </button>
      </div>

      <label className="pager__size">
        Rows
        <select value={pageSize} onChange={(event) => onPageSizeChange(Number(event.target.value))}>
          {[5, 10, 20, 50].map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}
