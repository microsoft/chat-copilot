export { };

declare global {
    interface Window { _env_: ENVType; }
}

interface ENVType {
    REACT_APP_BACKEND_URI: string;
}
