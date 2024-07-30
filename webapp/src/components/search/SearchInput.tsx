// Copyright (c) Microsoft. All rights reserved.
import { Button, Dropdown, makeStyles, Option, Input } from '@fluentui/react-components';
import { SendRegular, Dismiss20Regular } from '@fluentui/react-icons';
import React, { useId, useState } from 'react';
import { AlertType } from '../../libs/models/AlertType';
import { RootState } from '../../redux/app/store';
import { useAppDispatch, useAppSelector } from '../../redux/app/hooks';
import { addAlert } from '../../redux/features/app/appSlice';
import { Flex } from '@fluentui/react-northstar';
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
        width: '70%',
        '& .ui-box::after': {
            transformOrigin: 'left top',
        },
    },
});

interface SearchInputProps {
    onSubmit: (specialization: string, value: string) => Promise<void>;
}

interface Specialization {
    key: string;
    name: string;
}

export const SearchInput: React.FC<SearchInputProps> = ({ onSubmit }) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const [specialization, setSpecialization] = useState<Specialization>({ key: '', name: '' });
    const [value, setValue] = useState('');
    const { specializations } = useAppSelector((state: RootState) => state.app);

    const dropdownId = useId();

    const clearSearchInputState = () => {
        setSpecialization({ key: '', name: '' });
        setValue('');
        dispatch(setSearch({ count: 0, value: [] }));
    };

    const handleSubmit = () => {
        if (value.trim() === '' || specialization.key.trim() === '') {
            return; // only submit if value is not empty
        }
        onSubmit(specialization.key, value).catch((error) => {
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
                <Flex>
                    <Dropdown
                        className={classes.keyWidth}
                        aria-labelledby={dropdownId}
                        placeholder="Select specialization"
                        value={specialization.name}
                        selectedOptions={[specialization.name]}
                    >
                        {specializations.map(
                            (specialization) =>
                                specialization.key != 'general' && (
                                    <Option
                                        key={specialization.key}
                                        onClick={() => {
                                            setSpecialization({ key: specialization.key, name: specialization.name });
                                        }}
                                    >
                                        {specialization.name}
                                    </Option>
                                ),
                        )}
                    </Dropdown>
                    <Input
                        placeholder="Search..."
                        className={classes.inputWidth}
                        value={value}
                        onChange={(_event, data) => {
                            setValue(data.value);
                        }}
                    />
                    <Button
                        title="Submit"
                        aria-label="Search"
                        appearance="transparent"
                        icon={<SendRegular />}
                        onClick={() => {
                            handleSubmit();
                        }}
                    />
                    <Button
                        title="Reset"
                        aria-label="Reset Search"
                        appearance="transparent"
                        icon={<Dismiss20Regular />}
                        onClick={() => {
                            clearSearchInputState();
                        }}
                    />
                </Flex>
            </div>
        </>
    );
};
