import { Button, Card, CardHeader, Image, makeStyles, Subtitle2 } from '@fluentui/react-components';
import { DismissFilled, ImageAddRegular } from '@fluentui/react-icons';
import { useRef } from 'react';
import { FileUploader } from '../FileUploader';

const useStyles = makeStyles({
    card: {
        maxWidth: '400px',
    },
    cardFooter: {
        display: 'flex',
        justifyContent: 'space-between',
    },
    uploadBtn: {
        maxWidth: 'fit-content',
        minWidth: '150px',
    },
});

/**
 * File source path.
 *
 * @type {string} FileURL
 */
type FileURL = string;

interface ImageUploaderPreviewProps {
    /**
     * The file to display.
     *
     * @type {File | FileURL | null | undefined} file
     * @memberof ImageUploaderPreviewProps
     */
    file?: File | FileURL | null;
    /**
     * The label for the upload button.
     *
     * @type {string} buttonLabel
     * @memberof ImageUploaderPreviewProps
     */
    buttonLabel?: string;
    /**
     * Callback function for file updates ie: file upload or removal.
     *
     * @type {(file: File | null, src: string) => void} onFileUpdate
     * @memberof ImageUploaderPreviewProps
     */
    onFileUpdate?: (file: File | null, src: string) => void;
}

/**
 * ImageUploaderPreview component, handles image upload with preview.
 *
 * @param {ImageUploaderPreviewProps} props
 * @returns {*}
 */
export const ImageUploaderPreview = (props: ImageUploaderPreviewProps) => {
    const classes = useStyles();

    const imageUploaderRef = useRef<HTMLInputElement>(null);

    /**
     * Get the file source.
     *
     * @param {File | FileSource | null} file - File or file source path.
     * @returns {FileSource | null} File source path.
     */
    const getFileURL = (file?: File | FileURL | null): FileURL | undefined => {
        if (!file) {
            return;
        }

        // If the file is a instance of file, create a URL for it
        if (file instanceof File) {
            return URL.createObjectURL(file);
        }

        // If the file is already a URL, return it as is
        return file;
    };

    return (
        <>
            {props.file ? (
                <Card className={classes.card}>
                    <CardHeader
                        action={
                            <Button
                                appearance="transparent"
                                icon={<DismissFilled />}
                                onClick={() => {
                                    props.onFileUpdate?.(null, '');
                                    // Reset the ref value to allow re-uploading the same file
                                    if (imageUploaderRef.current) {
                                        imageUploaderRef.current.value = '';
                                    }
                                }}
                            >
                                Remove
                            </Button>
                        }
                        header={<Subtitle2>Image Preview</Subtitle2>}
                    />
                    <Image src={getFileURL(props.file)} shadow block shape={'rounded'} />
                </Card>
            ) : (
                <Button
                    id="image-upload"
                    className={classes.uploadBtn}
                    icon={<ImageAddRegular />}
                    iconPosition="after"
                    onClick={() => imageUploaderRef.current?.click()}
                >
                    {props.buttonLabel ?? 'Upload Image'}
                </Button>
            )}
            <FileUploader
                ref={imageUploaderRef}
                acceptedExtensions={['.png', '.jpg']}
                onSelectedFile={(file: File) => {
                    const src = URL.createObjectURL(file);
                    props.onFileUpdate?.(file, src);
                }}
            />
        </>
    );
};
