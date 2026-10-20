# Implementation plan

Use the existing React/Vite + FastAPI/SQLite application and Unity 6000.3.25f1 project. Keep implementation in place; no new service dependencies.

Website and Unity C# work are parallel, path-isolated workstreams. The parent exclusively drives the live Unity Editor, rebuilds/saves scenes, integrates, documents, audits Git contents, and publishes. Existing uncommitted edits are retained and reviewed.

Validation: website build plus browser/API scenarios against safe test data; Unity compile, modal-input static audit, live City scene probes and game-view capture. Provide exact verification provenance and known limitations. Exclude credentials, SQLite files, generated caches, and machine-local tooling from publication.
