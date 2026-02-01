#!/bin/bash
# Script to test DWSIM on ARM64 architecture using Docker
# This script builds and runs the ARM64 container to test DWSIM headless features

set -e

echo "=========================================="
echo "DWSIM ARM64 Compatibility Test"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Error: Docker is not installed${NC}"
    exit 1
fi

# Check if Docker buildx is available for multi-platform builds
if ! docker buildx version &> /dev/null; then
    echo -e "${YELLOW}Warning: Docker buildx not available. Installing...${NC}"
    docker buildx create --use
fi

echo -e "${GREEN}Step 1: Setting up QEMU for ARM64 emulation${NC}"
docker run --rm --privileged multiarch/qemu-user-static --reset -p yes

echo ""
echo -e "${GREEN}Step 2: Building ARM64 Docker image${NC}"
echo "This may take several minutes..."
docker buildx build \
    --platform linux/arm64 \
    -f Dockerfile.arm-test \
    -t enerflow-dwsim-arm-test:latest \
    --load \
    .

echo ""
echo -e "${GREEN}Step 3: Running Test05_FlashAlgorithmComparison on ARM64${NC}"
echo "=========================================="

# Create TestResults directory if it doesn't exist
mkdir -p TestResults

# Run the container
docker run --rm \
    --platform linux/arm64 \
    -v "$(pwd)/TestResults:/app/TestResults" \
    enerflow-dwsim-arm-test:latest

echo ""
echo "=========================================="
echo -e "${GREEN}Test completed!${NC}"
echo "Check TestResults/ directory for detailed logs"
echo "=========================================="
