using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApiHelpFast.Models;
using ApiHelpFast.Data;

namespace ApiHelpFast.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class FaqsController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public FaqsController(ApplicationDbContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			// Retorna apenas pergunta e resposta em JSON
			var faqs = await _context
				.Set<Faq>()
				.Select(f => new { pergunta = f.Pergunta, resposta = f.Resposta })
				.ToListAsync();

			return Ok(faqs);
		}
	}
}
