using Dapper;
using Npgsql;
using simulation.Models;

namespace simulation.Repository.Warehouse;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionStringLocal;

    public WarehouseRepository(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionStringLocal = configuration.GetConnectionString("LocalPostgres");
    }

    public List<int> GetAllPlatzIds()
    {
        const string sql =
            @" 
                    select
	                    platzid
                    from
	                    lagerplaetze l
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql).ToList();
    }

    public List<StoragePlace> GetAllNotExistingStoragePlaces()
    {
        const string sql =
            @" 
                    select
	                    l.platzid, 
                        l.isbodenzone
                    from
	                    lagerplaetze l
                    inner join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    l.x is null;
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<StoragePlace>(sql).ToList();
    }
    public List<StoragePlace> GetStoragePlacesWithLowStock()
    {
        const string sql =
            @" 
                    select
	                    substring(l.kurzbezeichnung,5,2) as unit,
	                    split_part(l.kurzbezeichnung,';',2) as gang,
	                    l.platzid,
	                    sum(b.menge)
                    from
	                    lagerplaetze l
                    inner join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    l.isbodenzone = 1
                    group by
	                    substring(l.kurzbezeichnung,5,2),
	                    split_part(l.kurzbezeichnung,';',2),
	                    l.platzid
                    having
	                    sum(b.menge) < @menge
                    order by
	                    unit,
	                    gang
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<StoragePlace>(sql, new
        {
            menge = _configuration.GetValue<int>("LowStockThreshold")
        }).ToList();
    }

    public List<int> GetStoragePlaceIdsWithoutStock(string storagePlace, bool groundZone)
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    l.isbodenzone = @groundZone
	                    and l.x is not null
	                    and b.artikelnummer is null
	                    and left(l.kurzbezeichnung,-2) = @storagePlace
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql, new
        {
            groundZone = groundZone ? 1 : 0,
            storagePlace
        }).ToList();
    }

    public int GetNewExistingStoragePlace(StoragePlace storagePlace)
    {
        const string sql =
            @" 
                    select
                        l.platzid
                    from
                        lagerplaetze l
                    left join bestaende b on
                        l.platzid = b.platzid
                    where
                        l.isbodenzone = @isBodenzone
                        and b.menge is null
                        and l.x is not null
                    order by
    	                abs(cast(substring(l.kurzbezeichnung,5,2) as integer) - cast(@unit as integer)),
    	                abs(cast(split_part(l.kurzbezeichnung,';',2) as integer) - cast(@gang as integer)),
                        abs(cast(split_part(l.kurzbezeichnung,';',3) as integer) - cast(@platz as integer))
                    limit 1
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            isBodenzone = storagePlace.IsBodenzone,
            unit = storagePlace.Unit,
            gang = storagePlace.Gang,
            platz = storagePlace.Platz
        });
    }

    public List<int> GetPlatzIdsWithoutStock()
    {
        const string sql =
            @" 
                    select
                        l.platzid
                    from
                        lagerplaetze l
                    where
                        l.platzid not in (
                            select
                                platzid
                            from
                                bestaende)
                        and l.x is not null;
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql).ToList();
    }

    public List<int> GetPlatzIdsWithoutStockInGroundZone()
    {
        const string sql =
            @" 
                    select
                        l.platzid
                    from
                        lagerplaetze l
                    where
                        l.platzid not in (
                            select
                                platzid
                            from
                                bestaende)
                        and l.x is not null
                        and l.isbodenzone = 1;
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql).ToList();
    }
    public List<string> GetStoragePlacesWithoutStock(bool groundZone)
    {
        const string sql =
            @" 
                    select
	                    left(l.kurzbezeichnung,-2)
                    from
                        lagerplaetze l
                    where
                        l.platzid not in (
                            select
                                platzid
                            from
                                bestaende)
                        and l.x is not null
                        and l.isbodenzone = @groundZone;
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<string>(sql, new
        {
            groundZone = groundZone ? 1 : 0
        }).ToList();
    }

    public List<int> GetPlatzIdsWithoutStockOrderedByDistance()
    {
        const string sql =
            @" 
                    select
                        l.platzid
                    from
                        lagerplaetze l
                    where
                        l.platzid not in (
                            select
                                platzid
                            from
                                bestaende)
                        and l.x is not null
                    order by
	                    l.distance
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql).ToList();
    }

    public int GetStoragePlacesWithSameArticleInSameAisle(StoragePlace storagePlace, StockBooking stock)
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    inner join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    l.isbodenzone = 1
	                    and substring(l.kurzbezeichnung,5,2) = @unit
	                    and split_part(l.kurzbezeichnung,';',2) = @gang
	                    and b.artikelnummer = @artikelnummer
	                    and b.variante = @variante
                        and b.platzid <> @platzid
                    group by
	                    l.platzid
                    order by
	                    sum(b.menge)
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            unit = storagePlace.Unit,
            gang = storagePlace.Gang,
            artikelnummer = stock.Artikelnummer,
            variante = stock.Variante,
            platzid = stock.PlatzId
        });
    }

    public int GetNextMixedArticleStoragePlace()
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    left(l.kurzbezeichnung,-2) in (
		                    select
			                    left(l2.kurzbezeichnung,-2)
		                    from
			                    lagerplaetze l2
		                    where
			                    l2.kurzbezeichnung like '%;7'
			                    and l2.x is not null)
	                    and l.isbodenzone = 1
	                    and x is not null
                    group by
	                    l.platzid
                    order by
	                    coalesce(sum(b.menge),0)
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql);
    }

    public List<int> GetAllMixedArticleStoragePlaces()
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    left(l.kurzbezeichnung,-2) in (
		                    select
			                    left(l2.kurzbezeichnung,-2)
		                    from
			                    lagerplaetze l2
		                    where
			                    l2.kurzbezeichnung like '%;7'
			                    and l2.x is not null)
	                    and l.isbodenzone = 1
	                    and x is not null
                    group by
	                    l.platzid
                    order by
	                    coalesce(sum(b.menge),0)
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql).ToList();
    }

    public List<int> GetPlatzIdsWithLowFillratio()
    {
        const string sql =
            @" 
                    select
	                    b.platzid
                    from
	                    bestaende b
                    inner join artikel a on
	                    b.artikelnummer = a.artikelnummer
	                    and b.variante = a.variante
                    inner join lagerplaetze l on
	                    b.platzid = l.platzid
	                    and l.isbodenzone = 1
                    group by
	                    b.platzid
                    having
	                    sum(b.menge::float / a.palletsize) < 0.5
                    order by
	                    sum(b.menge::float / a.palletsize)
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql).ToList();
    }

    public bool IsBodenzone(int platzId)
    {
        const string sql =
            @" 
                    select
	                    l.isbodenzone
                    from
	                    lagerplaetze l
                    where
	                    l.platzid = @platzid
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirst<int>(sql, new
        {
            platzId
        }) == 1;
    }

    public int GetStoragePlaceCount()
    {
        const string sql =
            @" 
                    select
	                    count(l.platzid)
                    from
	                    lagerplaetze l
                    where
	                    l.x is not null
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirst<int>(sql);
    }

    public List<int> GetStoragePlacesToClass(int limit, int offset)
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    where
	                    l.x is not null
                    order by
	                    l.distance,
                        l.kurzbezeichnung
                    limit @limit
                    offset @offset
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql, new
        {
            limit,
            offset
        }).ToList();
    }

    public List<int> GetStoragePlacesToClass(int articleClass)
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    b.menge is null
	                    and l.""class"" = @articleClass
                    order by
                        distance,
                        isbodenzone desc
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql, new
        {
            articleClass
        }).ToList();
    }

    public List<int> GetStoragePlacesToClass(int articleClass, bool groundZone)
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    b.menge is null
	                    and l.""class"" = @articleClass
                        and l.isbodenzone = @groundZone
                    order by
                        distance
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<int>(sql, new
        {
            articleClass,
            groundZone = groundZone ? 1 : 0
        }).ToList();
    }

    public async Task SetClassForStoragePlace(int platzId, int classToSet)
    {
        const string sql =
            @" 
                    update
	                    lagerplaetze
                    set
	                    ""class"" = @classToSet
                    where
	                    platzid = @platzId
                ";

        await using var connection = new NpgsqlConnection(_connectionStringLocal);
        await connection.ExecuteAsync(sql, new
        {
            classToSet,
            platzId
        });
    }

    public int GetMaximumClass()
    {
        const string sql =
            @" 
                    select
	                    max(l.""class"")
                    from
	                    lagerplaetze l
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirst<int>(sql);
    }

    public List<StoragePlace> GetStoragePlacesByDistance()
    {
        const string sql =
            @" 
                    select
	                    platzid,
	                    isbodenzone
                    from
	                    lagerplaetze l
                    where
	                    l.x is not null and
	                    ""class"" is null
                    order by
	                    distance,
	                    kurzbezeichnung
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<StoragePlace>(sql).ToList();
    }

    public async Task DeleteStoragePlaceClasses()
    {
        const string sql =
            @" 
                    update
	                    lagerplaetze
                    set
	                    ""class"" = null
                ";

        await using var connection = new NpgsqlConnection(_connectionStringLocal);
        await connection.ExecuteAsync(sql);
    }

    public int GetNextPlatzIdInHigherClass(int articleClass)
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    b.menge is null
	                    and l.""class"" >= @articleClass
                    order by
	                    ""class"",
	                    distance,
                        isbodenzone desc
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            articleClass
        });
    }

    public int GetNextPlatzIdInHigherClass(int articleClass, bool groundZone)
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    b.menge is null
	                    and l.""class"" >= @articleClass
                        and l.isbodenzone = @groundZone
                    order by
	                    ""class"",
	                    distance,
                        isbodenzone desc
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            articleClass,
            groundZone = groundZone ? 1 : 0
        });
    }

    public int GetNextPlatzIdInLowerClass(int articleClass)
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    b.menge is null
	                    and l.""class"" <= @articleClass
                    order by
	                    ""class"" desc,
	                    distance desc,
                        isbodenzone desc
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            articleClass
        });
    }

    public int GetNextPlatzIdInLowerClass(int articleClass, bool groundZone)
    {
        const string sql =
            @" 
                    select
	                    l.platzid
                    from
	                    lagerplaetze l
                    left join bestaende b on
	                    l.platzid = b.platzid
                    where
	                    b.menge is null
	                    and l.""class"" <= @articleClass
                        and l.isbodenzone = @groundZone
                    order by
	                    ""class"" desc,
	                    distance desc,
                        isbodenzone desc
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            articleClass,
            groundZone = groundZone ? 1 : 0
        });
    }
}