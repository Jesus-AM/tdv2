import { TableCell } from '@mui/material';
import type { BlockEditor, EditorState } from '@/lib/block-editor';
import BlockEditingStatus from './BlockEditingStatus';

/** Primera columna estable y compartida por ambas tablas de captura. */
export default function RowEditingPresence({ engine, state, blockKey }: {
    engine: BlockEditor; state: EditorState; blockKey: string;
}) {
    return <TableCell className="row-editing-cell">
        <BlockEditingStatus engine={engine} state={state} blockKey={blockKey} compact />
    </TableCell>;
}
