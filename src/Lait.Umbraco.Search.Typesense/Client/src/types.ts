export interface ContentEntity {
  alias: string;
  name: string;
  icon?: string;
}

export interface ContentData {
  contentType: ContentEntity;
  properties: ContentEntity[];
  propertiesDescription?: string[];
}

export interface IndexConfiguration {
  id: number;
  name: string;
  contentData: ContentData[];
}

export interface Result {
  success: boolean;
  failure: boolean;
  error: string;
}

export interface SearchResponse {
  itemsCount: number;
  pagesCount: number;
  itemsPerPage: number;
  hits: Array<Record<string, string>>;
}
