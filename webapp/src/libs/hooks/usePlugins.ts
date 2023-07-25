import { useAppDispatch } from '../../redux/app/hooks';
import { addPlugin } from '../../redux/features/plugins/pluginsSlice';
import { Plugin } from '../../redux/features/plugins/PluginsState';
import { PluginManifest, requiresUserLevelAuth } from '../models/PluginManifest';

export const usePlugins = () => {
    const dispatch = useAppDispatch();

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

    return {
        addCustomPlugin,
    };
};
