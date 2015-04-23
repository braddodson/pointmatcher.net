using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pointmatcher.net;

namespace pointmatcherTests
{
    [TestClass]
    public class QuickSelectTests
    {
        [TestMethod]
        public void QuickSelectTest()
        {
            var r = new Random();

            float[] d = Enumerable.Range(0, 100).Select(i => (float)r.NextDouble()).ToArray();

            var dSorted = new List<float>(d);
            dSorted.Sort();

            int[] indices = Enumerable.Range(0, d.Length).ToArray();
            int n = 30;
            QuickSelect.Select(indices, 0, d.Length - 1, n, i => d[i]);

            var selectedD = indices.Select(i => d[i]).ToArray();

            for (int i = 0; i < n; i++)
            {
                Assert.IsTrue(selectedD[i] < selectedD[n]);
            }

            float limit = d[indices[n]];

            Assert.AreEqual(30, d.Count(x => x < limit));
        }
    }
}
