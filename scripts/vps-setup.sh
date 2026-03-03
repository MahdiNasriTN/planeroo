#!/bin/bash
# ============================================================
# Planeroo VPS First-Time Setup Script
# Ubuntu 24.04 — Run as root
# ============================================================

set -e

echo "=== [1/5] Installing .NET 10 SDK ==="
# Add Microsoft package repo
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

apt-get update -y
apt-get install -y dotnet-sdk-10.0

echo "✅ .NET version: $(dotnet --version)"

echo ""
echo "=== [2/5] Creating deployment directory ==="
mkdir -p /var/www/planeroo-api
chown -R root:root /var/www/planeroo-api

echo ""
echo "=== [3/5] Placing production appsettings ==="
# Copy appsettings.Production.json into the deploy directory.
# This file must be created manually — see appsettings.Production.json.template
# in the backend/scripts folder for the template.
cat > /var/www/planeroo-api/appsettings.Production.json << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=planeroo_prod;Username=planeroo;Password=mahdi;Include Error Detail=false"
  },
  "Jwt": {
    "Secret": "CHANGE_ME_USE_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS",
    "Issuer": "Planeroo",
    "Audience": "PlanerooApp",
    "ExpirationInHours": 2
  },
  "Cors": {
    "AllowedOrigins": [
      "http://138.68.87.52:5000",
      "https://your-domain.com"
    ]
  },
  "BlobStorage": {
    "ConnectionString": "",
    "ContainerName": "planeroo-files"
  },
  "AI": {
    "OpenAIApiKey": "YOUR_OPENAI_API_KEY_HERE",
    "Model": "gpt-4",
    "MaxTokens": 2000,
    "SafetyFilterEnabled": true
  }
}
EOF
chmod 600 /var/www/planeroo-api/appsettings.Production.json
echo "⚠️  Edit /var/www/planeroo-api/appsettings.Production.json with real secrets before starting the service!"

echo ""
echo "=== [4/5] Creating systemd service ==="
cat > /etc/systemd/system/planeroo-api.service << 'EOF'
[Unit]
Description=Planeroo .NET 10 API
After=network.target postgresql.service

[Service]
Type=simple
User=root
WorkingDirectory=/var/www/planeroo-api
ExecStart=/usr/bin/dotnet /var/www/planeroo-api/Planeroo.API.dll
Restart=always
RestartSec=10
KillSignal=SIGINT

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

StandardOutput=syslog
StandardError=syslog
SyslogIdentifier=planeroo-api

[Install]
WantedBy=multi-user.target
EOF

echo ""
echo "=== [5/5] Enabling service ==="
systemctl daemon-reload
systemctl enable planeroo-api
# Don't start yet — appsettings must be filled first

echo ""
echo "============================================================"
echo "Setup complete! Next steps:"
echo ""
echo "1. Edit /var/www/planeroo-api/appsettings.Production.json"
echo "   and fill in your real JWT secret and OpenAI API key."
echo ""
echo "2. Restore the database (run on VPS):"
echo "   PGPASSWORD=mahdi pg_restore -U planeroo -h localhost \\"
echo "   -d planeroo_prod /tmp/planeroo_dev.dump --no-owner"
echo ""
echo "3. Start the service:"
echo "   systemctl start planeroo-api"
echo "   systemctl status planeroo-api"
echo ""
echo "4. Push to main branch on GitHub to trigger first deploy."
echo "============================================================"
