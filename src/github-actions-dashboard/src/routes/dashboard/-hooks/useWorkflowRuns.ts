import { useQuery } from "@tanstack/react-query";
import { postWorkflowsRunsOptions } from "../../../api/@tanstack/react-query.gen";

export const useWorkflowRuns = (repositories: Record<string, string[]>, branchFilters: string[]) => {
    return useQuery({
        ...postWorkflowsRunsOptions({
            body: {
                 repositories: repositories,
                 branchFilters: branchFilters,
            }
        })
    });
}
