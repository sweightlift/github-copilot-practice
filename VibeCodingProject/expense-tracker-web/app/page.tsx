"use client";

import { useState } from "react";
import { CopilotSidebar } from "@copilotkit/react-ui";
import ExpenseForm from "./components/ExpenseForm";
import ExpenseTable from "./components/ExpenseTable";
import StatsCharts from "./components/StatsCharts";

export default function Home() {
  // Shared refresh counter — bumped by any mutation (add / delete)
  const [refreshKey, setRefreshKey] = useState(0);
  const refresh = () => setRefreshKey((k) => k + 1);

  return (
    <div className="flex min-h-screen">
      {/* Main content area */}
      <main className="flex-1 px-6 py-8">
        {/* Header */}
        <header className="mb-8">
          <h1 className="text-2xl font-bold tracking-tight">
            💰 Expense Tracker
          </h1>
          <p className="mt-1 text-sm text-muted">
            Track spending, view analytics, and chat with the AI assistant.
          </p>
        </header>

        {/* Dashboard grid */}
        <div className="mx-auto max-w-5xl space-y-6">
          {/* Top row: form + stats */}
          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            <ExpenseForm onAdded={refresh} />
            <StatsCharts refreshKey={refreshKey} />
          </div>

          {/* Bottom row: table */}
          <ExpenseTable refreshKey={refreshKey} onDeleted={refresh} />
        </div>
      </main>

      {/* CopilotKit AI chat sidebar */}
      <CopilotSidebar
        defaultOpen={true}
        labels={{
          title: "Expense Assistant",
          initial: "Hi! I can help you manage expenses. Try asking:\n• What's my total spending this month?\n• Add a $50 groceries expense\n• Search for food expenses",
        }}
        clickOutsideToClose={false}
      />
    </div>
  );
}
