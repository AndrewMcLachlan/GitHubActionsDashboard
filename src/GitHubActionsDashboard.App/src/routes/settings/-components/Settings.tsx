import { useGroupedRepositories } from "../../../hooks/useGroupedRepositories";
import { Tabs } from "../../../components/tabs/Tabs";
import { RepoSelector } from "./RepoSelector";

export const Settings = () => {

    const accounts = useGroupedRepositories();

    return (
        <article>
            <h2>Repositories</h2>
            {accounts.isLoading ? (
                <p>Loading repositories...</p>
            ) : accounts.isError ? (
                <p>Error loading repositories: {accounts.error.message}</p>
            ) : (
                <Tabs selectedTab={accounts.data?.[0]?.login}>
                    <Tabs.List>
                        {accounts.data?.map(account => (
                            <Tabs.Tab key={account.login} value={account.login} label={account.login} icon={account.avatarUrl} />
                        ))}
                    </Tabs.List>
                    {accounts.data?.map(account => (
                        <Tabs.Content className="repo-selector" key={account.login} value={account.login}>
                            <RepoSelector account={account} />
                        </Tabs.Content>
                    ))}
                </Tabs>
            )
            }
        </article>
    );
}
