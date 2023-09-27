// Copyright (c) Microsoft. All rights reserved.

import { FC, useCallback, useState } from 'react';

import { useMsal } from '@azure/msal-react';
import {
    Avatar,
    Button,
    Menu,
    MenuDivider,
    MenuItem,
    MenuList,
    MenuPopover,
    MenuTrigger,
    Persona,
    makeStyles,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import { Settings24Regular } from '@fluentui/react-icons';
import { AuthHelper } from '../../libs/auth/AuthHelper';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState, resetState } from '../../redux/app/store';
import { FeatureKeys } from '../../redux/features/app/AppState';
import { SettingsDialog } from './settings-dialog/SettingsDialog';

export const useClasses = makeStyles({
    root: {
        marginRight: tokens.spacingHorizontalXL,
    },
    persona: {
        ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingVerticalMNudge),
        overflowWrap: 'break-word',
    },
});

interface IUserSettingsProps {
    setLoadingState: () => void;
}

export const UserSettingsMenu: FC<IUserSettingsProps> = ({ setLoadingState }) => {
    const classes = useClasses();
    const { instance } = useMsal();

    const { activeUserInfo, features } = useAppSelector((state: RootState) => state.app);

    const [openSettingsDialog, setOpenSettingsDialog] = useState(false);

    const onLogout = useCallback(() => {
        setLoadingState();
        AuthHelper.logoutAsync(instance);
        resetState();
    }, [instance, setLoadingState]);

    return (
        <>
            {AuthHelper.isAuthAAD() ? (
                <Menu>
                    <MenuTrigger disableButtonEnhancement>
                        {
                            <Avatar
                                className={classes.root}
                                key={activeUserInfo?.username}
                                name={activeUserInfo?.username}
                                size={28}
                                badge={
                                    !features[FeatureKeys.SimplifiedExperience].enabled
                                        ? { status: 'available' }
                                        : undefined
                                }
                                data-testid="userSettingsButton"
                            />
                        }
                    </MenuTrigger>
                    <MenuPopover>
                        <MenuList>
                            <Persona
                                className={classes.persona}
                                name={activeUserInfo?.username}
                                secondaryText={activeUserInfo?.email}
                                presence={
                                    !features[FeatureKeys.SimplifiedExperience].enabled
                                        ? { status: 'available' }
                                        : undefined
                                }
                                avatar={{ color: 'colorful' }}
                            />
                            <MenuDivider />
                            <MenuItem
                                data-testid="settingsMenuItem"
                                onClick={() => {
                                    setOpenSettingsDialog(true);
                                }}
                            >
                                Settings
                            </MenuItem>
                            <MenuItem data-testid="logOutMenuButton" onClick={onLogout}>
                                Sign out
                            </MenuItem>
                        </MenuList>
                    </MenuPopover>
                </Menu>
            ) : (
                <Button
                    data-testid="settingsButtonWithoutAuth"
                    style={{ color: 'white' }}
                    appearance="transparent"
                    icon={<Settings24Regular color="white" />}
                    onClick={() => {
                        setOpenSettingsDialog(true);
                    }}
                >
                    Settings
                </Button>
            )}
            <SettingsDialog
                open={openSettingsDialog}
                closeDialog={() => {
                    setOpenSettingsDialog(false);
                }}
            />
        </>
    );
};
