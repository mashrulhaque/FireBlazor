#!/bin/bash
# Strip Firebase credentials from sample files
# Usage: ./scripts/strip-credentials.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
SAMPLE_PROGRAM="$ROOT_DIR/samples/FireBlazor.Sample.Wasm/Program.cs"

echo "Stripping credentials from: $SAMPLE_PROGRAM"

# Detect OS for sed compatibility
if [[ "$OSTYPE" == "darwin"* ]]; then
    SED_INPLACE="sed -i ''"
else
    SED_INPLACE="sed -i"
fi

# Replace credentials with placeholders
$SED_INPLACE \
    -e 's/\.WithProject("[^"]*")/.WithProject("your-project-id")/g' \
    -e 's/\.WithApiKey("[^"]*")/.WithApiKey("your-api-key")/g' \
    -e 's/\.WithAppId("[^"]*")/.WithAppId("your-app-id")/g' \
    -e 's/\.WithAuthDomain("[^"]*")/.WithAuthDomain("your-project.firebaseapp.com")/g' \
    -e 's/\.WithStorageBucket("[^"]*")/.WithStorageBucket("your-project.firebasestorage.app")/g' \
    -e 's/\.WithDatabaseUrl("[^"]*")/.WithDatabaseUrl("https:\/\/your-project.firebasedatabase.app")/g' \
    -e 's/\.ReCaptchaV3("[^"]*")/.ReCaptchaV3("your-recaptcha-site-key")/g' \
    -e 's/\.ReCaptchaEnterprise("[^"]*")/.ReCaptchaEnterprise("your-recaptcha-enterprise-key")/g' \
    "$SAMPLE_PROGRAM"

# Remove backup file on macOS
rm -f "${SAMPLE_PROGRAM}''"

echo "Done! Credentials have been replaced with placeholders."
echo ""
echo "Verify changes:"
grep -E "With(Project|ApiKey|AppId|AuthDomain|StorageBucket|DatabaseUrl)|ReCaptcha" "$SAMPLE_PROGRAM" || true
