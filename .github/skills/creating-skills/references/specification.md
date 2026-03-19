# Agent Skills Specification Reference

Detailed field constraints and validation rules from the [Agent Skills specification](https://agentskills.io/specification).

## Contents

- [Frontmatter fields](#frontmatter-fields)
- [Name field rules](#name-field-rules)
- [Description field rules](#description-field-rules)
- [Optional fields](#optional-fields)
- [Body content](#body-content)
- [Progressive disclosure](#progressive-disclosure)
- [Validation](#validation)

## Frontmatter fields

| Field | Required | Constraints |
|-------|----------|-------------|
| `name` | Yes | Max 64 chars. Lowercase letters, numbers, hyphens only. Must match parent directory name. |
| `description` | Yes | Max 1024 chars. Non-empty. No XML tags. Describes what + when. |
| `license` | No | License name or reference to a bundled license file. |
| `compatibility` | No | Max 500 chars. Environment requirements (product, packages, network). |
| `metadata` | No | Map of string keys to string values. Use reasonably unique key names. |
| `allowed-tools` | No | Space-delimited list of pre-approved tools. Experimental. |

## Name field rules

The `name` field:
- Must be 1–64 characters
- May only contain lowercase alphanumeric characters (`a-z`, `0-9`) and hyphens (`-`)
- Must not start or end with a hyphen
- Must not contain consecutive hyphens (`--`)
- **Must match the parent directory name exactly**

Valid: `pdf-processing`, `data-analysis`, `code-review`
Invalid: `PDF-Processing` (uppercase), `-pdf` (starts with hyphen), `pdf--processing` (consecutive hyphens)

## Description field rules

The `description` field:
- Must be 1–1024 characters
- Must not be empty
- Must not contain XML tags
- Should describe both **what the skill does** and **when to use it**
- Should include specific keywords that help agents identify relevant tasks
- Write in third person

## Optional fields

### license

```yaml
license: Apache-2.0
```

Or reference a bundled file:

```yaml
license: Proprietary. LICENSE.txt has complete terms
```

### compatibility

Only include if the skill has specific environment requirements. Max 500 characters.

```yaml
compatibility: Designed for Claude Code (or similar products)
```

```yaml
compatibility: Requires git, docker, jq, and access to the internet
```

### metadata

Arbitrary key-value pairs. Both keys and values must be strings.

```yaml
metadata:
  author: my-org
  version: "1.0"
```

### allowed-tools

Space-delimited list of pre-approved tools. Support varies by agent implementation.

```yaml
allowed-tools: Bash(git:*) Bash(jq:*) Read
```

## Body content

The markdown body after the frontmatter has no format restrictions. Recommended sections:

- Step-by-step instructions
- Examples of inputs and outputs
- Common edge cases

Keep the body **under 500 lines**. If content exceeds this, split into referenced files using progressive disclosure.

## Progressive disclosure

Skills use three tiers of context loading:

1. **Metadata (~100 tokens)**: `name` and `description` loaded at startup for all skills
2. **Instructions (< 5000 tokens recommended)**: Full SKILL.md body loaded when skill activates
3. **Resources (as needed)**: Files in `scripts/`, `references/`, `assets/` loaded only when required

### File references

Use relative paths from the skill root:

```markdown
See [the reference guide](references/REFERENCE.md) for details.
Run the extraction script: `scripts/extract.py`
```

Keep references **one level deep** from SKILL.md. Do not chain: SKILL.md → file1.md → file2.md.

### Long reference files

For reference files over 100 lines, include a table of contents at the top so the agent can see the full scope even with partial reads.

## Validation

Use the [skills-ref](https://github.com/agentskills/agentskills/tree/main/skills-ref) reference library to validate skills:

```bash
skills-ref validate ./my-skill
```

This checks that SKILL.md frontmatter is valid and follows all naming conventions.

### Manual validation checklist

1. `name` matches the parent directory name
2. `name` uses only lowercase letters, numbers, and hyphens
3. `name` does not start/end with a hyphen or contain `--`
4. `description` is non-empty and under 1024 characters
5. `description` includes what the skill does and when to use it
6. YAML frontmatter is between `---` markers with no tabs
7. SKILL.md body is under 500 lines
8. All file paths use forward slashes
9. Referenced files exist and are one level deep
