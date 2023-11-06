using Microsoft.AspNetCore.Mvc;
using simulation.Managers.Stock;
using simulation.Managers.Strategies;

namespace simulation.Controllers;

[ApiController]
[Route("/api/stock")]
public class StockController : ControllerBase
{
    private readonly IStockManager _stockManager;
    private readonly IStrategyManager _strategyManager;

    public StockController(IStockManager stockManager, IStrategyManager strategyManager)
    {
        _stockManager = stockManager;
        _strategyManager = strategyManager;
    }

    [HttpPut]
    [Route("create-stock")]
    public async Task<ActionResult> CreateStock(DateTime date)
    {
        await _stockManager.CreateStock(date);
        return Ok();
    }

    [HttpPut]
    [Route("update-stock")]
    public async Task<ActionResult> UpdateStock(DateTime date)
    {
        await _strategyManager.Current(date);
        return Ok();
    }

    [HttpPost]
    [Route("rearrange")]
    public async Task<ActionResult> Rearrange()
    {
        await _stockManager.RearrangeToExistingStoragePlaces();
        return Ok();
    }
}