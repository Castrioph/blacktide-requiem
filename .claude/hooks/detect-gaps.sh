#!/bin/bash
# Hook: detect-gaps.sh
# Event: SessionStart
# Purpose: emit only actionable documentation gaps to keep startup context small.

set +e

SOURCE_ROOT=""
if [ -d "Assets/Scripts" ]; then
    SOURCE_ROOT="Assets/Scripts"
elif [ -d "src" ]; then
    SOURCE_ROOT="src"
fi

emit_header=false
emit_line() {
    if [ "$emit_header" = false ]; then
        echo "=== Documentation Gaps ==="
        emit_header=true
    fi
    echo "$1"
}

if [ -n "$SOURCE_ROOT" ]; then
    SRC_FILES=$(find "$SOURCE_ROOT" -type f \( -name "*.cs" -o -name "*.gd" -o -name "*.cpp" -o -name "*.c" -o -name "*.h" -o -name "*.hpp" -o -name "*.rs" -o -name "*.py" -o -name "*.js" -o -name "*.ts" \) 2>/dev/null | wc -l | tr -d ' ')
else
    SRC_FILES=0
fi

if [ -d "design/gdd" ]; then
    DESIGN_FILES=$(find design/gdd -type f -name "*.md" 2>/dev/null | wc -l | tr -d ' ')
else
    DESIGN_FILES=0
fi

if [ "${SRC_FILES:-0}" -gt 50 ] && [ "${DESIGN_FILES:-0}" -lt 5 ]; then
    emit_line "Sparse design docs for code size: $SOURCE_ROOT has $SRC_FILES source files, design/gdd has $DESIGN_FILES docs."
fi

if [ "${SRC_FILES:-0}" -gt 100 ] && [ ! -d "production/sprints" ] && [ ! -d "production/milestones" ]; then
    emit_line "Large codebase with no production planning detected. Consider /sprint-plan."
fi

if [ "$emit_header" = true ]; then
    echo "Tip: run /project-stage-detect for a deeper pass."
    echo "=========================="
fi

exit 0
