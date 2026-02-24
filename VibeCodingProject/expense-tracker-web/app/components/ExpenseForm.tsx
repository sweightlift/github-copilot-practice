"use client";

import { useState, FormEvent } from "react";

interface ExpenseFormProps {
  onAdded: () => void;
}

const CATEGORIES = [
  "Food",
  "Transport",
  "Entertainment",
  "Utilities",
  "Shopping",
  "Health",
  "Education",
  "Other",
];

export default function ExpenseForm({ onAdded }: ExpenseFormProps) {
  const [amount, setAmount] = useState("");
  const [category, setCategory] = useState(CATEGORIES[0]);
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [description, setDescription] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      const res = await fetch("/api/expenses", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          amount: parseFloat(amount),
          category,
          date,
          description: description || undefined,
        }),
      });
      if (!res.ok) {
        const text = await res.text();
        throw new Error(text || `HTTP ${res.status}`);
      }
      // Reset form
      setAmount("");
      setDescription("");
      onAdded();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to add expense");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="rounded-xl border border-border bg-card p-5 shadow-sm">
      <h2 className="mb-4 text-lg font-semibold">Add Expense</h2>

      {error && (
        <div className="mb-3 rounded-md bg-red-50 px-3 py-2 text-sm text-danger">{error}</div>
      )}

      <div className="grid grid-cols-2 gap-3">
        {/* Amount */}
        <div>
          <label className="mb-1 block text-sm font-medium text-muted">Amount ($)</label>
          <input
            type="number"
            step="0.01"
            min="0.01"
            required
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            className="w-full rounded-md border border-border px-3 py-2 text-sm focus:border-primary focus:outline-none"
            placeholder="0.00"
          />
        </div>

        {/* Category */}
        <div>
          <label className="mb-1 block text-sm font-medium text-muted">Category</label>
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            className="w-full rounded-md border border-border bg-white px-3 py-2 text-sm focus:border-primary focus:outline-none"
          >
            {CATEGORIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>

        {/* Date */}
        <div>
          <label className="mb-1 block text-sm font-medium text-muted">Date</label>
          <input
            type="date"
            required
            value={date}
            onChange={(e) => setDate(e.target.value)}
            className="w-full rounded-md border border-border px-3 py-2 text-sm focus:border-primary focus:outline-none"
          />
        </div>

        {/* Description */}
        <div>
          <label className="mb-1 block text-sm font-medium text-muted">Description</label>
          <input
            type="text"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="w-full rounded-md border border-border px-3 py-2 text-sm focus:border-primary focus:outline-none"
            placeholder="Optional note"
          />
        </div>
      </div>

      <button
        type="submit"
        disabled={loading}
        className="mt-4 w-full rounded-md bg-primary px-4 py-2 text-sm font-medium text-white transition hover:bg-primary-hover disabled:opacity-50"
      >
        {loading ? "Adding…" : "Add Expense"}
      </button>
    </form>
  );
}
