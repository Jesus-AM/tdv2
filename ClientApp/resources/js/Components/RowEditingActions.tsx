import type { ReactNode } from 'react';
import type { BlockEditor, EditorState } from '@/lib/block-editor';
import BlockEditingStatus from './BlockEditingStatus';

/** El espacio de presencia permanece reservado incluso cuando la fila está libre. */
export default function RowEditingActions({ engine, state, blockKey, children }: {
    engine: BlockEditor; state: EditorState; blockKey: string; children: ReactNode;
}) {
    return <div className="row-editing-actions">
        <BlockEditingStatus engine={engine} state={state} blockKey={blockKey} compact />
        <div>{children}</div>
    </div>;
}
