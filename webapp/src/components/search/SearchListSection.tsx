import {
    makeStyles,
    shorthands,
    tokens,
    Accordion,
    AccordionItem,
    AccordionHeader,
    AccordionPanel,
} from '@fluentui/react-components';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { ISearchValue } from '../../libs/models/SearchResponse';
import { Breakpoints } from '../../styles';
import { SearchListItem } from './search-list/SearchListItem';
import React, { useId } from 'react';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingVerticalXXS),
        paddingBottom: tokens.spacingVerticalXS,
    },
    header: {
        marginTop: 0,
        paddingBottom: tokens.spacingVerticalXS,
        marginLeft: tokens.spacingHorizontalXL,
        marginRight: tokens.spacingHorizontalXL,
        fontWeight: tokens.fontWeightRegular,
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground3,
        ...Breakpoints.small({
            display: 'none',
        }),
    },
});

interface ISearchListSectionProps {
    value: ISearchValue;
    index: number;
}

export const SearchListSection: React.FC<ISearchListSectionProps> = ({ value, index }) => {
    const classes = useClasses();
    const { selectedSearchItem } = useAppSelector((state: RootState) => state.search);
    const matches = value.matches;
    const accordionPanelId = useId();
    //const searchListItemId = useId();
    return matches.length > 0 ? (
        <div className={classes.root}>
            <Accordion collapsible={true} multiple={true}>
                <AccordionItem value={index}>
                    <AccordionHeader>{value.filename}</AccordionHeader>
                    {matches.map((match) => {
                        const label = match.label;
                        const id = match.id;
                        const selectedItem = match.id === selectedSearchItem;

                        return (
                            <AccordionPanel key={'acc' + accordionPanelId + id}>
                                <SearchListItem key={id} label={label} id={id} isSelected={selectedItem} />
                            </AccordionPanel>
                        );
                    })}
                </AccordionItem>
            </Accordion>
        </div>
    ) : null;
};
