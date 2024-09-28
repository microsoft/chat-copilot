// Copyright (c) Microsoft. All rights reserved.

import { InteractionStatus, IPublicClientApplication } from '@azure/msal-browser';
import { BaseService } from './BaseService';
import { TokenHelper } from '../auth/TokenHelper';
import { AuthProviderCallback, Client, ResponseType } from '@microsoft/microsoft-graph-client';

export interface BatchRequest {
    id: string;
    method: 'GET' | 'POST' | 'PUT' | 'UPDATE' | 'DELETE';
    url: string;
    headers: any;
}

export interface BatchResponseBody {
    responses: BatchResponse[];
}

export interface BatchResponse {
    id: number;
    status: number;
    body?: any;
    headers?: any;
}

/**
 * Graph service used to interact with Microsoft's Graph API.
 *
 * @export
 * @class
 * @extends {BaseService}
 */
export class GraphService extends BaseService {
    constructor() {
        super('https://graph.microsoft.com');
    }

    /**
     * The API version of Microsoft's Graph API.
     *
     * @type {string}
     */
    private version = 'v1.0';

    /**
     * Get the full command path for a resource.
     *
     * @param {string} resourcePath - The resource path.
     * @returns {string} The full command path.
     */
    private getCommandPath = (resourcePath: string): string => {
        return `/${this.version}/${resourcePath}`;
    };

    /**
     * Get the Graph client.
     *
     * Note: This can be used to make requests to the Graph API.
     * Useful for making requests where you need to control the response type.
     *
     * @param {string} accessToken - The access token.
     * @returns {Client} The Graph client.
     */
    private getGraphClient(accessToken: string) {
        return Client.init({
            authProvider: (callback: AuthProviderCallback) => {
                callback(undefined, accessToken);
            },
        });
    }

    /**
     *  Make a batch request to Microsoft's Graph API.
     *
     *  @param {BatchRequest[]} batchRequests - The batch requests to make.
     *  @param {string} accessToken - The access token.
     *  @returns {Promise<BatchResponse[]>} The batch responses.
     */
    public async makeBatchRequest(batchRequests: BatchRequest[], accessToken: string) {
        const result = await this.getResponseAsync<BatchResponseBody>(
            {
                commandPath: this.getCommandPath('$batch'),
                method: 'POST',
                body: { requests: batchRequests },
            },
            accessToken,
        );

        return result.responses;
    }

    /**
     * Get the current user's avatar from Microsoft's Graph API.
     *
     * Context: To be able to get the user's avatar the image needs to have been uploaded to the user's profile
     * on the current tenant. There is discussion on migrating the current Microsoft user image from the Quartech
     * tenant to the Quartech Lab tenant.
     *
     * Note: Passing `instance` and `inProgress` intentionally (vs accessToken)
     * as the generated token needs a specific scope ie: `User.Read`.
     *
     *
     * Note:  Using the Graph Client vs `getResponseAsync` to control the response type.
     *
     * @param {IPublicClientApplication} instance - The MSAL instance.
     * @param {InteractionStatus} inProgress - The interaction status.
     * @returns {Promise<Base64String | undefined>} The base64 encoded image.
     */
    public async getUserAvatar(instance: IPublicClientApplication, inProgress: InteractionStatus) {
        try {
            // Get graph token with specific scope
            const accessToken = await TokenHelper.getAccessTokenUsingMsal(inProgress, instance, ['User.Read']);

            // Get the Graph client
            const client = this.getGraphClient(accessToken);

            const response = (await client.api('me/photo/$value').responseType(ResponseType.BLOB).get()) as Blob;

            // Convert the Blob to a base64 string
            return URL.createObjectURL(response);
        } catch (error) {
            return;
        }
    }
}
