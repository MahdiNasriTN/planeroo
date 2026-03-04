#!/bin/bash
set -e

echo "=== Writing appsettings.Production.json ==="
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
    "Secret": "PlanerooVPSProductionSecretKey2026XYZ",
    "Issuer": "Planeroo",
    "Audience": "PlanerooApp",
    "ExpirationInHours": 2
  },
  "Cors": {
    "AllowedOrigins": ["http://138.68.87.52", "http://138.68.87.52:80"]
  },
  "BlobStorage": {
    "ConnectionString": "",
    "ContainerName": "planeroo-files"
  },
  "AI": {
    "OpenAIApiKey": "",
    "Model": "gpt-4",
    "MaxTokens": 2000,
    "SafetyFilterEnabled": true
  }
}
EOF
chmod 600 /var/www/planeroo-api/appsettings.Production.json
echo "✅ appsettings.Production.json written"

echo ""
echo "=== Installing Nginx ==="
apt-get install -y nginx

echo ""
echo "=== Configuring Nginx reverse proxy ==="
cat > /etc/nginx/sites-available/planeroo-api << 'EOF'
server {
    listen 80;
    server_name 138.68.87.52;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        client_max_body_size 20M;
    }
}
EOF

ln -sf /etc/nginx/sites-available/planeroo-api /etc/nginx/sites-enabled/planeroo-api
rm -f /etc/nginx/sites-enabled/default

nginx -t
systemctl restart nginx
systemctl enable nginx
echo "✅ Nginx configured on port 80"

echo ""
echo "=== Starting planeroo-api service ==="
systemctl restart planeroo-api
sleep 4
systemctl status planeroo-api --no-pager

echo ""
echo "=== Opening firewall ports ==="
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable
echo "✅ Firewall set"

echo ""
echo "================================================"
echo "✅ All done! Test at: http://138.68.87.52/swagger"
echo "================================================"
