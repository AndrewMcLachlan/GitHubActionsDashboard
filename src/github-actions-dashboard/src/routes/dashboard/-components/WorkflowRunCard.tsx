import { Icon } from "@andrewmclachlan/moo-ds";
import type { WorkflowRunModel } from "../../../api";
import { Badge } from "./Badge";
import RagStatus from "./RagStatus";
import { DateTime } from "luxon";

export const WorkflowRunCard: React.FC<WorkflowRunCardProps> = ({ workflowRun }) => {

    const formatter = new Intl.RelativeTimeFormat(navigator.language, { style: 'long' });

    const updatedAt = DateTime.fromISO(workflowRun.details.updatedAt!);
    const timeAgo = updatedAt.toRelative({ style: 'long' }) || formatter.format(0, 'seconds');

    return (
        <section className="workflow-run-card">
            <RagStatus ragStatus={workflowRun.ragStatus} />
            <span className="conclusion">{workflowRun.details.conclusion?.stringValue}</span>
            <Badge>{workflowRun.details.headBranch}</Badge>
            <span>{workflowRun.details.event}</span>
            <span>{workflowRun.details.runNumber}</span>
            <span>{workflowRun.details.triggeringActor?.name ?? workflowRun.details.triggeringActor?.login}</span>

            <span className={`run-status ${workflowRun.details.status?.stringValue}`}>{workflowRun.details.status?.stringValue}</span>
            <span className="run-timestamp" title={DateTime.fromISO(workflowRun.details.updatedAt!).toFormat('yyyy-MM-dd HH:mm:ss')}>{timeAgo}</span>
            <span><a href={workflowRun.details.htmlUrl!} target="_blank"><Icon icon="arrow-up-right-from-square" /></a></span>
        </section>
    );
}

interface WorkflowRunCardProps {
    workflowRun: WorkflowRunModel;
}
