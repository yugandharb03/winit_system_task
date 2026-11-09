import { Modal } from './Modal';

interface ConfirmDialogProps {
  title?: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * The task brief asks for "Are you sure you want to delete" with Yes / No,
 * so Yes and No are the defaults here.
 */
export function ConfirmDialog({
  title = 'Confirm',
  message,
  confirmLabel = 'Yes',
  cancelLabel = 'No',
  busy = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <Modal
      title={title}
      onClose={onCancel}
      small
      footer={
        <>
          <button type="button" className="btn btn--danger btn--wide" onClick={onConfirm} disabled={busy}>
            {busy ? 'Deleting…' : confirmLabel}
          </button>
          <button type="button" className="btn btn--ghost btn--wide" onClick={onCancel} disabled={busy}>
            {cancelLabel}
          </button>
        </>
      }
    >
      <p className="confirm__text">{message}</p>
    </Modal>
  );
}
