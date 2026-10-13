/** Abrir/consultar el menú no concede autoridad para cambiar la respuesta. */
export type RecordAutocompleteEvents = {
    readOnly: boolean;
    open: boolean;
    pending: boolean;
    selectionDisabled: boolean;
    status?: string;
    onOpen: () => Promise<void>;
    onClose: () => void;
};
