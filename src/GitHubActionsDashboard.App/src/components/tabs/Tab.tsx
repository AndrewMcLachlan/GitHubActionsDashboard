import { Icon, useLink, type IconProps } from "@andrewmclachlan/moo-ds";
import classNames from "classnames";
import { useTabs } from "./TabsProvider";

export type TabComponent = React.FC<React.PropsWithChildren<TabProps>>;

export const Tab: TabComponent = ({ children, to, value, label, icon, className, ...rest }) => {

    const { selectedTab, setSelectedTab } = useTabs();

    return (
        <li {...rest} className={classNames(className, value === selectedTab ? "active" : "")}>
            <span onClick={() => setSelectedTab?.(value)}>
                {icon && <Icon icon={icon} />} {label}
                {children}
            </span>
        </li>
    );
}

export interface TabProps extends React.DetailedHTMLProps<React.LiHTMLAttributes<HTMLLIElement>, HTMLLIElement> {
    label?: string | React.ReactNode;
    icon?: IconProps["icon"];
    value: string;
    to?: string;
    isActive?: boolean;
}