import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* proxy API calls to .NET backend during development */
  async rewrites() {
    return [
      { source: "/api/expenses/:path*", destination: "http://localhost:5000/api/expenses/:path*" },
      { source: "/api/expenses",        destination: "http://localhost:5000/api/expenses" },
      { source: "/stats/:path*",        destination: "http://localhost:5000/stats/:path*" },
      { source: "/health",              destination: "http://localhost:5000/health" },
    ];
  },
};

export default nextConfig;
