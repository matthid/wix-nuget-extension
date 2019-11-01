using System;
using System.IO;
using Matthid.WiX.NuGetExtensions;
using Microsoft.Tools.WindowsInstallerXml.Serialize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace TestNuGetExtensions
{
    public sealed class TempDir : IDisposable
    {
        public string Dir { get; }

        private TempDir(string dir)
        {
            Dir = dir;
        }

        public void Dispose()
        {
            Directory.Delete(Dir, true);
        }

        public static TempDir Create()
        {
            var dir = Path.GetTempFileName();
            File.Delete(dir);
            Directory.CreateDirectory(dir);
            return new TempDir(dir);
        }

        public static implicit operator string(TempDir dir)
        {
            return dir.Dir;
        }
    }

    [TestClass]
    public class NuGetLogicTests
    {
        [TestMethod]
        public void TestGetVersionPackagesConfig()
        {
            using (var tempDir = TempDir.Create())
            {
                File.WriteAllText(Path.Combine(tempDir, "packages.config"), @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Microsoft.CodeDom.Providers.DotNetCompilerPlatform"" version=""1.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Net.Compilers"" version=""1.0.0"" targetFramework=""net46"" developmentDependency=""true"" />
  <package id=""Microsoft.Web.Infrastructure"" version=""1.0.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Web.Xdt"" version=""2.1.1"" targetFramework=""net46"" />
  <package id=""Newtonsoft.Json"" version=""8.0.3"" allowedVersions=""[8,10)"" targetFramework=""net46"" />
  <package id=""NuGet.Core"" version=""2.11.1"" targetFramework=""net46"" />
  <package id=""NuGet.Server"" version=""2.11.2"" targetFramework=""net46"" />
  <package id=""RouteMagic"" version=""1.3"" targetFramework=""net46"" />
  <package id=""WebActivatorEx"" version=""2.1.0"" targetFramework=""net46"" />
</packages>");
                var version = NuGetLogic.GetPackageVersion(tempDir, "NuGet.Core");
                Assert.AreEqual("2.11.1", version);
            }
        }

        [TestMethod]
        public void TestGetVersionPackagesConfig_Casing()
        {
            using (var tempDir = TempDir.Create())
            {
                File.WriteAllText(Path.Combine(tempDir, "packages.config"), @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Microsoft.CodeDom.Providers.DotNetCompilerPlatform"" version=""1.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Net.Compilers"" version=""1.0.0"" targetFramework=""net46"" developmentDependency=""true"" />
  <package id=""Microsoft.Web.Infrastructure"" version=""1.0.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Web.Xdt"" version=""2.1.1"" targetFramework=""net46"" />
  <package id=""Newtonsoft.Json"" version=""8.0.3"" allowedVersions=""[8,10)"" targetFramework=""net46"" />
  <package id=""NuGet.Core"" version=""2.11.1"" targetFramework=""net46"" />
  <package id=""NuGet.Server"" version=""2.11.2"" targetFramework=""net46"" />
  <package id=""RouteMagic"" version=""1.3"" targetFramework=""net46"" />
  <package id=""WebActivatorEx"" version=""2.1.0"" targetFramework=""net46"" />
</packages>");
                var version = NuGetLogic.GetPackageVersion(tempDir, "nuGet.cOre");
                Assert.AreEqual("2.11.1", version);
            }
        }


        [TestMethod]
        public void TestGetPathPackagesConfig()
        {
            using (var tempDir = TempDir.Create())
            {
                File.WriteAllText(Path.Combine(tempDir, "packages.config"), @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Microsoft.CodeDom.Providers.DotNetCompilerPlatform"" version=""1.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Net.Compilers"" version=""1.0.0"" targetFramework=""net46"" developmentDependency=""true"" />
  <package id=""Microsoft.Web.Infrastructure"" version=""1.0.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Web.Xdt"" version=""2.1.1"" targetFramework=""net46"" />
  <package id=""Newtonsoft.Json"" version=""8.0.3"" allowedVersions=""[8,10)"" targetFramework=""net46"" />
  <package id=""NuGet.Core"" version=""2.11.1"" targetFramework=""net46"" />
  <package id=""NuGet.Server"" version=""2.11.2"" targetFramework=""net46"" />
  <package id=""RouteMagic"" version=""1.3"" targetFramework=""net46"" />
  <package id=""WebActivatorEx"" version=""2.1.0"" targetFramework=""net46"" />
</packages>");
                var d = Directory.CreateDirectory(Path.Combine(tempDir, "packages", "Nuget.Core.2.11.1"));
                var path = NuGetLogic.GetPackagePath(tempDir, "packages", "nuGet.cOre");
                Assert.AreEqual(d.FullName.ToLowerInvariant(), path.ToLowerInvariant());
            }
        }

        [TestMethod]
        public void TestGetPathPackagesConfig_UnknownPackage()
        {
            using (var tempDir = TempDir.Create())
            {
                File.WriteAllText(Path.Combine(tempDir, "packages.config"), @"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Microsoft.CodeDom.Providers.DotNetCompilerPlatform"" version=""1.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Net.Compilers"" version=""1.0.0"" targetFramework=""net46"" developmentDependency=""true"" />
  <package id=""Microsoft.Web.Infrastructure"" version=""1.0.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Web.Xdt"" version=""2.1.1"" targetFramework=""net46"" />
  <package id=""Newtonsoft.Json"" version=""8.0.3"" allowedVersions=""[8,10)"" targetFramework=""net46"" />
  <package id=""NuGet.Core"" version=""2.11.1"" targetFramework=""net46"" />
  <package id=""NuGet.Server"" version=""2.11.2"" targetFramework=""net46"" />
  <package id=""RouteMagic"" version=""1.3"" targetFramework=""net46"" />
  <package id=""SomeRandomNotInstalledPackage"" version=""2.1.0"" targetFramework=""net46"" />
</packages>");
                var e = Assert.ThrowsException<InvalidOperationException>(() => NuGetLogic.GetPackagePath(tempDir, "packages", "SomeRandomNotInstalledPackage"));
                Assert.IsTrue(e.Message.Contains("are all packages restored?"), "e.Message.Contains('are all packages restored?')");
            }
        }

        [TestMethod]
        public void TestGetPathPackagesConfig_CacheFallback()
        {
            using (var tempDir = TempDir.Create())
            {
                File.WriteAllText(Path.Combine(tempDir, "packages.config"), $@"<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""Microsoft.CodeDom.Providers.DotNetCompilerPlatform"" version=""1.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Net.Compilers"" version=""1.0.0"" targetFramework=""net46"" developmentDependency=""true"" />
  <package id=""Microsoft.Web.Infrastructure"" version=""1.0.0.0"" targetFramework=""net46"" />
  <package id=""Microsoft.Web.Xdt"" version=""2.1.1"" targetFramework=""net46"" />
  <package id=""Newtonsoft.Json"" version=""8.0.3"" allowedVersions=""[8,10)"" targetFramework=""net46"" />
  <package id=""NuGet.Core"" version=""2.11.1"" targetFramework=""net46"" />
  <package id=""NuGet.Server"" version=""2.11.2"" targetFramework=""net46"" />
  <package id=""RouteMagic"" version=""1.3"" targetFramework=""net46"" />
  <package id=""MSTest.TestAdapter"" version=""{PreprocessorTests.mstestVersion}"" targetFramework=""net46"" />
</packages>");
                var result = NuGetLogic.GetPackagePath(tempDir, "packages", "MSTest.TestAdapter");
                Assert.AreEqual(Path.Combine(NuGetLogic.GetNuGetCacheDir(), "MSTest.TestAdapter", PreprocessorTests.mstestVersion).ToLowerInvariant(), result.ToLowerInvariant());
            }
        }
    }
}
