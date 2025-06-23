#!/bin/bash

# Cross-Platform Local CI Build Script for Notebook Automation
# This script mimics the GitHub Actions CI build process for local development
# Supports Linux and macOS

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_separator() {
    echo "=============================================================="
}

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
CSHARP_ROOT="$PROJECT_ROOT/src/c-sharp"
SOLUTION_FILE="$CSHARP_ROOT/NotebookAutomation.sln"
CLI_PROJECT="$CSHARP_ROOT/NotebookAutomation.Cli/NotebookAutomation.Cli.csproj"
TEST_PROJECT="$CSHARP_ROOT/NotebookAutomation.Tests/NotebookAutomation.Tests.csproj"

# Default values
CONFIGURATION="Release"
SKIP_TESTS=false
SKIP_PUBLISH=false
SKIP_COVERAGE=false
CLEAN_BUILD=false
VERBOSE=false

# Detect OS and architecture
detect_platform() {
    case "$OSTYPE" in
        linux*)
            OS="linux"
            if [[ $(uname -m) == "aarch64" || $(uname -m) == "arm64" ]]; then
                ARCH="arm64"
                RUNTIME_ID="linux-arm64"
            else
                ARCH="x64"
                RUNTIME_ID="linux-x64"
            fi
            EXECUTABLE_EXT=""
            ;;
        darwin*)
            OS="osx"
            if [[ $(uname -m) == "arm64" ]]; then
                ARCH="arm64"
                RUNTIME_ID="osx-arm64"
            else
                ARCH="x64"
                RUNTIME_ID="osx-x64"
            fi
            EXECUTABLE_EXT=""
            ;;
        *)
            log_error "Unsupported operating system: $OSTYPE"
            exit 1
            ;;
    esac
    
    log_info "Detected platform: $OS-$ARCH (Runtime ID: $RUNTIME_ID)"
}

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_help
                exit 0
                ;;
            -c|--configuration)
                CONFIGURATION="$2"
                shift 2
                ;;
            --skip-tests)
                SKIP_TESTS=true
                shift
                ;;
            --skip-publish)
                SKIP_PUBLISH=true
                shift
                ;;
            --skip-coverage)
                SKIP_COVERAGE=true
                shift
                ;;
            --clean)
                CLEAN_BUILD=true
                shift
                ;;
            -v|--verbose)
                VERBOSE=true
                shift
                ;;
            *)
                log_error "Unknown parameter: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

show_help() {
    cat << EOF
Cross-Platform Local CI Build Script for Notebook Automation

Usage: $0 [OPTIONS]

Options:
    -h, --help              Show this help message
    -c, --configuration     Build configuration (Debug|Release) [default: Release]
    --skip-tests           Skip running tests
    --skip-publish         Skip publishing executables
    --skip-coverage        Skip code coverage collection
    --clean                Clean build (remove bin/obj folders first)
    -v, --verbose          Enable verbose output

Examples:
    $0                              # Full build with all steps
    $0 -c Debug                     # Debug build
    $0 --skip-tests --skip-coverage # Build and publish only
    $0 --clean -v                   # Clean verbose build

EOF
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if we're in the right directory
    if [[ ! -f "$SOLUTION_FILE" ]]; then
        log_error "Solution file not found: $SOLUTION_FILE"
        log_error "Please run this script from the project root or scripts directory"
        exit 1
    fi
    
    # Check for .NET
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET CLI not found. Please install .NET 9.0 or later"
        exit 1
    fi
    
    # Check .NET version
    local dotnet_version
    dotnet_version=$(dotnet --version)
    log_info "Found .NET version: $dotnet_version"
    
    # Check if version is 9.0 or later
    if [[ ! "$dotnet_version" =~ ^9\. ]]; then
        log_warning "This project targets .NET 9.0, but found version $dotnet_version"
    fi
    
    log_success "Prerequisites check completed"
}

# Set build version
set_build_version() {
    log_info "Setting build version..."
    
    # Calculate build number (days since Jan 1, 2024)
    local base_date current_date days_since_base
    base_date=$(date -d "2024-01-01" +%s 2>/dev/null || date -j -f "%Y-%m-%d" "2024-01-01" +%s)
    current_date=$(date +%s)
    days_since_base=$(( (current_date - base_date) / 86400 ))
    
    # Use current timestamp as revision for local builds
    local revision
    revision=$(date +%s)
    
    # Format version strings
    local major=1
    local minor=0
    
    export PACKAGE_VERSION="$major.$minor.0"
    export ASSEMBLY_VERSION="$major.$minor.0.0" 
    export FILE_VERSION="$major.$minor.$days_since_base.$revision"
    
    log_info "Package Version: $PACKAGE_VERSION"
    log_info "Assembly Version: $ASSEMBLY_VERSION"
    log_info "File Version: $FILE_VERSION"
}

# Clean build artifacts
clean_build_artifacts() {
    if [[ "$CLEAN_BUILD" == true ]]; then
        log_info "Cleaning build artifacts..."
        
        # Remove bin and obj directories
        find "$CSHARP_ROOT" -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
        find "$CSHARP_ROOT" -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
        
        # Remove test results and coverage reports
        rm -rf "$PROJECT_ROOT/TestResults" 2>/dev/null || true
        rm -rf "$PROJECT_ROOT/CoverageReport" 2>/dev/null || true
        rm -rf "$PROJECT_ROOT/publish" 2>/dev/null || true
        
        log_success "Build artifacts cleaned"
    fi
}

# Restore dependencies
restore_dependencies() {
    log_info "Restoring NuGet packages..."
    
    local restore_args=""
    if [[ "$VERBOSE" == true ]]; then
        restore_args="--verbosity detailed"
    fi
    
    if ! dotnet restore "$SOLUTION_FILE" $restore_args; then
        log_error "Failed to restore dependencies"
        exit 1
    fi
    
    log_success "Dependencies restored successfully"
}

# Build solution
build_solution() {
    log_info "Building solution in $CONFIGURATION configuration..."
    
    local build_args=(
        "$SOLUTION_FILE"
        "--configuration" "$CONFIGURATION"
        "--no-restore"
        "/p:Version=$PACKAGE_VERSION"
        "/p:FileVersion=$FILE_VERSION"
        "/p:AssemblyVersion=$ASSEMBLY_VERSION"
        "/p:InformationalVersion=$FILE_VERSION"
    )
    
    if [[ "$VERBOSE" == true ]]; then
        build_args+=("--verbosity" "detailed")
    fi
    
    if ! dotnet build "${build_args[@]}"; then
        log_error "Build failed"
        exit 1
    fi
    
    log_success "Build completed successfully"
}

# Run tests
run_tests() {
    if [[ "$SKIP_TESTS" == true ]]; then
        log_warning "Skipping tests (--skip-tests specified)"
        return 0
    fi
    
    log_info "Running tests..."
    
    # Create test results directory
    mkdir -p "$PROJECT_ROOT/TestResults"
    
    local test_args=(
        "$SOLUTION_FILE"
        "--configuration" "$CONFIGURATION"
        "--no-build"
        "--logger" "trx;LogFileName=test-results.trx"
        "--results-directory" "$PROJECT_ROOT/TestResults/"
    )
    
    # Add coverage collection if not skipped
    if [[ "$SKIP_COVERAGE" != true ]]; then
        test_args+=(
            "--collect:XPlat Code Coverage"
            "--settings" "$PROJECT_ROOT/coverlet.runsettings"
        )
    fi
    
    if [[ "$VERBOSE" == true ]]; then
        test_args+=("--verbosity" "detailed")
    fi
    
    if ! DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet test "${test_args[@]}"; then
        log_error "Tests failed"
        exit 1
    fi
    
    log_success "Tests completed successfully"
}

# Generate coverage report
generate_coverage_report() {
    if [[ "$SKIP_COVERAGE" == true ]] || [[ "$SKIP_TESTS" == true ]]; then
        log_warning "Skipping coverage report generation"
        return 0
    fi
    
    log_info "Generating coverage report..."
    
    # Check if coverage files exist
    local coverage_files
    coverage_files=$(find "$PROJECT_ROOT/TestResults" -name "coverage.cobertura.xml" 2>/dev/null)
    
    if [[ -z "$coverage_files" ]]; then
        log_warning "No coverage files found, skipping report generation"
        return 0
    fi
    
    # Check if reportgenerator tool is available
    if ! command -v reportgenerator &> /dev/null; then
        log_info "Installing ReportGenerator tool..."
        if ! dotnet tool install -g dotnet-reportgenerator-globaltool; then
            log_warning "Failed to install ReportGenerator, skipping coverage report"
            return 0
        fi
    fi
    
    # Generate coverage report
    local report_args=(
        "-reports:$PROJECT_ROOT/TestResults/**/coverage.cobertura.xml"
        "-targetdir:$PROJECT_ROOT/CoverageReport"
        "-reporttypes:HtmlInline;Cobertura;TextSummary"
        "-assemblyfilters:+NotebookAutomation.*;-*.Tests"
        "-classfilters:+*;-*Test*"
        "-title:Notebook Automation Code Coverage Report (Local $OS-$ARCH)"
    )
    
    if ! reportgenerator "${report_args[@]}"; then
        log_warning "Failed to generate coverage report"
        return 0
    fi
    
    # Display text summary if available
    if [[ -f "$PROJECT_ROOT/CoverageReport/Summary.txt" ]]; then
        log_info "Coverage Summary:"
        cat "$PROJECT_ROOT/CoverageReport/Summary.txt"
    fi
    
    log_success "Coverage report generated at: $PROJECT_ROOT/CoverageReport/index.html"
}

# Publish executables
publish_executables() {
    if [[ "$SKIP_PUBLISH" == true ]]; then
        log_warning "Skipping publish (--skip-publish specified)"
        return 0
    fi
    
    log_info "Publishing single-file executable for $RUNTIME_ID..."
    
    local publish_dir="$PROJECT_ROOT/publish/$RUNTIME_ID"
    mkdir -p "$publish_dir"
    
    local publish_args=(
        "$CLI_PROJECT"
        "-c" "$CONFIGURATION"
        "-r" "$RUNTIME_ID"
        "/p:PublishSingleFile=true"
        "/p:SelfContained=true"
        "/p:Version=$PACKAGE_VERSION"
        "/p:FileVersion=$FILE_VERSION"
        "/p:AssemblyVersion=$ASSEMBLY_VERSION"
        "/p:InformationalVersion=$FILE_VERSION"
        "--output" "$publish_dir"
    )
    
    if [[ "$VERBOSE" == true ]]; then
        publish_args+=("--verbosity" "detailed")
    fi
    
    if ! dotnet publish "${publish_args[@]}"; then
        log_error "Publish failed"
        exit 1
    fi
    
    # Test the published executable
    local executable_path="$publish_dir/na$EXECUTABLE_EXT"
    if [[ -f "$executable_path" ]]; then
        log_info "Testing published executable..."
        if ! "$executable_path" --version; then
            log_warning "Published executable test failed"
        else
            log_success "Published executable test passed"
        fi
        
        # Show file size
        local file_size
        if command -v stat &> /dev/null; then
            if [[ "$OS" == "osx" ]]; then
                file_size=$(stat -f%z "$executable_path")
            else
                file_size=$(stat -c%s "$executable_path")
            fi
            local size_mb=$((file_size / 1024 / 1024))
            log_info "Executable size: ${size_mb}MB"
        fi
    else
        log_error "Published executable not found: $executable_path"
        exit 1
    fi
    
    log_success "Publish completed: $publish_dir"
}

# Main execution
main() {
    print_separator
    log_info "Starting Cross-Platform Local CI Build"
    log_info "Project: Notebook Automation"
    log_info "Script: $(basename "$0")"
    print_separator
    
    detect_platform
    parse_arguments "$@"
    check_prerequisites
    set_build_version
    clean_build_artifacts
    
    print_separator
    restore_dependencies
    build_solution
    run_tests
    generate_coverage_report
    publish_executables
    
    print_separator
    log_success "Build completed successfully!"
    
    # Summary
    log_info "Build Summary:"
    log_info "  Platform: $OS-$ARCH"
    log_info "  Configuration: $CONFIGURATION"
    log_info "  Version: $FILE_VERSION"
    
    if [[ "$SKIP_TESTS" != true ]]; then
        if [[ -f "$PROJECT_ROOT/TestResults/test-results.trx" ]]; then
            log_info "  Test Results: $PROJECT_ROOT/TestResults/"
        fi
    fi
    
    if [[ "$SKIP_COVERAGE" != true ]] && [[ "$SKIP_TESTS" != true ]]; then
        if [[ -f "$PROJECT_ROOT/CoverageReport/index.html" ]]; then
            log_info "  Coverage Report: $PROJECT_ROOT/CoverageReport/index.html"
        fi
    fi
    
    if [[ "$SKIP_PUBLISH" != true ]]; then
        if [[ -d "$PROJECT_ROOT/publish/$RUNTIME_ID" ]]; then
            log_info "  Published Executable: $PROJECT_ROOT/publish/$RUNTIME_ID/"
        fi
    fi
    
    print_separator
}

# Run main function with all arguments
main "$@"
