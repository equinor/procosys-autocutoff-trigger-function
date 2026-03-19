---
name: replacing-editorconfig
description: Replace the .editorconfig in a .NET project with the standard procosys-main editorconfig, then fix all formatting violations and validate the build. Use when asked to update, replace, or standardize the editorconfig for a .NET function app or project, or when asked to apply the procosys-main code style.
---

# Replacing EditorConfig with procosys-main Standard

**Before doing anything else**, inform the user that the `replacing-editorconfig` skill has been activated and is being used to guide the task.

## Overview

This skill replaces the `.editorconfig` in a .NET project with the comprehensive version from `procosys-main/Source/.editorconfig`, fixes all resulting formatting violations, and validates the build.

## Prerequisites

- The project must be a .NET solution with a `.sln` file
- The standard editorconfig is bundled with this skill at [references/procosys-main.editorconfig](references/procosys-main.editorconfig)

## Workflow

### Phase 1: Pre-flight verification

1. Run `dotnet build <SolutionName>.sln` — confirm baseline build succeeds
2. Run `dotnet test <SolutionName>.sln` — confirm baseline tests pass
3. If either fails, stop and inform the user before proceeding

### Phase 2: Capture existing suppressions

1. Read the current `.editorconfig` in the target project
2. Note any `dotnet_diagnostic.*` rules and `dotnet_code_quality_*` settings — these are project-specific analyzer suppressions that must be preserved
3. Also note any custom settings not present in the procosys-main version

### Phase 3: Replace .editorconfig

1. Read the standard editorconfig from [references/procosys-main.editorconfig](references/procosys-main.editorconfig) and use it to replace the target project's `.editorconfig`
2. Append the preserved analyzer suppressions from Phase 2 at the bottom of the `[*.cs]` section
3. Do NOT discard the `IDE0060` setting from the old file. The new editorconfig sets `dotnet_code_quality_unused_parameters = all:error` which is stricter — any old relaxed setting is intentionally replaced

### Phase 4: Diagnose formatting violations

1. Run `dotnet format <SolutionName>.sln --verify-no-changes --verbosity diagnostic` to list all violations without changing anything
2. Review the output to understand the scope of changes

### Phase 5: Auto-fix formatting

1. Run `dotnet format <SolutionName>.sln` to auto-fix what it can

Common auto-fixable issues:
- **Import ordering**: `dotnet_sort_system_directives_first = true` means `System.*` usings must come before `Microsoft.*`
- **`var` usage**: `csharp_style_var_* = true:error` enforces `var` everywhere the type is apparent
- **Whitespace/indentation**: The new config enforces 4-space indent for `.cs` files
- **Final newlines**: `insert_final_newline = true` adds trailing newlines

### Phase 6: Fix remaining issues manually

`dotnet format` cannot auto-fix all violations. Common manual fixes:

1. **Unused Azure Functions trigger parameters** (IDE0060): Function trigger parameters like `[TimerTrigger] TimerInfo timer` or `[HttpTrigger] HttpRequestData req` cannot be removed because the Azure Functions runtime requires them for binding. Rename unused trigger parameters to `_` (e.g., `TimerInfo _`). The trigger binding still works via the attribute.

2. **Naming convention violations**: The new config enforces:
   - Private/internal fields must start with `_` (e.g., `_myField`)
   - Static fields must start with `s_` (e.g., `s_instance`)
   - Constants must be PascalCase
   - Review each violation and rename accordingly

3. **`var` conversions that `dotnet format` missed**: Manually convert explicit type declarations to `var` where the type is apparent from the right-hand side

### Phase 7: Final validation

1. Run `dotnet format <SolutionName>.sln --verify-no-changes` — confirm zero remaining violations
2. Run `dotnet build <SolutionName>.sln` — confirm build succeeds with no errors
3. Run `dotnet test <SolutionName>.sln` — confirm all tests pass

If any step fails, fix the issue and re-run validation.

## Key rules from the procosys-main editorconfig

| Rule | Setting | Severity |
|------|---------|----------|
| Use `var` everywhere | `csharp_style_var_*` | error |
| Sort `System.*` usings first | `dotnet_sort_system_directives_first` | true |
| All unused params are errors | `dotnet_code_quality_unused_parameters` | all:error |
| Private fields start with `_` | `camel_case_underscore_style` | error |
| Braces on new lines | `csharp_new_line_before_open_brace` | all |
| Prefer braces | `csharp_prefer_braces` | suggestion |
| Final newline required | `insert_final_newline` | true |

## Checklist

Copy and track progress:

- [ ] Baseline build passes
- [ ] Baseline tests pass
- [ ] Old analyzer suppressions captured
- [ ] .editorconfig replaced with procosys-main version
- [ ] Old suppressions re-added
- [ ] `dotnet format` auto-fix applied
- [ ] Remaining IDE0060 (unused trigger params) fixed — renamed to `_`
- [ ] Other manual fixes applied
- [ ] `dotnet format --verify-no-changes` passes (zero violations)
- [ ] Final build passes
- [ ] Final tests pass
