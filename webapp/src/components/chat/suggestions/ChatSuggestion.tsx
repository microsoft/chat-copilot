import { Card, CardHeader, Text, makeStyles, tokens } from '@fluentui/react-components';
import React from 'react';

const useStyles = makeStyles({
    main: {
        display: 'flex',
        flexWrap: 'wrap',
    },

    card: {
        maxWidth: '250px',
        height: '200px',
    },

    root: {
        padding: tokens.spacingHorizontalS,
    },

    caption: {
        color: tokens.colorNeutralForeground3,
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },

    textContainer: {
        display: 'flex',
        flexDirection: 'column',
    },
});

interface ChatSuggestionProps {
    onClick: (message: string) => void;
    suggestionMainText: string;
}

/**
 * Chat suggestion card. Simple element that invokes a function when clicked.
 *
 * @param suggestionMainText Text to be rendered by the suggestion card
 * @param onClick function to invoke when clicking the card
 */
export const ChatSuggestion: React.FC<ChatSuggestionProps> = ({ suggestionMainText, onClick }) => {
    const styles = useStyles();
    return (
        <div className={styles.root} key={'suggestionDivId'}>
            <Card
                className={styles.card}
                data-testid="chatSuggestionItem"
                onClick={() => {
                    onClick(suggestionMainText);
                }}
                key={'suggestionCardId'}
            >
                <CardHeader
                    header={
                        <div className={styles.textContainer}>
                            <Text size={500} weight="bold">
                                Q:
                            </Text>
                            <Text weight="semibold">{suggestionMainText}</Text>
                        </div>
                    }
                />
            </Card>
        </div>
    );
};
