// Adapted from winforms-datavisualization at 2a1fcc400700401f655e6e7df01fc397afad9901.
// Licensed under the MIT license; see ../LICENSE.TXT and ../THIRD-PARTY-NOTICES.TXT.

using System;
using Sphere10.Framework.Windows.Forms.DataVisualization;

using NUnit.Framework;
using System.Threading;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DataVisualizationCollectionTests
{    
    [TestCase(1, new string[] { })]
    [TestCase(1, new string[] { "test" })]
    [TestCase(1, new string[] { "test", "tes2" })]
    [TestCase(1, new string[] { "test", "10", "20", "3", "4", "5" })]    
    public void Insert_Test(int fakeParam, string [] names)
    {
        DataVisualizationNamedCollection ncl = new DataVisualizationNamedCollection(null);
        foreach (var n in names)
        {
            ncl.Insert(0, new DataPointCustomProperties() { Name = n });

            for (int i = 0; i < ncl.Count; i++)
            {
                Assert.That(ncl.IndexOf(names[i]), Is.EqualTo(ncl.Count - i - 1));
            }
        }
    }

    [TestCase(1, new string[] { })]
    [TestCase(1, new string[] { "test" })]
    [TestCase(1, new string[] { "test", "tes2" })]
    [TestCase(1, new string[] { "test", "10", "20", "3", "4", "5" })]
    public void RemoveAt_Test(int fakeParam, string[] names)
    {
        DataVisualizationNamedCollection ncl = new DataVisualizationNamedCollection(null);

        foreach (var n in names)
            ncl.Insert(0, new DataPointCustomProperties() { Name = n });

        for (int i = 0; i < names.Length; i++)
        {
            ncl.RemoveAt(0);
            for (int j = 0; j < ncl.Count; j++)
                Assert.That(ncl.IndexOf(names[j]), Is.EqualTo(ncl.Count - j - 1));
        }
    }

    [TestCase(1, new string[] { })]
    [TestCase(1, new string[] { "test" })]
    [TestCase(1, new string[] { "test", "tes2" })]
    [TestCase(1, new string[] { "test", "10", "20", "3", "4", "5" })]
    public void Remove_Test(int fakeParam, string[] names)
    {
        DataVisualizationNamedCollection ncl = new DataVisualizationNamedCollection(null);

        foreach (var n in names)
            ncl.Insert(0, new DataPointCustomProperties() { Name = n });

        while(ncl.Count > 0)
        {
            ncl.Remove(ncl[0]);
            for (int j = 0; j < ncl.Count; j++)
                Assert.That(ncl.IndexOf(names[j]), Is.EqualTo(ncl.Count - j - 1));
        }
    }

    [TestCase(1, new string[] { })]
    [TestCase(1, new string[] { "test" })]
    [TestCase(1, new string[] { "test", "tes2" })]
    [TestCase(1, new string[] { "test", "10", "20", "3", "4", "5" })]
    public void Clear_Test(int fakeParam, string[] names)
    {
        DataVisualizationNamedCollection ncl = new DataVisualizationNamedCollection(null);

        foreach (var n in names)
            ncl.Insert(0, new DataPointCustomProperties() { Name = n });

        ncl.Clear();

        foreach (var n in names)
            Assert.That(ncl.IndexOf(n), Is.EqualTo(-1));

    }

    [TestCase(1, new string[] { })]
    [TestCase(1, new string[] { "test" })]
    [TestCase(1, new string[] { "test", "tes2" })]
    [TestCase(1, new string[] { "test", "10", "20", "3", "31", "5" })]
    public void Rename_Test(int fakeParam, string[] names)
    {
        DataVisualizationNamedCollection ncl = new DataVisualizationNamedCollection(null);

        foreach (var n in names)
            ncl.Add(new DataPointCustomProperties() { Name = n });

        foreach (var n in names)
        {
            var oldInd = ncl.IndexOf(n);
            if (ncl.IndexOf(n + 1) != -1)
            {
                Assert.That(() => ncl[n].Name = n + 1, Throws.TypeOf<ArgumentException>());
            }
            else
            {
                ncl[n].Name = n + 1;
                Assert.That(ncl.IndexOf(n + 1), Is.EqualTo(oldInd));
            }
        }
    }
}

