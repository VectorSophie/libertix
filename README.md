# Fork-only validation

This branch exists only in VectorSophie/libertix and is not meant to be merged or submitted
upstream. `.github/workflows/fork-validation.yml` checks out each commit listed in its matrix,
confirms the checked-out SHA, and runs the checks from upstream `ci.yml` that do not need its
identity job: Python quality and tests, shell and JSON checks, PowerShell syntax,
PSScriptAnalyzer, Pester, the WPF build with `/warnaserror`, and the C# contract tests.

It does not run upstream's identity, ISO, release, signing, Pages or release-cleanup jobs. It
also skips the version stamping step, which only changes file-version metadata. It needs read
access only, uses no secrets, and uploads only test result files.

Upstream's `ci.yml` and the fork's `dev` and `main` branches are not modified.
