export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface Uom {
  code: string;
  name: string;
}

export interface LoadTemplateListItem {
  uid: string;
  templateName: string;
  templateDescription: string | null;
  itemCount: number;
}

export interface LoadTemplateLine {
  uid: string;
  skuCode: string;
  skuName: string;
  uomCode: string;
  uomName: string;
  availableUoms: Uom[];
  monQty: number;
  tueQty: number;
  wedQty: number;
  thuQty: number;
  friQty: number;
  satQty: number;
  sunQty: number;
}

export interface LoadTemplateDetail {
  uid: string;
  templateName: string;
  templateDescription: string | null;
  itemCount: number;
  lines: LoadTemplateLine[];
}

export interface SkuListItem {
  uid: string;
  code: string;
  name: string;
  uoms: Uom[];
}

export interface SaveLoadTemplateLine {
  skuCode: string;
  uomCode: string;
  monQty: number;
  tueQty: number;
  wedQty: number;
  thuQty: number;
  friQty: number;
  satQty: number;
  sunQty: number;
}

export interface SaveLoadTemplateRequest {
  templateName: string;
  templateDescription: string | null;
  lines: SaveLoadTemplateLine[];
}

export type SortDirection = 'asc' | 'desc';

/** Columns the API allows sorting on. */
export type SortableColumn = 'templateName' | 'templateDescription' | 'itemCount';

/** The seven weekday quantity fields, in display order. */
export const DAY_FIELDS = [
  'monQty',
  'tueQty',
  'wedQty',
  'thuQty',
  'friQty',
  'satQty',
  'sunQty',
] as const;

export type DayField = (typeof DAY_FIELDS)[number];

export const DAY_LABELS: Record<DayField, string> = {
  monQty: 'MON (Qty)',
  tueQty: 'Tue (Qty)',
  wedQty: 'Wed (Qty)',
  thuQty: 'Thu (Qty)',
  friQty: 'Fri (Qty)',
  satQty: 'Sat (Qty)',
  sunQty: 'Sun (Qty)',
};
