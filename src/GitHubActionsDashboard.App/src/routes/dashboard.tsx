import { createFileRoute, Link, Outlet } from "@tanstack/react-router";
import { DashboardProvider } from "./dashboard/-providers/DashboardProvider";
import { Filters } from "./dashboard/-components/Filters";
import { Icon } from "@andrewmclachlan/moo-ds";


export const Route = createFileRoute("/dashboard")({
    component: () => (
        <DashboardProvider>
            <article>
                <div className="controls">
                    <Filters />
                    <div className="views">
                        <Link to="/dashboard"><Icon icon="bars-staggered" /></Link>
                        <Link to="/dashboard/list"><Icon icon="list-ul" /></Link>
                    </div>
                </div>
                <Outlet />
            </article>
        </DashboardProvider>
    )
});
