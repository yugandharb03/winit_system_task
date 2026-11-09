import { api, toQuery } from './client';
import type { PagedResult, SkuListItem, Uom } from '../types';

export interface SkuQuery {
  search?: string;
  /** SKU codes already in the grid — the API filters them out. */
  excludeCodes?: string[];
  page?: number;
  pageSize?: number;
}

export const skusApi = {
  search: (query: SkuQuery, signal?: AbortSignal) =>
    api.get<PagedResult<SkuListItem>>(
      `/skus${toQuery({
        search: query.search,
        excludeCodes: query.excludeCodes?.length ? query.excludeCodes.join(',') : undefined,
        page: query.page,
        pageSize: query.pageSize,
      })}`,
      signal,
    ),

  uomCatalogue: (signal?: AbortSignal) => api.get<Uom[]>('/skus/uoms', signal),
};
