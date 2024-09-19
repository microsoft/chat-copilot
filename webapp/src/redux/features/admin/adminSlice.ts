import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { ISpecialization } from '../../../libs/models/Specialization';
import { AdminState, initialState } from './AdminState';

export const adminSlice = createSlice({
    name: 'admin',
    initialState,
    reducers: {
        // Set the currently selected chat specialization
        setChatSpecialization: (state: AdminState, action: PayloadAction<ISpecialization>) => {
            state.chatSpecialization = action.payload;
        },
        setSpecializations: (state: AdminState, action: PayloadAction<ISpecialization[]>) => {
            // const updatedSpecializations = action.payload.filter(
            //     (specialization: ISpecialization) => specialization.isActive,
            // );
            state.specializations = action.payload;
        },
        setSpecializationIndexes: (state: AdminState, action: PayloadAction<string[]>) => {
            state.specializationIndexes = action.payload;
        },
        setChatCompletionDeployments: (state: AdminState, action: PayloadAction<string[]>) => {
            state.chatCompletionDeployments = action.payload;
        },
        setAdminSelected: (state: AdminState, action: PayloadAction<boolean>) => {
            state.isAdminSelected = action.payload;
        },
        setSelectedKey: (state: AdminState, action: PayloadAction<string>) => {
            state.selectedId = action.payload;
        },
        addSpecialization: (state: AdminState, action: PayloadAction<ISpecialization>) => {
            state.specializations.push(action.payload);
        },
        editSpecialization: (state: AdminState, action: PayloadAction<ISpecialization>) => {
            const specializations = state.specializations;
            const updatedSpecializations = specializations.filter(
                (specialization: ISpecialization) => specialization.id !== action.payload.id,
            );
            state.specializations = updatedSpecializations;
            state.specializations.push(action.payload);
        },
        removeSpecialization: (state: AdminState, action: PayloadAction<string>) => {
            const specializations = state.specializations;
            const selectedKey = action.payload;
            const updatedSpecializations = specializations.filter(
                (specialization: ISpecialization) => specialization.id !== selectedKey,
            );
            state.specializations = updatedSpecializations;
        },
    },
});

export const {
    setSpecializations,
    setChatSpecialization,
    setSpecializationIndexes,
    setChatCompletionDeployments,
    setAdminSelected,
    setSelectedKey,
    addSpecialization,
    editSpecialization,
    removeSpecialization,
} = adminSlice.actions;

export default adminSlice.reducer;
