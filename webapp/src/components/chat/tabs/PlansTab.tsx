// Copyright (c) Microsoft. All rights reserved.

import {
    Table,
    TableBody,
    TableCell,
    TableCellLayout,
    TableColumnDefinition,
    TableColumnId,
    TableHeader,
    TableHeaderCell,
    TableHeaderCellProps,
    TableRow,
    createTableColumn,
    makeStyles,
    tokens,
    useTableFeatures,
    useTableSort,
} from '@fluentui/react-components';
import { ChatMessageType, IChatMessage } from '../../../libs/models/ChatMessage';
import { PlanState, ProposedPlan } from '../../../libs/models/Plan';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { timestampToDateString } from '../../utils/TextUtils';
import { PlanDialogView } from '../plan-viewer/PlanDialogView';
import { TabView } from './TabView';

const useClasses = makeStyles({
    table: {
        backgroundColor: tokens.colorNeutralBackground1,
    },
    tableHeader: {
        fontWeight: tokens.fontSizeBase600,
    },
});

interface TableItem {
    index: number;
    parsedPlan: ProposedPlan;
    goal: string;
    createdOn: {
        label: string;
        timestamp: number;
    };
    tokens: number;
}

interface IPlansTabProps {
    setChatTab: () => void;
}

export const PlansTab: React.FC<IPlansTabProps> = ({ setChatTab }) => {
    const classes = useClasses();

    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const chatMessages = conversations[selectedId].messages;
    const planMessages = chatMessages.filter((message) => message.type === ChatMessageType.Plan);

    const { columns, rows } = useTable(planMessages, setChatTab);
    return (
        <TabView title="Plans" learnMoreDescription="custom plans" learnMoreLink="https://aka.ms/sk-docs-planner">
            <Table aria-label="Processes plan table" className={classes.table}>
                <TableHeader>
                    <TableRow>{columns.map((column) => column.renderHeaderCell())}</TableRow>
                </TableHeader>
                <TableBody>
                    {rows.map((item) => (
                        <TableRow key={item.goal}>{columns.map((column) => column.renderCell(item))}</TableRow>
                    ))}
                </TableBody>
            </Table>
        </TabView>
    );
};

function useTable(planMessages: IChatMessage[], setChatTab: () => void) {
    const headerSortProps = (columnId: TableColumnId): TableHeaderCellProps => ({
        onClick: (e: React.MouseEvent) => {
            toggleColumnSort(e, columnId);
        },
        sortDirection: getSortDirection(columnId),
    });

    const columns: Array<TableColumnDefinition<TableItem>> = [
        createTableColumn<TableItem>({
            columnId: 'goal',
            renderHeaderCell: () => (
                <TableHeaderCell key="goal" {...headerSortProps('goal')}>
                    Goal
                </TableHeaderCell>
            ),
            renderCell: (item) => (
                <TableCell key={`plan-${item.index}`}>
                    <TableCellLayout>
                        <PlanDialogView goal={item.goal} plan={item.parsedPlan} setChatTab={setChatTab} />
                    </TableCellLayout>
                </TableCell>
            ),
            compare: (a, b) => {
                const comparison = a.goal.localeCompare(b.goal);
                return getSortDirection('goal') === 'ascending' ? comparison : comparison * -1;
            },
        }),
        createTableColumn<TableItem>({
            columnId: 'createdOn',
            renderHeaderCell: () => (
                <TableHeaderCell key="createdOn" {...headerSortProps('createdOn')}>
                    Created on
                </TableHeaderCell>
            ),
            renderCell: (item) => (
                <TableCell key={item.createdOn.timestamp} title={new Date(item.createdOn.timestamp).toLocaleString()}>
                    {item.createdOn.label}
                </TableCell>
            ),
            compare: (a, b) => {
                const comparison = a.createdOn.timestamp > b.createdOn.timestamp ? 1 : -1;
                return getSortDirection('createdOn') === 'ascending' ? comparison : comparison * -1;
            },
        }),
        createTableColumn<TableItem>({
            columnId: 'tokenCounts',
            renderHeaderCell: () => (
                <TableHeaderCell key="tokenCounts" {...headerSortProps('tokenCounts')}>
                    Token Count
                </TableHeaderCell>
            ),
            renderCell: (item) => (
                <TableCell key={`plan-${item.index}-tokens`}>
                    {
                        // TODO: [Issue #150, sk#2106] Remove static text once core team finishes work to return token usage.
                        // item.tokens
                        'Coming soon'
                    }
                </TableCell>
            ),
            compare: (a, b) => {
                const comparison = a.tokens - b.tokens;
                return getSortDirection('tokenCounts') === 'ascending' ? comparison : comparison * -1;
            },
        }),
    ];

    const items = planMessages
        .map((message, index) => {
            const parsedPlan = JSON.parse(message.content) as ProposedPlan;
            const plangoal =
                parsedPlan.userIntent ?? parsedPlan.originalUserInput ?? parsedPlan.proposedPlan.description;

            return {
                index: index,
                goal: plangoal,
                parsedPlan: parsedPlan,
                createdOn: {
                    label: timestampToDateString(message.timestamp),
                    timestamp: message.timestamp,
                },
                tokens: 0, // TODO: [Issue #2106] Get token count from plan
            };
        })
        .filter((item) => item.parsedPlan.state !== PlanState.Derived);

    const {
        sort: { getSortDirection, toggleColumnSort, sortColumn },
    } = useTableFeatures(
        {
            columns,
            items,
        },
        [
            useTableSort({
                defaultSortState: { sortColumn: 'createdOn', sortDirection: 'descending' },
            }),
        ],
    );

    if (sortColumn) {
        items.sort((a, b) => {
            const compare = columns.find((column) => column.columnId === sortColumn)?.compare;
            return compare?.(a, b) ?? 0;
        });
    }

    return { columns, rows: items };
}
