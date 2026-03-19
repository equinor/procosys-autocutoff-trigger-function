---
name: creating-skills
description: Create, review, or fix Agent Skills (SKILL.md files). Use when asked to create a new skill, author a skill, write a SKILL.md, scaffold a skill folder, review skill structure, fix skill frontmatter, or package domain knowledge into a reusable agent skill.
---

# Creating Agent Skills

**Before doing anything else**, inform the user that the `creating-skills` skill has been activated and is being used to guide the task.

**Then**, fetch the latest specification and best practices from [https://agentskills.io/specification](https://agentskills.io/specification) and [https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices). Compare the fetched content against the instructions in this skill. If there are meaningful differences (new fields, changed constraints, updated best practices), inform the user and update this skill's files before proceeding with the task.

An Agent Skill is a folder containing a `SKILL.md` file that gives agents on-demand access to procedural knowledge and workflows. This skill guides you through authoring new skills that follow the open Agent Skills specification.

## Skill folder structure

```
skill-name/
├── SKILL.md          # Required: YAML frontmatter + markdown instructions
├── scripts/          # Optional: executable code
├── references/       # Optional: additional documentation
└── assets/           # Optional: templates, resources
```

The folder name **must match** the `name` field in the YAML frontmatter.

### Where to place skills

Skills can live in any of these directories (relative to workspace root):

| Directory | Notes |
|-----------|-------|
| `.github/skills/<name>/` | GitHub convention |
| `.agents/skills/<name>/` | VS Code default scan path |
| `.claude/skills/<name>/` | Claude Code convention |

## Creation workflow

Follow these steps when creating a new skill.

### Step 1: Choose a name

- Max 64 characters
- Lowercase letters, numbers, and hyphens only (`a-z`, `0-9`, `-`)
- Must not start or end with a hyphen
- Must not contain consecutive hyphens (`--`)
- Prefer gerund form: `processing-pdfs`, `analyzing-data`, `reviewing-code`
- Avoid vague names: `helper`, `utils`, `tools`

### Step 2: Create the folder and SKILL.md

Create `<location>/<skill-name>/SKILL.md` with YAML frontmatter:

```yaml
---
name: skill-name
description: Describe what the skill does and when to use it. Use when...
---
```

### Step 3: Write the description

The `description` field is how agents decide whether to activate the skill. It is critical for discovery.

Rules:
- Max 1024 characters
- Must include **what the skill does** and **when to use it**
- Write in **third person** (not "I can help you..." or "You can use this...")
- Include specific trigger keywords the user might say
- Be concrete, not vague

Good example:
```yaml
description: Extract text and tables from PDF files, fill forms, merge documents. Use when working with PDF files or when the user mentions PDFs, forms, or document extraction.
```

Bad example:
```yaml
description: Helps with documents.
```

### Step 4: Write the body

The markdown body after the frontmatter contains the actual instructions.

**Important:** The first instruction in the body should always tell the agent to inform the user that the skill has been activated. Add this immediately after the heading:

```markdown
**Before doing anything else**, inform the user that the `<skill-name>` skill has been activated and is being used to guide the task.
```

Additional guidelines:

- **Keep under 500 lines.** If longer, split into referenced files.
- **Be concise.** Only add context the agent doesn't already have.
- **Set appropriate freedom.** Use exact commands for fragile operations; use general guidance for flexible tasks.
- **Use consistent terminology.** Pick one term and stick with it throughout.
- Include step-by-step instructions, examples of inputs/outputs, and common edge cases.

### Step 5: Add reference files (if needed)

For detailed content that shouldn't bloat the main SKILL.md, create separate files and link to them:

```markdown
**Form filling**: See [FORMS.md](references/FORMS.md) for the complete guide.
**API reference**: See [REFERENCE.md](references/REFERENCE.md) for all methods.
```

Rules for references:
- Keep references **one level deep** from SKILL.md (no nested chains)
- Use relative paths with **forward slashes** (even on Windows)
- Name files descriptively (`form_validation_rules.md`, not `doc2.md`)
- Add a table of contents at the top of reference files longer than 100 lines

### Step 6: Add scripts (if needed)

Place executable scripts in `scripts/`. Scripts should:
- Be self-contained or clearly document dependencies
- Include helpful error messages
- Handle edge cases gracefully
- State required packages explicitly (e.g., `pip install pypdf`)

Make the intent clear in SKILL.md:
- To execute: "Run `scripts/extract.py` to process the file"
- To read as reference: "See `scripts/extract.py` for the algorithm"

### Step 7: Validate

After creating the skill:

1. Confirm `name` in frontmatter matches the folder name exactly
2. Confirm YAML frontmatter is syntactically valid (between `---` markers, no tabs, no unescaped colons in values)
3. Confirm `description` is present, non-empty, and includes what + when
4. Confirm SKILL.md body is under 500 lines
5. Confirm all file references use forward slashes and are one level deep

For full frontmatter field constraints, see [references/specification.md](references/specification.md).

## Optional frontmatter fields

Beyond the required `name` and `description`, these optional fields are available:

| Field | Purpose |
|-------|---------|
| `license` | License name or reference to a bundled LICENSE file |
| `compatibility` | Environment requirements (max 500 chars) |
| `metadata` | Arbitrary key-value pairs (author, version, etc.) |
| `allowed-tools` | Space-delimited list of pre-approved tools (experimental) |

Example with optional fields:
```yaml
---
name: pdf-processing
description: Extract PDF text, fill forms, merge files. Use when handling PDFs.
license: Apache-2.0
compatibility: Requires Python 3.9+ and pdfplumber
metadata:
  author: my-team
  version: "1.0"
allowed-tools: Bash(python:*) Read
---
```

## Patterns

### Template pattern

Provide output templates when format consistency matters:

```markdown
## Report structure

ALWAYS use this exact template:

# [Title]
## Executive summary
[One-paragraph overview]
## Key findings
- Finding 1
- Finding 2
## Recommendations
1. Actionable recommendation
```

### Workflow with checklist

For complex multi-step tasks, provide a checklist the agent can track:

```markdown
## Migration workflow

Copy this checklist and track progress:

- [ ] Step 1: Back up the database
- [ ] Step 2: Run migration script
- [ ] Step 3: Validate schema
- [ ] Step 4: Run integration tests
- [ ] Step 5: Update documentation
```

### Conditional workflow

Guide the agent through decision points:

```markdown
1. Determine the task type:
   **Creating new content?** → Follow "Creation workflow" below
   **Editing existing content?** → Follow "Editing workflow" below
```

## Anti-patterns to avoid

- **Windows-style paths**: Always use forward slashes (`scripts/helper.py`, not `scripts\helper.py`)
- **Deeply nested references**: SKILL.md → file1.md → file2.md → actual info. Keep it one level deep.
- **Too many options**: Provide one default approach, with an escape hatch for edge cases
- **Vague descriptions**: "Helps with files" tells the agent nothing. Be specific about what and when.
- **Time-sensitive info**: Don't write "use API v1 before August 2025". Use a "current method" / "old patterns" structure.
- **Over-explaining**: The agent already knows common concepts. Only add context it doesn't have.
- **Magic constants in scripts**: Document why every non-obvious value was chosen
- **Inconsistent terminology**: Pick one term (e.g., "endpoint" not alternating with "URL", "route", "path")

## Iterative development

1. Complete a task without a skill first — notice what context you repeatedly provide
2. Create a minimal skill capturing that context
3. Test the skill on similar tasks with a fresh agent session
4. Observe where the agent struggles and refine the instructions
5. Repeat the test-observe-refine cycle
