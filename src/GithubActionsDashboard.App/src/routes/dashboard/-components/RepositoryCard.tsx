import { Icon } from "@andrewmclachlan/moo-ds";
import type { RepositoryModel } from "../../../api";
import { Collapsible } from "./Collapsible";
import RagStatus from "./RagStatus";
import { WorkflowList } from "./WorkflowList";

export const RepositoryCard: React.FC<RepositoryCardProps> = ({ repository }) => {
    return (
        <Collapsible className="repository-card" header={
            <>
                <RagStatus ragStatus={repository.overallStatus} />
                <h3>{repository.details.owner?.name ?? repository.details.owner?.login} {repository.details.name}</h3>
                <span><a href={`${repository.details.htmlUrl}/actions`} target="_blank"><Icon icon="arrow-up-right-from-square" /></a></span>
            </>
        }>
            <WorkflowList workflows={repository.workflows} />
        </Collapsible>
    );
}

export interface RepositoryCardProps {
    repository: RepositoryModel
}
