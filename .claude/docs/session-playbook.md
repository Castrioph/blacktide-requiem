# Session Playbook

Use this order to recover or continue work with minimal context cost:

1. Read `production/session-state/active.md`.
2. Read `.claude/docs/project-map.md`.
3. Read the current sprint or ADR only if needed.
4. Read the exact files listed in `active.md`.

## Active State Rules

- Keep `production/session-state/active.md` under 40 lines.
- Store only what the next session needs: task, files, decisions, blockers,
  verify commands, and next step.
- Do not paste long logs, diffs, or chat transcripts into `active.md`.
- Update `active.md` after meaningful milestones and before `/clear`.

## Low-Token Defaults

- Prefer named-file reads over repo-wide scans.
- Avoid reopening settled decisions if they are already in `decision-log.md`.
- Summarize completed work into files; do not rely on chat history as memory.
- If the task changes, replace stale state instead of appending another layer.

## End-of-Session Handoff

- Confirm `active.md` reflects the latest verified state.
- List only the files that matter for the immediate next step.
- Record the exact verification command when one is known.
- Record one next action, not a long backlog.
