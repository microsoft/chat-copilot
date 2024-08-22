// Copyright (c) Microsoft. All rights reserved.
import { Button, Dropdown, makeStyles, Option, SearchBox } from '@fluentui/react-components';
import { Dismiss20Regular, SendRegular } from '@fluentui/react-icons';
import React, { useId, useState } from 'react';
import { AlertType } from '../../libs/models/AlertType';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { addAlert } from '../../redux/features/app/appSlice';
import { setSearch } from '../../redux/features/search/searchSlice';

const useClasses = makeStyles({
    root: {
        paddingTop: '2%',
    },
    keyWidth: {
        width: '20%',
        '& .ui-box::after': {
            transformOrigin: 'left top',
        },
    },
    inputWidth: {
        maxWidth: '70%',
        width: '70%',
        '& .ui-box::after': {
            transformOrigin: 'left top',
        },
    },
    flex: {
        display: 'flex',
    },
});

interface SearchInputProps {
    onSubmit: (specialization: string, value: string) => Promise<void>;
    defaultSpecializationId?: string;
}

interface Specialization {
    id: string;
    name: string;
}

export const SearchInput: React.FC<SearchInputProps> = ({ onSubmit, defaultSpecializationId = '' }) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const { specializations } = useAppSelector((state: RootState) => state.admin);

    // Find the specialization name based on the defaultSpecializationId
    const defaultSpecialization = specializations.find((spec) => spec.id === defaultSpecializationId) ?? {
        id: '',
        name: '',
    };

    const [specialization, setSpecialization] = useState<Specialization>(defaultSpecialization);
    const [value, setValue] = useState('');
    const { app } = useAppSelector((state: RootState) => state);
    const filteredSpecializations = specializations.filter((_specialization) => {
        const hasMembership =
            app.activeUserInfo?.groups.some((val) => _specialization.groupMemberships.includes(val)) ?? false;
        if (hasMembership || _specialization.groupMemberships.length === 0) {
            return _specialization;
        }
        return;
    });

    const dropdownId = useId();

    const clearSearchInputState = () => {
        // setSpecialization({ key: '', name: '' });
        setValue('');
        dispatch(setSearch({ count: 0, value: [] }));
    };

    const handleSubmit = () => {
        if (value.trim() === '' || specialization.id.trim() === '') {
            return; // only submit if value is not empty
        }
        onSubmit(specialization.id, value).catch((error) => {
            const message = `Error submitting search input: ${(error as Error).message}`;
            dispatch(
                addAlert({
                    type: AlertType.Error,
                    message,
                }),
            );
        });
        //clearSearchInputState();
    };

    return (
        <>
            <div className={classes.root}>
                <div className={classes.flex}>
                    <Dropdown
                        className={classes.keyWidth}
                        aria-labelledby={dropdownId}
                        placeholder="Select specialization"
                        value={specialization.name}
                        selectedOptions={[specialization.name]}
                    >
                        {filteredSpecializations.map(
                            (specialization) =>
                                specialization.id != 'general' && (
                                    <Option
                                        key={specialization.id}
                                        onClick={() => {
                                            setSpecialization({ id: specialization.id, name: specialization.name });
                                        }}
                                    >
                                        {specialization.name}
                                    </Option>
                                ),
                        )}
                    </Dropdown>
                    <SearchBox
                        placeholder="Search..."
                        className={classes.inputWidth}
                        value={value}
                        appearance="outline"
                        onChange={(_event, data) => {
                            setValue(data.value);
                        }}
                        onKeyDown={(event) => {
                            if (event.key === 'Enter' && !event.shiftKey) {
                                event.preventDefault();
                                handleSubmit();
                            }
                        }}
                        dismiss={
                            <Button
                                title="Reset"
                                aria-label="Reset Search"
                                appearance="transparent"
                                icon={<Dismiss20Regular />}
                                onClick={() => {
                                    clearSearchInputState();
                                }}
                            />
                        }
                    />
                    {/* <Input
                        
                        className={classes.inputWidth}
                        value={value}
                        onChange={(_event, data) => {
                            setValue(data.value);
                        }}
                        onKeyDown={(event) => {
                            if (event.key === 'Enter' && !event.shiftKey) {
                                event.preventDefault();
                                handleSubmit();
                            }
                        }}
                    /> */}
                    <Button
                        title="Submit"
                        aria-label="Search"
                        appearance="transparent"
                        icon={<SendRegular />}
                        onClick={() => {
                            handleSubmit();
                        }}
                    />
                </div>
            </div>
        </>
    );
};
