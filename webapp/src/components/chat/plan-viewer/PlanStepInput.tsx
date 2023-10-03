import { Badge, Button, makeStyles, mergeClasses, shorthands, tokens } from '@fluentui/react-components';
import { Checkmark16Regular, Dismiss16Regular, Edit16Regular } from '@fluentui/react-icons';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Constants } from '../../../Constants';
import { IPlanInput } from '../../../libs/models/Plan';

const useClasses = makeStyles({
    root: {
        ...shorthands.overflow('hidden'),
        display: 'flex',
    },
    buttons: {
        ...shorthands.padding(tokens.spacingVerticalNone),
        paddingLeft: tokens.spacingHorizontalXXS,
        minWidth: '18px',
    },
    input: {
        ...shorthands.margin(tokens.spacingHorizontalXXS),
        maxHeight: '10px',
        minHeight: '10px',
        fontSize: '12px',
    },
    interactable: {
        zIndex: '50',
    },
});

// Regex to match interpolated variables in the form of $VARIABLE_NAME or $VARIABLE2_NAME.
// Variables that are not interpolated will fail to match.
// \$[A-Za-z]+[_-]*[\w]+ matches the variable name
// (?=([^-_\d\w])+) is a positive lookahead matching the end of static string (matches any character that is not a letter, number, underscore, or dash)
// (?:.+\s*) is a noncapturing group that matches the start of static string (matches any character followed by whitespace)
// Matches: "Interpolated $variable_name", "$variable_name Interpolated", "Interpolated $variable_name Interpolated"
// Doesn't match: standalone variables (e.g. "$variable_name") or dollar amounts (e.g. "$1.00", "$100")
const INTERPOLATED_VARIABLE_REGEX = /((\$[A-Za-z]+[_-]*[\w]+)(?=([^-_\d\w])+))|((?:.+\s*)(\$[A-Za-z]+[_-]*[\w]+))/g;

interface PlanStepInputProps {
    input: IPlanInput;
    onEdit: (newValue: string) => void;
    enableEdits: boolean;
    validationErrors: number;
    setValidationErrors: React.Dispatch<React.SetStateAction<number>>;
}

export const PlanStepInput: React.FC<PlanStepInputProps> = ({
    input,
    onEdit,
    enableEdits,
    validationErrors,
    setValidationErrors,
}) => {
    const classes = useClasses();

    // Prompt user to edit input if it contains an unknown variable or interpolated variable string
    const requiresEdits = useCallback(
        (input = '') => {
            input = input.trim();
            return (
                enableEdits &&
                (input.includes(Constants.sk.UNKNOWN_VARIABLE_FLAG) ||
                    input.match(INTERPOLATED_VARIABLE_REGEX) !== null)
            );
        },
        [enableEdits],
    );

    const [formValue, setFormValue] = useState(input.Value);
    const [isEditingInput, setIsEditingInput] = useState(requiresEdits(input.Value));
    const [editsRequired, setEditsRequired] = useState(enableEdits && requiresEdits(input.Value));

    useEffect(() => {
        if (editsRequired) setValidationErrors(validationErrors + 1);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [input.Value]);

    const onEditClick = useCallback(() => {
        setIsEditingInput(true);
    }, []);

    const keyStrokeTimeout = useRef(-1);
    const updateAndValidateInput = useCallback(
        (event: React.ChangeEvent<HTMLInputElement>) => {
            window.clearTimeout(keyStrokeTimeout.current);
            setFormValue(event.target.value);

            // Debounce validation to avoid unnecessary re-renders
            keyStrokeTimeout.current = window.setTimeout(() => {
                if (requiresEdits(event.target.value) || event.target.value === '') {
                    setEditsRequired(true);
                } else {
                    setEditsRequired(false);
                }
            }, Constants.KEYSTROKE_DEBOUNCE_TIME_MS);
        },
        [requiresEdits],
    );

    const onSubmitEdit = useCallback(() => {
        // Input was corrected, remove validation error from parent component
        if (input.Value.includes(Constants.sk.UNKNOWN_VARIABLE_FLAG)) {
            setValidationErrors(validationErrors - 1);
        }

        setEditsRequired(false);
        setIsEditingInput(false);
        input.Value = formValue;
        onEdit(formValue);
    }, [formValue, validationErrors, input, onEdit, setValidationErrors]);

    const onCancel = useCallback(() => {
        setEditsRequired(requiresEdits(input.Value));
        setIsEditingInput(requiresEdits(input.Value));
        setFormValue(input.Value);
    }, [requiresEdits, input.Value]);

    return (
        <Badge
            color={enableEdits && editsRequired ? 'danger' : 'informative'}
            shape="rounded"
            appearance="tint"
            className={classes.root}
        >
            {`${input.Key}: `}
            {!enableEdits && input.Value}
            {enableEdits && (
                <>
                    {isEditingInput ? (
                        <input
                            className={mergeClasses(classes.input, classes.interactable)}
                            style={{ width: input.Value.length * 6, minWidth: '75px' }}
                            placeholder={input.Value}
                            value={formValue}
                            onChange={updateAndValidateInput}
                            onKeyDown={(event) => {
                                if (event.key === 'Enter' && !event.shiftKey) {
                                    event.preventDefault();
                                    onSubmitEdit();
                                }
                            }}
                            autoFocus
                        />
                    ) : (
                        formValue
                    )}
                    <Button
                        icon={isEditingInput ? <Checkmark16Regular /> : <Edit16Regular />}
                        appearance="transparent"
                        className={mergeClasses(classes.buttons, classes.interactable)}
                        onClick={isEditingInput ? onSubmitEdit : onEditClick}
                        disabled={isEditingInput && editsRequired}
                    />
                    {isEditingInput && (
                        <Button
                            icon={<Dismiss16Regular />}
                            appearance="transparent"
                            className={mergeClasses(classes.buttons, classes.interactable)}
                            onClick={onCancel}
                        />
                    )}
                </>
            )}
        </Badge>
    );
};
