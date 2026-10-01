import axios from 'axios';
export interface ApiError {
    message?: string;
    errors?: Record<string, string[]>;
    redirect?: string;
}
export function errorResponse(error: unknown) {
    return axios.isAxiosError<ApiError>(error) ? error.response : undefined;
}
export function errorText(error: unknown): string {
    const data = errorResponse(error)?.data;

    return (
        (data?.errors && Object.values(data.errors).flat()[0]) ||
        data?.message ||
        'No se pudo completar la operación. Inténtalo de nuevo.'
    );
}
