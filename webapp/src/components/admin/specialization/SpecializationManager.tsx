import React, { useEffect, useId, useState } from 'react';

import { Button, Dropdown, Input, makeStyles, Option, shorthands, Textarea, tokens } from '@fluentui/react-components';
import { useSpecialization } from '../../../libs/hooks';
import { useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { ImageUploaderPreview } from '../../files/ImageUploaderPreview';

interface ISpecializationFile {
    file: File | null;
    src: string | null;
}

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
    fileUploadContainer: {
        display: 'flex',
        flexDirection: 'row',
        ...shorthands.gap(tokens.spacingHorizontalXXXL),
    },
    imageContainer: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingVerticalSNudge),
    },
});

const Rows = 8;

/**
 * Specialization Manager component.
 *
 * @returns {*}
 */
export const SpecializationManager: React.FC = () => {
    const specialization = useSpecialization();
    const classes = useClasses();

    const { specializations, specializationIndexes, chatCompletionDeployments, selectedId } = useAppSelector(
        (state: RootState) => state.admin,
    );

    const [editMode, setEditMode] = useState(false);

    const [id, setId] = useState('');
    const [label, setLabel] = useState('');
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [roleInformation, setRoleInformation] = useState('');
    const [indexName, setIndexName] = useState('');
    const [deployment, setDeployment] = useState('');
    const [membershipId, setMembershipId] = useState<string[]>([]);
    const [imageFile, setImageFile] = useState<ISpecializationFile>({ file: null, src: null });
    const [iconFile, setIconFile] = useState<ISpecializationFile>({ file: null, src: null });

    const [isValid, setIsValid] = useState(false);
    const dropdownId = useId();

    /**
     * Save specialization by creating or updating.
     *
     * Note: When we save a specialization we send the actual files (image / icon) to the server.
     * On fetch we get the file paths from the Specialization payload and display them.
     *
     * @returns {void}
     */
    const onSaveSpecialization = () => {
        if (editMode) {
            void specialization.updateSpecialization(id, {
                label,
                name,
                description,
                roleInformation,
                indexName,
                imageFile: imageFile.file,
                iconFile: iconFile.file,
                deleteImage: !imageFile.src, // Set the delete flag if the src is null
                deleteIcon: !iconFile.src, // Set the delete flag if the src is null,
                deployment,
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
                imageFile: imageFile.file,
                iconFile: iconFile.file,
                deployment,
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
        setImageFile({ file: null, src: null });
        setIconFile({ file: null, src: null });
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
                setDeployment(specializationObj.deployment);
                /**
                 * Set the image and icon file paths
                 * Note: The file is set to null because we only retrieve the file path from the server
                 */
                setImageFile({ file: null, src: specializationObj.imageFilePath });
                setIconFile({ file: null, src: specializationObj.iconFilePath });
                setIndexName(specializationObj.indexName);
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

    useEffect(() => {
        const isValid = !!label && !!name && !!roleInformation && membershipId.length > 0;
        setIsValid(isValid);
        return () => {};
    }, [specializations, selectedId, label, name, roleInformation, membershipId]);

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
                        if (!data.value) {
                            setMembershipId([]);
                            return;
                        }
                        setMembershipId(data.value.split(', '));
                    }}
                />
                <div className={classes.fileUploadContainer}>
                    <div className={classes.imageContainer}>
                        <label htmlFor="image-url">Specialization Image</label>
                        <ImageUploaderPreview
                            buttonLabel="Upload Image"
                            file={imageFile.file ?? imageFile.src}
                            onFileUpdate={(file, src) => {
                                setImageFile({ file, src });
                            }}
                        />
                    </div>
                    <div className={classes.imageContainer}>
                        <label htmlFor="image-url">Specialization Icon</label>
                        <ImageUploaderPreview
                            buttonLabel="Upload Icon"
                            file={iconFile.file ?? iconFile.src}
                            onFileUpdate={(file, src) => {
                                // Set the src to null if the file is falsy ie: '' or null
                                setIconFile({ file, src: src || null });
                            }}
                        />
                    </div>
                </div>
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
