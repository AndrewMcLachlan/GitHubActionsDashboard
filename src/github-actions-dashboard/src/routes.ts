import { createRootRoute } from "@tanstack/react-router"
import dashboardRoute from "./routes/dashboard"
import settingsRoute from "./routes/settings"
//import tableRoute from "./routes/table"
import App from "./App";

export const rootRoute = createRootRoute({
  component: App,
  
});

export const routeTree = rootRoute.addChildren([dashboardRoute, settingsRoute]);
