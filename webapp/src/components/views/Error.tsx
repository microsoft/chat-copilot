// Copyright (c) Microsoft. All rights reserved.

import { Subtitle2 } from '@fluentui/react-components';
import { ErrorCircleRegular } from '@fluentui/react-icons';
import { FC } from 'react';
import { useSharedClasses } from '../../styles';

interface IErrorProps {
    text: string;
}

export const Error: FC<IErrorProps> = ({ text }) => {
    const classes = useSharedClasses();
    return (
        <div className={classes.informativeView}>
            <ErrorCircleRegular fontSize={36} color="red" />
            <Subtitle2>{text}</Subtitle2>
        </div>
    );
};
