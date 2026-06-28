#!/bin/bash
# Tắt toàn bộ stack demo web: Localtunnel + Node server + MongoDB (Docker).

set -u

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MONGO_NAME="webgame-mongo"

echo "==> Tắt Tunnel (cloudflared/localtunnel)..."
pkill -f "cloudflared tunnel" 2>/dev/null && echo "    Đã tắt cloudflared." || echo "    (không thấy cloudflared)"
pkill -f "localtunnel --port" 2>/dev/null && echo "    Đã tắt localtunnel." || true

echo "==> Tắt Node server..."
if [ -f "$SCRIPT_DIR/.server.pid" ]; then
  kill "$(cat "$SCRIPT_DIR/.server.pid")" 2>/dev/null
  rm -f "$SCRIPT_DIR/.server.pid"
fi
# Quét thêm cho chắc (trường hợp chạy node trực tiếp)
pkill -f "node server.js" 2>/dev/null && echo "    Đã tắt node server." || echo "    (không thấy node server đang chạy)"

echo "==> Tắt MongoDB (Docker)..."
if docker ps --format '{{.Names}}' | grep -qx "$MONGO_NAME"; then
  docker stop "$MONGO_NAME" >/dev/null && echo "    Đã dừng container $MONGO_NAME (dữ liệu được giữ lại)."
else
  echo "    (container $MONGO_NAME không chạy)"
fi

echo ""
echo "Đã tắt tất cả. Dữ liệu save vẫn còn trong container — lần sau ./start-server.sh sẽ dùng lại."
echo "Muốn xóa hẳn MongoDB (mất dữ liệu): docker rm -f $MONGO_NAME"
