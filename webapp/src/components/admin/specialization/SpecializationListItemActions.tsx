import { makeStyles, Switch } from '@fluentui/react-components';
import React, { useState } from 'react';
import { Breakpoints } from '../../../styles';
import { useSpecialization } from '../../../libs/hooks';

const useClasses = makeStyles({
    root: {
        display: 'contents',
        ...Breakpoints.small({
            display: 'none',
        }),
    },
});

interface ISpecializationListItemActionsProps {
    specializationId: string;
    specializationMode: boolean;
}

export const SpecializationListItemActions: React.FC<ISpecializationListItemActionsProps> = ({
    specializationId,
    specializationMode,
}) => {
    const specialization = useSpecialization();
    const classes = useClasses();

    // This piece of state is technically NOT necessary but a nice to have for immediate feedback.
    const [on, turnOn] = useState(specializationMode);

    return (
        <div className={classes.root}>
            <Switch
                color="#0000"
                width={40}
                height={20}
                checked={on}
                onChange={(_event, { checked }) => {
                    turnOn(checked);
                    void specialization.toggleSpecialization(specializationId, checked);
                }}
                className="react-switch"
            />
        </div>
    );
};
