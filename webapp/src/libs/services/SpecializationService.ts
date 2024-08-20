import { ISpecialization, ISpecializationRequest, ISpecializationToggleRequest } from '../models/Specialization';
import { BaseService } from './BaseService';

export class SpecializationService extends BaseService {
    public getAllSpecializationsAsync = async (accessToken: string): Promise<ISpecialization[]> => {
        const result = await this.getResponseAsync<ISpecialization[]>(
            {
                commandPath: 'specializations',
                method: 'GET',
            },
            accessToken,
        );
        return result;
    };

    public getAllSpecializationIndexesAsync = async (accessToken: string): Promise<string[]> => {
        const result = await this.getResponseAsync<string[]>(
            {
                commandPath: 'specialization/indexes',
                method: 'GET',
            },
            accessToken,
        );
        return result;
    };

    public createSpecializationAsync = async (
        key: string,
        name: string,
        description: string,
        roleInformation: string,
        indexName: string,
        imageFilePath: string,
        accessToken: string,
    ): Promise<ISpecialization> => {
        const body: ISpecializationRequest = {
            key,
            name,
            description,
            roleInformation,
            indexName,
            imageFilePath,
        };
        const result = await this.getResponseAsync<ISpecialization>(
            {
                commandPath: 'specializations',
                method: 'POST',
                body,
            },
            accessToken,
        );
        return result;
    };

    public updateSpecializationAsync = async (
        specializationId: string,
        key: string,
        name: string,
        description: string,
        roleInformation: string,
        indexName: string,
        imageFilePath: string,
        accessToken: string,
    ): Promise<ISpecialization> => {
        const body: ISpecializationRequest = {
            key,
            name,
            description,
            roleInformation,
            indexName,
            imageFilePath,
        };
        const result = await this.getResponseAsync<ISpecialization>(
            {
                commandPath: `specializations/${specializationId}`,
                method: 'PATCH',
                body,
            },
            accessToken,
        );
        return result;
    };

    public onOffSpecializationAsync = async (
        specializationId: string,
        isActive: boolean,
        accessToken: string,
    ): Promise<ISpecialization> => {
        const body: ISpecializationToggleRequest = {
            isActive,
        };
        const result = await this.getResponseAsync<ISpecialization>(
            {
                commandPath: `specializations/${specializationId}`,
                method: 'PATCH',
                body,
            },
            accessToken,
        );
        return result;
    };

    public deleteSpecializationAsync = async (specializationId: string, accessToken: string): Promise<object> => {
        const result = await this.getResponseAsync<object>(
            {
                commandPath: `specializations/${specializationId}`,
                method: 'DELETE',
            },
            accessToken,
        );

        return result;
    };
}
