#!/bin/bash
# Local CI Build Script - Mirrors GitHub Actions CI Pipeline

set -e  # Exit on any error

# Configuration
CONFIGURATION=${1:-Release}
SKIP_TESTS=${2:-false}

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Get script directory and solution path
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_PATH="$SCRIPT_DIR/src/c-sharp/NotebookAutomation.sln"
TEST_PROJECT_PATH="$SCRIPT_DIR/src/c-sharp/NotebookAutomation.Core.Tests"

function write_step() {
    echo -e "\n${CYAN}=== $1 ===${NC}"
}

function write_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

function write_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

function write_error() {
    echo -e "${RED}❌ $1${NC}"
}

echo -e "${CYAN}🚀 Starting Local CI Build Pipeline${NC}"
echo -e "${YELLOW}Configuration: $CONFIGURATION${NC}"
echo -e "${YELLOW}Solution: $SOLUTION_PATH${NC}"

# Step 1: Restore Dependencies (mirrors CI)
write_step "Step 1: Restore Dependencies"
dotnet restore "$SOLUTION_PATH"
write_success "Dependencies restored successfully"

# Step 2: Build Solution (mirrors CI)
write_step "Step 2: Build Solution"
dotnet build "$SOLUTION_PATH" --configuration "$CONFIGURATION" --no-restore
write_success "Build completed successfully"

# Step 3: Run Tests (mirrors CI)
if [ "$SKIP_TESTS" != "true" ]; then
    write_step "Step 3: Run Tests with Coverage"
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    dotnet test "$TEST_PROJECT_PATH" \
        --configuration "$CONFIGURATION" \
        --no-build \
        --logger "trx;LogFileName=test-results.trx" \
        --collect:"XPlat Code Coverage"
    write_success "All tests passed"
else
    write_warning "Skipping tests"
fi

# Step 4: Test Publish Operations (mirrors CI publish steps)
write_step "Step 4: Test Publish Operations"
CLI_PROJECT_PATH="$SCRIPT_DIR/src/c-sharp/NotebookAutomation.Cli/NotebookAutomation.Cli.csproj"
TEMP_PUBLISH_DIR="$SCRIPT_DIR/temp_publish_test"

# Test win-x64 publish (mirrors CI)
echo -e "${YELLOW}Testing win-x64 publish...${NC}"
dotnet publish "$CLI_PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -r win-x64 \
    /p:PublishSingleFile=true \
    /p:SelfContained=true \
    --output "$TEMP_PUBLISH_DIR/win-x64"

# Test win-arm64 publish (mirrors CI)
echo -e "${YELLOW}Testing win-arm64 publish...${NC}"
dotnet publish "$CLI_PROJECT_PATH" \
    -c "$CONFIGURATION" \
    -r win-arm64 \
    /p:PublishSingleFile=true \
    /p:SelfContained=true \
    --output "$TEMP_PUBLISH_DIR/win-arm64"

write_success "Publish operations completed successfully"

# Clean up temp publish directory
if [ -d "$TEMP_PUBLISH_DIR" ]; then
    rm -rf "$TEMP_PUBLISH_DIR"
fi

# Step 5: Run Static Code Analysis (mirrors CI - this runs last)
write_step "Step 5: Static Code Analysis"
if ! dotnet format "$SOLUTION_PATH" --verify-no-changes --severity error; then
    write_error "Code formatting issues detected!"
    echo -e "${YELLOW}Run 'dotnet format $SOLUTION_PATH' to fix formatting issues${NC}"
    exit 1
fi
write_success "Static code analysis passed"

# Success Summary
echo -e "\n${GREEN}🎉 LOCAL CI BUILD PIPELINE COMPLETED SUCCESSFULLY! 🎉${NC}"
echo -e "${GREEN}All steps that run in GitHub Actions CI have passed locally.${NC}"
echo -e "${GREEN}Your changes should pass CI when pushed to the repository.${NC}"

echo -e "\n${CYAN}Build completed at: $(date)${NC}"
