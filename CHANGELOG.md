# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Add 'Copy' and 'Copy message' context menu buttons to selected log window
- Add log fields: process id, location (class, method, file, line)
- Add namespace check to log4j converter
- Add configuration option 'AllowAnonymous' to recognize log entries without log4j namespace
- Add configuration option 'ApplicationFormat':
  - 'DoNotChange' to collect applications per process (as before)
  - 'Consolidate' to collect applications across processes

### Changed

- Selected log is displayed in grid style instead of as one text block
- Datetime format switching affects all currently cached logs, not just new ones

### Fixed

- Datetime format switching was not working

## Removed

- Remove AutoMapper dependency

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

