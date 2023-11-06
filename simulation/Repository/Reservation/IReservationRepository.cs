using simulation.Models;

namespace simulation.Repository.Reservation;

public interface IReservationRepository
{
    Task ReserveArticle (StockBooking booking);
    Task<int> RemoveReservation(StockBooking booking);
    Task DeleteReservations();
}