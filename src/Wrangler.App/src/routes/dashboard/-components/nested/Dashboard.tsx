import { RepositoryList } from "./RepositoryList";
import { useWorkflows } from "../../-hooks/useWorkflows";
import { Spinner } from "../../../../components/Spinner";

export const Dashboard = () => {

  const { data: repositories, isLoading, isError, error } = useWorkflows();

  if (isError) {
    console.error("Error fetching dashboard data:", error);
    return <p>Error loading build info.</p>;
  }

  // Cached data — restored from the last visit, or kept from the previous filter
  // — renders while the refetch happens behind it, so the spinner is only for a
  // genuinely cold start with nothing to show.
  const showSpinner = isLoading && !repositories;

  return (
    <>
      {showSpinner && <Spinner />}
      {(!showSpinner && (!repositories || repositories.length === 0)) && <p>No workflows found.</p>}
      {repositories && <RepositoryList repositories={repositories} />}
    </>
  );
}
