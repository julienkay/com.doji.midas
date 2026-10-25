# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).


## [2.0.0] - Unreleased

### Changed

- Models come from the Hugging Face repository `julienkay/sentis-MiDaS` through AI Model Hub (`tools.doji.hf-hub`). The package no longer contains model files or downloads them from GitHub releases. Include the models you use in `Window > AI Model Hub`.
- Requires Unity 6000.3 or later and Inference Engine 2.6.1.
- Changing `Backend` reuses the loaded model instead of loading it again.
- The constructors take an optional `BackendType`.

### Added

- `Midas.CreateAsync` loads the model on every platform, also when builds download models at runtime.

### Removed

- The `EstimateDepth(Texture, object)` overload, which was not implemented.
- The Editor assembly `Doji.Midas.Editor`, which downloaded models into the package.

### Fixed

- The async webcam sample no longer freezes when paused.

## [1.4.0] - 2025-06-27

### Changed

- Updated Sentis dependency to Inference Engine 2.2.1

## [1.3.0] - 2024-09-23

### Added

- Added method overloads to allow spreading the depth estimation over multiple frames.

### Changed

- Updated Sentis dependency to 2.1.0

## [1.2.0] - 2024-09-03

### Changed

- Updated Sentis dependency to 2.0.0

## [1.1.0] - 2024-08-17

### Changed

- Updated Sentis dependency to 1.6.0-pre.1
- Normalization is now handled by a separate class using the Functional API

## [1.0.1] - 2024-02-09

### Added

- Add a sample scene for depth estimation on videos.

### Fixed

- Fixed scale/shift calculation from min/max distance in sample scenes.

## [1.0.0] - 2024-01-22

- Initial Release