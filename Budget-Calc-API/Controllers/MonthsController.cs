using aspnetcore_auth.Models.DTOs.Requests;
using aspnetcore_auth.Models.DTOs.Responses;
using aspnetcore_auth.Models.UI;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Budget;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnetcore_auth.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/[controller]")] // api/months
[ApiController]
public class MonthsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    
    public MonthsController(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetMonths()
    {
        var months = await _context.Months.Include(x => x.Items).ToListAsync();
        return Ok(months);
    }
    
    [HttpPost]
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
    [Route("{id:int}")]
    public async Task<IActionResult> GetMonth(int id)
    {
        var month = await _context.Months
            .Include(m => m.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (month == null)
            return NotFound();

        var mappedMonth = _mapper.Map<MonthDto>(month);
        
        return Ok(mappedMonth);
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetMonthForUser(string id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        var monthsForUser = await _context.Months
            .Where(m => m.ApplicationUserId == id)
            .Select(x => new ResponseMonthsForUser()
            {
                Id = x.Id,
                MonthNum = x.MonthNum,
                Year = x.Year,
                UserId = x.ApplicationUserId,
                Income = x.Items.Where(x => x.Type == 0).Sum(s => s.Value),
                Expense = x.Items.Where(x => x.Type != 0).Sum(s => s.Value),
                AdminChanged = x.Items.Any(i => i.CreatedBy != id) // true or false
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMonth(int id)
    {
        var existMonth = await _context.Months
            .Include(m => m.Items)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existMonth == null)
            return NotFound();

        _context.Months.Remove(existMonth);
        await _context.SaveChangesAsync();
        var mappedMonth = _mapper.Map<MonthDto>(existMonth);

        return Ok(mappedMonth);
    }
}