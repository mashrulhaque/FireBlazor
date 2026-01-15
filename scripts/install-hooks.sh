#!/bin/bash
# Install git hooks for credential protection
# Usage: ./scripts/install-hooks.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
HOOKS_DIR="$ROOT_DIR/.git/hooks"

echo "Installing git hooks..."

# Create pre-commit hook
cat > "$HOOKS_DIR/pre-commit" << 'EOF'
#!/bin/bash
# Pre-commit hook: Check for leaked credentials

SAMPLE_FILE="samples/FireBlazor.Sample.Wasm/Program.cs"

# Check if sample file is staged
if git diff --cached --name-only | grep -q "$SAMPLE_FILE"; then
    # Check for real Firebase credentials (API keys start with AIzaSy)
    if git diff --cached -- "$SAMPLE_FILE" | grep -E "AIzaSy[a-zA-Z0-9_-]{33}"; then
        echo "ERROR: Firebase API key detected in staged changes!"
        echo "Run './scripts/strip-credentials.sh' before committing."
        exit 1
    fi

    # Check for real project IDs (contain numbers typically)
    if git diff --cached -- "$SAMPLE_FILE" | grep -E '\.WithProject\("[a-z]+-[0-9]+"\)'; then
        echo "ERROR: Real Firebase project ID detected in staged changes!"
        echo "Run './scripts/strip-credentials.sh' before committing."
        exit 1
    fi
fi

exit 0
EOF

chmod +x "$HOOKS_DIR/pre-commit"

echo "Git hooks installed successfully!"
echo ""
echo "The pre-commit hook will now:"
echo "  - Block commits containing Firebase API keys"
echo "  - Block commits containing real project IDs"
echo ""
echo "To strip credentials before committing, run:"
echo "  ./scripts/strip-credentials.sh"
