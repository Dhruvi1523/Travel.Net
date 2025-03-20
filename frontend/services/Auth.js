const axiosInstance = require("../utils/AxiosInstance")

const register = async (data) => {
    try {
        const response = await axiosInstance.post("/api/auth/register", data);
        return response.data; // give javascript object
    } catch (error) {
        console.error("Registration failed:", error.response ? error.response.data : error.message);
        throw error;
    }
};

const login = async (data) => {
    try {
        const response = await axiosInstance.post("/api/auth/register", data);
        return response.data; 
    } catch (error) {
        console.error("Login failed:", error.response ? error.response.data : error.message);
        throw error;
    }
};

const refreshAccessToken = async ()=>{
    try {
        const response = await axiosInstance.get("/api/auth/refresh");
        return response.data; 
    } catch (error) {
        console.error("Refresh Access Token failed:", error.response ? error.response.data : error.message);
        throw error;
    }
}

const getUserDetails = async ()=>{
    try {
        const response = await axiosInstance.get("/api/auth/me");
        return response.data; 
    } catch (error) {
        console.error("Get User Details  failed:", error.response ? error.response.data : error.message);
        throw error;
    }
}

module.exports  = {
    register ,
    login ,
    refreshAccessToken , 
    getUserDetails
}