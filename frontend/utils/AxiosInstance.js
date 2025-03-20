const axios = require("axios");

const axiosInstance = axios.create({
    baseURL: "http://localhost:5096", // Change this to your backend URL
    timeout: 10000,
    withCredentials : true ,
    headers: {
        "Content-Type": "application/json"
    },
});

module.exports = axiosInstance;