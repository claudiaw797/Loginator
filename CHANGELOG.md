# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.5] - 2024-09-19

### Added

- 'Copy' and 'Copy message' context menu buttons in selected log window
- Log fields: process id, location (class, method, file, line), properties
- Namespace checking in log4j converter
- Configuration option 'AllowAnonymous' to recognize log entries without log4j namespace
- Configuration option 'ApplicationFormat':
  - 'DoNotChange' to collect applications per process (as before)
  - 'Consolidate' to collect applications across processes
- Dependencies: FakeItEasy 8.3.0

### Changed

- Datetime format switching affects all currently cached logs
- Selected log is displayed in grid style
- Content of log fields:
  - log4j:NDC is displayed as context
  - log4j:MDC is added to properties
  - properties are displayed on their own
- Upgrade dependencies: CommunityToolkit.Mvvm 8.3.2, FluentAssertions 6.12.1, Microsoft.Extensions.TimeProvider.Testing 8.9.1, Microsoft.NET.Test.Sdk 17.11.1, NLog 5.3.4, NLog.Extensions.Hosting 5.3.13, NUnit 4.2.2

### Fixed

- Datetime format switching

## Removed

- Dependencies: AutoMapper

## [1.7.4] - 2024-08-24

### Added

- Add GPLv3 license and third-party notice file from dependencies

### Changed

- Make application hosted
- Exchange CommonServiceLocator/StructureMap packages for .NET dependency injection
- Exchange settings from App.config (.NET Framework style) for appsettings.json
- Improve logging and respective capabilities by adding a second configuration file for development
- Upgrade dependencies: Microsoft.NET.Test.Sdk 17.11.0, NUnit 4.2.1

## [1.7.3] - 2024-08-20

### Added

- Add interface and implementations for Stopwatch, allowing configuration 'IsTimingTraceEnabled' to do nothing if set to false, otherwise provide a simple to use, low resource stopwatch

### Changed

- Improve user-friendliness of setting the maximum number of loaded log entries per level
- Allow using fake TimeProvider for testing log entry loading
- Refactorings (LoginatorViewModel)

## [1.7.2] - 2024-08-17

### Added

- Add 'Copy exception' context menu button to selected log window

### Changed

- Fix logging

## [1.7.1] - 2024-08-16

### Added

- Add test projects for Backend and Loginator

### Changed


- Improve user-friendliness of search
- Refactorings (ApplicationViewModel, LoggingLevel, OrderedObservableCollection)
- Upgrade dependencies: NLog 5.3.3

## [1.7.0] - 2024-08-10

### Changed

- Upgrade target framework to .NET 8
- Upgrade dependencies: AutoMapper 13.0.1, CommonServiceLocator 2.0.7, NLog 5.3.2, StructureMap 4.7.1, System.Configuration.ConfigurationManager 8.0.0
- Switch dependency: CommunityToolkit.Mvvm 8.2.2
- Upgrade installer package builder: WiX 3.11.2

## [1.6.1] - 2024-08-08

### Added

- Add button to unselect all applications

