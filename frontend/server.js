const express = require("express");
const path = require("path");

const app = express();
const PORT = 3000;

// Serve static files from 'public' folder
app.use(express.static("public"));

app.get("/login" , (req , res) =>{
    res.sendFile(path.join(__dirname, "public/Login.html"));
})

app.get("/signup", (req, res) => {
    res.sendFile(path.join(__dirname, "public/SignUp.html"));
});

app.get("/", (req, res) => {
    res.sendFile(path.join(__dirname, "public/Index.html"));
});

app.listen(PORT, () => console.log(`Server running at http://localhost:${PORT}`));
