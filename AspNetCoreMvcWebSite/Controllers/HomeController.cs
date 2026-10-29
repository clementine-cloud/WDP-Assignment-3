using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetCoreMvcWebSite.Data;
using AspNetCoreMvcWebSite.Models;

namespace AspNetCoreMvcWebSite.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    // Home page: also shows a thumbnail for every uploaded AI image, most-liked first.
    public async Task<IActionResult> Index()
    {
        // A visitor who is not logged in has no likes (this is how "log out, log in" resets the likes).
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            LikeTracker.Clear(HttpContext.Session);
        }

        try
        {
            var images = await _context.AIImage
                .OrderByDescending(i => i.Like)
                .ThenByDescending(i => i.UploadDate)
                .ToListAsync();
            return View(images);
        }
        catch (Exception ex)
        {
            // If the database is not ready, still show the page (without thumbnails).
            _logger.LogError(ex, "Could not load AI images for the home page.");
            return View(new List<AIImage>());
        }
    }

    public IActionResult Contact()
    {
        return View();
    }

    // The extra creative page (design chosen by me): an AI prompt builder.
    public IActionResult PromptLab()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        ViewBag.StatusCode = statusCode;
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
