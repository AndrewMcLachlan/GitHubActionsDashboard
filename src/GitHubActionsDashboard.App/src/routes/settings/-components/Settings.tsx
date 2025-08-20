import { Section } from "@andrewmclachlan/moo-ds";
import type { Repository2 as Repository } from "../../../api";
import { useGroupedRepositories } from "../../../hooks/useGroupedRepositories";
import { useSelectedRepositories, useUpdateSelectedRepositories, type SelectedRepository } from "../../../hooks/useSelectedRepositories";

export const Settings = () => {

    const accounts = useGroupedRepositories();

    const { data: repositories } = useSelectedRepositories();
    const { mutate } = useUpdateSelectedRepositories();

    const handleRepositoryChange = (repo: SelectedRepository) => {

        mutate(repositories.some(r => equals(r, repo)) ?
            repositories.filter(r => !equals(r, repo)) :
            [...repositories, repo]
        );
    }

    return (
        <article>
            <h2>Repositories</h2>
            <Section className="settings">
                {accounts.isLoading ? (
                    <p>Loading repositories...</p>
                ) : accounts.isError ? (
                    <p>Error loading repositories: {accounts.error.message}</p>
                ) : (
                    accounts.data?.map(account => (
                        <section key={account.login}>
                            <h4>{account.avatarUrl && <img src={account.avatarUrl} height={32} width={32} />} {account.login}</h4>
                            <ul className="repository-select-list">
                                {account.repositories?.map(repo => (
                                    <li key={repo.fullName}>
                                        <input id={repo.fullName} type="checkbox" className="form-check-input" checked={repositories.some(r => equals(r, repo))} onChange={() => handleRepositoryChange(repo)} />
                                        <label htmlFor={repo.fullName}>{repo.name}</label>
                                    </li>
                                ))}
                            </ul>
                        </section>
                    ))
                )
                }
            </Section>
        </article>
    );
}

const equals = (r: SelectedRepository, repo: Repository | SelectedRepository): boolean =>
    r.owner === repo.owner && r.name === repo.name;
