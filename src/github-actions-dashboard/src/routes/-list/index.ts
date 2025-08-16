import { createRoute } from "@tanstack/react-router";
import { rootRoute } from "../../routes.ts";
import { DashboardTable } from "./DashboardTable.tsx";

const route = createRoute({
    getParentRoute: () => rootRoute,
    path: "/table-view",
    component: DashboardTable,
});

export default route;