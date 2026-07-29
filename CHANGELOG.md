# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-07-29

### Added

- Published the first stable `Pidamg.Expressions` package to NuGet.org.

### Changed

- Established the current public API as the compatibility baseline for the `0.x` release line.
- Updated the English and French installation guidance for NuGet.org consumers.

## [0.1.0-beta.1] - 2026-07-29

### Added

- Added grouped weekly Dependabot updates for NuGet packages and GitHub Actions.

### Changed

- Updated the GitHub Actions used for checkout, .NET setup, and artifact transfers.
- Updated the .NET test SDK and xUnit Visual Studio runner.
- Revalidated the build, package test, GitHub Packages publication, and GitHub Release pipeline.

## [0.1.0-alpha] - 2026-07-29

### Added

- Added the initial `Pidamg.Expressions` package with typed and untyped expression evaluation.
- Added scoped evaluation contexts, null-safe navigation, indexing, method calls, value coercion,
  and string interpolation.
- Added English and French documentation, XML IntelliSense documentation, and public API
  compatibility validation.
- Added automated package validation, GitHub Packages publication, and stable-release publication
  to NuGet.org.

### Changed

- Prepared the project for publication as a NuGet package.

[Unreleased]: https://github.com/pidamg/expressions-dotnet/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/pidamg/expressions-dotnet/compare/v0.1.0-beta.1...v0.1.0
[0.1.0-beta.1]: https://github.com/pidamg/expressions-dotnet/compare/v0.1.0-alpha...v0.1.0-beta.1
[0.1.0-alpha]: https://github.com/pidamg/expressions-dotnet/releases/tag/v0.1.0-alpha
