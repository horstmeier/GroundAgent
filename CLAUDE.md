# GroundAgent — CLAUDE.md

GroundAgent is a local Azure Pipelines runner. It executes `azure-pipelines.yml` files directly
on the developer's machine without requiring a connection to Azure DevOps or an on-premise TFS server.
It uses the original Microsoft task scripts from
[azure-pipelines-tasks](https://github.com/microsoft/azure-pipelines-tasks) for authentic task execution.

## Architecture Overview

The pipeline execution follows this flow:

```
azure-pipelines.yml
  └─ TemplatePreprocessor        (Phase 5 — not yet implemented)
       └─ Build model (stages/jobs/steps)
            └─ TaskBuilder        builds TaskStep objects, resolves variables
                 └─ StepInvoker   executes each TaskStep
                      └─ TaskStep.Run()
                           ├─ ProcessExecutioner   (.bat / Process handler)
                           ├─ PowerShellExecutioner (PowerShell3 handler)
                           └─ NodeExecutioner       (Node10/16/20 handler — Phase 2)
```

## Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point, wires up DI |
| `MainProgram.cs` | Top-level orchestration: read YAML → build tasks → run steps |
| `BuildDefinitions/BuildDefinitionReader.cs` | Deserialises YAML into `Build` model via YamlDotNet |
| `BuildDefinitions/Build.cs` | Model: currently `Variables` + `Steps` (stages/jobs to be added) |
| `BuildDefinitions/Steps/Step.cs` | Raw step as parsed from YAML |
| `Tasks/TaskBuilder.cs` | Converts `Step` → `TaskStep`, resolves `$(variable)` substitutions |
| `Tasks/TaskStep.cs` | Executable step; `Run()` dispatches to the right executioner |
| `Tasks/LocalTaskStore.cs` | Loads task definitions from a local `azure-pipelines-tasks` checkout |
| `Tasks/RemoteTaskStore.cs` | Loads task definitions from Azure DevOps (requires connectivity) |
| `TaskExecutioners/PowerShellExecutioner.cs` | Runs PowerShell3 handler tasks |
| `TaskExecutioners/ProcessExecutioner.cs` | Runs batch/process handler tasks |
| `Configuration/AppConfiguration.cs` | Reads `appsettings.json` |
| `appsettings.json` | Points to local task folder and temp dir |

## Configuration

`appsettings.json` controls runtime behaviour:

```json
{
  "taskLocation": {
    "localPath": "/path/to/azure-pipelines-tasks/_build/Tasks"
  },
  "tmpDir": "/tmp/groundagent",
  "systemDebug": false
}
```

`localPath` must point to a built copy of the
[microsoft/azure-pipelines-tasks](https://github.com/microsoft/azure-pipelines-tasks) repository.
Each task lives in a subfolder named `<TaskName>V<MajorVersion>` (e.g. `CmdLineV2`).

## Development Setup

The devcontainer (`/.devcontainer/devcontainer.json`) provides .NET 10, Node LTS, and Git.

```bash
# Build
dotnet build

# Run against a pipeline file
dotnet run -- path/to/azure-pipelines.yml

# Run tests (once test project exists)
dotnet test
```

## Variable Syntax

Two distinct syntaxes exist in Azure Pipelines and must be handled separately:

- **Runtime variables** `$(varName)` — substituted by `TaskBuilder` at build time (already implemented)
- **Compile-time expressions** `${{ expression }}` — must be evaluated by the `TemplatePreprocessor`
  *before* `TaskBuilder` runs (Phase 5, not yet implemented)

## Known Limitations / Open Work Items

### Phase 1 — Modernisation (do first)
- [ ] Upgrade target framework from `netcoreapp2.2` to `net10.0`
- [ ] Update all NuGet packages to current versions
- [ ] Replace Windows-only backslash paths in `TaskBuilder` with `Path.Combine`
- [ ] Replace hardcoded `appsettings.json` path `D:\ap\...` with sensible cross-platform default
- [ ] Add `System.CommandLine` for proper CLI argument handling (pipeline file, variable overrides)
- [ ] Rename solution and root namespace from `AzurePipelineRunner` to `GroundAgent`

### Phase 2 — Node.js Task Runner
- [ ] Implement `NodeExecutioner` supporting Node10, Node16, Node20, Node24 handlers
- [ ] Detect correct Node handler from `task.json` `execution` block
- [ ] Wire `NodeExecutioner` into `TaskStep.Run()` (currently prints a warning and skips)

### Phase 3 — Stages & Jobs Model
- [ ] Extend `Build` model with `Stages → Jobs → Steps` hierarchy
- [ ] Keep backward compatibility: a flat `steps:` list at root level still works
- [ ] Implement sequential execution of stages and jobs
- [ ] Add `dependsOn` resolution for jobs within a stage

### Phase 4 — Agent Variables
- [ ] Inject standard predefined variables at startup:
  `Build.SourcesDirectory`, `System.DefaultWorkingDirectory`, `Agent.TempDirectory`,
  `Build.ArtifactStagingDirectory`, `Build.BuildId`, `Build.Repository.LocalPath`, etc.
- [ ] Allow overriding any variable via CLI (`--var name=value`)
- [ ] Evaluate `condition:` fields on steps (currently parsed but ignored)

### Phase 5 — Template Preprocessor
- [ ] Implement recursive YAML loader: when a `template:` key is encountered, load and inline the
  referenced file before deserialisation
- [ ] Implement parameter substitution: replace `${{ parameters.name }}` tokens
- [ ] Implement compile-time expression engine for `${{ if condition }}:` blocks
  - Required functions: `eq`, `ne`, `and`, `or`, `not`, `contains`, `startsWith`, `endsWith`, `in`
- [ ] Support `${{ each item in collection }}:` loops (lower priority)
- [ ] The preprocessor runs as a pure YAML→YAML transformation step before `BuildDefinitionReader`

### Phase 6 — Quality & Tests
- [ ] Add unit tests for `TemplatePreprocessor` (expression engine is highly testable in isolation)
- [ ] Add unit tests for `TaskBuilder` variable substitution
- [ ] Add integration tests using simple self-contained pipelines (script steps only)
- [ ] Add CI pipeline (dogfooding: run GroundAgent on itself)

## Design Decisions

**Why use the real microsoft/azure-pipelines-tasks scripts?**
The original task scripts are the same code Azure DevOps runs in the cloud. Using them directly
maximises compatibility without reimplementing task logic.

**Template preprocessing is a separate phase**
Templates are a compile-time concern in Azure Pipelines. The preprocessor must fully expand all
`template:` references and `${{ }}` expressions into a plain YAML document before any execution
logic is involved. This keeps the execution engine simple.

**Deployment pipelines are out of scope**
`environment:`, `deployment:` jobs, approval gates, and service connections are not planned.
The tool targets build pipelines.

**No Azure DevOps connectivity required**
The only supported task store is `LocalTaskStore`. `RemoteTaskStore` may be kept for reference
but is not a development priority.
