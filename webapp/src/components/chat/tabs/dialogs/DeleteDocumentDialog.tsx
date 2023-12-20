import { Button } from '@fluentui/react-button';
import { Tooltip, makeStyles } from '@fluentui/react-components';
import {
    Dialog,
    DialogActions,
    DialogBody,
    DialogContent,
    DialogSurface,
    DialogTitle,
    DialogTrigger,
} from '@fluentui/react-dialog';
import { Delete16 } from '../../../shared/BundledIcons';
import { DocumentImportService } from '../../../../libs/services/DocumentImportService';
import { AuthHelper } from '../../../../libs/auth/AuthHelper';
import { useMsal } from '@azure/msal-react';
import { AlertType } from '../../../../libs/models/AlertType';
import { useAppDispatch } from '../../../../redux/app/hooks';
import { addAlert } from '../../../../redux/features/app/appSlice';

const useClasses = makeStyles({
    root: {
        width: '450px',
    },
    actions: {
        paddingTop: '10%',
    },
});

interface IDeleteDocumentProps {
    chatId: string;
    documentId: string;
    documentName: string;
}

export const DeleteDocumentDialog: React.FC<IDeleteDocumentProps> = ({ chatId, documentId, documentName }) => {
    const classes = useClasses();
    const dispatch = useAppDispatch();
    const { instance, inProgress } = useMsal();
    const documentImportService = new DocumentImportService();

    const onDeleteDocument = async () => {
        console.log(chatId);
        console.log(documentId);
        await documentImportService.deleteDocumentAsync(
            chatId,
            documentId,
            await AuthHelper.getSKaaSAccessToken(instance, inProgress))
        .catch((e: any) => {
            const errorDetails = (e as Error).message.includes('Failed to delete resources for chat id')
                ? "Some or all resources associated with this chat couldn't be deleted. Please try again."
                : `Details: ${(e as Error).message}`;
            dispatch(
                addAlert({
                    message: `Unable to delete document {${documentName}}. ${errorDetails}`,
                    type: AlertType.Error
                }),
            );
        });
;
    };

    return (
        <Dialog modalType="alert">
            <DialogTrigger>
                <Tooltip content={'Delete chat session'} relationship="label">
                    <Button icon={<Delete16 />} appearance="transparent" aria-label="Edit" />
                </Tooltip>
            </DialogTrigger>
            <DialogSurface className={classes.root}>
                <DialogBody>
                    <DialogTitle>Are you sure you want to delete document: {documentName}?</DialogTitle>
                    <DialogContent>
                        This action will permanently delete the document: {documentName}
                        <p />
                        Note: Derived knowledge of this document in chat-history and synthetic memory may persist.
                    </DialogContent>
                    <DialogActions className={classes.actions}>
                        <DialogTrigger action="close" disableButtonEnhancement>
                            <Button appearance="secondary">Cancel</Button>
                        </DialogTrigger>
                        <DialogTrigger action="close" disableButtonEnhancement>
                            <Button appearance="primary" onClick={() => void (async () => { await onDeleteDocument(); })()}>
                                Delete
                            </Button>
                        </DialogTrigger>
                    </DialogActions>
                </DialogBody>
            </DialogSurface>
        </Dialog>
    );
};
