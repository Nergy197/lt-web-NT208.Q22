#!/bin/bash
# Chạy toàn bộ stack cho demo web: MongoDB (Docker) + Node server + Localtunnel.
# Dùng stop-server.sh để tắt tất cả.

set -u

# Luôn chạy từ thư mục chứa script (an toàn dù gọi từ đâu)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

MONGO_NAME="webgame-mongo"
MONGO_IMAGE="mongo:7"
PORT=3000

echo "==> [1/3] Khởi động MongoDB (Docker)..."
if ! docker info >/dev/null 2>&1; then
  echo "LỖI: Docker daemon chưa chạy. Hãy mở Docker Desktop rồi chạy lại."
  exit 1
fi

if docker ps --format '{{.Names}}' | grep -qx "$MONGO_NAME"; then
  echo "    MongoDB đang chạy sẵn ($MONGO_NAME)."
elif docker ps -a --format '{{.Names}}' | grep -qx "$MONGO_NAME"; then
  echo "    Bật lại container cũ $MONGO_NAME..."
  docker start "$MONGO_NAME" >/dev/null
else
  echo "    Tạo container mới $MONGO_NAME..."
  docker run -d -p 27017:27017 --name "$MONGO_NAME" "$MONGO_IMAGE" >/dev/null
fi

echo "    Đang chờ MongoDB sẵn sàng..."
for i in $(seq 1 30); do
  if docker exec "$MONGO_NAME" mongosh --quiet --eval "db.runCommand({ ping: 1 })" >/dev/null 2>&1; then
    echo "    MongoDB đã sẵn sàng."
    break
  fi
  sleep 1
done

echo "==> [2/3] Khởi động Node server..."
cd "$SCRIPT_DIR/Backend"
node server.js &
SERVER_PID=$!
echo "$SERVER_PID" > "$SCRIPT_DIR/.server.pid"

# Chờ server lên
sleep 2

echo "==> [3/3] Khởi động Cloudflare Tunnel..."

CF_LOG="$SCRIPT_DIR/.cloudflare.log"
: > "$CF_LOG"

# Cloudflare quick tunnel (URL ngẫu nhiên mỗi lần). Ghi output ra file để lấy URL.
cloudflared tunnel --url "http://localhost:$PORT" > "$CF_LOG" 2>&1 &
CF_PID=$!
echo "$CF_PID" > "$SCRIPT_DIR/.cloudflared.pid"

# Ctrl+C: kill node server + cloudflared (Mongo giữ nguyên — dùng stop-server.sh để tắt hẳn)
trap 'echo; echo "Đang dừng..."; kill "$SERVER_PID" "$CF_PID" 2>/dev/null; rm -f "$SCRIPT_DIR/.server.pid" "$SCRIPT_DIR/.cloudflared.pid"; exit 0' INT TERM

# Chờ và trích link công khai
echo "    Đang lấy link công khai..."
PUBLIC_URL=""
for i in $(seq 1 30); do
  PUBLIC_URL=$(grep -oE "https://[a-z0-9-]+\.trycloudflare\.com" "$CF_LOG" | head -1)
  [ -n "$PUBLIC_URL" ] && break
  sleep 1
done

echo ""
echo "============================================================"
if [ -n "$PUBLIC_URL" ]; then
  echo "$PUBLIC_URL" > "$SCRIPT_DIR/cloudflare-url.txt"
  echo "  LINK CÔNG KHAI (đưa cho người chơi máy khác):"
  echo "      $PUBLIC_URL"
  echo "  (đã lưu vào cloudflare-url.txt)"
  open "$PUBLIC_URL" 2>/dev/null || xdg-open "$PUBLIC_URL" 2>/dev/null
else
  echo "  Chưa lấy được link — xem .cloudflare.log"
  open "http://localhost:$PORT" 2>/dev/null
fi
echo "  Local: http://localhost:$PORT"
echo "  Tắt: ./stop-server.sh  (hoặc Ctrl+C cửa sổ này)"
echo "============================================================"

# Giữ tiến trình sống theo cloudflared
wait "$CF_PID"

# Khi cloudflared thoát thì kill server
kill "$SERVER_PID" 2>/dev/null
rm -f "$SCRIPT_DIR/.server.pid" "$SCRIPT_DIR/.cloudflared.pid"
