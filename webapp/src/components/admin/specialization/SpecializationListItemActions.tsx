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
    const [activeMode, setActiveMode] = useState(false);

    const onSwitchSpecialization = (checked: boolean) => {
        setActiveMode(checked);
        void specialization.toggleSpecialization(specializationId, activeMode);
    };

    return (
        <div className={classes.root}>
            <>
                <Switch
                    color="#0000"
                    width={40}
                    height={20}
                    checked={specializationMode}
                    onChange={(_event, data) => {
                        onSwitchSpecialization(data.checked);
                    }}
                    className="react-switch"
                />
            </>
        </div>
    );
};
