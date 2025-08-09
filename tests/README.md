# Tests: Outputs and Artifacts

This repository writes all test outputs and generated artifacts under the `tests/` folder to keep the root clean.

- Generated artifacts (from real OpenAI tests):
  - Saved under `tests/Results/<UTC_TIMESTAMP>/`
  - File: `generate.cs` (auto-formatted C#)

- Test run results (TRX):
  - Emitted under `tests/Results/`
  - Filename typically `test_results.trx` (or the runner’s default)
  - Controlled via `tests/RunSettings.runsettings` and referenced by the test project.

- Build safety:
  - `tests/Results/**/*.cs` is excluded from compilation.
  - `tests/Results/` is ignored by Git.

## How to run

From repo root:

```powershell
# Uses the runsettings and writes TRX to tests/Results
dotnet test
```

Optional overrides (if needed):

```powershell
# Force a specific results directory or logger
dotnet test --results-directory tests/Results --logger "trx;LogFileName=test_results.trx"
```

## Notes
- The root `TestResults/` folder is no longer used by our default `dotnet test` runs.
- If you see `TestResults/` appear, it likely came from an explicit CLI override or an IDE setting; you can safely delete it.
