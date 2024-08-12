// Copyright (c) Microsoft. All rights reserved.

import { makeStyles, shorthands, tokens } from '@fluentui/react-components';
import React from 'react';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { SharedStyles } from '../../styles';
import { SearchInput } from './SearchInput';
import { useSearch } from '../../libs/hooks/useSearch';
import { ISearchMetaData } from '../../libs/models/SearchResponse';

const useClasses = makeStyles({
    root: {
        ...shorthands.overflow('hidden'),
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        height: '100%',
    },
    scroll: {
        ...shorthands.margin(tokens.spacingVerticalXS),
        ...SharedStyles.scroll,
    },
    history: {
        ...shorthands.padding(tokens.spacingVerticalM),
        paddingLeft: tokens.spacingHorizontalM,
        paddingRight: tokens.spacingHorizontalM,
        display: 'flex',
        justifyContent: 'center',
    },
    input: {
        display: 'flex',
        flexDirection: 'row',
        justifyContent: 'center',
        ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingVerticalNone),
    },
});

export const SearchRoom: React.FC = () => {
    const classes = useClasses();
    const search = useSearch();

    const { searchData, selectedSearchItem, selectedSpecializationKey } = useAppSelector(
        (state: RootState) => state.search,
    );
    const values = searchData.value;
    let displayContent: string[] = [];
    let metaData: ISearchMetaData = {};

    values.forEach((data) => {
        data.matches.map((match) => {
            if (match.id === selectedSearchItem) {
                displayContent = match.content;
                metaData = match.metadata;
            }
        });
    });

    const scrollViewTargetRef = React.useRef<HTMLDivElement>(null);

    const handleSubmit = async (specialization: string, value: string) => {
        await search.getResponse(specialization, value);
    };

    return (
        <div className={classes.root}>
            <SearchInput onSubmit={handleSubmit} defaultSpecializationKey={selectedSpecializationKey} />
            <div ref={scrollViewTargetRef} className={classes.scroll}>
                <div>
                    {displayContent.map((content, index) => (
                        <p key={index} dangerouslySetInnerHTML={{ __html: content }} />
                    ))}
                </div>
                <div id="meta-data">
                    {metaData.source?.filename && (
                        <div>
                            <span>
                                <b>Filename</b>
                            </span>
                            : <span>{metaData.source.filename}</span>
                        </div>
                    )}
                    {metaData.source?.url && (
                        <div>
                            <span>
                                <b>URL</b>
                            </span>
                            : <span>{metaData.source.url}</span>
                        </div>
                    )}
                    {metaData.page_number !== undefined && (
                        <div>
                            <span>
                                <b>Page Number</b>
                            </span>
                            : <span>{metaData.page_number}</span>
                        </div>
                    )}
                </div>
            </div>
            <div className={classes.input}></div>
        </div>
    );
};
