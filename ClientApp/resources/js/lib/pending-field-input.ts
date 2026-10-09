import type { BlockEditor } from './block-editor';
import { blockValue } from './form-blocks';

type TextInput = HTMLInputElement | HTMLTextAreaElement;
type Intent = { input: TextInput; base: string; text: string; start: number; end: number; done: Promise<void> };

/** Conserva las primeras teclas mientras llega la reserva; nunca las aplica sobre una respuesta distinta. */
export class PendingFieldInput {
    private intents = new Map<string, Intent>();
    constructor(private editor: BlockEditor) {}
    private target(target: EventTarget | null) {
        return (target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement)
            && target.dataset.field && !['hidden', 'checkbox', 'radio'].includes(target.type) ? target : null;
    }
    key(key: string, event: { target: EventTarget | null; key: string; ctrlKey: boolean; metaKey: boolean; altKey: boolean; preventDefault(): void }) {
        if (this.editor.canEdit(key)) return;
        const input = this.target(event.target);
        if (!input || event.ctrlKey || event.metaKey || event.altKey || !(event.key.length === 1 || ['Backspace', 'Delete', 'Enter'].includes(event.key))) return;
        if (event.key === 'Enter' && !(input instanceof HTMLTextAreaElement)) return;
        this.capture(key, input, event.key, event);
    }
    paste(key: string, event: { target: EventTarget | null; clipboardData: DataTransfer; preventDefault(): void }) {
        if (this.editor.canEdit(key)) return;
        const input = this.target(event.target);
        if (input) this.capture(key, input, event.clipboardData.getData('text/plain'), event, true);
    }
    private capture(key: string, input: TextInput, text: string, event: { preventDefault(): void }, paste = false) {
        if (this.editor.busy(key) || this.editor.state.locked || !this.editor.state.initialized) return;
        event.preventDefault();
        const field = input.dataset.field!, identity = key + '/' + field;
        let intent = this.intents.get(identity);
        const first = !intent;
        intent ||= { input, base: input.value, text: input.value, start: input.selectionStart ?? input.value.length,
            end: input.selectionEnd ?? input.value.length, done: Promise.resolve() };
        let { start, end } = intent;
        if (!paste && text === 'Backspace') { if (start === end) start = Math.max(0, start - 1); text = ''; }
        else if (!paste && text === 'Delete') { if (start === end) end++; text = ''; }
        else if (!paste && text === 'Enter') text = '\n';
        intent.text = intent.text.slice(0, start) + text + intent.text.slice(end);
        intent.start = intent.end = start + text.length;
        this.intents.set(identity, intent);
        if (!first) return;
        const pending = intent;
        pending.done = (async () => {
            const ready = await this.editor.focus(key);
            const current = blockValue(this.editor.state.content, key) as Record<string, unknown> | undefined;
            // Tab hacia otro campo de la misma fila cambia la intención de foco, no su autoridad confirmada.
            if ((ready || this.editor.canEdit(key)) && String(current?.[field] ?? '') === pending.base) {
                this.editor.changeBlock<Record<string, unknown>>(key, value => { value[field] = pending.text; });
                requestAnimationFrame(() => {
                    if (document.activeElement === input && input.isConnected && input.selectionStart !== null)
                        input.setSelectionRange(pending.start, pending.end);
                });
            } else this.editor.keepUnacceptedInput(key, field, pending.text);
            this.intents.delete(identity);
        })();
    }
    async finish(key: string) {
        await Promise.all([...this.intents.entries()].filter(([id]) => id.startsWith(key + '/')).map(([, intent]) => intent.done));
    }
}
