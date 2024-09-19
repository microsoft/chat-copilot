import React, { useEffect, useId, useState } from 'react';

import { Button, Dropdown, Input, makeStyles, Option, shorthands, Textarea, tokens } from '@fluentui/react-components';
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
    required: {
        color: '#990000',
    },
    scrollableContainer: {
        overflowY: 'auto',
        maxHeight: 'calc(100vh - 100px)', // Adjust this value as needed
        '&:hover': {
            '&::-webkit-scrollbar-thumb': {
                backgroundColor: tokens.colorScrollbarOverlay,
                visibility: 'visible',
            },
        },
        '&::-webkit-scrollbar-track': {
            backgroundColor: tokens.colorSubtleBackground,
        },
        ...shorthands.padding('10px'),
    },
});

const Rows = 8;

export const SpecializationManager: React.FC = () => {
    const specialization = useSpecialization();
    const classes = useClasses();

    const [editMode, setEditMode] = useState(false);

    const [id, setId] = useState('');
    const [label, setLabel] = useState('');
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [roleInformation, setRoleInformation] = useState('');
    const [indexName, setIndexName] = useState('');
    const [deployment, setDeployment] = useState('');
    const [imageFilePath, setImageFilePath] = useState('');
    const [iconFilePath, setIconFilePath] = useState('');
    const [membershipId, setMembershipId] = useState<string[]>([]);

    const dropdownId = useId();

    const { specializations, specializationIndexes, chatCompletionDeployments, selectedId } = useAppSelector(
        (state: RootState) => state.admin,
    );

    const onSaveSpecialization = () => {
        if (editMode) {
            void specialization.updateSpecialization(id, {
                label,
                name,
                description,
                roleInformation,
                indexName,
                deployment,
                imageFilePath,
                iconFilePath,
                groupMemberships: membershipId,
            });
            resetSpecialization();
        } else {
            void specialization.createSpecialization({
                label,
                name,
                description,
                roleInformation,
                indexName,
                deployment,
                imageFilePath,
                iconFilePath,
                groupMemberships: membershipId,
            });
            resetSpecialization();
        }
    };

    const resetSpecialization = () => {
        setId('');
        setLabel('');
        setName('');
        setDescription('');
        setRoleInformation('');
        setMembershipId([]);
        setImageFilePath('');
        setIconFilePath('');
        setIndexName('');
        setDeployment('');
    };

    useEffect(() => {
        if (selectedId != '') {
            setEditMode(true);
            const specializationObj = specializations.find((specialization) => specialization.id === selectedId);
            if (specializationObj) {
                setId(specializationObj.id);
                setLabel(specializationObj.label);
                setName(specializationObj.name);
                setDescription(specializationObj.description);
                setRoleInformation(specializationObj.roleInformation);
                setMembershipId(specializationObj.groupMemberships);
                setImageFilePath(specializationObj.imageFilePath);
                setIconFilePath(specializationObj.iconFilePath);
                setIndexName(specializationObj.indexName);
                setDeployment(specializationObj.deployment);
            }
        } else {
            setEditMode(false);
            resetSpecialization();
        }
    }, [editMode, selectedId, specializations]);

    const onDeleteChat = () => {
        void specialization.deleteSpecialization(id);
        resetSpecialization();
    };

    const [isValid, setIsValid] = useState(false);
    useEffect(() => {
        const isValid = !!label && !!name && !!roleInformation;
        setIsValid(isValid);
        return () => {};
    }, [specializations, selectedId, label, name, roleInformation]);

    return (
        <div className={classes.scrollableContainer}>
            <div className={classes.root}>
                <div className={classes.horizontal}></div>
                <label htmlFor="name">
                    Name<span className={classes.required}>*</span>
                </label>
                <Input
                    id="name"
                    required
                    value={name}
                    onChange={(_event, data) => {
                        setName(data.value);
                    }}
                />
                <label htmlFor="label">
                    Label<span className={classes.required}>*</span>
                </label>
                <Input
                    id="label"
                    required
                    value={label}
                    onChange={(_event, data) => {
                        setLabel(data.value);
                    }}
                />
                <label htmlFor="index-name">Enrichment Index</label>
                <Dropdown
                    clearable
                    id="index-name"
                    aria-labelledby={dropdownId}
                    onOptionSelect={(_control, data) => {
                        setIndexName(data.optionValue ?? '');
                    }}
                    value={indexName}
                >
                    {specializationIndexes.map((specializationIndex) => (
                        <Option key={specializationIndex}>{specializationIndex}</Option>
                    ))}
                </Dropdown>
                <label htmlFor="deployment">Deployment</label>
                <Dropdown
                    clearable
                    id="deployment"
                    aria-labelledby={dropdownId}
                    onOptionSelect={(_control, data) => {
                        setDeployment(data.optionValue ?? '');
                    }}
                    value={deployment}
                >
                    {chatCompletionDeployments.map((deployment) => (
                        <Option key={deployment} value={deployment}>
                            {deployment}
                        </Option>
                    ))}
                </Dropdown>
                <label htmlFor="description">
                    Short Description<span className={classes.required}>*</span>
                </label>
                <Textarea
                    id="description"
                    required
                    resize="vertical"
                    value={description}
                    rows={2}
                    onChange={(_event, data) => {
                        setDescription(data.value);
                    }}
                />
                <label htmlFor="context">
                    Chat Context<span className={classes.required}>*</span>
                </label>
                <Textarea
                    id="context"
                    required
                    resize="vertical"
                    value={roleInformation}
                    rows={Rows}
                    onChange={(_event, data) => {
                        setRoleInformation(data.value);
                    }}
                />
                <label htmlFor="membership">
                    Entra Membership IDs<span className={classes.required}>*</span>
                </label>
                <Input
                    id="membership"
                    required
                    value={membershipId.join(', ')}
                    onChange={(_event, data) => {
                        setMembershipId(data.value.split(', '));
                    }}
                />
                <label htmlFor="image-url">Image URL</label>
                <Input
                    id="image-url"
                    value={imageFilePath}
                    onChange={(_event, data) => {
                        setImageFilePath(data.value);
                    }}
                />
                <label htmlFor="image-url">Bot Icon URL</label>
                <Input
                    id="icon-url"
                    value={iconFilePath}
                    onChange={(_event, data) => {
                        setIconFilePath(data.value);
                    }}
                />
                <div className={classes.controls}>
                    <Button appearance="secondary" disabled={!id} onClick={onDeleteChat}>
                        Delete
                    </Button>

                    <Button appearance="primary" disabled={!isValid} onClick={onSaveSpecialization}>
                        Save
                    </Button>
                </div>
            </div>
        </div>
    );
};
