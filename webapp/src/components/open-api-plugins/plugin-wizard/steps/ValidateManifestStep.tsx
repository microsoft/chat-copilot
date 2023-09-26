import {
    Accordion,
    AccordionHeader,
    AccordionItem,
    AccordionPanel,
    Body1,
    Body2,
    Spinner,
    makeStyles,
    shorthands,
    tokens,
} from '@fluentui/react-components';
import { CheckmarkCircle20Regular, DismissCircle20Regular } from '@fluentui/react-icons';
import { useEffect, useState } from 'react';
import { usePlugins } from '../../../../libs/hooks';
import { PluginManifest } from '../../../../libs/models/PluginManifest';
import { isValidOpenAPISpec, isValidPluginManifest } from '../../../utils/PluginUtils';

const useClasses = makeStyles({
    start: {
        justifyContent: 'start',
    },
    status: {
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        ...shorthands.gap(tokens.spacingHorizontalMNudge),
    },
    details: {
        paddingLeft: tokens.spacingHorizontalXXXL,
    },
});

enum FileType {
    Manifest = 'OpenAI manifest',
    OpenApiSpec = 'OpenAPI spec',
}

enum ValidationState {
    InputRequired,
    Loading,
    Success,
    Failed,
}

interface IValidateManifestStepProps {
    manifestDomain: string;
    onPluginValidated: () => void;
    pluginManifest?: PluginManifest;
    onManifestValidated: (manifest: PluginManifest) => void;
}

export const ValidateManifestStep: React.FC<IValidateManifestStepProps> = ({
    manifestDomain,
    onPluginValidated,
    pluginManifest,
    onManifestValidated,
}) => {
    const classes = useClasses();
    const [errorMessage, setErrorMessage] = useState<string | undefined>();

    const [manifestValidationState, setManifestValidationState] = useState(ValidationState.Loading);
    const [openApiSpecValidationState, setOpenApiSpecValidationState] = useState(ValidationState.InputRequired);

    const onManifestValidationFailed = (errorMessage: string) => {
        setManifestValidationState(ValidationState.Failed);
        setErrorMessage(errorMessage);
    };

    const { getPluginManifest } = usePlugins();
    useEffect(() => {
        setErrorMessage(undefined);
        getPluginManifest(manifestDomain)
            .then((pluginManifest) => {
                if (isValidPluginManifest(pluginManifest)) {
                    setManifestValidationState(ValidationState.Success);
                    setOpenApiSpecValidationState(ValidationState.Loading);
                    onManifestValidated(pluginManifest);

                    try {
                        if (isValidOpenAPISpec(pluginManifest.api.url)) {
                            onPluginValidated();
                        }
                        setOpenApiSpecValidationState(ValidationState.Success);
                    } catch (e: any) {
                        setOpenApiSpecValidationState(ValidationState.Failed);
                        setErrorMessage((e as Error).message);
                    }
                }
            })
            .catch((e: unknown) => {
                onManifestValidationFailed((e as Error).message);
            });
    }, [manifestDomain, onManifestValidated, onPluginValidated, getPluginManifest]);

    const statusComponent = (type: FileType, status: ValidationState) => {
        const fileType = type;
        switch (status) {
            case ValidationState.Loading:
                return (
                    <Spinner
                        labelPosition="after"
                        label={`Validating ${fileType} file`}
                        size="tiny"
                        className={classes.start}
                    />
                );
            case ValidationState.Failed:
            case ValidationState.Success: {
                const icon =
                    status === ValidationState.Success ? (
                        <CheckmarkCircle20Regular color="green" />
                    ) : (
                        <DismissCircle20Regular color="red" />
                    );
                const text =
                    status === ValidationState.Success ? `Validated ${fileType}` : `Could not validate ${fileType}.`;

                return (
                    <AccordionItem value={fileType}>
                        <AccordionHeader expandIconPosition="end">
                            <div className={classes.status}>
                                {icon}
                                <Body2> {text}</Body2>
                            </div>
                        </AccordionHeader>
                        <AccordionPanel className={classes.details}>
                            {
                                status === ValidationState.Failed && <Body1 color="red">{errorMessage}</Body1>
                                // TODO: [Issue #1973] Add Manifest details
                            }
                            {status === ValidationState.Success &&
                                (type === FileType.Manifest ? (
                                    <div>
                                        <Body1>Plugin: {pluginManifest?.name_for_human}</Body1>
                                        <br />
                                        <Body1>Contact: {pluginManifest?.contact_email}</Body1>
                                        <br />
                                        <Body1>Auth: {pluginManifest?.auth.type}</Body1>
                                    </div>
                                ) : (
                                    <div>
                                        <Body1>{pluginManifest?.api.url}</Body1>
                                    </div>
                                ))}
                        </AccordionPanel>
                    </AccordionItem>
                );
            }
            default:
                return;
        }
    };

    return (
        <Accordion collapsible multiple defaultOpenItems={[FileType.Manifest, FileType.OpenApiSpec]}>
            {statusComponent(FileType.Manifest, manifestValidationState)}
            {statusComponent(FileType.OpenApiSpec, openApiSpecValidationState)}
        </Accordion>
    );
};
