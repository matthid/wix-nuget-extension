using System;
using Microsoft;
using Microsoft.Tools.WindowsInstallerXml;

namespace Matthid.WiX.NuGetExtensions
{
    using System;
    using System.IO;
    using Microsoft.Tools.WindowsInstallerXml;

    public class NuGetPreprocessorExtension : PreprocessorExtension
    {
        private const string Usage = "Usage: 'nuget.GetPath' or 'nuget.GetVersion'";
        private const string UsageGetPath = "Usage: 'nuget.GetPath(PackageName)' (preferred, searches nuget cache dir), 'nuget.GetPath(../path/to/packages, PackageName)'";
        private const string UsageGetVersion = "Usage: 'nuget.GetVersion(PackageName)'";
        private const string NugetPrefix = "nuget";

        public override string[] Prefixes => new [] { NugetPrefix };

        public override string EvaluateFunction(string prefix, string function, string[] args)
        {
            string result = null;
            switch (prefix)
            {
                case NugetPrefix:
                    switch (function)
                    {
                        case "GetPath":
                            if (args != null && args.Length > 0)
                            {
                                if (args.Length == 1)
                                {
                                    var packageName = args[0];
                                    result = NuGetLogic.GetPackagePath(@"..\packages", packageName);
                                }
                                else if (args.Length == 2)
                                {
                                    var packagesPath = args[0];
                                    var packageName = args[1];
                                    result = NuGetLogic.GetPackagePath(packagesPath, packageName);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Invalid number of arguments passed. Valid are one or two arguments. " + UsageGetPath);
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException("The 'GetPath' function needs at least one argument. " + UsageGetPath);
                            }

                            break;
                        case "GetVersion":
                            if (args != null && args.Length > 0)
                            {
                                if (args.Length == 1)
                                {
                                    var packageName = args[0];
                                    result = NuGetLogic.GetPackageVersion(packageName);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Invalid number of arguments passed. Valid is one argument. " + UsageGetVersion);
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException("The 'GetVersion' function needs at least one argument. " + UsageGetVersion);
                            }

                            break;
                        default:
                            throw new InvalidOperationException("Only support 'GetPath' and 'GetVersion'. " + Usage);
                    }

                    break;
            }

            return result;
        }

    }
}
