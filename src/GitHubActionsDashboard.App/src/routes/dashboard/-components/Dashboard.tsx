import { useSelectedRepositories } from "../../../hooks/useSelectedRepositories";
import { RepositoryList } from "./RepositoryList";
import { useWorkflowRuns } from "../-hooks/useWorkflowRuns";
import { useWorkflows } from "../-hooks/useWorkflows";
import { Spinner } from "../../../components/Spinner";
import { useDashboardContext } from "../-providers/DashboardProvider";

export const Dashboard = () => {

    const { data: selectedRepositories } = useSelectedRepositories();

    const request = selectedRepositories.reduce((acc, repo) => {
        const key = repo.owner;
        if (!acc[key]) {
            acc[key] = [];
        }
        acc[key].push(repo.name);
        return acc;
    }, {} as Record<string, string[]>);

    const { branchFilter } = useDashboardContext();

    const { data: repositories, isLoading, isError, error } = useWorkflowRuns(request, branchFilter);
    //const { data: repositories, isLoading, isError, error } = useWorkflows(request);

    if (isError) {
        console.error("Error fetching dashboard data:", error);
        return <p>Error loading build info.</p>;
    }

    return (
        <>
            {isLoading && <Spinner />}
            {(!isLoading && (!repositories || repositories.length === 0)) && <p>No workflows found.</p>}
            {repositories && <RepositoryList repositories={repositories} />}
        </>
    );
}
