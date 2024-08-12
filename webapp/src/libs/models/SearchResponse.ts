export interface ISearchMetaData {
    page_number?: number;
    source?: {
        filename?: string;
        url?: string;
    };
}

export interface ISearchMatch {
    id: string;
    label: string;
    content: string[];
    metadata: ISearchMetaData;
}

export interface ISearchValue {
    filename: string;
    matches: ISearchMatch[];
}
