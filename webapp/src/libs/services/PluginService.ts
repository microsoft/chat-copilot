// Copyright (c) Microsoft. All rights reserved.

import { PluginManifest } from '../models/PluginManifest';
import { HostedPlugin } from '../models/ServiceOptions';
import { BaseService } from './BaseService';

export class PluginService extends BaseService {
    public getPluginManifestAsync = async (plugin: HostedPlugin, accessToken: string): Promise<PluginManifest> => {
        return await this.getResponseAsync<PluginManifest>(
            {
                baseUrl: plugin.url,
                commandPath: '.well-known/ai-plugin.json',
                method: 'GET',
            },
            accessToken,
        );
    };

    // public setPluginStateAsync = async (plugin: Plugin, accessToken: string, enabled: boolean): Promise<void> => {
    //     await this.getResponseAsync(
    //         {
    //             commandPath: plugin.url + 'api/plugins/state',
    //             method: 'POST',
    //             body: {
    //                 enabled,
    //             },
    //         },
    //         accessToken,
    //     );
    // };
}
