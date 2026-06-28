const express = require("express");
const cors = require("cors");
const compression = require("compression");
const { MongoClient } = require("mongodb");
const fs = require("fs");
const path = require("path");

// Load biến môi trường từ .env (nếu có)
require("dotenv").config();

const app = express();

/* ================= MIDDLEWARE ================= */

// BUG FIX: Thêm CORS để Unity WebGL có thể gọi API. Cần expose header Bypass-Tunnel-Reminder.
app.use(cors({
  origin: '*',
  allowedHeaders: ['Content-Type', 'Authorization', 'Bypass-Tunnel-Reminder']
}));

// Nén gzip on-the-fly — ÉP nén cả .wasm/.data (Unity build ~84MB raw) để tải nhanh
// qua tunnel. Mặc định compression bỏ qua octet-stream/wasm nên phải tự cho phép.
app.use(compression({
  filter: (req, res) => {
    const type = String(res.getHeader("Content-Type") || "");
    if (/wasm|octet-stream|javascript|json|text/i.test(type)) return true;
    return compression.filter(req, res); // phần còn lại theo mặc định (bỏ qua ảnh đã nén)
  }
}));

app.use(express.json());

/* ================= DATABASE ================= */

const client = new MongoClient(process.env.MONGO_URI || "mongodb://127.0.0.1:27017");
let db;

/* ================= SERVE UNITY WEBGL ================= */

// Serve static files
app.use(express.static(path.join(__dirname, "public")));

/* ================= SEED DATABASE ================= */

async function seedDatabase() {
  try {
    const filePath = path.join(
      __dirname,
      "Database",
      "webgame.players.json"
    );

    if (!fs.existsSync(filePath)) {
      console.error("Seed file not found:", filePath);
      return;
    }

    const raw = fs.readFileSync(filePath, "utf8");
    const data = JSON.parse(raw);

    const collection = db.collection("players");

    for (const player of data) {
      const exists = await collection.findOne({ _id: player._id });

      if (!exists) {
        await collection.insertOne(player);
        console.log("Created player:", player._id);
      }
    }

    console.log("Seed complete");
  } catch (err) {
    console.error("SEED ERROR:", err);
  }
}

/* ================= API ================= */

app.get("/player/:id", async (req, res) => {
  try {
    const player = await db
      .collection("players")
      .findOne({ _id: req.params.id });

    if (!player) {
      return res.status(404).json({ error: "Player not found" });
    }

    res.json(player);
  } catch (err) {
    console.error(err);
    res.status(500).send("error");
  }
});

app.post("/player/save", async (req, res) => {
  try {
    if (!req.body._id) {
      return res.status(400).json({ error: "Missing _id" });
    }

    // Safeguard: Không cho phép lưu party rỗng
    if (!req.body.party || req.body.party.length === 0) {
      console.warn("[SAVE] Rejected empty party for:", req.body._id);
      return res.status(400).json({ error: "Cannot save empty party" });
    }

    console.log("[SAVE] data received:", req.body);

    const updateData = { ...req.body };
    delete updateData._id;

    await db.collection("players").updateOne(
      { _id: req.body._id },
      { $set: updateData },
      { upsert: true }
    );

    res.send("saved");
  } catch (err) {
    console.error(err);
    res.status(500).send("error");
  }
});

// Kiểm tra một Transfer Code (vd "guest_a3f9c1e8") có save nào trên server không.
// Trả về { exists, slots: [...] } — dùng để validate mã trước khi Start Game,
// và cho biết save nằm ở những slot nào (khắc phục việc transfer code không mang slot).
app.get("/player/check/:code", async (req, res) => {
  try {
    let code = (req.params.code || "").trim().toLowerCase();

    if (!code.startsWith("guest_")) {
      return res.status(400).json({ error: "Invalid transfer code format" });
    }

    // Escape ký tự regex để tránh injection, rồi khớp prefix "<code>_slot_"
    const safe = code.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
    const docs = await db
      .collection("players")
      .find({ _id: { $regex: `^${safe}_slot_\\d+$` } })
      .project({ _id: 1, slotId: 1, saveTime: 1 })
      .toArray();

    res.json({
      exists: docs.length > 0,
      slots: docs.map(d => ({ slotId: d.slotId, saveTime: d.saveTime })),
    });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: "check failed" });
  }
});

app.get("/sync", async (req, res) => {
  // Bảo vệ: /sync ghi đè toàn bộ DB bằng file JSON → chỉ cho phép khi có token đúng.
  // Đặt SYNC_TOKEN trong .env để bật; nếu không đặt, route bị khoá (an toàn mặc định).
  const expected = process.env.SYNC_TOKEN;
  if (!expected || req.query.token !== expected) {
    console.warn("[SYNC] Rejected: missing/invalid token");
    return res.status(403).send("Forbidden: set SYNC_TOKEN and pass ?token=");
  }
  try {
    const filePath = path.join(
      __dirname,
      "Database",
      "webgame.players.json"
    );

    const raw = fs.readFileSync(filePath, "utf8");
    const data = JSON.parse(raw);

    const collection = db.collection("players");

    for (const player of data) {
      await collection.updateOne(
        { _id: player._id },
        { $set: player },
        { upsert: true }
      );
    }

    res.send("SYNC DONE");
  } catch (err) {
    console.error(err);
    res.status(500).send("SYNC ERROR");
  }
});

/* ================= SPA FALLBACK ================= */

// BUG FIX: SPA fallback phải đăng ký SAU tất cả API routes
// nhưng TRƯỚC khi server listen, tránh race condition
app.get("*", (req, res) => {
  res.sendFile(path.join(__dirname, "public/index.html"));
});

/* ================= CONNECT + START ================= */

client.connect()
  .then(async () => {
    console.log("MongoDB connected");

    db = client.db("webgame");

    await seedDatabase();

    const PORT = process.env.PORT || 3000;
    app.listen(PORT, () => {
      console.log("Server running on port " + PORT);
    });
  })
  .catch(err => {
    console.error(err);
  });

// Đóng MongoDB connection khi process thoát
process.on('SIGINT', async () => {
  await client.close();
  console.log("MongoDB disconnected");
  process.exit(0);
});