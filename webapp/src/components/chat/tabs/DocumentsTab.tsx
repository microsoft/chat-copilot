// Copyright (c) Microsoft. All rights reserved.

import { TabView } from './TabView';

interface IChatResourceListProps {
    chatId: string;
}

export const DocumentsTab: React.FC<IChatResourceListProps> = () => {
    return (
        <TabView
            title="Documents"
            learnMoreDescription="document embeddings"
            learnMoreLink="https://aka.ms/sk-docs-vectordb"
        ></TabView>
    );
};
