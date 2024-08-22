import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { initialState, SearchResponse, SearchState } from './SearchState';

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
            action: PayloadAction<{ selected: boolean; specializationId: string }>,
        ) => {
            state.selected = action.payload.selected;
            state.selectedSpecializationId = action.payload.specializationId;
        },
    },
});

export const { setSearch, setSelectedSearchItem, setSearchSelected } = searchSlice.actions;

export default searchSlice.reducer;
