import { Button } from '@fluentui/react-button';
import { Tooltip } from '@fluentui/react-components';
import { Dialog, DialogBody, DialogContent, DialogSurface, DialogTitle, DialogTrigger } from '@fluentui/react-dialog';
import { Add20, Dismiss24 } from '../../../shared/BundledIcons';
import { SpecializationCardList } from '../../../../components/specialization/SpecializationCardList';
import { RootState } from '../../../../redux/app/store';
import { useAppSelector } from '../../../../redux/app/hooks';
import { useEffect, useState } from 'react';

export const SpecializationDialog: React.FC = ({}) => {
    const { specializations } = useAppSelector((state: RootState) => state.admin);
    const { conversations } = useAppSelector((state: RootState) => state.conversations);

    const [showSpecialization, setShowSpecialization] = useState(false);

    useEffect(() => {
        if (Object.keys(conversations).length < 1) {
            setShowSpecialization(true);
        }
    }, [conversations]);
    return (
        <Dialog open={showSpecialization}>
            <DialogTrigger disableButtonEnhancement>
                <Tooltip content={'Add a chat'} relationship="label">
                    <Button
                        icon={<Add20 />}
                        appearance="transparent"
                        aria-label="Add"
                        onClick={() => {
                            setShowSpecialization(true);
                        }}
                    />
                </Tooltip>
            </DialogTrigger>
            <DialogSurface>
                <DialogBody>
                    <DialogTitle
                        action={
                            <DialogTrigger action="close">
                                <Button
                                    data-testid="closeEnableCCPluginsPopUp"
                                    appearance="subtle"
                                    aria-label="close"
                                    icon={<Dismiss24 />}
                                    onClick={() => {
                                        setShowSpecialization(false);
                                    }}
                                />
                            </DialogTrigger>
                        }
                    >
                        Select Specialization
                    </DialogTitle>
                    <DialogTrigger action="close" disableButtonEnhancement>
                        <DialogContent>
                            <SpecializationCardList specializations={specializations} />
                        </DialogContent>
                    </DialogTrigger>
                </DialogBody>
            </DialogSurface>
        </Dialog>
    );
};
