// Copyright (c) Microsoft. All rights reserved.

import { PluginManifest } from '../models/PluginManifest';
import { HostedPlugin } from '../models/ServiceOptions';
import { BaseService } from './BaseService';

export class PluginService extends BaseService {
    public getHostedPluginManifestAsync = async (
        plugin: HostedPlugin,
        accessToken: string,
    ): Promise<PluginManifest> => {
        return await this.getResponseAsync<PluginManifest>(
            {
                baseUrl: plugin.url,
                commandPath: '.well-known/ai-plugin.json',
                method: 'GET',
            },
            accessToken,
        );
    };

    public setPluginStateAsync = async (
        chatId: string,
        pluginName: string,
        accessToken: string,
        enabled: boolean,
    ): Promise<void> => {
        await this.getResponseAsync(
            {
                commandPath: `chatSession/pluginState/${chatId}/${pluginName}/${enabled}`,
                method: 'PUT',
            },
            accessToken,
        );
    };
}
