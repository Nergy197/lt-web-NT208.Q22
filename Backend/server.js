const express = require("express");
const { MongoClient } = require("mongodb");
const cors = require("cors");
const fs = require("fs");
const path = require("path");

const app = express();

app.use(cors());
app.use(express.json());

const client =
new MongoClient(
"mongodb://127.0.0.1:27017");

let db;



// ================= SEED DATABASE =================

async function seedDatabase()
{
 try
 {
  const filePath =
  path.join(
  __dirname,
  "Database",
  "webgame.players.json");


  if(!fs.existsSync(filePath))
  {
   console.error("Seed file not found:", filePath);
   return;
  }


  const raw =
  fs.readFileSync(
  filePath,
  "utf8");


  const data =
  JSON.parse(raw);


  const collection =
  db.collection("players");


  for(const player of data)
  {
   const exists =
   await collection.findOne(
   {_id:player._id});


   if(!exists)
   {
    await collection.insertOne(player);

    console.log(
    "Created player:",
    player._id);
   }
   else
   {
    console.log(
    "Player exists:",
    player._id);
   }
  }


  console.log("Seed complete");
 }

 catch(err)
 {
  console.error("SEED ERROR:", err);
 }
}



// ================= LOAD PLAYER =================

app.get("/player/:id",
async (req,res)=>
{
 try
 {
  const player =
  await db.collection("players")
  .findOne({_id:req.params.id});

  res.json(player);
 }
 catch(err)
 {
  console.error(err);
  res.status(500).send("error");
 }
});



// ================= SAVE PLAYER =================

app.post("/player/save",
async(req,res)=>
{
 try
 {
  await db.collection("players")
  .updateOne(

   {_id:req.body._id},

   {$set:{party:req.body.party}},

   {upsert:true}

  );

  res.send("saved");
 }
 catch(err)
 {
  console.error(err);
  res.status(500).send("error");
 }
});



// ================= CONNECT + START SERVER =================

client.connect()
.then(async () =>
{
 console.log("MongoDB connected");

 db = client.db("webgame");

 await seedDatabase();


 app.listen(3000,()=>
 {
  console.log(
  "Server running on port 3000");
 });

})
.catch(err =>
{
 console.error(err);
});

// ================= SYNC FROM FILE =================

app.get("/sync", async (req,res)=>
{
 try
 {
  const filePath =
  path.join(
  __dirname,
  "Database",
  "webgame.players.json");


  const raw =
  fs.readFileSync(filePath,"utf8");

  const data =
  JSON.parse(raw);


  const collection =
  db.collection("players");


  for(const player of data)
  {
   await collection.updateOne(

    {_id:player._id},

    {$set:player},

    {upsert:true}

   );
  }


  res.send("SYNC DONE");

 }
 catch(err)
 {
  console.error(err);

  res.send("SYNC ERROR");
 }
});