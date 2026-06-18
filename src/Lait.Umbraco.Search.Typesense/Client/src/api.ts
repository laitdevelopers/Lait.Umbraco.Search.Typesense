import type {
  ContentEntity,
  IndexConfiguration,
  Result,
  SearchResponse,
} from "./types.js";

const BASE = "/umbraco/management/api/v1/typesense";

/**
 * Thin typed wrapper over the Typesense Management API. A token getter is injected so the
 * caller (the dashboard element) can resolve the current backoffice bearer token from the
 * auth context.
 */
export class TypesenseApi {
  #getToken: () => Promise<string | undefined>;

  constructor(getToken: () => Promise<string | undefined>) {
    this.#getToken = getToken;
  }

  async #request<T>(path: string, init?: RequestInit): Promise<T> {
    const token = await this.#getToken();

    const response = await fetch(`${BASE}${path}`, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(init?.headers ?? {}),
      },
    });

    if (!response.ok) {
      throw new Error(`Request failed (${response.status} ${response.statusText})`);
    }

    const text = await response.text();
    return (text ? JSON.parse(text) : undefined) as T;
  }

  getIndices(): Promise<IndexConfiguration[]> {
    return this.#request<IndexConfiguration[]>("/index", { method: "GET" });
  }

  saveIndex(configuration: IndexConfiguration): Promise<Result> {
    return this.#request<Result>("/index", {
      method: "POST",
      body: JSON.stringify(configuration),
    });
  }

  buildIndex(configuration: IndexConfiguration): Promise<Result> {
    return this.#request<Result>("/index/build", {
      method: "POST",
      body: JSON.stringify(configuration),
    });
  }

  deleteIndex(id: number): Promise<Result> {
    return this.#request<Result>(`/index/${id}`, { method: "DELETE" });
  }

  search(indexId: number, query: string): Promise<SearchResponse> {
    return this.#request<SearchResponse>(
      `/search?indexId=${indexId}&query=${encodeURIComponent(query)}`,
      { method: "GET" }
    );
  }

  getContentTypes(): Promise<ContentEntity[]> {
    return this.#request<ContentEntity[]>("/content-types", { method: "GET" });
  }

  getProperties(alias: string): Promise<ContentEntity[]> {
    return this.#request<ContentEntity[]>(
      `/content-types/${encodeURIComponent(alias)}/properties`,
      { method: "GET" }
    );
  }
}
