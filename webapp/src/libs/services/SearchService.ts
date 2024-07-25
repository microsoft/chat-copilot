import { ServiceInfo } from "../models/ServiceInfo";
import { IAskSearch } from "../semantic-kernel/model/Ask";
import { IAskSearchResult } from "../semantic-kernel/model/AskResult";
import { BaseService } from "./BaseService";

export class SearchService extends BaseService {

    public getSearchResponseAsync = async (
        ask: IAskSearch,
        accessToken: string,
    ): Promise<IAskSearchResult> => {

        const result = await this.getResponseAsync<IAskSearchResult>(
            {
                commandPath: ``,
                method: 'POST',
                body: ask,
            },
            accessToken,
        );

        return result;
    };

    public getServiceInfoAsync = async (accessToken: string): Promise<ServiceInfo> => {
        const result = await this.getResponseAsync<ServiceInfo>(
            {
                commandPath: `info`,
                method: 'GET',
            },
            accessToken,
        );

        return result;
    };
}