# AI Agent Guidelines: Automatic Change Logging

**CRITICAL INSTRUCTION FOR ALL AI AGENTS:**
Every time you receive a command, perform code edits, or modify files in this workspace, you **MUST** automatically document the change in the `change_log.csv` file at the root of the workspace. This action is mandatory and must be executed at the end of every turn without requiring explicit instructions or prompts from the user.

## Logging File Path
- File: [change_log.csv](file:///C:/Users/downk.KHG-HOME/source/repos/GOS-Lifeis/change_log.csv)

## Logging Format (CSV Headers)
`Date,Time,Category,Description,Status`

- **Date**: Format as `YYYY-MM-DD`
- **Time**: Format as `HH:MM:SS` (24-hour format)
- **Category**: Area modified (e.g., `Refactoring`, `Feature`, `Database`, `Scene`, `Prefab`)
- **Description**: Concise summary of what was added, modified, or deleted.
- **Status**: Status of the work (e.g., `Completed`, `In Progress`)
