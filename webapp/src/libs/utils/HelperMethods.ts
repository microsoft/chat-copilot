import { v4 } from 'uuid';

const getUUID = (): string => {
    return v4();
};

export { getUUID };
