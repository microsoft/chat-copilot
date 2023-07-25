// Copyright (c) Microsoft. All rights reserved.

import {
    makeStyles,
    shorthands,
    tokens
} from '@fluentui/react-components';
import * as React from 'react';
import { useChat } from '../../libs/useChat';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { editConversationSystemDescription } from '../../redux/features/conversations/conversationsSlice';
import { SharedStyles } from '../../styles';
import { MemoryBiasSlider } from './persona/MemoryBiasSlider';
import { PromptEditor } from './persona/PromptEditor';

const useClasses = makeStyles({
    root: {
        ...shorthands.margin(tokens.spacingVerticalM, tokens.spacingHorizontalM),
        ...SharedStyles.scroll,
    },
});

export const ChatPersona: React.FC = () => {
    const chat = useChat();
    const classes = useClasses();
    const dispatch = useAppDispatch();

    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const chatState = conversations[selectedId];

    const [shortTermMemory, setShortTermMemory] = React.useState<string>('');
    const [longTermMemory, setLongTermMemory] = React.useState<string>('');

    React.useEffect(() => {
        chat.getSemanticMemories(
            selectedId,
            "WorkingMemory",
        ).then((memories) => {
            setShortTermMemory(memories.join('\n'));
        }).catch(() => { });

        chat.getSemanticMemories(
            selectedId,
            "LongTermMemory",
        ).then((memories) => {
            setLongTermMemory(memories.join('\n'));
        }).catch(() => { });
    }, [chat, selectedId]);

    return (
        <div className={classes.root}>
            <h2>Persona</h2>
            <PromptEditor
                title="Meta Prompt"
                prompt={chatState.systemDescription}
                isEditable={true}
                info="The prompt that defines the chat bot's persona."
                modificationHandler={async (newSystemDescription: string) => {
                    await chat.editChat(
                        selectedId,
                        chatState.title,
                        newSystemDescription,
                    ).finally(() => {
                        dispatch(
                            editConversationSystemDescription({
                                id: selectedId,
                                newSystemDescription: newSystemDescription,
                            }),
                        );
                    });
                }}
            />
            <PromptEditor
                title="Short Term Memory"
                prompt={`<label>: <details>\n${shortTermMemory}`}
                isEditable={false}
                info="Extract information for a short period of time, such as a few seconds or minutes. It should be useful for performing complex cognitive tasks that require attention, concentration, or mental calculation."
            />
            <PromptEditor
                title="Long Term Memory"
                prompt={`<label>: <details>\n${longTermMemory}`}
                isEditable={false}
                info="Extract information that is encoded and consolidated from other memory types, such as working memory or sensory memory. It should be useful for maintaining and recalling one's personal identity, history, and knowledge over time."
            />
            <MemoryBiasSlider />
        </div>
    );
};