using Dapper;
using Npgsql;
using simulation.Models;

namespace simulation.Repository.Storage;

public class StorageRepository : IStorageRepository
{
    private readonly string _connectionString;

    public StorageRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("LocalPostgres");
    }

    public async Task StoreArticles(StockBooking booking)
    {
        try
        {
            const string sql =
                @" 
                        insert into
	                        public.bestaende (platzid, artikelnummer, variante, menge)
	                        values(@platzid,@artikelnummer,@variante,@mengeToAdd)
                        on conflict (platzid, artikelnummer, variante) do 
                           update set
	                        menge = bestaende.menge + excluded.menge;
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

    public async Task<int> RemoveArticles(StockBooking booking)
    {
        const string sql =
            @" 
                    update public.bestaende
                    set menge = case
		                    when bestaende.menge - @menge < 0 then 0
		                    else bestaende.menge - @menge end
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

    public StoragePlace? GetPickplatzForArticle(StockBooking booking)
    {
        const string sql =
            @" 
                    select
	                    b.platzid, 
                        l.kurzbezeichnung as platzbezeichnung
                    from
	                    bestaende b
                    inner join lagerplaetze l on
	                    b.platzid = l.platzid
	                    and l.isbodenzone = 1
	                    and l.x is not null
                    left join reservation r on
	                    r.platzid = b.platzid
	                    and r.artikelnummer = b.artikelnummer
	                    and r.variante = b.variante
                    where
	                    b.artikelnummer = @artikelnummer
	                    and b.variante = @variante
	                    and b.menge - coalesce(r.menge, 0) >= @menge
                    order by
	                    b.menge,
	                    b.platzid
                ";

        using var connection = new NpgsqlConnection(_connectionString);
        return connection.QueryFirstOrDefault<StoragePlace>(sql, new
        {
            artikelnummer = booking.Artikelnummer,
            variante = booking.Variante,
            menge = booking.Menge
        });
    }

    public StockBooking? GetHochzoneplatzAndAmountForArticle(StockBooking booking)
    {
        const string sql =
            @" 
                    select
	                    b.platzid, b.menge
                    from
	                    bestaende b
                    inner join lagerplaetze l on
	                    b.platzid = l.platzid
	                    and l.isbodenzone = 0
                        and l.x is not null
                    where
                        artikelnummer = @artikelnummer
                        and variante = @variante
                        and menge >= @menge
                    order by
                        menge, platzid
                ";

        using var connection = new NpgsqlConnection(_connectionString);
        return connection.QueryFirstOrDefault<StockBooking>(sql, new
        {
            artikelnummer = booking.Artikelnummer,
            variante = booking.Variante,
            menge = booking.Menge
        });
    }
}