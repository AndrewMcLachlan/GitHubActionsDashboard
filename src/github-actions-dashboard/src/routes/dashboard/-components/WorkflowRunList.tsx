import type { WorkflowRunModel } from "../../../api";
import { WorkflowRunCard } from "./WorkflowRunCard";

export const WorkflowRunList: React.FC<WorkflowRunListProps> = ({ runs }) => {

    if (!runs || runs.length === 0) {
        return <p>No runs found.</p>;
    }

    return (
        <div className="workflow-run-list">
            {runs.map((run) => (
                <WorkflowRunCard key={run.details.id} workflowRun={run} />
            ))}
        </div>
    );
}

export interface WorkflowRunListProps {
    runs?: WorkflowRunModel[]
}