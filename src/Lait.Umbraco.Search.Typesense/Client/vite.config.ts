import { defineConfig } from "vite";

// Builds the backoffice extension bundle directly into the App_Plugins folder that the
// .csproj packs into the NuGet package.
export default defineConfig({
  build: {
    lib: {
      entry: { typesense: "src/index.ts" },
      formats: ["es"],
    },
    outDir: "../App_Plugins/Lait.Umbraco.Search.Typesense",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      // Do not bundle the Umbraco backoffice packages – they are provided by the host.
      external: [/^@umbraco/],
    },
  },
});
