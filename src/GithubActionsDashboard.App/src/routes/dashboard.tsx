import { createFileRoute, Outlet } from "@tanstack/react-router";
import { DashboardProvider } from "./dashboard/-providers/DashboardProvider";
import { Filters } from "./dashboard/-components/Filters";


export const Route = createFileRoute("/dashboard")({
    component: () => (
        <DashboardProvider>
            <article>
                <Filters />
                <Outlet />
            </article>
        </DashboardProvider>
    )
});
