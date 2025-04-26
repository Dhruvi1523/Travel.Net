// Services/IBookingService.cs
using backend.Models;
using backend.Models;
using MongoDB.Driver;

namespace backend.Services
{
    public interface IBookingService
    {
        Task SaveBookingAsync(FlightBookingModel booking);
        Task<FlightBookingModel> GetBookingByIdAsync(string id);
        Task UpdateBookingAsync(string bookingId, FlightBookingModel updatedBooking);
    }



    public class BookingService : IBookingService
    {
        private readonly IMongoCollection<FlightBookingModel> _bookingCollection;

        public BookingService(IMongoDbService mongoDbService)
        {
            _bookingCollection = mongoDbService.FlightBooking;
        }

        public async Task SaveBookingAsync(FlightBookingModel booking)
        {
            await _bookingCollection.InsertOneAsync(booking);
        }

        public async Task<FlightBookingModel> GetBookingByIdAsync(string id)
        {
            return await _bookingCollection.Find(b => b.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateBookingAsync(string bookingId, FlightBookingModel updatedBooking)
    {
        await _bookingCollection.ReplaceOneAsync(
            booking => booking.Id == bookingId,
            updatedBooking
        );
    }
    }

    



}
