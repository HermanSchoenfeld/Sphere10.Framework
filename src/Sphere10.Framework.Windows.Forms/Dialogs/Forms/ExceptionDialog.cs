using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class ExceptionDialog : DialogEx {

	public ExceptionDialog() : this(string.Empty, new Exception()) {

	}

	private ExceptionDialog(string title, Exception error)
		: base(SystemIconType.Error, title, error.ToDisplayString(), false, "&Close", "&Detail") {
		InitializeComponent();
		Exception = error;
	}

	public Exception Exception { get; init; }

	protected override void OnActivated(EventArgs e) {
		base.OnActivated(e);
		TopMost = false;
	}

	protected override async Task OnProcessButtonAsync(DialogExResult button) {
		if (button == DialogExResult.Button2) {
			using var detailForm = new TextEditorForm(Exception.ToDiagnosticString());
			await detailForm.ShowDialogAsync(this);
		} else {
			await base.OnProcessButtonAsync(button);
		}
	}

	/// <summary>Awaitably shows the exception dialog without blocking the calling context.</summary>
	public static async Task ShowAsync(IWin32Window owner, string title, Exception error) {
		// The supplied owner determines the dispatcher; OpenForms can retain disposed handle-free forms.
		var invoker = owner as Control ?? (owner != null ? Control.FromHandle(owner.Handle) : null);
		invoker ??= System.Windows.Forms.Application.OpenForms.Cast<Form>()
			.FirstOrDefault(form => form.IsHandleCreated && !form.IsDisposed && !form.Disposing);
		if (invoker != null) {
			await invoker.InvokeAsyncEx(async _ => {
				using var form = new ExceptionDialog(title, error);
				form.InitializeModalOwner(owner);
				await form.ShowDialogAsync(owner);
			});
		} else {
			using var form = new ExceptionDialog(title, error);
			form.InitializeModalOwner(owner);
			await form.ShowDialogAsync(owner);
		}
	}

	/// <summary>Awaitably shows the exception dialog (titled "Error") without blocking the calling context.</summary>
	public static Task ShowAsync(IWin32Window owner, Exception error) => ShowAsync(owner, "Error", error);

	/// <summary>Shows the exception using an available live window when no owner is supplied.</summary>
	public static Task ShowAsync(Exception Error) => ShowAsync(null, Error);
}
