#!/bin/bash
# Claude Code PreCompact hook: keep only the minimum recovery context inline.

echo "=== SESSION STATE BEFORE COMPACTION ==="
echo "Timestamp: $(date)"

STATE_FILE="production/session-state/active.md"
if [ -f "$STATE_FILE" ]; then
    echo ""
    echo "## Active Session State ($STATE_FILE)"
    STATE_LINES=$(wc -l < "$STATE_FILE" 2>/dev/null | tr -d ' ')
    if [ "${STATE_LINES:-0}" -gt 40 ]; then
        head -n 40 "$STATE_FILE"
        echo "... (truncated - $STATE_LINES total lines)"
    else
        cat "$STATE_FILE"
    fi
else
    echo ""
    echo "## No active session state file found"
    echo "Template: .claude/docs/session-state-template.md"
fi

echo ""
echo "## Files Modified (git working tree)"
CHANGED=$(git diff --name-only 2>/dev/null)
STAGED=$(git diff --staged --name-only 2>/dev/null)
UNTRACKED=$(git ls-files --others --exclude-standard 2>/dev/null)

if [ -n "$CHANGED" ]; then
    echo "Unstaged:"
    echo "$CHANGED" | while read -r f; do echo "  - $f"; done
fi
if [ -n "$STAGED" ]; then
    echo "Staged:"
    echo "$STAGED" | while read -r f; do echo "  - $f"; done
fi
if [ -n "$UNTRACKED" ]; then
    echo "Untracked:"
    echo "$UNTRACKED" | while read -r f; do echo "  - $f"; done
fi
if [ -z "$CHANGED" ] && [ -z "$STAGED" ] && [ -z "$UNTRACKED" ]; then
    echo "  (no uncommitted changes)"
fi

SESSION_LOG_DIR="production/session-logs"
mkdir -p "$SESSION_LOG_DIR" 2>/dev/null
echo "Context compaction occurred at $(date)." >> "$SESSION_LOG_DIR/compaction-log.txt" 2>/dev/null

echo ""
echo "## Recovery"
echo "After compaction: read active.md, then project-map, then the exact files in focus."
echo "=== END SESSION STATE ==="

exit 0
