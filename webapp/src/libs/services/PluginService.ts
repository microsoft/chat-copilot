// Copyright (c) Microsoft. All rights reserved.

import { PluginManifest } from '../models/PluginManifest';
import { BaseService } from './BaseService';

export class PluginService extends BaseService {
    public getPluginManifestAsync = async (manifestDomain: string, accessToken: string): Promise<PluginManifest> => {
        return await this.getResponseAsync<PluginManifest>(
            {
                commandPath: 'pluginManifests',
                method: 'GET',
                query: new URLSearchParams({
                    manifestDomain: manifestDomain,
                }),
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
                commandPath: `chats/${chatId}/plugins/${pluginName}/${enabled}`,
                method: 'PUT',
            },
            accessToken,
        );
    };
}
