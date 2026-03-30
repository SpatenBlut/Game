#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
EXE="$SCRIPT_DIR/publish/App2"

echo "=== BRAWLHAVEN Launcher ==="
echo ""

if [ ! -f "$EXE" ]; then
    echo "FEHLER: App2 nicht gefunden in $SCRIPT_DIR/publish/"
    read -p "Enter drücken zum Beenden..."
    exit 1
fi

chmod +x "$EXE"

if ! ldconfig -p | grep -q "libSDL2-2.0"; then
    echo "SDL2 fehlt – wird installiert..."
    sudo apt install -y libsdl2-2.0-0 libsdl2-image-2.0-0
fi

echo "Starte Spiel..."
echo ""
cd "$SCRIPT_DIR/publish"
"$EXE"
