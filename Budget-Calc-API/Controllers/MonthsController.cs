using aspnetcore_auth.Models.UI;
using Budget;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnetcore_auth.Controllers;

[Route("api/[controller]")] // api/months
[ApiController]
public class MonthsController : Controller
{
    private readonly ApplicationDbContext _context;

    public MonthsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> GetMonths()
    {
        var months = await _context.Months.Include(x => x.Items).ToListAsync();
        return Ok(months);
    }


    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> CreateMonth(MonthUserData userData)
    {
        if (!ModelState.IsValid) return new JsonResult("Model is not valid") { StatusCode = 500 };

        var newMonth = new Month()
        {
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow,
            CreatedBy = userData.UserId,
            MonthNum = userData.MonthNum,
            Year = userData.Year,
            ApplicationUserId = userData.UserId,
            Items = null
        };

        await _context.Months.AddAsync(newMonth);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetMonth", new { newMonth.Id }, newMonth);
    }

    [HttpGet]
    [Route("[action]/{id:int}")]
    public async Task<IActionResult> GetMonth(int id)
    {
        var month = await _context.Months
            .Include(m => m.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (month == null)
            return NotFound();

        return Ok(month);
    }

    [HttpGet]
    [Route("[action]/{id}")]
    public async Task<IActionResult> GetMonthForUser(string id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        // возможно можно запустить тут проверку на изменение.
        // сравнить id с createdBy items 
        
        var monthsForUser = await _context.Months
            .Where(m => m.ApplicationUserId == id)
            .Select(m => new
            {
                id = m.Id,
                monthNum = m.MonthNum,
                year = m.Year,
                userId = m.ApplicationUserId,
                income = m.Items.Where(x => x.Type == 0).Sum(s => s.Value),
                expense = m.Items.Where(x => x.Type != 0).Sum(s => s.Value),
                adminChanged = m.Items.Any(i => i.CreatedBy != id) // true or false
            })
            .ToListAsync();

        /*[
        {
           "monthId": 65,
           "monthNum": 6,
           "year": 2022,
           "userId": "5776c438-ece1-44c4-a2a4-7fab51c10e01",
           "incomes": 446013,
           "expenses": 300221
        },
        {
           "monthId": 80,
           "monthNum": 7,
           "year": 2022,
           "userId": "5776c438-ece1-44c4-a2a4-7fab51c10e01",
           "incomes": 3768,
           "expenses": 1677
        }
        ]*/

        // return Ok(userMonths);
        return Ok(monthsForUser);
    }

    [HttpDelete("deleteMonth/{id}")]
    public async Task<IActionResult> DeleteMonth(int id)
    {
        var existMonth = await _context.Months
            .Include(m => m.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existMonth == null)
            return NotFound();

        _context.Months.Remove(existMonth);
        await _context.SaveChangesAsync();

        return Ok(existMonth);
    }
}