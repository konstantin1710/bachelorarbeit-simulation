using Microsoft.AspNetCore.Mvc;
using simulation.Managers.Picklists;
using simulation.Models;

namespace simulation.Controllers;

[ApiController]
[Route("/api/picklist")]
public class PicklistController : ControllerBase
{
    private readonly IPicklistManager _picklistManager;

    public PicklistController(IPicklistManager picklistManager)
    {
        _picklistManager = picklistManager;
    }

    [HttpGet]
    [Route("get-picklists")]
    public ActionResult<List<Picklist>> GetPicklists(bool betterPicklists, int picklistCount = 0)
    {
        return Ok(betterPicklists? _picklistManager.GetOptimizedPicklists(picklistCount) : _picklistManager.GetPicklists(picklistCount));
    }

    [HttpPost]
    [Route("get-lengths-for-picklists")]
    public ActionResult<List<Picklist>> GetLengthsForPicklists(List<Picklist> picklists)
    {
        return Ok(_picklistManager.GetLengthsForPicklists(picklists));
    }

    [HttpPut]
    [Route("set-reservations")]
    public async Task<ActionResult<UpdatePickspotsResult>> UpdatePickspots(DateTime date)
    {
        return Ok(await _picklistManager.SetReservations(date));
    }
}