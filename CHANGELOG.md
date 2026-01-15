# Changelog

All notable changes to FireBlazor will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2025-01-15

### Added

- **Firebase Authentication** - Email/password, Google, GitHub, Microsoft OAuth providers
- **Cloud Firestore** - Full CRUD, LINQ-style queries, real-time subscriptions, transactions, batch operations, aggregate queries
- **Cloud Storage** - Upload/download with progress tracking, metadata management, file listing
- **Realtime Database** - CRUD operations, real-time listeners, presence detection, server values, transactions
- **App Check** - reCAPTCHA v3/Enterprise integration
- **Firebase AI Logic** - Gemini model integration with streaming, function calling, multimodal input, grounding
- **Result Pattern** - Functional error handling with `Result<T>`
- **Blazor Authorization** - Integration with `[Authorize]` and `<AuthorizeView>`
- **Emulator Support** - Full local development with Firebase Emulator Suite
- **Testing Infrastructure** - Fake implementations (`FakeFirebase`, `FakeAuth`, etc.)
- **FirebaseComponentBase** - Base component with automatic subscription cleanup

### Fixed

- Function call parameter serialization uses camelCase
- Google Search grounding configuration simplified
- `FunctionCalls` deserialization in `GenerateContentResponse`

[Unreleased]: https://github.com/user/FireBlazor/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/user/FireBlazor/releases/tag/v1.0.0
