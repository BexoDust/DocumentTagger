#!/usr/bin/env bash
set -euo pipefail

# Usage: ./scripts/install-systemd-service.sh <publish-dir>
# Example: ./scripts/install-systemd-service.sh ./DocumentTagger/bin/Release/net10.0/publish

PUBLISH_DIR="${1:-./publish}"
SERVICE_NAME=documenttagger
INSTALL_DIR=/opt/documenttagger
USER=documenttagger

if [ ! -d "$PUBLISH_DIR" ]; then
  echo "Publish directory '$PUBLISH_DIR' not found. Run: dotnet publish -c Release -r linux-x64 --self-contained false -o $PUBLISH_DIR"
  exit 1
fi

echo "Installing DocumentTagger from $PUBLISH_DIR to $INSTALL_DIR"

sudo mkdir -p "$INSTALL_DIR"
sudo cp -r "$PUBLISH_DIR"/* "$INSTALL_DIR/"

if ! id -u "$USER" >/dev/null 2>&1; then
  echo "Creating system user $USER"
  sudo useradd --system --no-create-home --shell /usr/sbin/nologin "$USER" || true
fi

sudo chown -R "$USER":"$USER" "$INSTALL_DIR"

# Detect single-file publish (executable) vs framework-dependent (DLL)
if [ -x "$PUBLISH_DIR/DocumentTagger" ]; then
  EXEC_START="$INSTALL_DIR/DocumentTagger"
elif [ -f "$PUBLISH_DIR/DocumentTagger" ] && [ -x "$PUBLISH_DIR/DocumentTagger" ]; then
  EXEC_START="$INSTALL_DIR/DocumentTagger"
elif [ -f "$PUBLISH_DIR/DocumentTagger.dll" ]; then
  EXEC_START="/usr/bin/dotnet $INSTALL_DIR/DocumentTagger.dll"
else
  echo "Cannot detect start command in publish directory. Expected DocumentTagger (executable) or DocumentTagger.dll"
  exit 1
fi

UNIT_PATH="/etc/systemd/system/${SERVICE_NAME}.service"
echo "Writing systemd unit to $UNIT_PATH (ExecStart: $EXEC_START)"
sudo tee "$UNIT_PATH" > /dev/null <<EOF
[Unit]
Description=Document Tagger Service
After=network.target

[Service]
Type=simple
User=$USER
Group=$USER
Restart=on-failure
RestartSec=10
ExecStart=$EXEC_START
WorkingDirectory=$INSTALL_DIR
StandardOutput=journal
StandardError=journal
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl start "$SERVICE_NAME"

echo "Service $SERVICE_NAME installed and started. Check status with: sudo systemctl status $SERVICE_NAME"
