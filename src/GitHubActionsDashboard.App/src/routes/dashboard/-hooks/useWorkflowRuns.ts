import { useQuery } from "@tanstack/react-query";
import { postRepositoriesByOwnerByRepoWorkflowsByWorkflowIdRunsOptions } from "../../../api/@tanstack/react-query.gen";

export const useWorkflowRuns = (owner: string, repo: string, workflowId: number, branchFilters: string[]) => {
    return useQuery({
        ...postRepositoriesByOwnerByRepoWorkflowsByWorkflowIdRunsOptions({
            path: {
                owner,
                repo,
                workflowId,
            },
            body: {
                branchFilters: branchFilters,
            }
        }),
        refetchOnWindowFocus: false,
        refetchInterval: 1000 * 60 * 2, // 2 minutes
        staleTime: 1000 * 60, // 1 minute
    });
}
