import type { RepositoryModel } from "../../../../api";
import { useWorkflows } from "../../-hooks/useWorkflows";
import { Spinner } from "../../../../components/Spinner";
import { AccountSection } from "./AccountSection";

export const Overview = () => {
  const { data: repositories, isLoading, isError, error } = useWorkflows();

  if (isError) {
    console.error("Error fetching dashboard data:", error);
    return <p>Error loading build info.</p>;
  }

  // Cached data — restored from the last visit, or kept from the previous filter
  // — renders while the refetch happens behind it, so the spinner is only for a
  // genuinely cold start with nothing to show.
  const showSpinner = isLoading && !repositories;

  const sorted = [...(repositories ?? [])].sort((a, b) => a.name.localeCompare(b.name));
  const grouped = sorted.reduce<Record<string, RepositoryModel[]>>((acc, repo) => {
    (acc[repo.owner] ??= []).push(repo);
    return acc;
  }, {});
  const sortedGroups = Object.entries(grouped).sort(([a], [b]) => a.localeCompare(b));

  return (
    <div className="overview">
      {showSpinner && <Spinner />}
      {!showSpinner && (!repositories || repositories.length === 0) && <p>No workflows found.</p>}
      {sortedGroups.map(([owner, repos]) => (
        <AccountSection key={owner} owner={owner} repositories={repos} />
      ))}
    </div>
  );
};
