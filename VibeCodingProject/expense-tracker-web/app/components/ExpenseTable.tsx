"use client";

import { useEffect, useState, useCallback } from "react";

interface Expense {
  id: number;
  amount: number;
  category: string;
  date: string;
  description: string;
}

interface ExpenseTableProps {
  refreshKey: number; // bump to re-fetch
  onDeleted: () => void;
}

export default function ExpenseTable({ refreshKey, onDeleted }: ExpenseTableProps) {
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchExpenses = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch("/api/expenses");
      if (res.ok) {
        const data: Expense[] = await res.json();
        setExpenses(data);
      }
    } catch {
      // silently handle — user sees empty table
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchExpenses();
  }, [refreshKey, fetchExpenses]);

  async function handleDelete(id: number) {
    if (!confirm("Delete this expense?")) return;
    const res = await fetch(`/api/expenses/${id}`, { method: "DELETE" });
    if (res.ok) {
      onDeleted();
    }
  }

  if (loading) {
    return (
      <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
        <p className="text-sm text-muted">Loading expenses…</p>
      </div>
    );
  }

  return (
    <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
      <h2 className="mb-4 text-lg font-semibold">
        Expenses{" "}
        <span className="text-sm font-normal text-muted">({expenses.length})</span>
      </h2>

      {expenses.length === 0 ? (
        <p className="text-sm text-muted">No expenses yet. Add one above!</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-border text-muted">
                <th className="py-2 pr-4 font-medium">Date</th>
                <th className="py-2 pr-4 font-medium">Category</th>
                <th className="py-2 pr-4 font-medium">Amount</th>
                <th className="py-2 pr-4 font-medium">Description</th>
                <th className="py-2 font-medium"></th>
              </tr>
            </thead>
            <tbody>
              {expenses.map((exp) => (
                <tr key={exp.id} className="border-b border-border last:border-0 hover:bg-gray-50">
                  <td className="py-2 pr-4">{new Date(exp.date).toLocaleDateString()}</td>
                  <td className="py-2 pr-4">
                    <span className="inline-block rounded-full bg-blue-50 px-2 py-0.5 text-xs font-medium text-primary">
                      {exp.category}
                    </span>
                  </td>
                  <td className="py-2 pr-4 font-mono">${exp.amount.toFixed(2)}</td>
                  <td className="py-2 pr-4 text-muted">{exp.description || "—"}</td>
                  <td className="py-2">
                    <button
                      onClick={() => handleDelete(exp.id)}
                      className="rounded px-2 py-1 text-xs text-danger hover:bg-red-50"
                      title="Delete"
                    >
                      ✕
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
