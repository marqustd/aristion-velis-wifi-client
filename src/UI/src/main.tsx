import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";

const queryClient = new QueryClient();

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <div className="min-h-screen bg-gray-950 text-white flex justify-center">
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </div>
  </React.StrictMode>
);
