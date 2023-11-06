using System.Data.SqlClient;
using Dapper;
using Npgsql;
using simulation.Models;

namespace simulation.Repository.Articles;

public class ArticleRepository : IArticleRepository
{
    private readonly string _connectionStringOlRewe;
    private readonly string _connectionStringLocal;

    public ArticleRepository(IConfiguration configuration)
    {
        _connectionStringOlRewe = configuration.GetConnectionString("OLReweAbf");
        _connectionStringLocal = configuration.GetConnectionString("LocalPostgres");
    }
    public ArticleAttributes GetAllDimensionsForArticle(int artikelnummer, int variante)
    {
        const string sql =
            @" 
                    select
	                    artikelnummer,
	                    variante,
	                    length,
	                    width,
	                    height,
	                    flaechemax,
	                    flaechemin
                    from
	                    public.artikel
                    where
                        artikelnummer = @artikelnummer
	                    and variante = @variante
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<ArticleAttributes>(sql, new
        {
            artikelnummer,
            variante
        });
    }

    public ArticleAttributes GetMaxAndMinAreas(ArticleAttributes article)
    {
        const string sql =
            @" 
                    SELECT
                        @length AS Length,
                        @width AS Width,
                        @height AS Height,
	                    dbo.fnGetFlaeche(@length,@width,@height,0) AS FlaecheMax,
	                    dbo.fnGetFlaeche(@length,@width,@height,1) AS FlaecheMin
                ";

        using var connection = new SqlConnection(_connectionStringOlRewe);
        return connection.QueryFirstOrDefault<ArticleAttributes>(sql, new
        {
            article.Length,
            article.Width,
            article.Height
        });
    }

    public List<Article> GetAllArticles()
    {
        const string sql =
            @"
                select
	                a.artikelnummer,
	                a.variante
                from
	                artikel a
                ";
        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<Article>(sql).ToList();
    }

    public int GetPalletSize(Article article)
    {
        const string sql =
            @"
                select
	                coalesce(max(menge), 0) as palletsize
                from
	                dumps.khklagerplatzbuchungen k
                where
	                herkunftslpkennung not in (
	                    select
		                    platzid
	                    from
		                    lagerplaetze l)
	                and artikelnummer = @artikelnummer
	                and variante = @variante
                ";
        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirst<int>(sql, new
        {
            artikelnummer = article.Artikelnummer,
            variante = article.Variante
        });
    }

    public async Task SetPalletSize(Article article)
    {
        const string sql =
            @"
                update
	                artikel
                set
	                palletsize = @palletsize
                where
	                artikelnummer = @artikelnummer
	                and variante = @variante
                ";
        await using var connection = new NpgsqlConnection(_connectionStringLocal);
        await connection.ExecuteAsync(sql, new
        {
            palletsize = article.PalletSize,
            artikelnummer = article.Artikelnummer,
            variante = article.Variante
        });
    }

    public List<Article> GetArticlesWithMultipleGroundzoneStorageplaces()
    {
        const string sql =
            @"
                select
	                b.artikelnummer,
	                b.variante
                from
	                lagerplaetze l
                inner join bestaende b on
	                l.platzid = b.platzid
                where
	                l.isbodenzone = 1
                group by
	                b.artikelnummer,
	                b.variante
                having
	                count(*) > 1
                order by
	                count(*) desc
                ";
        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<Article>(sql).ToList();
    }

    public List<Article> GetSalesRanks(int month, DateTime date)
    {
        const string sql =
            @"
                select
	                v.artikelnummer,
	                v.variante
                from
	                dumps.verkaufszahlen v
                where
	                v.monat >= @month - 1
	                and v.monat <= @month + 1
                    and v.datum < @date
                group by
	                v.artikelnummer,
	                v.variante
                order by
	                SUM(v.menge) desc;
                ";
        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<Article>(sql, new
        {
            month,
            date
        }).ToList();
    }

    public async Task SetSalesRank(Article article, int rank)
    {
        const string sql =
            @"
                update
	                artikel
                set
	                ""rank"" = @rank
                where
	                artikelnummer = @artikelnummer
	                and variante = @variante
                ";
        await using var connection = new NpgsqlConnection(_connectionStringLocal);
        await connection.ExecuteAsync(sql, new
        {
            rank,
            artikelnummer = article.Artikelnummer,
            variante = article.Variante
        });
    }

    public int GetArticleCount()
    {
        const string sql =
            @" 
                    select
                        count(*)
                    from
                        artikel a 
                    where
                        a.palletsize <> 0
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirst<int>(sql);
    }

    public List<Article> GetArticlesToClass(int limit, int offset) //TODO alle Artikel dabei?
    {
        const string sql =
            @" 
                    select
                        a.artikelnummer,
                        a.variante
                    from
                        artikel a 
                    where
                        a.palletsize <> 0
                    order by
                        a.""rank""
                    limit @limit
                    offset @offset
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<Article>(sql, new
        {
            limit,
            offset
        }).ToList();
    }

    public async Task SetClassForArticle(Article article, int classToSet)
    {
        const string sql =
            @" 
                    update
	                    artikel
                    set
	                    ""class"" = @classToSet
                    where
	                    artikelnummer = @artikelnummer and
                        variante = @variante
                ";

        await using var connection = new NpgsqlConnection(_connectionStringLocal);
        await connection.ExecuteAsync(sql, new
        {
            classToSet,
            artikelnummer = article.Artikelnummer,
            variante = article.Variante
        });
    }

    public int GetClassToArticle(Article article)
    {
        const string sql =
            @" 
                    select
	                    coalesce(a.""class"", 0)
                    from
	                    artikel a
                    where
	                    a.artikelnummer = @artikelnummer
	                    and a.variante = @variante
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            artikelnummer = article.Artikelnummer,
            variante = article.Variante
        });
    }

    public async Task DeleteSalesRanks()
    {
        const string sql =
            @"
                update
	                artikel
                set
	                ""rank"" = null
                ";
        await using var connection = new NpgsqlConnection(_connectionStringLocal);
        await connection.ExecuteAsync(sql);
    }

    public async Task DeleteArticleClasses()
    {
        const string sql =
            @"
                update
	                artikel
                set
	                ""class"" = null
                ";
        await using var connection = new NpgsqlConnection(_connectionStringLocal);
        await connection.ExecuteAsync(sql);
    }

    public List<Article> GetExactSalesRank(int month, int year)
    {
        const string sql =
            @"
                select
	                v.artikelnummer,
	                v.variante
                from
	                dumps.verkaufszahlen v
                where
	                v.monat = @month
                    and v.jahr = @year
                group by
	                v.artikelnummer,
	                v.variante
                order by
	                SUM(v.menge) desc;
                ";
        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.Query<Article>(sql, new
        {
            month,
            year
        }).ToList();
    }

    public int GetExpectedSalesNumber(Article article, DateTime date)
    {
        const string sql =
            @"
                select
	                coalesce(sum(v.menge), 0)
                from
	                dumps.verkaufszahlen v
                where
	                v.datum >= @date ::date - interval '1 year'
	                and v.datum <= @date ::date + interval '30 days' - interval '1 year'
	                and v.artikelnummer = @artikelnummer
	                and v.variante = @variante
                ";
        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            date,
            artikelnummer = article.Artikelnummer,
            variante = article.Variante
        });
    }

    public int GetExactSalesNumber(Article article, DateTime date)
    {
        const string sql =
            @"
                select
	                coalesce(sum(v.menge), 0)
                from
	                dumps.verkaufszahlen v
                where
	                v.datum >= @date ::date
	                and v.datum <= @date ::date + interval '30 days'
	                and v.artikelnummer = @artikelnummer
	                and v.variante = @variante
                ";
        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirstOrDefault<int>(sql, new
        {
            date,
            artikelnummer = article.Artikelnummer,
            variante = article.Variante
        });
    }

    public int GetArticleWithRankCount()
    {
        const string sql =
            @" 
                    select
                        count(*)
                    from
                        artikel a 
                    where
                        a.""rank"" > 0
                ";

        using var connection = new NpgsqlConnection(_connectionStringLocal);
        return connection.QueryFirst<int>(sql);
    }
}