namespace simulation.Models;

public class IncomingGoods
{
    public int PlatzId { get; set; }
    public int Artikelnummer { get; set; }
    public int Variante { get; set; }
    public int Menge { get; set; }
    public double Rank { get; set; }

    public StockBooking ToStockBooking()
    {
        return new StockBooking
        {
            PlatzId = PlatzId,
            Artikelnummer = Artikelnummer,
            Variante = Variante,
            Menge = Menge
        };
    }

    public Article ToArticle()
    {
        return new Article
        {
            Artikelnummer = Artikelnummer,
            Variante = Variante
        };
    }
}