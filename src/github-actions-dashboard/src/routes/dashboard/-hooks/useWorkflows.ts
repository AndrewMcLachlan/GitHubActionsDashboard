import { useQuery } from "@tanstack/react-query";
import { postWorkflowsOptions } from "../../../api/@tanstack/react-query.gen";

export const useWorkflows = (repositories: Record<string, string[]>) => {
    return useQuery({
        ...postWorkflowsOptions({
            body: {
                 repositories: repositories
            }
        })
    });
}
