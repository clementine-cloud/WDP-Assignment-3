using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetCoreMvcWebSite.Data;
using AspNetCoreMvcWebSite.Models;

namespace AspNetCoreMvcWebSite.Controllers
{
    // Who can do what (from the brief):
    //   Visitor (not logged in) : Index (list, no buttons)
    //   Member  (logged in)     : Index + Like button + Create
    //   Admin   (role "admin")  : everything - Details, Edit, Delete as well
    public class AIImagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AIImagesController> _logger;

        // Only real picture files are accepted, and nothing bigger than 5 MB.
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public AIImagesController(ApplicationDbContext context, IWebHostEnvironment env, ILogger<AIImagesController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: AIImages  - everyone can see the list, sorted by likes (highest first)
        public async Task<IActionResult> Index()
        {
            // Logged-out visitors have no likes, so clear the remembered likes.
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                LikeTracker.Clear(HttpContext.Session);
            }

            try
            {
                var liked = LikeTracker.Get(HttpContext.Session);
                var images = await _context.AIImage
                    .OrderByDescending(i => i.Like)
                    .ThenByDescending(i => i.UploadDate)
                    .ToListAsync();

                // The Like button is only enabled for blocks this user has not liked yet.
                foreach (var image in images)
                {
                    image.canIncreaseLike = !liked.Contains(image.Id);
                }

                return View(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load the AI image list.");
                ViewBag.ErrorMessage = "Sorry, the images could not be loaded right now. Please try again later.";
                return View(new List<AIImage>());
            }
        }

        // GET: AIImages/Details/5  (admin only)
        [Authorize(Roles = SeedData.AdminRole)]
        public async Task<IActionResult> Details(int? id)
        {
            var aiImage = await FindImageAsync(id);
            if (aiImage == null)
            {
                return NotFound();
            }
            return View(aiImage);
        }

        // GET: AIImages/Create  (any logged-in user)
        [Authorize]
        public IActionResult Create()
        {
            // Upload date is set automatically and shown read-only.
            return View(new AIImage { UploadDate = DateTime.Now });
        }

        // POST: AIImages/Create
        // [Bind] lists the only fields the user is allowed to send. UploadDate, Like and Filename
        // are controlled by the server, so a user cannot cheat by posting a fake like count (over-posting).
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("Prompt,ImageGenerator")] AIImage aiImage, IFormFile? imageFile)
        {
            // These two are not typed by the user, so they must not fail validation.
            ModelState.Remove(nameof(AIImage.Filename));
            ModelState.Remove(nameof(AIImage.UploadDate));

            var uploadError = ValidateImageFile(imageFile);
            if (uploadError != null)
            {
                ModelState.AddModelError("imageFile", uploadError);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    aiImage.UploadDate = DateTime.Now;
                    aiImage.Like = 0;
                    aiImage.Filename = (imageFile != null && imageFile.Length > 0)
                        ? await SaveImageFileAsync(imageFile)
                        : "";

                    _context.Add(aiImage);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not save a new AI image.");
                    ModelState.AddModelError(string.Empty, "Sorry, your image could not be saved. Please try again.");
                }
            }

            aiImage.UploadDate = DateTime.Now;
            return View(aiImage);
        }

        // GET: AIImages/Edit/5  (admin only)
        [Authorize(Roles = SeedData.AdminRole)]
        public async Task<IActionResult> Edit(int? id)
        {
            var aiImage = await FindImageAsync(id);
            if (aiImage == null)
            {
                return NotFound();
            }
            return View(aiImage);
        }

        // POST: AIImages/Edit/5  (admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SeedData.AdminRole)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Prompt,ImageGenerator")] AIImage aiImage, IFormFile? imageFile)
        {
            if (id != aiImage.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(AIImage.Filename));
            ModelState.Remove(nameof(AIImage.UploadDate));

            var uploadError = ValidateImageFile(imageFile);
            if (uploadError != null)
            {
                ModelState.AddModelError("imageFile", uploadError);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Load the real row and only change the fields the admin is allowed to edit.
                    var existing = await _context.AIImage.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.Prompt = aiImage.Prompt;
                    existing.ImageGenerator = aiImage.ImageGenerator;

                    // A new file is optional. If one is chosen, replace the old picture on disk.
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var oldFilename = existing.Filename;
                        existing.Filename = await SaveImageFileAsync(imageFile);
                        DeleteImageFile(oldFilename);
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not update AI image {Id}.", id);
                    ModelState.AddModelError(string.Empty, "Sorry, your changes could not be saved. Please try again.");
                }
            }

            // Show the read-only values again when the form is redisplayed.
            var current = await _context.AIImage.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
            if (current != null)
            {
                aiImage.UploadDate = current.UploadDate;
                aiImage.Like = current.Like;
                aiImage.Filename = current.Filename;
            }
            return View(aiImage);
        }

        // GET: AIImages/Delete/5  (admin only)
        [Authorize(Roles = SeedData.AdminRole)]
        public async Task<IActionResult> Delete(int? id)
        {
            var aiImage = await FindImageAsync(id);
            if (aiImage == null)
            {
                return NotFound();
            }
            return View(aiImage);
        }

        // POST: AIImages/Delete/5  (admin only)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SeedData.AdminRole)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var aiImage = await _context.AIImage.FindAsync(id);
                if (aiImage != null)
                {
                    var filename = aiImage.Filename;
                    _context.AIImage.Remove(aiImage);
                    await _context.SaveChangesAsync();
                    DeleteImageFile(filename); // also remove the picture from wwwroot/uploads
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not delete AI image {Id}.", id);
                TempData["ErrorMessage"] = "Sorry, the image could not be deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: AIImages/IncreaseLike/5  (any logged-in user, max one like per block per login)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> IncreaseLike(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var liked = LikeTracker.Get(HttpContext.Session);
                if (!liked.Contains(id.Value))
                {
                    var aiImage = await _context.AIImage.FindAsync(id.Value);
                    if (aiImage == null)
                    {
                        return NotFound();
                    }

                    aiImage.Like++;
                    await _context.SaveChangesAsync();

                    liked.Add(id.Value);
                    LikeTracker.Save(HttpContext.Session, liked);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not like AI image {Id}.", id);
            }

            // The "#block-5" part (a fragment) scrolls the page to the block that was just liked,
            // even though the list has been re-sorted.
            return RedirectToAction(nameof(Index), "AIImages", null, $"block-{id}");
        }

        // ---------------- helper methods ----------------

        private async Task<AIImage?> FindImageAsync(int? id)
        {
            if (id == null)
            {
                return null;
            }
            return await _context.AIImage.FirstOrDefaultAsync(i => i.Id == id);
        }

        // Returns an error message, or null when the file is fine (or no file was chosen).
        private static string? ValidateImageFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null; // choosing a file is optional
            }
            if (file.Length > MaxFileSize)
            {
                return "The image is too large. Please choose a file under 5 MB.";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension) || !file.ContentType.StartsWith("image/"))
            {
                return "Please choose a picture file (.jpg, .jpeg, .png, .gif or .webp).";
            }
            return null;
        }

        // Saves the picture into wwwroot/uploads and returns the new file name.
        // A short random suffix stops two files with the same name overwriting each other.
        private async Task<string> SaveImageFileAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var baseName = Regex.Replace(Path.GetFileNameWithoutExtension(file.FileName), "[^a-zA-Z0-9_-]", "_");
            var filename = $"{baseName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{extension}";

            var fullPath = Path.Combine(uploadsFolder, filename);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return filename;
        }

        // Deletes a picture from wwwroot/uploads. Never throws: a missing file is not a reason to crash.
        private void DeleteImageFile(string? filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return;
            }

            try
            {
                // Path.GetFileName strips any folder parts, so only files inside /uploads can be removed.
                var fullPath = Path.Combine(_env.WebRootPath, "uploads", Path.GetFileName(filename));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete the file {Filename}.", filename);
            }
        }
    }
}
