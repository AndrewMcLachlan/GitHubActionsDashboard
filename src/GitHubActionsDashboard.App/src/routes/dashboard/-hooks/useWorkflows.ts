import { useQuery } from "@tanstack/react-query";
import { postWorkflowsOptions } from "../../../api/@tanstack/react-query.gen";
import { useSelectedRepositories } from "../../settings/-hooks/useSelectedRepositories";

export const useWorkflows = () => {

    const { data: selectedRepositories } = useSelectedRepositories();

    return useQuery({
        ...postWorkflowsOptions({
            body: {
                 repositories: selectedRepositories
            }
        })
    });
}
