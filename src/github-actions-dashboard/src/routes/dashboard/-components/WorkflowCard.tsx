import { Icon } from "@andrewmclachlan/moo-ds";
import type { WorkflowModel } from "../../../api";
import { Collapsible } from "./Collapsible";
import RagStatus from "./RagStatus";
import { WorkflowRunList } from "./WorkflowRunList";

export const WorkflowCard: React.FC<WorkflowCardProps> = ({ workflow }) => {

    console.debug("Rendering WorkflowCard", workflow.details);

    return (
        <Collapsible className="workflow-card" header={
            <>
                <RagStatus ragStatus={workflow.overallStatus} />
                <h3>{workflow.details.name}</h3>
                <span><a href={workflow.details.htmlUrl?.replace("blob/main/.github", "actions")} target="_blank"><Icon icon="arrow-up-right-from-square" /></a></span>
            </>
        }>
            <WorkflowRunList runs={workflow.runs} />
        </Collapsible>
    );
};

interface WorkflowCardProps {
    workflow: WorkflowModel;
}
