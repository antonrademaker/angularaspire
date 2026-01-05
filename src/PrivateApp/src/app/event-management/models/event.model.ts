export interface Event {
  id: string;
  title: string;
  description: string;
  detailedDescription?: string;
  slug: string;
  startDate: string;
  endDate: string;
  status: EventStatus;
  logoUrl?: string;
  primaryColor?: string;
  seriesId?: string;
  createdBy: string;
  updatedBy: string;
  createdAt: string;
  updatedAt: string;
}

export enum EventStatus {
  Draft = 0,
  Published = 1,
  Cancelled = 2,
  Completed = 3,
  Postponed = 4
}

export interface EventSearchResult {
  events: Event[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface EventSearchRequest {
  searchTerm?: string;
  startDate?: string;
  endDate?: string;
  status?: EventStatus;
  page?: number;
  pageSize?: number;
  sortAscending?: boolean;
}
