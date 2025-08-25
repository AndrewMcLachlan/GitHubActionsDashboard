export type TabsListComponent = React.FC<React.PropsWithChildren<TabsListProps>>;

export const TabsList: TabsListComponent = ({ children, ...rest }) => {
    return (
        <ul className="nav nav-tabs" {...rest}>
            {children}
        </ul>
    );
}

export interface TabsListProps extends React.DetailedHTMLProps<React.HTMLAttributes<HTMLUListElement>, HTMLUListElement> {
    
}