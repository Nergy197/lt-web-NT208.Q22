const express = require("express");
const { MongoClient } = require("mongodb");
const cors = require("cors");
const fs = require("fs");

const app = express();

app.use(cors());
app.use(express.json());

const client =
 new MongoClient(
 "mongodb://127.0.0.1:27017");

let db;



// CONNECT
client.connect().then(async () =>
{
 db = client.db("webgame");

 console.log("MongoDB connected");

 await seedDatabase();

});



// AUTO SEED
async function seedDatabase()
{
 const count =
 await db.collection("players")
 .countDocuments();


 if(count > 0)
 {
  console.log("Database already exists");
  return;
 }


 const data =
 JSON.parse(
 fs.readFileSync(
 "./database/players.json",
 "utf8"));


 await db
 .collection("players")
 .insertMany(data);


 console.log("Database created");
}



// LOAD PLAYER
app.get("/player/:id",
async (req,res)=>
{
 const player =
 await db.collection("players")
 .findOne(
 {_id:req.params.id});


 res.json(player);
});



// SAVE PLAYER
app.post("/player/save",
async(req,res)=>
{
 await db.collection("players")
 .updateOne(

 {_id:req.body._id},

 {$set:{party:req.body.party}}

 );

 res.send("saved");
});



app.listen(3000,()=>
{
 console.log(
 "Server running on port 3000");
});