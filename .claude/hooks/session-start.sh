#!/bin/bash
# Claude Code SessionStart hook: emit a minimal recovery-oriented context block.

echo "=== Session Context ==="

BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
if [ -n "$BRANCH" ]; then
    echo "Branch: $BRANCH"
fi

LATEST_SPRINT=$(ls -t production/sprints/sprint-*.md 2>/dev/null | head -1)
if [ -n "$LATEST_SPRINT" ]; then
    echo "Sprint: $(basename "$LATEST_SPRINT" .md)"
fi

STATE_FILE="production/session-state/active.md"
if [ -f "$STATE_FILE" ]; then
    echo "State: $STATE_FILE"
    echo "--- active.md preview ---"
    head -12 "$STATE_FILE" 2>/dev/null
    TOTAL_LINES=$(wc -l < "$STATE_FILE" 2>/dev/null | tr -d ' ')
    if [ "${TOTAL_LINES:-0}" -gt 12 ]; then
        echo "... ($TOTAL_LINES lines total)"
    fi
else
    echo "State: missing"
    echo "Template: .claude/docs/session-state-template.md"
fi

echo "Recovery order: active.md -> project-map -> decision-log -> exact files."
echo "==================================="

exit 0
