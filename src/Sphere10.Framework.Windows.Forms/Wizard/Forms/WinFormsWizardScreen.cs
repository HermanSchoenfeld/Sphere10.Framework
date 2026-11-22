// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsWizardScreen<T> : UserControlEx, IWinFormsWizardScreen<T> {
	private SizeF _scaling = new(1F, 1F);
	private bool _isScaling;

	public IWinFormsWizard<T> Wizard { get; internal set; }

	/// <summary>
	/// The step's size in its designer's 96-DPI units, whether or not it has scaled itself for the display. A step whose designer declares
	/// AutoScaleDimensions scales its own size when it is built, as Visual Studio's designer expects; the wizard sizes its dialog from this
	/// size, scaled once to the dialog's DPI, so such a step is not scaled twice (21 November 2026: a 680-unit rule step made a 1,790-pixel
	/// dialog at 150 %).
	/// </summary>
	public Size DesignSize => new((int)Math.Round(Width / _scaling.Width), (int)Math.Round(Height / _scaling.Height));

	public T Model => Wizard.Model;

	/// <summary>
	/// <paramref name="designSize"/> in device pixels for this step on <paramref name="dialog"/>: scaled as the step scaled its own controls, or
	/// by the dialog's DPI for a step that does not scale itself. Font autoscaling scales across and down by different factors (10/7 and
	/// 25/15 for Segoe UI at 150 %), so sizing such a step by the DPI alone clipped its last line (21 November 2026).
	/// </summary>
	public Size ScaleToDevice(Size designSize, Control dialog)
		=> _scaling == new SizeF(1F, 1F)
			? dialog.LogicalToDeviceUnits(designSize)
			: new Size((int)Math.Round(designSize.Width * _scaling.Width), (int)Math.Round(designSize.Height * _scaling.Height));

	public virtual WizardStepTraits Traits { get; set; }

	public virtual async Task Initialize() {
	}

	public virtual async Task OnPresent() => CopyModelToUI();

	public virtual async Task OnPrevious() {
	}

	public virtual async Task OnNext() {
	}

	public new virtual async Task<Result> Validate() => Result.Default;

	/// <summary>
	/// The height in device pixels this step's content needs at <paramref name="width"/>, wrapped text included, for a wizard sized with
	/// <see cref="WizardStepSizing.EachStep"/>; null keeps the step's designer height. Called once the step is hosted and presented.
	/// </summary>
	public virtual int? MeasureHeight(int width) => null;

	// Every scaling of the step passes through here: its own autoscaling when built and any later DPI change. Only the dimensions
	// WinForms actually scales count.
	protected override void ScaleControl(SizeF factor, BoundsSpecified specified) {
		_isScaling = true;
		try {
			base.ScaleControl(factor, specified);
		} finally {
			_isScaling = false;
		}
		_scaling = new SizeF(
			specified.HasFlag(BoundsSpecified.Width) ? _scaling.Width * factor.Width : _scaling.Width,
			specified.HasFlag(BoundsSpecified.Height) ? _scaling.Height * factor.Height : _scaling.Height
		);
	}

	// A size set while the step is built is in designer units again: a step deriving from a bridge class runs two designers, each
	// setting the size and scaling it, and counting both scalings halved the design size at 150 % (21 November 2026).
	protected override void OnSizeChanged(EventArgs e) {
		if (!_isScaling && Parent == null)
			_scaling = new SizeF(1F, 1F);
		base.OnSizeChanged(e);
	}

}

