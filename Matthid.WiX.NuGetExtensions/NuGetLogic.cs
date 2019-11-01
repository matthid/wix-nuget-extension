using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Matthid.WiX.NuGetExtensions
{
    public class NuGetLogic
    {
        private static string GetNuGetCacheDir()
        {
            var cacheDir = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (!string.IsNullOrEmpty(cacheDir))
            {
                return cacheDir;
            }

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(userProfile))
            {
                userProfile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            if (string.IsNullOrEmpty(userProfile))
            {
                userProfile = Path.GetFullPath(".paket");
                Console.WriteLine($"Could not detect a root for our (user specific) temporary files. Try to set the 'HOME' or 'LocalAppData' or 'NUGET_PACKAGES' environment variable!. Using '{userProfile}' instead.");
            }

            if (!Directory.Exists(userProfile))
            {
                Directory.CreateDirectory(userProfile);
            }

            return Path.Combine(userProfile, ".nuget", "packages");
        }

        public static string GetPackageVersion(string packageName)
        {
            return RetrievePackageVersion(packageName);
        }

        public static string GetPackagePath(string packagesPath, string packageName)
        {
            var version = RetrievePackageVersion(packageName);

            // Look in the nuget cache first
            var cacheDir = GetNuGetCacheDir();
            if (Directory.Exists(cacheDir))
            {
                var packDir = Path.Combine(cacheDir, packageName, version);
                if (Directory.Exists(packDir))
                {
                    return packDir;
                }
            }

            if (Directory.Exists(packagesPath))
            {
                var available = Directory.EnumerateDirectories(packagesPath, $"{packageName}.*").ToList();
                var first = available.FirstOrDefault();
                if (first != null && available.Count == 1)
                {
                    return first;
                }
                else
                {
                    if (available.Count <= 1)
                    {
                        throw new InvalidOperationException(
                            "Package path could not be found (are all packages restored?).");
                    }
                    
                    var correctPath = available.FirstOrDefault(folder => folder.Contains(version));
                    if (correctPath == null)
                    {
                        throw new InvalidOperationException(
                                $"Package path with correct version '{version}' could not be found (are all packages restored?).");
                    }

                    return correctPath;
                }
            }

            throw new InvalidOperationException(
                    $"Package path with correct version '{version}' could not be found (are all packages restored?).");
        }

        private static string RetrievePackageVersion(string packageName)
        {
            var errs = new List<Exception>();
            var misses = new List<string>();

            bool ShouldReturn(RetrievePackageVersionResult result, string version, string file, string errorPrefix, Exception error, out string finalResult)
            {
                finalResult = null;
                switch (result)
                {
                    case RetrievePackageVersionResult.VersionFound:
                        finalResult = version;

                        if (errs.Count > 0 || misses.Count > 0)
                        {
                            var err = new AggregateException(
                                "The following warnings occured: \n -" +
                                string.Join("\n - ", misses), errs);
                            Console.WriteLine(err.ToString());
                        }

                        return true;
                    case RetrievePackageVersionResult.NoPackagesConfig:
                    case RetrievePackageVersionResult.MissingProjectFile:
                        misses.Add(errorPrefix + $"File '{file}' was not found");
                        break;
                    case RetrievePackageVersionResult.PackageNotFoundInFile:
                        misses.Add(errorPrefix + $"package '{packageName}' was not found in the file '{file}'");
                        break;
                    case RetrievePackageVersionResult.VersionAttributeMissing:
                        misses.Add(errorPrefix + "Version attribute could not be found");
                        break;
                    case RetrievePackageVersionResult.ErrorOccured:
                        errs.Add(error);
                        misses.Add(errorPrefix + error.Message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return false;
            }
            if (File.Exists("packages.config"))
            {
                var res = TryRetrieveVersionFromPackagesConfig(packageName, out var version, out var error);
                var msg = "Count not retrieve from packages.config: ";
                if (ShouldReturn(res, version, "packages.config", msg, error, out var result))
                {
                    return result;
                }
            }

            var projectFiles = Directory.EnumerateFiles("*.*proj");
            foreach (var projectFile in projectFiles)
            {
                var res = TryRetrieveVersionFromProjectFile(projectFile, packageName, out var version, out var error);
                var msg = "Count not retrieve from via project file: ";
                if (ShouldReturn(res, version, projectFile, msg, error, out var result))
                {
                    return result;
                }
            }

            throw new AggregateException("Unable to find packages path, the following failures occured: \n -" + string.Join("\n - ", misses), errs);
        }

        private static RetrievePackageVersionResult TryRetrieveVersionFromProjectFile(string projectName, string packageName, out string version, out Exception error)
        {
            version = null;
            error = null;
            if (!File.Exists(projectName))
            {
                return RetrievePackageVersionResult.MissingProjectFile;
            }

            var packageLine = File.ReadAllLines(projectName).FirstOrDefault(l => l.Contains($"nclude=\"{packageName}\""));
            if (packageLine == null)
            {
                return RetrievePackageVersionResult.PackageNotFoundInFile;
            }

            try
            {
                var packageLineElement = XElement.Parse(packageLine);
                var xAttribute = packageLineElement.Attribute("Version") ?? packageLineElement.Attribute("version");
                if (xAttribute != null)
                {
                    version = xAttribute.Value;
                    return RetrievePackageVersionResult.VersionFound;
                }
                else
                {
                    return RetrievePackageVersionResult.VersionAttributeMissing;
                }
            }
            catch (Exception e)
            {
                error = e;
                return RetrievePackageVersionResult.ErrorOccured;
            }
        }

        private static RetrievePackageVersionResult TryRetrieveVersionFromPackagesConfig(string packageName, out string version, out Exception error)
        {
            version = null;
            error = null;
            if (!File.Exists("packages.config"))
            {
                return RetrievePackageVersionResult.NoPackagesConfig;
            }

            var packageLine = File.ReadAllLines("packages.config").FirstOrDefault(l => l.Contains($"id=\"{ packageName}\""));
            if (packageLine == null)
            {
                return RetrievePackageVersionResult.PackageNotFoundInFile;
            }

            try
            {
                var packageLineElement = XElement.Parse(packageLine);
                var xAttribute = packageLineElement.Attribute("version");
                if (xAttribute != null)
                {
                    version = xAttribute.Value;
                    return RetrievePackageVersionResult.VersionFound;
                }
                else
                {
                    return RetrievePackageVersionResult.VersionAttributeMissing;
                }
            }
            catch (Exception e)
            {
                error = e;
                return RetrievePackageVersionResult.ErrorOccured;
            }
        }
    }

    internal enum RetrievePackageVersionResult
    {
        VersionFound,
        MissingProjectFile,
        NoPackagesConfig,
        PackageNotFoundInFile,
        VersionAttributeMissing,
        ErrorOccured
    }
}