import { ISearchValue } from '../../../libs/models/SearchResponse';

export interface SearchState {
    selected: boolean;
    searchData: SearchResponse;
    selectedSearchItem: string;
}

export const initialState: SearchState = {
    selected: false,
    searchData: {count:0, value:[]},
    selectedSearchItem: '',
};

export interface SearchResponse {
    count: number;
    value: ISearchValue[];
}