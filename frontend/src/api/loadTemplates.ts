import { api, toQuery } from './client';
import type {
  LoadTemplateDetail,
  LoadTemplateListItem,
  PagedResult,
  SaveLoadTemplateRequest,
  SortDirection,
  SortableColumn,
} from '../types';

export interface LoadTemplateQuery {
  name?: string;
  sortBy?: SortableColumn | '';
  sortDir?: SortDirection;
  page?: number;
  pageSize?: number;
}

export const loadTemplatesApi = {
  list: (query: LoadTemplateQuery, signal?: AbortSignal) =>
    api.get<PagedResult<LoadTemplateListItem>>(`/load-templates${toQuery({ ...query })}`, signal),

  getByUid: (uid: string, signal?: AbortSignal) =>
    api.get<LoadTemplateDetail>(`/load-templates/${encodeURIComponent(uid)}`, signal),

  create: (body: SaveLoadTemplateRequest) =>
    api.post<LoadTemplateDetail>('/load-templates', body),

  update: (uid: string, body: SaveLoadTemplateRequest) =>
    api.put<LoadTemplateDetail>(`/load-templates/${encodeURIComponent(uid)}`, body),

  remove: (uid: string) => api.delete(`/load-templates/${encodeURIComponent(uid)}`),
};
