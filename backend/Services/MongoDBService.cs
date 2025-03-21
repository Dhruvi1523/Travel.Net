using MongoDB.Driver ;
using backend.Models;

namespace backend.Services{
    public interface IMongoDbService{
        IMongoCollection<User> User { get; }
    }

    public class MongoDbService : IMongoDbService {

       private readonly IMongoDatabase _database ;

       public MongoDbService(IConfiguration configuration)  
       {
        var client = new MongoClient(configuration["MongoDb:ConnectionString"]);
        _database = client.GetDatabase(configuration["MongoDb:DatabaseName"]);
       }

       public IMongoCollection<User> User => _database.GetCollection<User>("User");

       
    }
}