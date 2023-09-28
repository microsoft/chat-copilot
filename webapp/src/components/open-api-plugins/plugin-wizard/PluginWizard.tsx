import {
    Body2,
    Button,
    Dialog,
    DialogActions,
    DialogBody,
    DialogContent,
    DialogSurface,
    DialogTitle,
    DialogTrigger,
    Persona,
    Text,
    makeStyles,
    tokens,
} from '@fluentui/react-components';
import { CheckmarkCircle48Regular, Dismiss24Regular } from '@fluentui/react-icons';
import React, { ReactElement, useCallback, useState } from 'react';
import AddPluginIcon from '../../../assets/plugin-icons/add-plugin.png';
import { usePlugins } from '../../../libs/hooks';
import { PluginManifest } from '../../../libs/models/PluginManifest';
import { EnterManifestStep } from './steps/EnterManifestStep';
import { ValidateManifestStep } from './steps/ValidateManifestStep';

export const useClasses = makeStyles({
    root: {
        height: '400px',
    },
    surface: {
        width: '500px',
    },
    center: {
        paddingTop: '75px',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        rowGap: tokens.spacingVerticalL,
        'place-self': 'center',
        width: '90%',
    },
    content: {
        display: 'flex',
        flexDirection: 'column',
        rowGap: '10px',
    },
});

interface IWizardStep {
    id: CreatePluginSteps;
    header?: ReactElement;
    body: ReactElement;
    buttons?: ReactElement;
}

enum CreatePluginSteps {
    EnterManifest,
    ValidateManifest,
    Confirmation,
}

export const PluginWizard: React.FC = () => {
    const classes = useClasses();
    const plugins = usePlugins();

    const [activeStep, setActiveStep] = useState(CreatePluginSteps.EnterManifest);
    const [manifestDomain, setManifestDomain] = useState<string | undefined>();
    const [manifestDomainError, setManifestDomainError] = useState<string | undefined>();
    const [pluginValidated, setPluginValidated] = useState(false);
    const [pluginManifest, setPluginManifest] = useState<PluginManifest | undefined>();

    // Resets wizard to first step and blank slate
    const resetLocalState = useCallback(() => {
        setManifestDomain(undefined);
        setManifestDomainError(undefined);
        setActiveStep(CreatePluginSteps.EnterManifest);
        setPluginValidated(false);
    }, []);

    const onAddPlugin = useCallback(() => {
        if (pluginManifest && manifestDomain) {
            plugins.addCustomPlugin(pluginManifest, manifestDomain);
            setActiveStep(CreatePluginSteps.Confirmation);
        } else {
            setPluginValidated(false);
            // TODO: [Issue #1973] add error handling
        }
    }, [pluginManifest, manifestDomain, plugins]);

    const onPluginValidated = useCallback(() => {
        setPluginValidated(true);
    }, []);

    const onManifestValidated = useCallback((manifest: PluginManifest) => {
        setPluginManifest(manifest);
    }, []);

    const setDomainUrl = useCallback((domain: string, error?: string) => {
        setManifestDomain(domain);
        setManifestDomainError(error);
    }, []);

    const wizardSteps: IWizardStep[] = [
        {
            id: CreatePluginSteps.EnterManifest,
            header: (
                <Persona
                    size="huge"
                    name="Custom plugin"
                    avatar={{
                        image: {
                            src: AddPluginIcon,
                        },
                        initials: '', // Set to empty string so no initials are rendered behind image
                    }}
                    secondaryText="Connect an OpenAI Plugin to expose Chat Copilot to third-party applications."
                />
            ),
            body: (
                <EnterManifestStep
                    manifestDomain={manifestDomain}
                    manifestDomainError={manifestDomainError}
                    setDomainUrl={setDomainUrl}
                />
            ),
            buttons: (
                <>
                    <DialogTrigger>
                        <Button appearance="secondary">Cancel</Button>
                    </DialogTrigger>
                    <Button
                        data-testid="find-manifest-button"
                        appearance="primary"
                        disabled={!manifestDomain || manifestDomainError !== undefined}
                        onClick={() => {
                            setActiveStep(CreatePluginSteps.ValidateManifest);
                        }}
                    >
                        Find manifest file
                    </Button>
                </>
            ),
        },
        {
            id: CreatePluginSteps.ValidateManifest,
            header: <>Verify Plugin</>,
            body: (
                <ValidateManifestStep
                    manifestDomain={manifestDomain ?? ''}
                    onPluginValidated={onPluginValidated}
                    pluginManifest={pluginManifest}
                    onManifestValidated={onManifestValidated}
                />
            ),
            buttons: (
                <>
                    <Button
                        data-testid="back-button"
                        appearance="secondary"
                        onClick={() => {
                            setActiveStep(CreatePluginSteps.EnterManifest);
                        }}
                    >
                        Back
                    </Button>
                    <Button
                        data-testid="add-plugin-button"
                        appearance="primary"
                        disabled={!pluginValidated}
                        onClick={onAddPlugin}
                    >
                        Add Plugin
                    </Button>
                </>
            ),
        },
        {
            id: CreatePluginSteps.Confirmation,
            body: (
                <div className={classes.center}>
                    <CheckmarkCircle48Regular color="green" />
                    <Text size={600} align="center">
                        Your plugin has been added successfully!
                    </Text>
                    <Body2 align="center">
                        You have to enable it from the plugin gallery before it can be used in your chats.
                    </Body2>
                    <DialogTrigger disableButtonEnhancement>
                        <Button data-testid="close-plugin-wizard" aria-label="Close Wizard" appearance="secondary">
                            Close
                        </Button>
                    </DialogTrigger>
                </div>
            ),
        },
    ];

    const currentStep = wizardSteps[activeStep];

    return (
        <Dialog
            onOpenChange={() => {
                resetLocalState();
            }}
            modalType="alert"
        >
            <DialogTrigger>
                <Button data-testid="add-custom-plugin" aria-label="Add Custom Plugin" appearance="primary">
                    Add
                </Button>
            </DialogTrigger>
            <DialogSurface className={classes.surface}>
                <DialogBody className={classes.root}>
                    <DialogTitle
                        action={
                            currentStep.id < CreatePluginSteps.Confirmation ? (
                                <DialogTrigger action="close" disableButtonEnhancement>
                                    <Button
                                        data-testid="closeEnableCCPluginsPopUp"
                                        appearance="subtle"
                                        aria-label="close"
                                        icon={<Dismiss24Regular />}
                                    />
                                </DialogTrigger>
                            ) : undefined
                        }
                    >
                        {currentStep.header}
                    </DialogTitle>
                    <DialogContent className={classes.content}>{currentStep.body}</DialogContent>
                    <DialogActions>{currentStep.buttons}</DialogActions>
                </DialogBody>
            </DialogSurface>
        </Dialog>
    );
};
