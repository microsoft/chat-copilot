import { Body1, Input, InputOnChangeData, Subtitle1, Text, makeStyles, tokens } from '@fluentui/react-components';
import { ErrorCircle16Regular } from '@fluentui/react-icons';
import { useCallback, useRef, useState } from 'react';
import { Constants } from '../../../../Constants';

export const useClasses = makeStyles({
    error: {
        display: 'flex',
        color: tokens.colorPaletteRedBorderActive,
        columnGap: tokens.spacingHorizontalS,
        alignItems: 'center',
    },
});

interface IEnterManifestStepProps {
    setDomainUrl: (domain: string, error?: string) => void;
    manifestDomain?: string;
    manifestDomainError?: string;
}

export const EnterManifestStep: React.FC<IEnterManifestStepProps> = ({
    setDomainUrl,
    manifestDomain,
    manifestDomainError,
}) => {
    const classes = useClasses();
    const [input, setInput] = useState<string>(manifestDomain ?? '');

    const keyStrokeTimeout = useRef(-1);

    const onInputChange = useCallback(
        (ev: React.ChangeEvent<HTMLInputElement>, data: InputOnChangeData) => {
            ev.preventDefault();
            setDomainUrl(data.value);
            setInput(data.value);

            window.clearTimeout(keyStrokeTimeout.current);

            keyStrokeTimeout.current = window.setTimeout(() => {
                try {
                    const validUrl = new URL(data.value);
                    setDomainUrl(validUrl.toString(), undefined);
                } catch (e) {
                    setDomainUrl(data.value, 'Domain is an invalid URL.');
                }
            }, Constants.KEYSTROKE_DEBOUNCE_TIME_MS);
        },
        [setDomainUrl],
    );

    return (
        <>
            <Subtitle1>Enter your website domain</Subtitle1>
            <Text size={400}>
                To connect a plugin, provide the website domain where your{' '}
                <a
                    href={'https://platform.openai.com/docs/plugins/getting-started/plugin-manifest'}
                    target="_blank"
                    rel="noreferrer noopener"
                >
                    OpenAI plugin manifest
                </a>{' '}
                is hosted. This is the{' '}
                <Text size={400} weight="bold">
                    ai-plugin.json
                </Text>{' '}
                file.
            </Text>
            <Input
                required
                type="text"
                id={'plugin-domain-input'}
                value={input}
                onChange={onInputChange}
                placeholder={`yourdomain.com`}
                autoFocus
            />
            {manifestDomainError && (
                <div className={classes.error}>
                    <ErrorCircle16Regular />
                    <Body1>{manifestDomainError}</Body1>
                </div>
            )}
            <Body1 italic>
                Note: Chat Copilot currently only supports plugins requiring{' '}
                <a
                    href={'https://platform.openai.com/docs/plugins/authentication/no-authentication'}
                    target="_blank"
                    rel="noreferrer noopener"
                >
                    no auth
                </a>{' '}
                or{' '}
                <a
                    href={'https://platform.openai.com/docs/plugins/authentication/user-level'}
                    target="_blank"
                    rel="noreferrer noopener"
                >
                    user-level
                </a>{' '}
                authentication.
            </Body1>
        </>
    );
};

export default EnterManifestStep;
