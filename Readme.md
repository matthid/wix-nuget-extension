# NuGet Extension for WiX Projects

Simple WiX extension to resolve paths to installed NuGet packages.

## Install

Just install the `Matthid.WiX.NuGetExtensions` package into your WiX project via NuGet.

## Usage

You can use:

- `nuget.GetVersion(packName)`: Searches `packages.config` and `*.*proj` files for package entries to detect the package version. Returns the first result.
- `nuget.GetPath(packName)`: First detects the version (like `GetVersion`) then returns either the path to the nuget package cache (preferred) or a local `packages` directory.
- `nuget.GetPath(packagesDir, packName)`: First detects the version (like `GetVersion`) then returns either the path to the nuget package cache (preferred) or the given local `packages` directory (use this if you use a non-standard package directory location).

```xml
<Property Id="FUNCTIONTEST" Value="$(nuget.GetPath(MyPackageName))/tools/Installer.msi" />
<Property Id="FUNCTIONTEST" Value="$(nuget.GetPath(MyPackageName))/tools/Installer_$(nuget.GetVersion(MyPackageName)).msi" />
```

## Build/Release

Version=1.0.0 dotnet pack -c Release wix-nuget-extension.sln
