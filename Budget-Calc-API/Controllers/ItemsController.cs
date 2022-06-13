using aspnetcore_auth.Models.UI;
using Budget;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnetcore_auth.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class ItemsController : Controller
{
   private readonly ApplicationDbContext _context;

   public ItemsController(ApplicationDbContext context)
   {
      _context = context;
   }
   
   [HttpGet]
   [Route("")] // api/items/getitems
   [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
   public async Task<IActionResult> GetItems()
   {
      var items = await _context.Items.ToListAsync();
      return Ok(items);
   }

   [HttpGet("{id:int}")]
   public async Task<IActionResult> GetItem(int id)
   {
      var item = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);

      if (item == null)
         return NotFound();

      return Ok(item);
   }

   [HttpGet("{id:int}")]
   public async Task<IActionResult> GetIncomeItemsForMonth(int id)
   {
      var month = await _context.Months.FirstOrDefaultAsync(m => m.Id == id);
      if (month == null)
         return NotFound();

      var itemsMonth = await _context.Items
         .Where(i => i.MonthId == id)
         .Where(i => i.Type == 0)
         .ToListAsync();

      // var calcInc = itemsMonth.Sum(x => x.Value);
      //
      // {
      //    totalSum;
      //    List<>
      // }

      return Ok(itemsMonth);
   }
   
   [HttpGet("{id:int}")]
   public async Task<IActionResult> GetExpenseItemsForMonth(int id)
   {
      var month = await _context.Months.FirstOrDefaultAsync(m => m.Id == id);
      if (month == null)
         return NotFound();

      var itemsMonth = await _context.Items
         .Where(i => i.MonthId == id)
         .Where(i => i.Type != 0)
         .ToListAsync();

      return Ok(itemsMonth);
   }

   [HttpPost]
   [Route("")]
   public async Task<IActionResult> CreateItem(ItemUserData userData)
   {
      if (!ModelState.IsValid)
         return new JsonResult("Model is not valid") {StatusCode = 500};

      var newItem = new Item()
      {
         CreateDate = DateTime.UtcNow,
         UpdateDate = DateTime.UtcNow,
         CreatedBy = userData.CreatedBy,
         Value = userData.Value,
         Description = userData.Description,
         Type = userData.Type,
         MonthId = userData.MonthId
      };
      
      await _context.Items.AddAsync(newItem);
      await _context.SaveChangesAsync();

      return CreatedAtAction("GetItem", new { newItem.Id }, newItem);
   }

   [HttpPut]
   [Route("{id:int}")]
   public async Task<IActionResult> UpdateItem(int id, UpdateItemUserData item)
   {
      var existItem = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);

      if (existItem == null)
         return NotFound();
      
      existItem.UpdateDate = DateTime.UtcNow;
      existItem.CreatedBy = item.CreatedBy;
      existItem.Value = item.Value;
      existItem.Description = item.Description;

      await _context.SaveChangesAsync();

      return NoContent();
      // return Ok(existItem);
   }

   [HttpDelete]
   [Route("{id:int}")]
   public async Task<IActionResult> DeleteItem(int id)
   {
      var existItem = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);

      if (existItem == null)
         return NotFound();

      _context.Items.Remove(existItem);
      await _context.SaveChangesAsync();

      return Ok(existItem);
   }
}