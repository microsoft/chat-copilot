import { Subtitle1 } from '@fluentui/react-components';
import React from 'react';
import { AuthHelper } from '../..//libs/auth/AuthHelper';
import { AppState, useClasses } from '../../App';
import { UserSettingsMenu } from '../header/UserSettingsMenu';
import { PluginGallery } from '../open-api-plugins/PluginGallery';
import { BackendProbe, ChatView, Error, Loading } from '../views';
import { useEffect, useState } from 'react';
import { RootState } from '../../redux/app/store';
import { useAppSelector } from '../../redux/app/hooks';
import { makeStyles } from '@fluentui/react-components';

const useStyles = makeStyles({
    title: {
        display: 'flex',
        justifyContent: 'flex-start',
        textAlign: 'left',
    },
    specialization: {
        position: 'absolute',
        left: '50%',
        transform: 'translateX(-50%)',
        textAlign: 'center',
        fontSize: '1.25rem', // Slightly smaller font size for specialization
    },
});

const Chat = ({
    classes,
    appState,
    setAppState,
}: {
    classes: ReturnType<typeof useClasses>;
    appState: AppState;
    setAppState: (state: AppState) => void;
}) => {
    const { conversations, selectedId } = useAppSelector((state: RootState) => state.conversations);
    const { specializations } = useAppSelector((state: RootState) => state.app);
    const [currentSpecialization, setCurrentSpecialization] = useState<string | null>(null);
    const styles = useStyles();
    useEffect(() => {
        if (selectedId) {
            const specializationKey = conversations[selectedId].specializationKey;
            const specialization = specializations.find((spec) => spec.key === specializationKey);
            if (specialization) {
                setCurrentSpecialization(specialization.name);
            }
        }
    }, [selectedId, conversations, specializations]);

    const onBackendFound = React.useCallback(() => {
        setAppState(
            AuthHelper.isAuthAAD()
                ? // if AAD is enabled, we need to set the active account before loading chats
                  AppState.SettingUserInfo
                : // otherwise, we can load chats immediately
                  AppState.LoadingChats,
        );
    }, [setAppState]);
    return (
        <div className={classes.container}>
            <div className={classes.header}>
                <Subtitle1 as="h1" className={styles.title}>
                    Chat Copilot
                </Subtitle1>
                {currentSpecialization && (
                    <Subtitle1 as="h1" className={styles.specialization}>
                        {currentSpecialization}
                    </Subtitle1>
                )}
                {appState > AppState.SettingUserInfo && (
                    <div className={classes.cornerItems}>
                        <div className={classes.cornerItems}>
                            <PluginGallery />
                            <UserSettingsMenu
                                setLoadingState={() => {
                                    setAppState(AppState.SigningOut);
                                }}
                            />
                        </div>
                    </div>
                )}
            </div>
            {appState === AppState.ProbeForBackend && <BackendProbe onBackendFound={onBackendFound} />}
            {appState === AppState.SettingUserInfo && (
                <Loading text={'Hang tight while we fetch your information...'} />
            )}
            {appState === AppState.ErrorLoadingUserInfo && (
                <Error text={'Unable to load user info. Please try signing out and signing back in.'} />
            )}
            {appState === AppState.ErrorLoadingChats && (
                <Error text={'Unable to load chats. Please try refreshing the page.'} />
            )}
            {appState === AppState.LoadingChats && <Loading text="Loading chats..." />}
            {appState === AppState.Chat && <ChatView />}
        </div>
    );
};
export default Chat;
