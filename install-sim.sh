#!/bin/bash

# AviationApp iOS Simulator Installer
# Run this after GitHub Actions build completes

set -e

REPO="StevoKeano/AviationApp-DMMS"
WORKFLOW_RUN_ID=$(gh run list --repo "$REPO" --limit 1 --json databaseId --jq '.[0].databaseId')
ARTIFACT_URL=$(gh api repos/$REPO/actions/runs/$WORKFLOW_RUN_ID/artifacts --jq '.artifacts[0].archive_download_url')

if [ "$ARTIFACT_URL" = "null" ]; then
    echo "No artifact found. Waiting for build..."
    exit 1
fi

echo "Downloading artifact..."
curl -L -o artifact.zip "$ARTIFACT_URL" -H "Authorization: Bearer $GITHUB_TOKEN"
unzip -o artifact.zip
rm artifact.zip

ARTIFACT_NAME=$(unzip -l ios-simulator.zip | grep -oP '\d{13}_\d+' | head -1)
unzip -o ios-simulator.zip
rm ios-simulator.zip

BOOTED_SIM=$(xcrun simctl list devices available | grep "iPhone" | head -1 | grep -oP '\([A-F0-9-]+\)' | tr -d '()')
echo "Booting simulator: $BOOTED_SIM"
xcrun simctl boot "$BOOTED_SIM" 2>/dev/null || true

APP_PATH=$(find . -name "*.app" -type d | head -1)
echo "Installing $APP_PATH..."
xcrun simctl install "$BOOTED_SIM" "$APP_PATH"

echo "Launching app..."
xcrun simctl launch "$BOOTED_SIM" com.dmms.aviationapp

echo "Done! Check the simulator."
