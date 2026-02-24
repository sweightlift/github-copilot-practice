import {
  CopilotRuntime,
  ExperimentalEmptyAdapter,
  copilotRuntimeNextJSAppRouterEndpoint,
} from "@copilotkit/runtime";
import { HttpAgent } from "@ag-ui/client";

// Bridge: CopilotKit ↔ AG-UI SSE ↔ .NET backend
const backendUrl = process.env.AGENT_URL ?? "http://localhost:5000/agent";

const agent = new HttpAgent({
  url: backendUrl,
  name: "expense_agent",
});

const runtime = new CopilotRuntime({
  agents: { expense_agent: agent },
});

export const POST = async (req: Request) => {
  const { handleRequest } = copilotRuntimeNextJSAppRouterEndpoint({
    runtime,
    serviceAdapter: new ExperimentalEmptyAdapter(),
    endpoint: "/api/copilotkit",
  });
  return handleRequest(req);
};
