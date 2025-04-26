using backend.Models;
using MongoDB.Driver;

namespace backend.Services
{
    public interface IMongoDbService
    {
        IMongoCollection<User> User { get; }
        IMongoCollection<FlightBookingModel> FlightBooking { get; }
    }

    public class MongoDbService : IMongoDbService
    {
        private readonly IMongoDatabase _database;

        public MongoDbService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration["MongoDb:ConnectionString"]);
            _database = client.GetDatabase(configuration["MongoDb:DatabaseName"]);
        }

        public IMongoCollection<User> User => _database.GetCollection<User>("User");
        public IMongoCollection<FlightBookingModel> FlightBooking => _database.GetCollection<FlightBookingModel>("FlightBooking");

        
    }
}
