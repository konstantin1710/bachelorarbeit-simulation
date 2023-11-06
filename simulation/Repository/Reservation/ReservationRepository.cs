using Dapper;
using Npgsql;
using simulation.Models;

namespace simulation.Repository.Reservation;

public class ReservationRepository : IReservationRepository
{

    private readonly string _connectionString;

    public ReservationRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("LocalPostgres");
    }

    public async Task ReserveArticle (StockBooking booking)
    {
        try
        {
            const string sql =
                @" 
                        insert into
	                        public.reservation (platzid, artikelnummer, variante, menge)
	                        values(@platzid,@artikelnummer,@variante,@mengeToAdd)
                        on conflict (platzid, artikelnummer, variante) do
                            update set
	                        menge = reservation.menge + excluded.menge;
                    ";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                platzid = booking.PlatzId,
                artikelnummer = booking.Artikelnummer,
                variante = booking.Variante,
                mengeToAdd = booking.Menge
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task<int> RemoveReservation(StockBooking booking)
    {
        const string sql =
            @" 
                    update public.reservation
                    set menge = case
		                    when reservation.menge - @menge < 0 then 0
		                    else reservation.menge - @menge end
                    where platzid = @platzid
                        and artikelnummer = @artikelnummer
                        and variante = @variante
                ";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.ExecuteAsync(sql, new
        {
            platzid = booking.PlatzId,
            artikelnummer = booking.Artikelnummer,
            variante = booking.Variante,
            menge = booking.Menge
        });
    }

    public async Task DeleteReservations()
    {
        const string sql =
            @" 
                    delete from reservation;
                ";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql);
    }
}