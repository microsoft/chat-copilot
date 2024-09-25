import { makeStyles } from '@fluentui/react-components';
import React from 'react';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { ChatSuggestion } from './ChatSuggestion';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'row',
        width: '100%',
        justifyContent: 'center',
    },
});

interface ChatSuggestionListProps {
    onClickSuggestion: (message: string) => void;
}

/**
 * ChatSuggestionList - enumerate a list of suggestions loaded from the current conversation state.
 *
 * @param onClickSuggestion function that should be passed to each element so they may invoke it on click
 */
export const ChatSuggestionList: React.FC<ChatSuggestionListProps> = ({
    onClickSuggestion,
}: ChatSuggestionListProps) => {
    const conversation = useAppSelector((state: RootState) => state.conversations);
    const classes = useClasses();
    return (
        <div className={classes.root}>
            {conversation.conversations[conversation.selectedId].suggestions.map((suggestion, idx) => (
                <ChatSuggestion
                    onClick={onClickSuggestion}
                    key={`suggestions-${idx}`}
                    suggestionMainText={suggestion}
                />
            ))}
        </div>
    );
};
