---
name: coplay-unity-verify
description: "Run Unity verification with minimal iteration cost. Use when the task is to check compile state, run EditMode or PlayMode verification, or playtest a scene flow through Coplay or Unity batch mode."
argument-hint: "[compile | editmode | playmode | scene-flow | path-to-scene-or-test]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash, Write
---

# Coplay Unity Verification

Use this skill for verification passes, not for feature implementation.

## 1. Recover Minimal Context

- Read `production/session-state/active.md` first.
- Read the current sprint file only if `active.md` points to it.
- Read only the exact test, scene, or script files already named by the task.
- Do not open full test files unless debugging a failure or generating a small
  temporary runner requires method names or namespaces.

## 2. Emit a Short Plan First

Before using Coplay or Unity commands, print a plan with 3 steps maximum:

1. Check compile/editor state.
2. Run exactly one verification path.
3. Summarize result, blocker, and next action.

If you expect to switch paths, say why before doing it.

## 3. Choose One Verification Path

Pick one path and stay on it unless it fails.

### Path A: `editor-tools`

Use this when Coplay editor tools are available and the task needs in-editor
verification or playtesting.

Order:

1. `mcp__coplay-mcp__check_compile_errors`
2. If needed, write the smallest possible temporary runner script
3. `mcp__coplay-mcp__execute_script`
4. `mcp__coplay-mcp__get_unity_logs` only after execution or when the result is unclear

### Path B: `batchmode`

Use this when you only need automated test verification and the editor path is
not required.

Order:

1. Check the exact Unity test command from repo state or prior notes
2. Run one batch test command
3. Read results/log output once

### Path C: `manual`

Use this when the editor is unavailable, the path is blocked, or the task is
waiting on a human playtest.

Output the blocker clearly and stop instead of exploring alternatives.

## 4. Avoid Wasteful Iterations

- Do not run tool discovery for Coplay tools that are already approved in
  `.claude/settings.local.json`.
- Do not bounce between `editor-tools` and `batchmode` in the same pass unless
  the first path fails and you explain the switch.
- Do not update sprint files or `active.md` until the verification result is
  known.
- If a path fails for environmental reasons, report the exact blocker and the
  next cheapest recovery step.

## 5. Output Format

Keep the final verification report short:

```md
Verification path: [editor-tools | batchmode | manual]
Compile status: [clean | errors found | not checked]
Run result: [pass | fail | blocked]
Evidence: [tool or command used]
Next action: [one concrete action]
```

## 6. Cleanup

- Delete any temporary runner script after use.
- If the task is complete, update `production/session-state/active.md` with the
  chosen verification path and the exact next step.
