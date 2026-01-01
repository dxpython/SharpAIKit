#!/bin/bash

# Test script for all SharpAIKit samples
# This script tests compilation and basic functionality of all sample projects

set -e  # Exit on error

API_KEY="YOUR-API-KEY"
SAMPLES_DIR="samples"
PROJECT_ROOT="/Users/huidongdezhizhen/Desktop/SharpAIKit"

cd "$PROJECT_ROOT"

echo "🧪 Testing SharpAIKit Samples"
echo "================================"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test results
PASSED=0
FAILED=0

# Function to test a sample
test_sample() {
    local sample_name=$1
    local sample_dir="$SAMPLES_DIR/$sample_name"
    
    echo -e "${YELLOW}Testing: $sample_name${NC}"
    
    if [ ! -d "$sample_dir" ]; then
        echo -e "${RED}✗ $sample_name: Directory not found${NC}\n"
        ((FAILED++))
        return 1
    fi
    
    cd "$sample_dir"
    
    # Clean and restore
    echo "  Cleaning..."
    dotnet clean -q 2>/dev/null || true
    
    # Restore packages
    echo "  Restoring packages..."
    if ! dotnet restore -q 2>&1 | grep -v "warning"; then
        echo -e "${RED}✗ $sample_name: Restore failed${NC}\n"
        ((FAILED++))
        cd "$PROJECT_ROOT"
        return 1
    fi
    
    # Build
    echo "  Building..."
    if ! dotnet build -c Release -q --no-restore 2>&1 | grep -v "warning"; then
        echo -e "${RED}✗ $sample_name: Build failed${NC}\n"
        ((FAILED++))
        cd "$PROJECT_ROOT"
        return 1
    fi
    
    echo -e "${GREEN}✓ $sample_name: Build successful${NC}\n"
    ((PASSED++))
    
    cd "$PROJECT_ROOT"
    return 0
}

# Test all samples
echo "Testing compilation of all samples..."
echo ""

test_sample "ChatDemo"
test_sample "AgentDemo"
test_sample "RAGDemo"
test_sample "AdvancedFeaturesDemo"
test_sample "KillerFeaturesDemo"

# Summary
echo "================================"
echo -e "${GREEN}Passed: $PASSED${NC}"
if [ $FAILED -gt 0 ]; then
    echo -e "${RED}Failed: $FAILED${NC}"
else
    echo -e "${GREEN}Failed: $FAILED${NC}"
fi
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All samples compiled successfully!${NC}"
    exit 0
else
    echo -e "${RED}❌ Some samples failed to compile${NC}"
    exit 1
fi

