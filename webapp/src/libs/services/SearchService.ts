import { IAskSearch } from '../semantic-kernel/model/Ask';
import { IAskSearchResult } from '../semantic-kernel/model/AskResult';
import { BaseService } from './BaseService';

export class SearchService extends BaseService {
    public getSearchResponseAsync = async (ask: IAskSearch, accessToken: string): Promise<IAskSearchResult> => {
        const result = await this.getResponseAsync<IAskSearchResult>(
            {
                commandPath: 'search',
                method: 'POST',
                body: ask,
            },
            accessToken,
        );

        return result;
    };
}
