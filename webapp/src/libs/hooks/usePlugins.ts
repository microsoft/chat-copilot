import { useMsal } from '@azure/msal-react';
import * as React from 'react';
import { useAppDispatch } from '../../redux/app/hooks';
import { Plugin } from '../../redux/features/plugins/PluginsState';
import { addPlugin } from '../../redux/features/plugins/pluginsSlice';
import { AuthHelper } from '../auth/AuthHelper';
import { PluginManifest, requiresUserLevelAuth } from '../models/PluginManifest';
import { PluginService } from '../services/PluginService';

export const usePlugins = () => {
    const { instance, inProgress } = useMsal();
    const dispatch = useAppDispatch();
    const pluginService = React.useMemo(() => new PluginService(), []);

    const addCustomPlugin = (manifest: PluginManifest, manifestDomain: string) => {
        const newPlugin: Plugin = {
            name: manifest.name_for_human,
            nameForModel: manifest.name_for_model,
            publisher: 'Custom Plugin',
            description: manifest.description_for_human,
            enabled: false,
            authRequirements: {
                personalAccessToken: requiresUserLevelAuth(manifest.auth),
            },
            headerTag: manifest.name_for_model,
            icon: manifest.logo_url,
            manifestDomain: manifestDomain,
        };

        dispatch(addPlugin(newPlugin));
    };

    const getPluginManifest = React.useCallback(
        async (manifestDomain: string) => {
            const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
            return await pluginService.getPluginManifestAsync(manifestDomain, accessToken);
        },
        [pluginService, inProgress, instance],
    );

    const setPluginStateAsync = async (chatId: string, pluginName: string, enabled: boolean): Promise<void> => {
        const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
        await pluginService.setPluginStateAsync(chatId, pluginName, accessToken, enabled);
    };

    return {
        addCustomPlugin,
        getPluginManifest,
        setPluginStateAsync,
    };
};
