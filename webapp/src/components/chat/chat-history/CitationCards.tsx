// Copyright (c) Microsoft. All rights reserved.

import {
    Badge,
    Caption1,
    Card,
    CardHeader,
    makeStyles,
    shorthands,
    Text,
    ToggleButton,
} from '@fluentui/react-components';
import { ChevronDown20Regular, ChevronUp20Regular } from '@fluentui/react-icons';
import React, { useState } from 'react';
import { IChatMessage } from '../../../libs/models/ChatMessage';
import { customTokens } from '../../../styles';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        ...shorthands.gap(customTokens.spacingVerticalS),
        flexDirection: 'column',
    },
    card: {
        display: 'flex',
        width: '100%',
        height: 'fit-content',
    },
});

interface ICitationCardsProps {
    message: IChatMessage;
}

export const CitationCards: React.FC<ICitationCardsProps> = ({ message }) => {
    const classes = useClasses();

    const [showSnippetStates, setShowSnippetStates] = useState<boolean[]>([]);
    React.useEffect(() => {
        initShowSnippetStates();
        // This will only run once, when the component is mounted
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    if (!message.citations || message.citations.length === 0) {
        return null;
    }

    const initShowSnippetStates = () => {
        if (!message.citations) {
            return;
        }

        const newShowSnippetStates = [...showSnippetStates];
        message.citations.forEach((_, index) => {
            newShowSnippetStates[index] = false;
        });
        setShowSnippetStates(newShowSnippetStates);
    };

    const showSnippet = (index: number) => {
        const newShowSnippetStates = [...showSnippetStates];
        newShowSnippetStates[index] = !newShowSnippetStates[index];
        setShowSnippetStates(newShowSnippetStates);
    };

    return (
        <div className={classes.root}>
            {message.citations.map((citation, index) => {
                return (
                    <Card className={classes.card} size="small" key={`citation-card-${index}`}>
                        <CardHeader
                            image={
                                <Badge shape="rounded" appearance="outline" color="informative">
                                    {index + 1}
                                </Badge>
                            }
                            header={<Text weight="semibold">{citation.sourceName}</Text>}
                            description={<Caption1>Relevance score: {citation.relevanceScore.toFixed(3)}</Caption1>}
                            action={
                                <ToggleButton
                                    appearance="transparent"
                                    icon={showSnippetStates[index] ? <ChevronUp20Regular /> : <ChevronDown20Regular />}
                                    onClick={() => {
                                        showSnippet(index);
                                    }}
                                />
                            }
                        />

                        {showSnippetStates[index] && <p>{citation.snippet}</p>}
                    </Card>
                );
            })}
        </div>
    );
};
