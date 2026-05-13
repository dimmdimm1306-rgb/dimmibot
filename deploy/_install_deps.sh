#!/bin/bash
# Install all system dependencies on VPS (Ubuntu).
# Run as root.

set -e

echo "==================================================="
echo " INSTALL DEPENDENCIES on VPS"
echo "==================================================="
echo ""

# Update apt
echo "[1/6] apt update..."
apt update -qq

# Node.js 20
if ! command -v node &> /dev/null || [ "$(node -v | cut -c2)" -lt 20 ]; then
    echo "[2/6] Installing Node.js 20..."
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - > /dev/null 2>&1
    apt install -y nodejs
else
    echo "[2/6] Node.js already installed: $(node -v)"
fi

# Python 3
echo "[3/6] Installing Python 3 + venv..."
apt install -y -qq python3 python3-pip python3-venv python3-full

# cloudflared
if ! command -v cloudflared &> /dev/null; then
    echo "[4/6] Installing cloudflared..."
    curl -sL https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb -o /tmp/cf.deb
    dpkg -i /tmp/cf.deb
    rm /tmp/cf.deb
else
    echo "[4/6] cloudflared already installed: $(cloudflared --version)"
fi

# Git
echo "[5/6] Installing git..."
apt install -y -qq git

# Swap (kalau < 2GB RAM, tambah swap)
echo "[6/6] Checking swap..."
if [ "$(free -m | awk '/^Swap:/ {print $2}')" -lt 1000 ]; then
    echo "  Adding 2GB swap..."
    fallocate -l 2G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    if ! grep -q '/swapfile' /etc/fstab; then
        echo '/swapfile none swap sw 0 0' >> /etc/fstab
    fi
    echo "  Swap added."
else
    echo "  Swap already OK: $(free -h | awk '/^Swap:/ {print $2}')"
fi

echo ""
echo "==================================================="
echo " VERSIONS"
echo "==================================================="
echo "Node:        $(node -v)"
echo "npm:         $(npm -v)"
echo "Python:      $(python3 --version)"
echo "cloudflared: $(cloudflared --version 2>&1 | head -1)"
echo "git:         $(git --version)"
echo ""
echo "DONE"
