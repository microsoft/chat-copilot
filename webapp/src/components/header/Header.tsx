import { Subtitle1, Tag, makeStyles, tokens } from '@fluentui/react-components';
import { FC } from 'react';
import { useAppSelector } from '../../redux/app/hooks';
import { PluginGallery } from '../open-api-plugins/PluginGallery';
import { UserSettingsMenu } from '../header/UserSettingsMenu';
import { AppState } from '../../App';
import logo from '../../assets/quartech-icons/logo.png';
import darkLogo from '../../assets/quartech-icons/dark_logo.png';
import { RootState } from '../../redux/app/store';
import { FeatureKeys } from '../../redux/features/app/AppState';

const useStyles = makeStyles({
    header: {
        alignItems: 'center',
        display: 'flex',
        height: '48px',
        justifyContent: 'space-between',
        width: '100%',
        padding: '0 16px',
        position: 'relative',
    },
    title: {
        display: 'flex',
        flex: 1,
        textAlign: 'left',
        alignItems: 'center',
    },
    betaTag: {
        marginLeft: tokens.spacingHorizontalM,
    },
    logo: {
        maxWidth: '70%',
        maxHeight: '70%',
        position: 'absolute',
        left: '50%',
        transform: 'translateX(-50%)',
    },
    cornerItems: {
        display: 'flex',
        gap: tokens.spacingHorizontalS,
        textAlign: 'right',
        paddingRight: '16px',
    },
});

interface HeaderProps {
    appState: AppState;
    setAppState: (state: AppState) => void;
    showPluginsAndSettings: boolean;
}

const Header: FC<HeaderProps> = ({ appState, setAppState, showPluginsAndSettings }) => {
    const classes = useStyles();
    const isDarkMode = useAppSelector((state: RootState) => state.app.features[FeatureKeys.DarkMode].enabled);

    return (
        <div
            className={classes.header}
            style={{
                backgroundColor: isDarkMode ? 'black' : 'white',
                color: isDarkMode ? 'white' : tokens.colorNeutralForegroundOnBrand,
                boxShadow: isDarkMode ? '0 2px 4px rgba(255, 255, 255, 0.1)' : '0 2px 4px rgba(0, 0, 0, 0.1)',
            }}
        >
            <div className={classes.title}>
                <Subtitle1 as="h1" style={{ color: isDarkMode ? 'white' : 'black' }}>
                    Chat Q-Pilot
                </Subtitle1>
                <Tag shape={'rounded'} size="small" appearance="brand" className={classes.betaTag}>
                    Beta
                </Tag>
            </div>
            <img src={isDarkMode ? darkLogo : logo} alt="Logo" className={classes.logo} />
            {showPluginsAndSettings && appState > AppState.SettingUserInfo && (
                <div className={classes.cornerItems} style={{ color: isDarkMode ? 'white' : 'black' }}>
                    <PluginGallery isDarkMode={isDarkMode} />
                    <UserSettingsMenu
                        setLoadingState={() => {
                            setAppState(AppState.SigningOut);
                        }}
                    />
                </div>
            )}
        </div>
    );
};

export default Header;
