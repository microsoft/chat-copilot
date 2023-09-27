// Copyright (c) Microsoft. All rights reserved.

import { AuthConfig } from '../auth/AuthHelper';
import { ServiceOptions } from '../models/ServiceOptions';
import { BaseService } from './BaseService';

export class ConfigService extends BaseService {
    public getServiceOptionsAsync = async (accessToken: string): Promise<ServiceOptions> => {
        const result = await this.getResponseAsync<ServiceOptions>(
            {
                commandPath: `serviceOptions`,
            },
            accessToken,
        );

        return result;
    };

    public getAuthConfig = async (): Promise<AuthConfig> => {
        return await this.getResponseAsync<AuthConfig>({
            commandPath: 'authConfig',
        });
    };
}
