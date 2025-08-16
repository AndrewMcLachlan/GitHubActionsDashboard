import { useWorkflowRuns } from "../-hooks/useWorkflowRuns";
import { useDashboardContext } from "../-providers/DashboardProvider";
import { Spinner } from "../../../components/Spinner";
import { useSelectedRepositories } from "../../../hooks/useSelectedRepositories";
import { WorkflowRunRow } from "./WorkflowRunRow";

export const List = () => {

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

    
    return (
        <table>
            <thead>
                <tr>
                    <th>Owner</th>
                    <th>Repository</th>
                    <th>Workflow</th>
                    <th>Branch</th>
                    <th>Status</th>
                    <th>Run</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                {isLoading && <Spinner />}
                {isError && <tr><td colSpan={6}>Error loading build info: {error.message}</td></tr>}
                {(!isLoading && (!repositories || repositories.length === 0)) && <tr><td colSpan={6}>No workflows found.</td></tr>}
                {repositories && repositories.map((repository) => {
                    return repository.workflows?.map(workflow => {
                        return workflow.runs?.map(run => (
                            <WorkflowRunRow repository={repository} workflow={workflow} run={run} key={run.details.id} />
                        ))
                    })
                })
                }
            </tbody>
        </table>
    )


}
