// Copyright (c) Microsoft. All rights reserved.

import { Spinner } from '@fluentui/react-components';
import { FC } from 'react';
import { useSharedClasses } from '../../styles';

interface ILoadingProps {
    text: string;
}

export const Loading: FC<ILoadingProps> = ({ text }) => {
    const classes = useSharedClasses();
    return (
        <div className={classes.informativeView}>
            <Spinner labelPosition="below" label={text} />
        </div>
    );
};
