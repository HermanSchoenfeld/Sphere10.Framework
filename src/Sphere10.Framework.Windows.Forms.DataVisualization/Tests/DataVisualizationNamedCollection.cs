// Adapted from winforms-datavisualization at 2a1fcc400700401f655e6e7df01fc397afad9901.
// Licensed under the MIT license; see ../LICENSE.TXT and ../THIRD-PARTY-NOTICES.TXT.

using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms.Tests;

internal class DataVisualizationNamedCollection : ChartNamedElementCollection<DataPointCustomProperties>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataVisualizationNamedCollection"/> class.
    /// </summary>
    /// <param name="parent">The parent chart element.</param>
    internal DataVisualizationNamedCollection(IChartElement? parent) : base(parent) { }
}
