// Copyright (c) Microsoft. All rights reserved.

import {
    Body1Strong,
    Button,
    Dialog,
    DialogActions,
    DialogBody,
    DialogContent,
    DialogSurface,
    DialogTitle,
    DialogTrigger,
    Divider,
    Label,
    Link,
    SelectTabEventHandler,
    Tab,
    TabList,
    TabValue,
    Tooltip,
    makeStyles,
    mergeClasses,
    shorthands,
} from '@fluentui/react-components';
import { Info16Regular } from '@fluentui/react-icons';
import React from 'react';
import { BotResponsePrompt, DependencyDetails, PromptSectionsNameMap } from '../../../libs/models/BotResponsePrompt';
import { ChatMessageType, IChatMessage } from '../../../libs/models/ChatMessage';
import { PlanType } from '../../../libs/models/Plan';
import { PlanExecutionMetadata } from '../../../libs/models/PlanExecutionMetadata';
import { useDialogClasses } from '../../../styles';
import { TokenUsageGraph } from '../../token-usage/TokenUsageGraph';
import { formatParagraphTextContent } from '../../utils/TextUtils';
import { StepwiseThoughtProcessView } from './stepwise-planner/StepwiseThoughtProcessView';

const useClasses = makeStyles({
    infoButton: {
        ...shorthands.padding(0),
        ...shorthands.margin(0),
        minWidth: 'auto',
        marginLeft: 'auto', // align to right
    },
    text: {
        width: '100%',
        overflowWrap: 'break-word',
    },
});

interface IPromptDialogProps {
    message: IChatMessage;
}

export const PromptDialog: React.FC<IPromptDialogProps> = ({ message }) => {
    const classes = useClasses();
    const dialogClasses = useDialogClasses();

    const [selectedTab, setSelectedTab] = React.useState<TabValue>('formatted');
    const onTabSelect: SelectTabEventHandler = (_event, data) => {
        setSelectedTab(data.value);
    };

    let prompt: string | BotResponsePrompt;
    try {
        prompt = JSON.parse(message.prompt ?? '{}') as BotResponsePrompt;
    } catch (e) {
        prompt = message.prompt ?? '';
    }

    let promptDetails;
    if (typeof prompt === 'string') {
        promptDetails = formatParagraphTextContent(prompt);
    } else {
        promptDetails = Object.entries(prompt).map(([key, value]) => {
            let isStepwiseThoughtProcess = false;
            if (key === 'externalInformation') {
                const information = value as DependencyDetails;
                if (information.context) {
                    // TODO: [Issue #150, sk#2106] Accommodate different planner contexts once core team finishes work to return prompt and token usage.
                    const details = information.context as PlanExecutionMetadata;
                    isStepwiseThoughtProcess = details.plannerType === PlanType.Stepwise;

                    // Backend can be configured to return the raw response from Stepwise Planner. In this case, no meta prompt was generated or completed
                    // and we should show the Stepwise thought process as the raw content view.
                    if ((prompt as BotResponsePrompt).metaPromptTemplate.length <= 0) {
                        (prompt as BotResponsePrompt).rawView = (
                            <pre className={mergeClasses(dialogClasses.text, classes.text)}>
                                {JSON.stringify(JSON.parse(details.stepsTaken), null, 2)}
                            </pre>
                        );
                    }
                }

                if (!isStepwiseThoughtProcess) {
                    value = information.result;
                }
            }

            if (
                key === 'chatMemories' &&
                value &&
                !(value as string).includes('User has also shared some document snippets:')
            ) {
                value += '\nNo relevant document memories.';
            }

            return value && key !== 'metaPromptTemplate' ? (
                <div className={dialogClasses.paragraphs} key={`prompt-details-${key}`}>
                    <Body1Strong>{PromptSectionsNameMap[key]}</Body1Strong>
                    {isStepwiseThoughtProcess ? (
                        <StepwiseThoughtProcessView thoughtProcess={value as DependencyDetails} />
                    ) : (
                        formatParagraphTextContent(value as string)
                    )}
                </div>
            ) : null;
        });
    }

    return (
        <Dialog>
            <DialogTrigger disableButtonEnhancement>
                <Tooltip content={'Show prompt'} relationship="label">
                    <Button className={classes.infoButton} icon={<Info16Regular />} appearance="transparent" />
                </Tooltip>
            </DialogTrigger>
            <DialogSurface className={dialogClasses.surface}>
                <DialogBody
                    style={{
                        height: message.type !== ChatMessageType.Message || !message.prompt ? 'fit-content' : '825px',
                    }}
                >
                    <DialogTitle>Prompt</DialogTitle>
                    <DialogContent className={dialogClasses.content}>
                        <TokenUsageGraph promptView tokenUsage={message.tokenUsage ?? {}} />
                        {message.prompt && typeof prompt !== 'string' && (
                            <TabList selectedValue={selectedTab} onTabSelect={onTabSelect}>
                                <Tab data-testid="formatted" id="formatted" value="formatted">
                                    Formatted
                                </Tab>
                                <Tab data-testid="rawContent" id="rawContent" value="rawContent">
                                    Raw Content
                                </Tab>
                            </TabList>
                        )}
                        <div
                            className={
                                message.prompt && typeof prompt !== 'string' ? dialogClasses.innerContent : undefined
                            }
                        >
                            {selectedTab === 'formatted' && promptDetails}
                            {selectedTab === 'rawContent' &&
                                ((prompt as BotResponsePrompt).metaPromptTemplate.length > 0
                                    ? (prompt as BotResponsePrompt).metaPromptTemplate.map((contextMessage, index) => {
                                          return (
                                              <div key={`context-message-${index}`}>
                                                  <p>{`Role: ${contextMessage.Role.Label}`}</p>
                                                  {formatParagraphTextContent(`Content: ${contextMessage.Content}`)}
                                                  <Divider />
                                              </div>
                                          );
                                      })
                                    : (prompt as BotResponsePrompt).rawView)}
                        </div>
                    </DialogContent>
                    <DialogActions position="start" className={dialogClasses.footer}>
                        <Label size="small" color="brand">
                            Want to learn more about prompts? Click{' '}
                            <Link href="https://aka.ms/sk-about-prompts" target="_blank" rel="noreferrer">
                                here
                            </Link>
                            .
                        </Label>
                        <DialogTrigger disableButtonEnhancement>
                            <Button appearance="secondary">Close</Button>
                        </DialogTrigger>
                    </DialogActions>
                </DialogBody>
            </DialogSurface>
        </Dialog>
    );
};
