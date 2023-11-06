using System.Data.SqlClient;
using Dapper;
using Npgsql;
using simulation.Models;

namespace simulation.Repository.Pickpool;

public class PickpoolRepository : IPickpoolRepository
{
    private readonly string _connectionString;
    private readonly string _connectionStringOlRewe;

    public PickpoolRepository(IConfiguration configuration)
    {
        _connectionStringOlRewe = configuration.GetConnectionString("OLReweAbf");
        _connectionString = configuration.GetConnectionString("LocalPostgres");
    }
    public List<PickpoolEntry> SelectPickpool(DateTime date)
    {
        const string sql =
            @" 
                    select
                        platzid,
                        menge,
                        belposid,
                        belid,
                        artikelnummer,
                        variante,
                        picklistenid,
                        pickzeit,
                        liefertermin
                    from
                        public.pickpool
                    where date(pickzeit) = @date
                    order by
                        liefertermin, pickzeit
                ";

        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<PickpoolEntry>(sql, new
        {
            date.Date
        }).OrderBy(p => p.Liefertermin).ToList();
    }

    public string GetKurzbezeichnungByPlatzId(int platzId)
    {
        const string sql =
            @" 
                    select
	                    kurzbezeichnung
                    from
	                    lagerplaetze l
                    where
	                    platzid = @platzId
                ";

        using var connection = new NpgsqlConnection(_connectionString);
        return connection.QueryFirst<string>(sql, new
        {
            platzId
        });
    }

    public int GetPlatzIdByKurzbezeichnung(string storagePlace)
    {
        const string sql =
            @" 
                    select
	                    platzid
                    from
	                    lagerplaetze l
                    where
	                    kurzbezeichnung = @kurzbezeichnung
                ";

        using var connection = new NpgsqlConnection(_connectionString);
        return connection.QueryFirst<int>(sql, new
        {
            storagePlace
        });
    }

    public List<PicklistsWithDuration> GetPicklistsWithDuration()
    {
        const string sql =
            @" 
                    select
	                    picklistenid,
	                    max(pickzeit) - min(pickzeit) as duration
                    from
	                    pickpool p
                    where
	                    pickzeit > '2022-10-01'
                    group by
	                    picklistenid
                    order by
	                    picklistenid
                ";

        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<PicklistsWithDuration>(sql).ToList();
    }

    public List<PicklistEntry> GetPlatzIdsToPicklist(int picklistId)
    {
        const string sql =
            @" 
                    select
	                    distinct l.kurzbezeichnung as PlatzBezeichnung
                    from
	                    pickpool p
                    inner join lagerplaetze l on
	                    p.platzid = l.platzid
                    where
	                    picklistenid = @picklistid
                ";

        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<PicklistEntry>(sql, new
        {
            picklistId
        }).ToList();
    }

    public List<PicklistWithTotalSeconds> GetPicklistsWithDurationOlRewe()
    {
        const string sql =
            @" 
                    SELECT
	                    PicklistenID,
	                    Erstellungsdatum,
	                    DATEDIFF(SECOND, Erstellungsdatum, PufferschieneTime) AS duration
                    FROM
	                    lvs.dbo.Picklisten p (NOLOCK)
                    WHERE
	                    PufferschieneTime IS NOT NULL
	                    AND Erstellungsdatum IS NOT NULL
	                    AND Erstellungsdatum >= '2023-01-09'
                        AND Erstellungsdatum <= '2023-30-09'
                        AND Standort = 'GRO'
                ";

        using var connection = new SqlConnection(_connectionStringOlRewe);
        return connection.Query<PicklistWithTotalSeconds>(sql).ToList();
    }

    public List<PicklistEntry> GetPlatzIdsToPicklistOlRewe(int picklistId)
    {
        const string sql =
            @" 
                    SELECT
	                    k.Kurzbezeichnung as PlatzBezeichnung
                    FROM
	                    LVS.dbo.PicklistenPositionen pp (NOLOCK)
                    INNER JOIN KHKLagerplaetze k (NOLOCK) ON
	                    pp.PlatzIDInitial = k.PlatzID
                    WHERE
	                    PicklistenID = @picklistId
                ";

        using var connection = new SqlConnection(_connectionStringOlRewe);
        return connection.Query<PicklistEntry>(sql, new
        {
            picklistId
        }).ToList();
    }

    public List<Article> GetHistoricSaleFigures()
    {
        const string sql =
            @" 
                    select
	                v.artikelnummer,
	                v.variante
                from
	                dumps.verkaufszahlen v
                where
	                v.monat >= 10
	                and v.monat <= 12
                    and v.jahr < 2022
                group by
	                v.artikelnummer,
	                v.variante
                order by
	                SUM(v.menge) desc
                limit 1000;
                ";

        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<Article>(sql).ToList();
    }

    public List<Article> GetActualSaleFigures()
    {
        const string sql =
            @" 
                    select
	                v.artikelnummer,
	                v.variante
                from
	                dumps.verkaufszahlen v
                where
	                v.monat >= 10
	                and v.monat <= 12
                    and v.jahr = 2022
                group by
	                v.artikelnummer,
	                v.variante
                order by
	                SUM(v.menge) desc
                limit 1000;
                ";

        using var connection = new NpgsqlConnection(_connectionString);
        return connection.Query<Article>(sql).ToList();
    }
}