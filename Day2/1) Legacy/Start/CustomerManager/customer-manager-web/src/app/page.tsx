"use client";

import { CopilotSidebar } from "@copilotkit/react-ui";

export default function Home() {
  return (
    <div className="flex min-h-screen bg-zinc-50 dark:bg-black font-sans">
      <CopilotSidebar
        labels={{
          title: "Customer Manager Agent",
          initial:
            "Hi! I can help you manage customers.\n\nTry asking:\n• List all customers\n• Search for Jane\n• Add a new customer\n• Update customer info\n• Delete a customer",
        }}
        defaultOpen={true}
        clickOutsideToClose={false}
      />
      <main className="flex-1 p-8">
        <h1 className="text-3xl font-bold mb-4 text-zinc-900 dark:text-zinc-50">
          Customer Manager
        </h1>
        <p className="text-lg text-zinc-600 dark:text-zinc-400 mb-8">
          Use the AI assistant in the sidebar to manage customers. Powered by{" "}
          <span className="font-semibold">CopilotKit</span> +{" "}
          <span className="font-semibold">Microsoft Agent Framework</span> +{" "}
          <span className="font-semibold">GitHub Models</span>.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 p-6 bg-white dark:bg-zinc-900">
            <h2 className="text-lg font-semibold mb-2 text-zinc-800 dark:text-zinc-200">
              📋 List &amp; Search
            </h2>
            <p className="text-sm text-zinc-500 dark:text-zinc-400">
              Ask the agent to list all customers or search by name.
            </p>
          </div>
          <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 p-6 bg-white dark:bg-zinc-900">
            <h2 className="text-lg font-semibold mb-2 text-zinc-800 dark:text-zinc-200">
              ➕ Add &amp; Update
            </h2>
            <p className="text-sm text-zinc-500 dark:text-zinc-400">
              Create new customers or update existing ones via natural language.
            </p>
          </div>
          <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 p-6 bg-white dark:bg-zinc-900">
            <h2 className="text-lg font-semibold mb-2 text-zinc-800 dark:text-zinc-200">
              🗑️ Delete
            </h2>
            <p className="text-sm text-zinc-500 dark:text-zinc-400">
              Remove customers by asking the agent with the customer ID or name.
            </p>
          </div>
        </div>

        <div className="mt-8 p-4 rounded-lg bg-zinc-100 dark:bg-zinc-800 text-sm text-zinc-600 dark:text-zinc-400">
          <strong>Architecture:</strong> Next.js (CopilotKit) → AG-UI protocol → .NET 9 (Microsoft Agent Framework) → GitHub Models (GPT-4o-mini)
        </div>
      </main>
    </div>
  );
}
