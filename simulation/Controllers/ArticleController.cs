using Microsoft.AspNetCore.Mvc;
using simulation.Managers.Articles;
using simulation.Models;

namespace simulation.Controllers;

[ApiController]
[Route("/api/article")]
public class ArticleController : ControllerBase
{
    private readonly IArticleManager _articleManager;

    public ArticleController(IArticleManager articleManager)
    {
        _articleManager = articleManager;
    }

    [HttpGet]
    [Route("get-attributes/{articles}")]
    public async Task<ActionResult<List<ArticleAttributes>>> GetArticleAttributes(string articles, bool useContentApi = false)
    {
        var articlesSplit = articles.Split(',').ToList();
        return Ok(await _articleManager.GetArticleAttributes(articlesSplit, useContentApi));
    }

    [HttpPut]
    [Route("calculate-pallet-sizes")]
    public async Task<ActionResult> CalculatePalletSizes()
    {
        await _articleManager.CalculatePalletSizes();
        return Ok();
    }
}