# Attachments

Select one file or clipboard image through a compact icon and filename. The picker supports Select File, Paste, Ctrl+V and a single dropped file. Images receive a preview. OK accepts the provisional selection; Cancel or closing preserves the previous attachment.

## Contents

| Type | Purpose |
|---|---|
| [AttachmentControl](AttachmentControl.cs) | Public control with filename, selection event, picker and copied attachment bytes. |
| [AttachmentPickerDialog](AttachmentPickerDialog.cs) | Internal modal editor and image preview. |
| [AttachmentSelection](AttachmentSelection.cs) | Internal immutable filename/content snapshot for files and clipboard images. |

```csharp
var attachment = new AttachmentControl();
attachment.SetAttachment("diagnostic.txt", System.Text.Encoding.UTF8.GetBytes("Diagnostic details"));
if (attachment.TryGetAttachment(out var name, out var data)) {
    // Validate size/type for the receiving service, then submit name and data.
}
```

Selection copies the bytes, so later file/clipboard changes do not alter the pending attachment. `TryGetAttachment` returns a separate copy. Receiving applications own their attachment size and type limits.
