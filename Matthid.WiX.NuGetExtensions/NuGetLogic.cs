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
        internal static string GetNuGetCacheDir()
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

        public static string GetPackageVersion(string baseDir, string packageName)
        {
            return RetrievePackageVersion(baseDir, packageName);
        }

        public static string GetPackagePath(string baseDir, string packagesPath, string packageName)
        {
            var version = RetrievePackageVersion(baseDir, packageName);

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

            var resolvedPackagePath = Path.Combine(baseDir, packagesPath);
            if (Directory.Exists(resolvedPackagePath))
            {
                var available = Directory.EnumerateDirectories(resolvedPackagePath, $"{packageName}.*").ToList();
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
                    
                    var correctPath = available.FirstOrDefault(folder => folder.ContainsEx(version));
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

        private static string RetrievePackageVersion(string baseDir, string packageName)
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
                        misses.Add(errorPrefix + $"Package '{packageName}' was not found in the file '{file}'");
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

            var packagesConfig = Path.Combine(baseDir, "packages.config");
            if (File.Exists(packagesConfig))
            {
                var res = TryRetrieveVersionFromPackagesConfig(baseDir, packageName, out var version, out var error);
                var msg = "Could not retrieve from packages.config: ";
                if (ShouldReturn(res, version, packagesConfig, msg, error, out var result))
                {
                    return result;
                }
            }

            var projectFiles = Directory.EnumerateFiles(baseDir, "*.*proj");
            foreach (var projectFile in projectFiles)
            {
                var res = TryRetrieveVersionFromProjectFile(projectFile, packageName, out var version, out var error);
                var msg = "Could not retrieve via project file: ";
                if (ShouldReturn(res, version, projectFile, msg, error, out var result))
                {
                    return result;
                }
            }

            if (misses.Count == 0)
            {
                misses.Add($"Neither 'packages.config' nor any project file was found in '{Path.GetFullPath(baseDir)}'");
            }

            throw new AggregateException("Unable to find packages path, the following failures occured: \n - " + string.Join("\n - ", misses), errs);
        }

        private static RetrievePackageVersionResult TryRetrieveVersionFromProjectFile(string projectName, string packageName, out string version, out Exception error)
        {
            version = null;
            error = null;
            if (!File.Exists(projectName))
            {
                return RetrievePackageVersionResult.MissingProjectFile;
            }

            var packageLine = File.ReadAllLines(projectName).FirstOrDefault(l => l.ContainsEx($"nclude=\"{packageName}\""));
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

        private static RetrievePackageVersionResult TryRetrieveVersionFromPackagesConfig(string baseDir, string packageName, out string version, out Exception error)
        {
            version = null;
            error = null;
            var packagesConfigFile = Path.Combine(baseDir, "packages.config");
            if (!File.Exists(packagesConfigFile))
            {
                return RetrievePackageVersionResult.NoPackagesConfig;
            }

            var packageLine = File.ReadAllLines(packagesConfigFile).FirstOrDefault(l => l.ContainsEx($"id=\"{ packageName}\""));
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