import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { SearchState, initialState, SearchResponse } from './SearchState';

export const searchSlice = createSlice({
    name: 'search',
    initialState,
    reducers: {
        setSearch: (state: SearchState, action: PayloadAction<SearchResponse>) => {
            state.searchData = action.payload;
        },
        setSelectedSearchItem: (state: SearchState, action: PayloadAction<string>) => {
            state.selectedSearchItem = action.payload;
        },
        setSearchSelected: (
            state: SearchState,
            action: PayloadAction<{ selected: boolean; specializationKey: string }>,
        ) => {
            state.selected = action.payload.selected;
            state.selectedSpecializationKey = action.payload.specializationKey;
        },
    },
});

export const { setSearch, setSelectedSearchItem, setSearchSelected } = searchSlice.actions;

export default searchSlice.reducer;
