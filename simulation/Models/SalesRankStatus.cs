namespace simulation.Models;

public class SalesRankStatus
{
    public SalesRankStatus()
    {
        IncomingGoods = new List<IncomingGoods>();
    }

    public DateTime Date { get; set; }
    public List<IncomingGoods> IncomingGoods { get; set; }
}