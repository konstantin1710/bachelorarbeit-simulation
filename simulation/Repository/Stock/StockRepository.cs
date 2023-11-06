using Dapper;
using Npgsql;
using simulation.Models;

namespace simulation.Repository.Stock;

public class StockRepository : IStockRepository
{
    private readonly string _connectionStringLocal;

    public StockRepository(IConfiguration configuration)
    {
        _connectionStringLocal = configuration.GetConnectionString("LocalPostgres");
    }
    public async Task DeleteStock()
    {
        const string sql =
            @" 
                    delete from bestaende;
                ";

        await using var connection = new NpgsqlConnection(_connectionStringLocal);
        await connection.ExecuteAsync(sql);
    }

    public List<StockBooking> GetStock(int platzId)
    {
        const string sql =
            @" 
                    select
                        platzid,
	                    artikelnummer,
	                    variante,
	                    menge
                    from
	                    bestaende b
                    where 
                        platzid = @platzId
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<StockBooking>(sql, new
        {
            platzId
        }).ToList();
    }

    public List<StockBooking> GetZugaengeBeforeDate(int platzId, DateTime date)
    {
        const string sql =
            @" 
                    select
                        k.artikelnummer,
                        k.variante,
                        SUM(k.menge) AS menge
                    from
                        dumps.khklagerplatzbuchungen k
                    where
                        k.bewegungsdatum <= @date
	                    and k.zielLPKennung = @platzId
                    group by 
                        k.artikelnummer, 
                        k.variante
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<StockBooking>(sql, new
        {
            date,
            platzId
        }).ToList();
    }


    public List<StockBooking> GetAbgaengeBeforeDate(int platzId, DateTime date)
    {
        const string sql =
            @" 
                    select
                        k.artikelnummer,
                        k.variante,
                        SUM(k.menge) AS menge
                    from
                        dumps.khklagerplatzbuchungen k
                    where
                        k.bewegungsdatum < @date
	                    and k.herkunftslpkennung = @platzId
                    group by 
                        k.artikelnummer,
                        k.variante
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<StockBooking>(sql, new
        {
            date,
            platzId
        }).ToList();
    }

    public List<IncomingGoods> GetZugaengeAtSpecificDate(DateTime date)
    {
        const string sql =
            @" 
                    select
	                    k.ziellpkennung as platzid,
	                    k.artikelnummer,
	                    k.variante,
	                    SUM(k.menge) as menge,
	                    coalesce(a.""rank"", float8 '+infinity') as ""rank""
                    from
	                    dumps.khklagerplatzbuchungen k
                    inner join lagerplaetze l on
	                    k.ziellpkennung = l.platzid
                    inner join artikel a on
	                    k.artikelnummer = a.artikelnummer
	                    and k.variante = a.variante
                    where
	                    k.bewegungsdatum = @date
	                    and k.herkunftslpkennung not in (
	                    select
		                    platzid
	                    from
		                    lagerplaetze hl)
                    group by
	                    k.ziellpkennung,
	                    k.artikelnummer,
	                    k.variante,
	                    a.""rank"" 
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<IncomingGoods>(sql, new
        {
            date
        }).ToList();
    }

    public List<IncomingGoods> GetZugaengeForNextMonthOrderedByRank(DateTime date)
    {
        const string sql =
            @" 
                    select
                        k.ziellpkennung as platzid,
                        k.artikelnummer,
                        k.variante,
                        SUM(k.menge) as menge,
                        coalesce(a.""rank"", float8 '+infinity') as ""rank""
                    from
                        dumps.khklagerplatzbuchungen k
                    inner join lagerplaetze l on
                        k.ziellpkennung = l.platzid
                    inner join artikel a on 
	                    k.artikelnummer = a.artikelnummer
	                    and k.variante = a.variante
                    where
                        k.bewegungsdatum >= @date
                        and k.bewegungsdatum <= @date ::date + interval '1 month'
                        and k.herkunftslpkennung not in (
                            select
                                platzid
                            from
                                lagerplaetze hl)
                    group by
                        k.ziellpkennung,
                        k.artikelnummer,
                        k.variante,
                        a.""rank"",
                        k.bewegungsdatum
                    order by
	                    ""rank"",
	                    menge desc
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<IncomingGoods>(sql, new
        {
            date
        }).ToList();
    }

    public int GetStockInGroundZone(Article article)
    {
        const string sql =
            @" 
                    select
	                    coalesce(sum(b.menge), 0)
                    from
	                    lagerplaetze l
                    inner join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    l.isbodenzone = 1
	                    and b.artikelnummer = @artikelnummer
	                    and b.variante = @variante
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            artikelnummer = article.Artikelnummer,
            variante = article.Variante
        });
    }

    public bool HasEnoughStock(PicklistEntry picklistEntry)
    {
        const string sql =
            @" 
                    select
	                    count(*)
                    from
	                    bestaende b
                    where
	                    platzid = @platzid
	                    and artikelnummer = @artikelnummer
	                    and variante = @variante
	                    and menge >= @menge
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirst<int>(sql, new
        {
            platzid = picklistEntry.PlatzId,
            artikelnummer = picklistEntry.Artikelnummer,
            variante = picklistEntry.Variante,
            menge = picklistEntry.Menge
        }) > 0;
    }

    public List<StoragePlace> GetStoragePlacesInGroundzoneToArticle(Article article)
    {
        const string sql =
            @" 
                    select
	                    b.platzid,
	                    l.distance,
	                    b.menge::float / a.palletsize as fillratio
                    from
	                    lagerplaetze l
                    inner join bestaende b on
	                    l.platzid = b.platzid
                    inner join artikel a on
	                    b.artikelnummer = a.artikelnummer
	                    and b.variante = a.variante
                    where
	                    l.isbodenzone = 1
	                    and b.artikelnummer = @artikelnummer
	                    and b.variante = @variante
                    order by
	                    fillratio
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<StoragePlace>(sql, new
        {
            artikelnummer = article.Artikelnummer,
            variante = article.Variante
        }).ToList();
    }

    public double GetTakenStorageplaceRateInGroundzone()
    {
        const string sql =
            @" 
                    select
	                    count(b.artikelnummer)::float / count(l.isbodenzone)
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    l.isbodenzone = 1
	                    and l.x is not null
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirst<double>(sql);
    }
}