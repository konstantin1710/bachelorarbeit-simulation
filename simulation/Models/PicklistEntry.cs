namespace simulation.Models;

public class PicklistEntry
{
    public int PlatzId { get; set; }
    public string? PlatzBezeichnung { get; set; }
    public int Menge { get; set; }
    public int Artikelnummer { get; set; }
    public int Variante { get; set; }
    public int PicklistenId { get; set; }
    public DateTime Pickzeit { get; set; }

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
}