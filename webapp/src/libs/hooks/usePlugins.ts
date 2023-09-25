import { useMsal } from '@azure/msal-react';
import * as React from 'react';
import { useAppDispatch } from '../../redux/app/hooks';
import { addAlert } from '../../redux/features/app/appSlice';
import { Plugin } from '../../redux/features/plugins/PluginsState';
import { addPlugin } from '../../redux/features/plugins/pluginsSlice';
import { AuthHelper } from '../auth/AuthHelper';
import { AlertType } from '../models/AlertType';
import { PluginManifest, requiresUserLevelAuth } from '../models/PluginManifest';
import { HostedPlugin } from '../models/ServiceOptions';
import { ChatService } from '../services/ChatService';
import { PluginService } from '../services/PluginService';
import { getErrorDetails } from './useChat';

export const usePlugins = () => {
    const { instance, inProgress } = useMsal();
    const dispatch = useAppDispatch();
    const chatService = React.useMemo(() => new ChatService(process.env.REACT_APP_BACKEND_URI as string), []);
    const pluginService = new PluginService(process.env.REACT_APP_BACKEND_URI as string);

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
        (manifestDomain: string) =>
            AuthHelper.getSKaaSAccessToken(instance, inProgress)
                .then((accessToken) => chatService.getPluginManifest(manifestDomain, accessToken))
                .catch((e: unknown) => {
                    const errorMessage = `Error getting plugin manifest. Details: ${getErrorDetails(e)}`;
                    dispatch(addAlert({ message: errorMessage, type: AlertType.Error }));
                    return undefined;
                }),
        [chatService, dispatch, inProgress, instance],
    );

    const getHostedPluginManifestAsync = async (plugin: HostedPlugin): Promise<PluginManifest> => {
        const accessToken = await AuthHelper.getSKaaSAccessToken(instance, inProgress);
        return await pluginService.getPluginManifestAsync(plugin, accessToken);
    };

    return {
        addCustomPlugin,
        getPluginManifest,
        getHostedPluginManifestAsync,
    };
};
