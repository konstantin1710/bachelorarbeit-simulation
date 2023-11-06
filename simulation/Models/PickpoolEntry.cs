namespace simulation.Models;

public class PickpoolEntry
{
    public int PlatzId { get; set; }
    public int Menge { get; set; }
    public int BelPosId { get; set; }
    public int BelId { get; set; }
    public int Artikelnummer { get; set; }
    public int Variante { get; set; }
    public int PicklistenId { get; set; }
    public DateTime Pickzeit { get; set; }
    public DateTime Liefertermin { get; set; }
    public string? PlatzBezeichnung { get; set; }
    public decimal FlaecheMax { get; set; }

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