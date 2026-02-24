"use client";

import { useEffect, useState, useCallback } from "react";

interface CategoryBreakdown {
  category: string;
  total: number;
  count: number;
  percentage: number;
}

interface MonthlyStats {
  total: number;
  dailyAverage: number;
  highestCategory: string;
  transactionCount: number;
}

interface StatsChartsProps {
  refreshKey: number;
}

const BAR_COLORS = [
  "bg-blue-500", "bg-red-500", "bg-green-500", "bg-amber-500",
  "bg-violet-500", "bg-cyan-500", "bg-pink-500", "bg-slate-500",
];

export default function StatsCharts({ refreshKey }: StatsChartsProps) {
  const [categories, setCategories] = useState<CategoryBreakdown[]>([]);
  const [stats, setStats] = useState<MonthlyStats | null>(null);
  const [month, setMonth] = useState(() => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
  });
  const [loading, setLoading] = useState(true);

  const fetchStats = useCallback(async () => {
    setLoading(true);
    try {
      const [catRes, statsRes] = await Promise.all([
        fetch(`/stats/categories?month=${month}`),
        fetch(`/stats/monthly?month=${month}`),
      ]);
      if (catRes.ok) setCategories(await catRes.json());
      if (statsRes.ok) setStats(await statsRes.json());
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, [month]);

  useEffect(() => {
    fetchStats();
  }, [refreshKey, fetchStats]);

  return (
    <div className="rounded-xl border border-border bg-card p-5 shadow-sm">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-semibold">Analytics</h2>
        <input
          type="month"
          value={month}
          onChange={(e) => setMonth(e.target.value)}
          className="rounded-md border border-border px-2 py-1 text-sm focus:border-primary focus:outline-none"
        />
      </div>

      {loading ? (
        <p className="text-sm text-muted">Loading statistics…</p>
      ) : (
        <>
          {/* Summary cards */}
          {stats && (
            <div className="mb-5 grid grid-cols-2 gap-3 sm:grid-cols-4">
              <StatCard label="Total" value={`$${stats.total.toFixed(2)}`} />
              <StatCard label="Daily Avg" value={`$${stats.dailyAverage.toFixed(2)}`} />
              <StatCard label="Top Category" value={stats.highestCategory || "N/A"} />
              <StatCard label="Transactions" value={String(stats.transactionCount)} />
            </div>
          )}

          {/* Category breakdown as simple bar visualization */}
          {categories.length > 0 ? (
            <div>
              <h3 className="mb-3 text-sm font-medium text-muted">Spending by Category</h3>
              <div className="space-y-2">
                {categories.map((cat, i) => (
                  <div key={cat.category} className="flex items-center gap-3 text-sm">
                    <span className="w-24 truncate font-medium">{cat.category}</span>
                    <div className="flex-1 rounded-full bg-gray-100 h-5 overflow-hidden">
                      <div
                        className={`h-full rounded-full ${BAR_COLORS[i % BAR_COLORS.length]}`}
                        style={{ width: `${cat.percentage}%` }}
                      />
                    </div>
                    <span className="w-20 text-right font-mono text-muted">
                      ${cat.total.toFixed(2)}
                    </span>
                    <span className="w-12 text-right text-xs text-muted">
                      {cat.percentage.toFixed(0)}%
                    </span>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <p className="text-sm text-muted">No data for this month.</p>
          )}
        </>
      )}
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg bg-background p-3 text-center">
      <p className="text-xs text-muted">{label}</p>
      <p className="mt-1 text-lg font-semibold">{value}</p>
    </div>
  );
}
