import {
  LitElement,
  html,
  customElement,
  state,
  nothing,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";
import {
  UMB_NOTIFICATION_CONTEXT,
  type UmbNotificationContext,
} from "@umbraco-cms/backoffice/notification";

import { TypesenseApi } from "./api.js";
import type {
  ContentData,
  ContentEntity,
  IndexConfiguration,
  SearchResponse,
} from "./types.js";

@customElement("typesense-search-dashboard")
export default class TypesenseSearchDashboardElement extends UmbElementMixin(
  LitElement,
) {
  #api = new TypesenseApi(() => this.#getToken());
  #authContext?: typeof UMB_AUTH_CONTEXT.TYPE;
  #notificationContext?: UmbNotificationContext;

  @state() private _indices: IndexConfiguration[] = [];
  @state() private _contentTypes: ContentEntity[] = [];
  @state() private _propertyCache: Record<string, ContentEntity[]> = {};
  @state() private _editing: IndexConfiguration | null = null;
  @state() private _busy = false;

  @state() private _searchIndexId: number | null = null;
  @state() private _searchQuery = "";
  @state() private _searchResults: SearchResponse | null = null;

  constructor() {
    super();
    this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => {
      this.#authContext = ctx;
    });
    this.consumeContext(UMB_NOTIFICATION_CONTEXT, (ctx) => {
      this.#notificationContext = ctx;
    });
  }

  override connectedCallback(): void {
    super.connectedCallback();
    this.#load();
  }

  async #getToken(): Promise<string | undefined> {
    return this.#authContext?.getLatestToken();
  }

  #peek(color: "positive" | "warning" | "danger", message: string) {
    this.#notificationContext?.peek(color, { data: { message } });
  }

  async #load() {
    try {
      this._busy = true;
      const [indices, contentTypes] = await Promise.all([
        this.#api.getIndices(),
        this.#api.getContentTypes(),
      ]);
      this._indices = indices ?? [];
      this._contentTypes = contentTypes ?? [];
    } catch (error) {
      this.#peek("danger", `Failed to load: ${(error as Error).message}`);
    } finally {
      this._busy = false;
    }
  }

  // ---- Index editing -------------------------------------------------------

  #startCreate() {
    this._editing = { id: 0, name: "", contentData: [] };
  }

  async #startEdit(index: IndexConfiguration) {
    // Deep clone to avoid mutating the list while editing.
    this._editing = JSON.parse(JSON.stringify(index)) as IndexConfiguration;

    // Pre-load the property lists for content types already in the index so their
    // saved property selections render immediately (instead of "No searchable properties").
    await Promise.all(
      this._editing.contentData.map((c) =>
        this.#ensureProperties(c.contentType.alias),
      ),
    );
  }

  #cancelEdit() {
    this._editing = null;
  }

  #isContentTypeSelected(alias: string): boolean {
    return !!this._editing?.contentData.some(
      (c) => c.contentType.alias === alias,
    );
  }

  async #toggleContentType(contentType: ContentEntity, checked: boolean) {
    if (!this._editing) return;

    if (checked) {
      await this.#ensureProperties(contentType.alias);
      this._editing = {
        ...this._editing,
        contentData: [
          ...this._editing.contentData,
          { contentType, properties: [] } as ContentData,
        ],
      };
    } else {
      this._editing = {
        ...this._editing,
        contentData: this._editing.contentData.filter(
          (c) => c.contentType.alias !== contentType.alias,
        ),
      };
    }
  }

  async #ensureProperties(alias: string) {
    if (this._propertyCache[alias]) return;
    try {
      const properties = await this.#api.getProperties(alias);
      this._propertyCache = {
        ...this._propertyCache,
        [alias]: properties ?? [],
      };
    } catch (error) {
      this.#peek(
        "danger",
        `Failed to load properties: ${(error as Error).message}`,
      );
    }
  }

  #isPropertySelected(alias: string, propertyAlias: string): boolean {
    const data = this._editing?.contentData.find(
      (c) => c.contentType.alias === alias,
    );
    return !!data?.properties.some((p) => p.alias === propertyAlias);
  }

  #toggleProperty(alias: string, property: ContentEntity, checked: boolean) {
    if (!this._editing) return;
    this._editing = {
      ...this._editing,
      contentData: this._editing.contentData.map((c) => {
        if (c.contentType.alias !== alias) return c;
        const properties = checked
          ? [...c.properties, property]
          : c.properties.filter((p) => p.alias !== property.alias);
        return { ...c, properties };
      }),
    };
  }

  async #saveEditing() {
    if (!this._editing) return;
    if (!this._editing.name.trim()) {
      this.#peek("warning", "Please provide a collection name.");
      return;
    }
    try {
      this._busy = true;
      const result = await this.#api.saveIndex(this._editing);
      if (result.success) {
        this.#peek("positive", "Collection saved.");
        this._editing = null;
        await this.#load();
      } else {
        this.#peek("danger", result.error || "Failed to save collection.");
      }
    } catch (error) {
      this.#peek(
        "danger",
        `Failed to save collection: ${(error as Error).message}`,
      );
    } finally {
      this._busy = false;
    }
  }

  // ---- Index actions -------------------------------------------------------

  async #build(index: IndexConfiguration) {
    try {
      this._busy = true;
      const result = await this.#api.buildIndex(index);
      result.success
        ? this.#peek("positive", `Collection "${index.name}" built.`)
        : this.#peek("danger", result.error || "Failed to build collection.");
    } catch (error) {
      this.#peek(
        "danger",
        `Failed to build collection: ${(error as Error).message}`,
      );
    } finally {
      this._busy = false;
    }
  }

  async #delete(index: IndexConfiguration) {
    try {
      this._busy = true;
      const result = await this.#api.deleteIndex(index.id);
      if (result.success) {
        this.#peek("positive", `Collection "${index.name}" deleted.`);
        if (this._searchIndexId === index.id) {
          this._searchIndexId = null;
          this._searchResults = null;
        }
        await this.#load();
      } else {
        this.#peek("danger", result.error || "Failed to delete collection.");
      }
    } catch (error) {
      this.#peek(
        "danger",
        `Failed to delete collection: ${(error as Error).message}`,
      );
    } finally {
      this._busy = false;
    }
  }

  async #runSearch() {
    if (this._searchIndexId === null) return;
    try {
      this._busy = true;
      this._searchResults = await this.#api.search(
        this._searchIndexId,
        this._searchQuery,
      );
    } catch (error) {
      this.#peek("danger", `Search failed: ${(error as Error).message}`);
    } finally {
      this._busy = false;
    }
  }

  // ---- Rendering -----------------------------------------------------------

  override render() {
    const headline = this._editing
      ? this._editing.id
        ? "Edit Collection Definition"
        : "Add Collection Definition"
      : "Typesense Collections";
    return html`
      ${this.#renderStyles()}
      <div class="dashboard">
        ${this._editing
          ? html`<button
              type="button"
              class="back-link"
              @click=${this.#cancelEdit}
            >
              &larr; Back to overview
            </button>`
          : nothing}
        <uui-box headline=${headline}>
          ${this._editing ? this.#renderEditor() : this.#renderList()}
        </uui-box>
        ${this._editing ? nothing : this.#renderSearch()}
        ${this._busy ? html`<uui-loader-bar></uui-loader-bar>` : nothing}
      </div>
    `;
  }

  #renderList() {
    return html`
      <h4>Manage Typesense Collections</h4>
      <p>
        Typesense is an open-source, typo-tolerant search engine optimised for
        fast, relevant, instant search experiences. You can self-host it or run
        it on Typesense Cloud.
      </p>
      <p>
        The Typesense integration provides Search as a Service through an
        externally hosted search engine, offering web search across the website
        based on the content payload pushed from the website to Typesense.
      </p>
      <p>
        To get started, you need to create a collection and define the content
        schema &ndash; document types and properties. Then you can build your
        collection, push data to Typesense and run searches across created
        collections.
      </p>
      <p>
        <a href="https://typesense.org/docs/" target="_blank" rel="noopener">
          Read more about integrating Typesense Search
        </a>
      </p>

      <uui-button
        look="primary"
        label="Add New Collection Definition"
        @click=${this.#startCreate}
      >
        Add New Collection Definition
      </uui-button>

      ${this._indices.length === 0
        ? nothing
        : html`
            <uui-table class="indices">
              <uui-table-head>
                <uui-table-head-cell>Name</uui-table-head-cell>
                <uui-table-head-cell>Content types</uui-table-head-cell>
                <uui-table-head-cell>Actions</uui-table-head-cell>
              </uui-table-head>
              ${this._indices.map(
                (index) => html`
                  <uui-table-row>
                    <uui-table-cell>${index.name}</uui-table-cell>
                    <uui-table-cell>
                      ${index.contentData
                        .map((c) => c.contentType.name)
                        .join(", ")}
                    </uui-table-cell>
                    <uui-table-cell>
                      <uui-button
                        look="secondary"
                        label="Edit"
                        @click=${() => this.#startEdit(index)}
                        >Edit</uui-button
                      >
                      <uui-button
                        look="primary"
                        label="Build"
                        @click=${() => this.#build(index)}
                        >Build</uui-button
                      >
                      <uui-button
                        look="secondary"
                        label="Search"
                        @click=${() => (this._searchIndexId = index.id)}
                        >Search</uui-button
                      >
                      <uui-button
                        look="primary"
                        color="danger"
                        label="Delete"
                        @click=${() => this.#delete(index)}
                        >Delete</uui-button
                      >
                    </uui-table-cell>
                  </uui-table-row>
                `,
              )}
            </uui-table>
          `}
    `;
  }

  #renderEditor() {
    const editing = this._editing!;
    const isExisting = editing.id !== 0;
    return html`
      <div class="form-row">
        <div class="form-label">
          <strong>Name</strong>
          <small
            >Please enter a name for the index. After save, you will not be able
            to change it.</small
          >
        </div>
        <div class="form-content">
          <uui-input
            .value=${editing.name}
            placeholder="Enter a name"
            ?disabled=${isExisting}
            @input=${(e: InputEvent) =>
              (this._editing = {
                ...editing,
                name: (e.target as HTMLInputElement).value,
              })}
          ></uui-input>
        </div>
      </div>

      <div class="form-row">
        <div class="form-label">
          <strong>Document Types</strong>
          <small
            >Please select the document types you would like to index, and
            choose the fields to include.</small
          >
        </div>
        <div class="form-content">
          <div class="doc-types">
            ${this._contentTypes.map((ct) => this.#renderContentType(ct))}
          </div>
        </div>
      </div>

      <div class="actions">
        <uui-button look="primary" label="Save" @click=${this.#saveEditing}
          >Save</uui-button
        >
        <uui-button look="secondary" label="Cancel" @click=${this.#cancelEdit}
          >Cancel</uui-button
        >
      </div>
    `;
  }

  #renderContentType(contentType: ContentEntity) {
    const selected = this.#isContentTypeSelected(contentType.alias);
    const properties = this._propertyCache[contentType.alias] ?? [];
    const iconName = contentType.icon?.split(" ")[0] || "icon-document";
    return html`
      <div class="doc-type ${selected ? "selected" : ""}">
        <button
          type="button"
          class="doc-type-header"
          @click=${() => this.#toggleContentType(contentType, !selected)}
        >
          <umb-icon name=${iconName}></umb-icon>
          <span class="doc-type-name">${contentType.name}</span>
          ${selected
            ? html`<uui-tag color="positive" look="primary">Selected</uui-tag>`
            : nothing}
        </button>
        ${selected
          ? html`
              <div class="properties">
                ${properties.length === 0
                  ? html`<small>No searchable properties.</small>`
                  : properties.map(
                      (p) => html`
                        <uui-checkbox
                          label=${p.name}
                          ?checked=${this.#isPropertySelected(
                            contentType.alias,
                            p.alias,
                          )}
                          @change=${(e: Event) =>
                            this.#toggleProperty(
                              contentType.alias,
                              p,
                              (e.target as HTMLInputElement).checked,
                            )}
                        >
                          ${p.name} <em>(${p.alias})</em>
                        </uui-checkbox>
                      `,
                    )}
              </div>
            `
          : nothing}
      </div>
    `;
  }

  #renderSearch() {
    if (this._searchIndexId === null) return nothing;
    const index = this._indices.find((i) => i.id === this._searchIndexId);
    return html`
      <uui-box headline=${`Search: ${index?.name ?? ""}`}>
        <div class="search-bar">
          <uui-input
            .value=${this._searchQuery}
            placeholder="Search query (empty matches everything)"
            @input=${(e: InputEvent) =>
              (this._searchQuery = (e.target as HTMLInputElement).value)}
            @keydown=${(e: KeyboardEvent) =>
              e.key === "Enter" && this.#runSearch()}
          ></uui-input>
          <uui-button look="primary" label="Search" @click=${this.#runSearch}
            >Search</uui-button
          >
        </div>
        ${this._searchResults
          ? html`
              <p>${this._searchResults.itemsCount} result(s)</p>
              ${this._searchResults.hits.map(
                (hit) => html`
                  <uui-box class="hit">
                    ${Object.entries(hit).map(
                      ([key, value]) =>
                        html`<div><strong>${key}:</strong> ${value}</div>`,
                    )}
                  </uui-box>
                `,
              )}
            `
          : nothing}
      </uui-box>
    `;
  }

  #renderStyles() {
    return html`<style>
      :host {
      display: block;
      padding: var(--uui-size-layout-1);
    }
    .header {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .actions,
    .search-bar {
      display: flex;
      gap: var(--uui-size-space-3);
      margin-top: var(--uui-size-space-4);
    }
    .indices {
      display: block;
      margin-top: var(--uui-size-space-5);
    }
    .back-link {
      display: inline-flex;
      align-items: center;
      background: none;
      border: none;
      cursor: pointer;
      padding: 0;
      margin-bottom: var(--uui-size-space-4);
      color: var(--uui-color-interactive);
      font: inherit;
      text-decoration: underline;
    }
    .back-link:hover {
      color: var(--uui-color-interactive-emphasis);
    }
    .form-row {
      display: grid;
      grid-template-columns: 220px 1fr;
      gap: var(--uui-size-space-5);
      padding: var(--uui-size-space-5) 0;
      border-bottom: 1px solid var(--uui-color-divider);
    }
    .form-label strong {
      display: block;
    }
    .form-label small {
      display: block;
      margin-top: var(--uui-size-space-2);
      color: var(--uui-color-text-alt);
    }
    .doc-types {
      display: flex;
      flex-direction: column;
    }
    .doc-type {
      border-bottom: 1px solid var(--uui-color-divider);
    }
    .doc-type.selected {
      border: 2px solid var(--uui-color-focus, #3544b1);
      border-radius: var(--uui-border-radius);
      margin: var(--uui-size-space-2) 0;
    }
    .doc-type-header {
      display: flex;
      align-items: center;
      gap: var(--uui-size-space-3);
      width: 100%;
      background: none;
      border: none;
      cursor: pointer;
      padding: var(--uui-size-space-3) var(--uui-size-space-4);
      text-align: left;
      font: inherit;
      color: inherit;
    }
    .doc-type-name {
      flex: 1;
    }
    .properties {
      margin: 0 0 var(--uui-size-space-4) var(--uui-size-space-6);
      display: flex;
      flex-direction: column;
      gap: var(--uui-size-space-1);
    }
    .hit {
      margin-top: var(--uui-size-space-3);
      display: block;
    }
    em {
      color: var(--uui-color-text-alt);
    }
    </style>`;
  }
}

declare global {
  interface HTMLElementTagNameMap {
    "typesense-search-dashboard": TypesenseSearchDashboardElement;
  }
}
