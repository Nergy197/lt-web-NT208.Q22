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
echo "--------------------------------------------------------"
echo "Local URL: http://localhost:$PORT"
echo "Link công khai (dạng https://xxx.trycloudflare.com) sẽ HIỆN BÊN DƯỚI."
echo "Giữ cửa sổ này mở. Tắt bằng: ./stop-server.sh (hoặc Ctrl+C)."
echo "--------------------------------------------------------"

# Mở trình duyệt tự chơi trên máy (macOS/Linux)
echo "Đang mở trình duyệt web (localhost)..."
open "http://localhost:$PORT" 2>/dev/null || xdg-open "http://localhost:$PORT" 2>/dev/null

# Nếu nhấn Ctrl+C ở cửa sổ này: kill luôn node server (không tắt Mongo — dùng stop-server.sh)
trap 'echo; echo "Đang dừng server..."; kill "$SERVER_PID" 2>/dev/null; rm -f "$SCRIPT_DIR/.server.pid"; exit 0' INT TERM

# Cloudflare quick tunnel — khỏe hơn localtunnel với file lớn. URL ngẫu nhiên mỗi lần.
cloudflared tunnel --url "http://localhost:$PORT"

# Khi cloudflared thoát thì kill server
kill "$SERVER_PID" 2>/dev/null
rm -f "$SCRIPT_DIR/.server.pid"
