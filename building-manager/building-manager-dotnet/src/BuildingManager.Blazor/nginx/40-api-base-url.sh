#!/bin/sh
set -eu

if [ -n "${API_BASE_URL:-}" ]; then
  root=/usr/share/nginx/html
  printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$API_BASE_URL" > "$root/appsettings.json"
  rm -f "$root/appsettings.json.gz" "$root/appsettings.json.br"
  echo "Client configured for API at $API_BASE_URL"
fi
