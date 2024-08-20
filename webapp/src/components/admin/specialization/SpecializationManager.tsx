import React, { useEffect, useId, useState } from 'react';

import { Button, Dropdown, Option, Input, makeStyles, shorthands, Textarea, tokens } from '@fluentui/react-components';
import { useSpecialization } from '../../../libs/hooks';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';

const useClasses = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingVerticalSNudge),
        ...shorthands.padding('80px'),
    },
    horizontal: {
        display: 'flex',
        ...shorthands.gap(tokens.spacingVerticalSNudge),
        alignItems: 'center',
    },
    controls: {
        display: 'flex',
        marginLeft: 'auto',
        ...shorthands.gap(tokens.spacingVerticalSNudge),
    },
    dialog: {
        maxWidth: '25%',
    },
});

const Rows = 8;

export const SpecializationManager: React.FC = () => {
    const specialization = useSpecialization();
    const classes = useClasses();

    const [id, setId] = useState('');
    const [key, setKey] = useState('');
    const [name, setName] = useState('');
    const [indexName, setIndexName] = useState('');
    const [description, setDescription] = useState('');
    const [roleInformation, setRoleInformation] = useState('');
    const [imageFilePath, setImageFilePath] = useState('');
    const [editMode, setEditMode] = useState(false);
    const dropdownId = useId();
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-return
    const { specializations, specializationIndexes, selectedKey } = useAppSelector((state: RootState) => state.admin);

    const onSaveSpecialization = () => {
        if (editMode) {
            void specialization.updateSpecialization(
                id,
                key,
                name,
                description,
                roleInformation,
                indexName,
                imageFilePath,
            );
            resetSpecialization();
        } else {
            void specialization.createSpecialization(key, name, description, roleInformation, indexName, imageFilePath);
            resetSpecialization();
        }
    };

    const resetSpecialization = () => {
        setKey('');
        setName('');
        setDescription('');
        setRoleInformation('');
        setImageFilePath('');
        setIndexName('');
    };

    useEffect(() => {
        if (selectedKey != '') {
            setEditMode(true);
            // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
            const specializationObj = specializations.find((specialization) => specialization.key === selectedKey);
            if (specializationObj) {
                setId(specializationObj.id);
                setKey(specializationObj.key);
                setName(specializationObj.name);
                setDescription(specializationObj.description);
                setRoleInformation(specializationObj.roleInformation);
                setImageFilePath(specializationObj.imageFilePath);
                setIndexName(specializationObj.indexName);
            }
        } else {
            setEditMode(false);
            resetSpecialization();
        }
    }, [editMode, selectedKey, specializations]);

    const onDeleteChat = () => {
        void specialization.deleteSpecialization(id);
        resetSpecialization();
    };

    return (
        <div className={classes.root}>
            <div className={classes.horizontal}></div>
            <label>Key</label>
            <Input
                value={key}
                onChange={(_event, data) => {
                    setKey(data.value);
                }}
            />
            <label>Name</label>
            <Input
                value={name}
                onChange={(_event, data) => {
                    setName(data.value);
                }}
            />
            <label>Index Name</label>
            <Dropdown
                aria-labelledby={dropdownId}
                placeholder="Select Index"
                value={indexName}
                selectedOptions={[indexName]}
            >
                {specializationIndexes.map((specializationIndex) => (
                    <Option
                        key={specializationIndex}
                        onClick={() => {
                            setIndexName(specializationIndex);
                        }}
                    >
                        {specializationIndex}
                    </Option>
                ))}
            </Dropdown>
            <label>Short Description</label>
            <Textarea
                resize="vertical"
                value={description}
                rows={2}
                onChange={(_event, data) => {
                    setDescription(data.value);
                }}
            />
            <label>Role Information</label>
            <Textarea
                resize="vertical"
                value={roleInformation}
                rows={Rows}
                onChange={(_event, data) => {
                    setRoleInformation(data.value);
                }}
            />
            <label>Image URL</label>
            <Input
                value={imageFilePath}
                onChange={(_event, data) => {
                    setImageFilePath(data.value);
                }}
            />
            <div className={classes.controls}>
                <Button appearance="secondary" onClick={onDeleteChat}>
                    Delete
                </Button>

                <Button appearance="primary" onClick={onSaveSpecialization}>
                    Save
                </Button>
            </div>
        </div>
    );
};
