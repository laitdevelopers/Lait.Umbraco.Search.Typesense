# Umbraco CMS Integrations: Search - Typesense

A search engine integration for Umbraco CMS (v15/16, new backoffice) that lets editors manage
[Typesense](https://typesense.org/) collections directly from the backoffice, map content types and
properties into search documents, build indices, keep them in sync on publish, and preview searches.

It is modelled on the official
[Umbraco.Cms.Integrations.Search.Algolia](https://github.com/umbraco/Umbraco.Cms.Integrations/tree/main/src/Umbraco.Cms.Integrations.Search.Algolia)
integration, adapted to Typesense and the new (Lit/TypeScript) backoffice.

## Features

- A **Settings** dashboard ("Typesense Search") to create, edit, build, delete and search indices.
- Map one or more **content types** and the **properties** to index per type.
- **Auto-schema** collections (`".*"` wildcard) so documents are schemaless, like Algolia records.
- **Automatic sync** of published / unpublished / trashed content via notification handlers.
- Extensible **value converters** (media picker, decimal, integer, boolean, tags) and a custom record
  builder pipeline.
- Works with both **self-hosted Typesense** and **Typesense Cloud** (configurable host/port/protocol/keys).

## Configuration

Add a `Typesense` settings section under `Umbraco:Cms:Integrations:Search` in `appsettings.json`:

```json
{
  "Umbraco": {
    "Cms": {
      "Integrations": {
        "Search": {
          "Typesense": {
            "Settings": {
              "Host": "localhost",
              "Port": "8108",
              "Protocol": "http",
              "ApiKey": "xyz",
              "SearchApiKey": ""
            }
          }
        }
      }
    }
  }
}
```

For Typesense Cloud, use your cluster hostname, `Port` `443`, `Protocol` `https`, and an admin API key.
`Port` may be left empty — the protocol's standard port (443 for `https`, 80 for `http`) is used.
`SearchApiKey` is optional; when empty the admin `ApiKey` is used for search requests.

## Running Typesense locally

```bash
docker run -p 8108:8108 -v /tmp/typesense-data:/data \
  typesense/typesense:27.1 \
  --data-dir /data --api-key=xyz --enable-cors
```

## How it works

| Concern | Type |
| --- | --- |
| Connection settings | `Configuration/TypesenseSettings.cs` |
| Collection CRUD + import | `Services/TypesenseIndexService.cs` |
| Search (`query_by`) | `Services/TypesenseSearchService.cs` |
| Index definition storage (DB) | `Services/TypesenseIndexDefinitionStorage.cs` + `Migrations/` |
| Document construction | `Builders/ContentRecordBuilder.cs` |
| Publish/sync handlers | `Handlers/` |
| Management API | `Controllers/` (`/umbraco/management/api/v1/typesense/...`) |
| Backoffice dashboard | `Client/` (built to `App_Plugins/...`) |
