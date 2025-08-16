import { DateTime } from "luxon";
import type { RepositoryModel, WorkflowModel, WorkflowRunModel } from "../../../api";
import { Badge } from "./Badge";

export const WorkflowRunRow: React.FC<WorkflowRunRowProps> = ({ repository, workflow, run }) => {

        const formatter = new Intl.RelativeTimeFormat(navigator.language, { style: 'long' });

    const updatedAt = DateTime.fromISO(run.details.updatedAt!);
    const timeAgo = updatedAt.toRelative({ style: 'long' }) || formatter.format(0, 'seconds');

    return (
        <tr key={`${repository.details.owner?.name ?? repository.details.owner?.login}|${repository.details.name}`}>
            <td><Badge className={run.ragStatus?.toLowerCase()}>{run.details.conclusion?.stringValue}</Badge></td>
            <td>{workflow.details.name}</td>
            <td><Badge>{run.details.headBranch}</Badge></td>
            <td title={DateTime.fromISO(run.details.updatedAt!).toFormat('yyyy-MM-dd HH:mm:ss')}>{timeAgo}</td>
            <td>{repository.details.owner?.name ?? repository.details.owner?.login}</td>
            <td>{repository.details.name}</td>
                <td><a href={repository.details.htmlUrl!} target="_blank" rel="noopener noreferrer">View Run</a></td>
        </tr>
    );
};

export interface WorkflowRunRowProps {
    repository: RepositoryModel;
    workflow: WorkflowModel;
    run: WorkflowRunModel;
}
