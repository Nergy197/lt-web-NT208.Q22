#!/bin/bash
echo "Starting Node.js server and Localtunnel for Game APK..."

# Bật backend server trong background
cd Backend
node server.js &
SERVER_PID=$!

# Chờ 2 giây để server khởi động
sleep 2

# Bật localtunnel với subdomain cố định
echo "--------------------------------------------------------"
echo "Localtunnel đang chạy ở URL: https://ntugame-nergy.loca.lt"
echo "Giữ cửa sổ này mở để giảng viên có thể chơi game qua APK."
echo "Nhấn Ctrl+C để tắt server."
echo "--------------------------------------------------------"

npx localtunnel --port 3000 --subdomain ntugame-nergy

# Khi tắt localtunnel thì kill luôn server
kill $SERVER_PID
