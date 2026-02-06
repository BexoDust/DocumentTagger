# DocumentTagger auf Linux

Diese Anleitung beschreibt die Installation und Ausführung von **DocumentTagger** auf Linux (getestet auf Ubuntu 20.04+, Debian 11+).

## Systemanforderungen

- **.NET Runtime**: .NET 10.0 SDK oder höher (oder nur Runtime für veröffentlichte Anwendungen)
- **lsof**: Zum Erkennen von Dateisperren
- **Optional** (je nach Konfiguration):
  - `ghostscript` oder `qpdf`: Für PDF-Kompression
  - `tesseract-ocr`: Für OCR-Funktionen
  - `poppler-utils`: Für PDF-Verarbeitung

### Installation der Abhängigkeiten

Auf Debian/Ubuntu:

```bash
# .NET SDK (falls noch nicht installiert)
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version latest

# lsof und weitere Tools
sudo apt-get update
sudo apt-get install -y lsof ghostscript qpdf tesseract-ocr poppler-utils
```

Auf RHEL/CentOS/Fedora:

```bash
# .NET SDK
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version latest

# Tools
sudo dnf install -y lsof ghostscript qpdf tesseract poppler-utils
```

## Build und Veröffentlichung

### 1. Repository klonen und bauen

```bash
git clone <repo-url>
cd DocumentTagger

# Restore und Build (alle Projekte außer DocumentOrganizer/MAUI)
dotnet restore
dotnet build -c Release
```

### 2. Publish-Optionen

#### Option A: Framework-abhängig (benötigt .NET Runtime auf dem Zielrechner)

```bash
dotnet publish DocumentTagger/DocumentTagger.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o ./publish
```

#### Option B: Single-file (eigenständig, einfache Bereitstellung)

```bash
dotnet publish DocumentTagger/DocumentTagger.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  -o ./publish
```

## Service-Installation als systemd

### Automatische Installation mit Skript

```bash
# Skript ausführbar machen (falls noch nicht geschehen)
chmod +x scripts/install-systemd-service.sh

# Service installieren (das Skript erkennt automatisch single-file oder framework-dependent)
./scripts/install-systemd-service.sh ./publish
```

Das Skript wird:
1. Die veröffentlichten Dateien nach `/opt/documenttagger` kopieren
2. Einen Systembenutzer `documenttagger` erstellen (falls nicht vorhanden)
3. Eine systemd-Unit-Datei unter `/etc/systemd/system/documenttagger.service` erzeugen
4. Den Service aktivieren und starten

### Manuelle Installation (falls nötig)

```bash
# Zielverzeichnis vorbereiten
sudo mkdir -p /opt/documenttagger
sudo cp -r ./publish/* /opt/documenttagger/

# Systembenutzer erstellen
sudo useradd --system --no-create-home --shell /usr/sbin/nologin documenttagger || true

# Berechtigungen setzen
sudo chown -R documenttagger:documenttagger /opt/documenttagger

# Unit-Datei erstellen (anpassen je nach Publish-Typ)
# Für framework-dependent:
sudo tee /etc/systemd/system/documenttagger.service > /dev/null <<EOF
[Unit]
Description=Document Tagger Service
After=network.target

[Service]
Type=simple
User=documenttagger
Group=documenttagger
Restart=on-failure
RestartSec=10
ExecStart=/usr/bin/dotnet /opt/documenttagger/DocumentTagger.dll
WorkingDirectory=/opt/documenttagger
StandardOutput=journal
StandardError=journal
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF

# Oder für single-file:
# ExecStart=/opt/documenttagger/DocumentTagger

# Service registrieren und starten
sudo systemctl daemon-reload
sudo systemctl enable documenttagger
sudo systemctl start documenttagger
```

## Konfiguration

### appsettings.json

Bearbeite `/opt/documenttagger/appsettings.json` mit Linux-Pfaden:

```json
{
  "DT": {
    "WatchCompress": "/home/user/Documents/compress",
    "WatchOcr": "/home/user/Documents/ocr",
    "WatchRename": "/home/user/Documents/rename",
    "WatchMove": "/home/user/Documents/move",
    "FolderRenameSuccess": "/home/user/Documents/success",
    "FolderCompressSuccess": "/home/user/Documents/compressed",
    "LogPath": "/var/log/documenttagger/documenttagger.log",
    "CompressorToolPath": "/usr/bin/qpdf",
    "CompressorToolOptions": "--compress-streams=y --object-streams=generate --output-file={0} {1}",
    "OcrToolPath": "/usr/bin/tesseract",
    "OcrToolOptions": "{0} {1} pdf"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Log-Verzeichnis

```bash
sudo mkdir -p /var/log/documenttagger
sudo chown documenttagger:documenttagger /var/log/documenttagger
```

## Service-Verwaltung

### Status prüfen

```bash
sudo systemctl status documenttagger
```

### Logs anzeigen

```bash
# systemd journal
sudo journalctl -u documenttagger -f

# oder die Logdatei direkt
sudo tail -f /var/log/documenttagger/documenttagger.log
```

### Service neu starten

```bash
sudo systemctl restart documenttagger
```

### Service stoppen

```bash
sudo systemctl stop documenttagger
```

### Service entfernen

```bash
sudo systemctl stop documenttagger
sudo systemctl disable documenttagger
sudo rm /etc/systemd/system/documenttagger.service
sudo systemctl daemon-reload
sudo rm -rf /opt/documenttagger
sudo userdel documenttagger || true
```

## Tests ausführen

```bash
# Unit Tests
dotnet test

# Oder nur spezifisches Test-Projekt
dotnet test DocumentTaggerTests/DocumentTaggerTests.csproj
```

## Troubleshooting

### "lsof not found"
Stelle sicher, dass `lsof` installiert ist:
```bash
sudo apt-get install lsof
```

### "Unable to locate dotnet"
Prüfe, ob .NET im PATH ist:
```bash
dotnet --version
```

Falls nicht, füge den Pfad zu `~/.bashrc` oder `~/.profile` hinzu:
```bash
export PATH="/root/.dotnet:$PATH"
source ~/.bashrc
```

### Service startet nicht
Logs prüfen:
```bash
sudo journalctl -u documenttagger -n 50 --no-pager
```

### Dateiberechtigungen-Fehler
Sicherstelle, dass der `documenttagger`-Benutzer Zugriff auf alle Arbeitsverzzeichnisse hat:
```bash
sudo chown -R documenttagger:documenttagger /home/user/Documents/compress
sudo chmod -R 755 /home/user/Documents/compress
```

## Unterschiede zu Windows

- **Service-Manager**: Systemd statt Windows Service Manager
- **Dateien-Locking**: Nutzt `lsof` statt Windows Restart Manager
- **Konfigurationspfade**: Linux-Stil (`/opt/`, `/var/log/`) statt Windows (`C:\Program Files\`)
- **GUI (DocumentOrganizer)**: Nicht auf Linux verfügbar (bleibt Windows-only)

## Weitere Ressourcen

- [.NET auf Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux)
- [systemd Dokumentation](https://systemd.io/)
- [Serilog File Sink](https://github.com/serilog/serilog-sinks-file)
