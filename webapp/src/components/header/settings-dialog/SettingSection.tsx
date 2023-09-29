import { Divider, Switch, Text, makeStyles, shorthands, tokens } from '@fluentui/react-components';
import { useCallback } from 'react';
import { AuthHelper } from '../../../libs/auth/AuthHelper';
import { useAppDispatch, useAppSelector } from '../../../redux/app/hooks';
import { RootState } from '../../../redux/app/store';
import { FeatureKeys, Setting } from '../../../redux/features/app/AppState';
import { toggleFeatureFlag } from '../../../redux/features/app/appSlice';
import { toggleMultiUserConversations } from '../../../redux/features/conversations/conversationsSlice';

const useClasses = makeStyles({
    feature: {
        display: 'flex',
        flexDirection: 'column',
        ...shorthands.gap(tokens.spacingVerticalNone),
    },
    featureDescription: {
        paddingLeft: '5%',
        paddingBottom: tokens.spacingVerticalS,
    },
});

interface ISettingsSectionProps {
    setting: Setting;
    contentOnly?: boolean;
}

export const SettingSection: React.FC<ISettingsSectionProps> = ({ setting, contentOnly }) => {
    const classes = useClasses();
    const { features } = useAppSelector((state: RootState) => state.app);
    const dispatch = useAppDispatch();

    const onFeatureChange = useCallback(
        (featureKey: FeatureKeys) => {
            dispatch(toggleFeatureFlag(featureKey));
            if (featureKey === FeatureKeys.MultiUserChat) {
                dispatch(toggleMultiUserConversations());
            }
        },
        [dispatch],
    );

    return (
        <>
            {!contentOnly && <h3>{setting.title}</h3>}
            {setting.description && <p>{setting.description}</p>}
            <div
                style={{
                    display: 'flex',
                    flexDirection: `${setting.stackVertically ? 'column' : 'row'}`,
                    flexWrap: 'wrap',
                }}
            >
                {setting.features.map((key) => {
                    const feature = features[key];
                    return (
                        <div key={key} className={classes.feature}>
                            <Switch
                                label={feature.label}
                                checked={feature.enabled}
                                disabled={
                                    !!feature.inactive || (key === FeatureKeys.MultiUserChat && !AuthHelper.isAuthAAD())
                                }
                                onChange={() => {
                                    onFeatureChange(key);
                                }}
                                data-testid={feature.label}
                            />
                            <Text
                                className={classes.featureDescription}
                                style={{
                                    color: feature.inactive
                                        ? tokens.colorNeutralForegroundDisabled
                                        : tokens.colorNeutralForeground2,
                                }}
                            >
                                {feature.description}
                            </Text>
                        </div>
                    );
                })}
            </div>
            {!contentOnly && <Divider />}
        </>
    );
};
