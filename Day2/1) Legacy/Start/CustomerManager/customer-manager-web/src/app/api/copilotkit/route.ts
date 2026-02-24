import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";
import { NextRequest } from "next/server";

// Use the empty adapter since we're only using one agent via AG-UI
const serviceAdapter = new ExperimentalEmptyAdapter();

// Connect to the .NET AG-UI agent endpoint
const runtime = new CopilotRuntime({
  agents: {
    customer_agent: new HttpAgent({ url: "http://localhost:5000/agent" }),
  },
});

export const POST = async (req: NextRequest) => {
  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter,
    endpoint: "/api/copilotkit",
  });
  return handleRequest(req);
};
