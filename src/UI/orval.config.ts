import { defineConfig } from "orval";

export default defineConfig({
  velis: {
    input: "../Velis/v1.yaml",
    output: {
      target: ".",
      schemas: ".",
      client: "react-query",
      httpClient: "axios",
      mock: false,
      prettier: true,
      mode: "single",
      indexFiles: true,
      workspace: "./src/api",
    },
    hooks: {
      afterAllFilesWrite: "prettier --write .",
    },
  },
});
