using System;
using System.IO;
using Matthid.WiX.NuGetExtensions;
using Microsoft.Tools.WindowsInstallerXml.Serialize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Directory = System.IO.Directory;

namespace TestNuGetExtensions
{
    [TestClass]
    public class PreprocessorTests
    {
        internal const string mstestVersion = "1.4.0";

        [TestMethod]
        public void TestMstestAdapter()
        {
            var preprocessor = new NuGetPreprocessorExtension();
            var result = preprocessor.EvaluateFunction("nuget", "GetVersion", new[] {"MSTest.TestAdapter"});
            Assert.AreEqual(mstestVersion, result);
        }

        [TestMethod]
        public void TestMstestAdapter_Casing()
        {
            var preprocessor = new NuGetPreprocessorExtension();
            var result = preprocessor.EvaluateFunction("nuget", "GetVersion", new[] { "Mstest.TestAdapter" });
            Assert.AreEqual(mstestVersion, result);
        }

        [TestMethod]
        public void TestMstestAdapter_Invalid()
        {
            var preprocessor = new NuGetPreprocessorExtension();
            var e = Assert.ThrowsException<AggregateException>(() => preprocessor.EvaluateFunction("nuget", "GetVersion", new[] { "Unknown" }));
            Assert.IsTrue(e.Message.Contains("Could not retrieve via project file"), "e.Message.Contains('Could not retrieve via project file')");
        }

        [TestMethod]
        public void TestMstestAdapter_Path()
        {
            var preprocessor = new NuGetPreprocessorExtension();
            var result = preprocessor.EvaluateFunction("nuget", "GetPath", new[] { "Mstest.TestAdapter" });
            Assert.AreEqual(Path.Combine(NuGetLogic.GetNuGetCacheDir(), "MSTest.TestAdapter", mstestVersion).ToLowerInvariant(), result.ToLowerInvariant());
        }


        [AssemblyInitialize]
        public static void InitializeTests(TestContext init)
        {
            var dir = Path.GetDirectoryName(typeof(PreprocessorTests).Assembly.Location);
            var projDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(dir)));
            Directory.SetCurrentDirectory(projDir);
        }
    }
}
