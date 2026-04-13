# Claude Code Game Studios

Unity 6.3 LTS project using C# and URP. Keep changes small, reversible, and
easy to verify.

## Startup Order

1. Read `production/session-state/active.md` first if it exists.
2. If state is missing or stale, read `.claude/docs/project-map.md`.
3. Read `.claude/docs/decision-log.md` only if the task needs architectural context.
4. Read the current sprint file before broad repo exploration.

## Working Rules

- Prefer direct reads of named files over broad repo scans.
- Production runtime code lives in `Assets/Scripts/Core/`.
- EditMode tests live in `Assets/Tests/EditMode/`.
- Prototype code under `Assets/Scripts/Prototypes/` is reference only.
- Ask before commits or risky cross-cutting changes.
- Preserve user changes already present in the worktree.
- Keep `production/session-state/active.md` under 40 lines.
- Update `active.md` at milestones, before `/clear`, and before ending a session.

## High-Value References

- Project map: `.claude/docs/project-map.md`
- Stable decisions: `.claude/docs/decision-log.md`
- Session workflow: `.claude/docs/session-playbook.md`
- Session template: `.claude/docs/session-state-template.md`
- Current sprint: `production/sprints/sprint-002.md`
- Combat architecture: `docs/architecture/adr-003-combat-architecture.md`

## Deep References

Use only when relevant to the current task:

- `.claude/docs/technical-preferences.md`
- `.claude/docs/coding-standards.md`
- `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md`
