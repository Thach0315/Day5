using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using ProductCategory.Data;
using ProductCategory.Models;
using System.Globalization;

namespace ProductCategory.Controllers
{
    public class ProductController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        private const int PageSize = 5;
        private const long MaxUploadSize = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".txt"
        };

        public ProductController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index(string? term, int page = 1, string sortBy = "ProductName", bool descending = false)
        {
            var query = from product in SeedData.Products
                        join category in SeedData.Categories on product.CategoryId equals category.CategoryId into productCategories
                        from category in productCategories.DefaultIfEmpty()
                        select new ProductViewModel
                        {
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            Price = product.ProductPrice,
                            CategoryName = category?.CategoryName ?? "(No category)",
                            DataFileName = product.DataFileName
                        };

            if (!string.IsNullOrWhiteSpace(term))
            {
                var searchTerm = term.Trim();
                query = query.Where(product =>
                    (product.ProductName ?? "").Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (product.CategoryName ?? "").Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    product.ProductId.ToString(CultureInfo.InvariantCulture).Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    product.Price.ToString(CultureInfo.InvariantCulture).Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            query = sortBy switch
            {
                "ProductId" => descending ? query.OrderByDescending(p => p.ProductId) : query.OrderBy(p => p.ProductId),
                "Price" => descending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "CategoryName" => descending ? query.OrderByDescending(p => p.CategoryName) : query.OrderBy(p => p.CategoryName),
                _ => descending ? query.OrderByDescending(p => p.ProductName) : query.OrderBy(p => p.ProductName)
            };

            var results = query.ToList();
            var pageCount = Math.Max(1, (int)Math.Ceiling(results.Count / (double)PageSize));
            page = Math.Clamp(page, 1, pageCount);

            ViewData["Term"] = term ?? "";
            ViewData["Page"] = page;
            ViewData["Pages"] = pageCount;
            ViewData["TotalCount"] = results.Count;
            ViewData["SortBy"] = sortBy;
            ViewData["Descending"] = descending;

            return View(results.Skip((page - 1) * PageSize).Take(PageSize).ToList());
        }

        [HttpGet]
        public IActionResult Search(string? term) => RedirectToAction(nameof(Index), new { term, page = 1 });

        public IActionResult Details(int id)
        {
            var product = SeedData.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();

            var category = SeedData.Categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);
            var viewModel = new ProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.ProductPrice,
                CategoryName = category?.CategoryName ?? "(No category)",
                DataFileName = product.DataFileName
            };
            return View(viewModel);
        }

        public IActionResult Create()
        {
            LoadCategories();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? upload)
        {
            ValidateUpload(upload);
            ValidateCategory(product.CategoryId);
            if (!ModelState.IsValid)
            {
                LoadCategories(product.CategoryId);
                return View(product);
            }

            if (upload is { Length: > 0 })
            {
                var saved = await SaveUploadAsync(upload);
                product.DataFile = saved.StoredName;
                product.DataFileName = saved.OriginalName;
            }

            product.ProductId = SeedData.Products.Count == 0 ? 1 : SeedData.Products.Max(p => p.ProductId) + 1;
            product.Category = null;
            SeedData.Products.Add(product);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var product = SeedData.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();
            LoadCategories(product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product updatedProduct, IFormFile? upload)
        {
            if (id != updatedProduct.ProductId) return BadRequest();
            ValidateUpload(upload);
            ValidateCategory(updatedProduct.CategoryId);
            if (!ModelState.IsValid)
            {
                LoadCategories(updatedProduct.CategoryId);
                return View(updatedProduct);
            }

            var product = SeedData.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();

            product.ProductName = updatedProduct.ProductName;
            product.ProductPrice = updatedProduct.ProductPrice;
            product.CategoryId = updatedProduct.CategoryId;
            if (upload is { Length: > 0 })
            {
                DeleteUpload(product.DataFile);
                var saved = await SaveUploadAsync(upload);
                product.DataFile = saved.StoredName;
                product.DataFileName = saved.OriginalName;
            }
            product.Category = null;
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var product = SeedData.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();
            var category = SeedData.Categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);
            ViewBag.CategoryName = category?.CategoryName ?? "(No category)";
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = SeedData.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null) return NotFound();
            DeleteUpload(product.DataFile);
            SeedData.Products.Remove(product);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Read(int id)
        {
            var product = SeedData.Products.FirstOrDefault(p => p.ProductId == id);
            if (product?.DataFile == null) return NotFound();

            var path = GetUploadPath(product.DataFile);
            if (!System.IO.File.Exists(path)) return NotFound();

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(product.DataFileName ?? product.DataFile, out var contentType))
                contentType = "application/octet-stream";

            return PhysicalFile(path, contentType, product.DataFileName ?? product.DataFile);
        }

        private void LoadCategories(int? selectedCategoryId = null)
        {
            ViewBag.Categories = new SelectList(SeedData.Categories, nameof(Category.CategoryId), nameof(Category.CategoryName), selectedCategoryId);
        }

        private void ValidateCategory(int categoryId)
        {
            if (!SeedData.Categories.Any(category => category.CategoryId == categoryId))
                ModelState.AddModelError(nameof(Product.CategoryId), "Please select a valid category.");
        }

        private void ValidateUpload(IFormFile? upload)
        {
            if (upload == null || upload.Length == 0) return;
            if (upload.Length > MaxUploadSize)
                ModelState.AddModelError("upload", "The file must be 10 MB or smaller.");
            if (!AllowedExtensions.Contains(Path.GetExtension(upload.FileName)))
                ModelState.AddModelError("upload", "Allowed file types: PDF, DOC, DOCX, and TXT.");
        }

        private async Task<(string StoredName, string OriginalName)> SaveUploadAsync(IFormFile upload)
        {
            var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
            var storedName = $"{Guid.NewGuid():N}{extension}";
            var directory = GetUploadDirectory();
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, storedName);
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await upload.CopyToAsync(stream);
            return (storedName, Path.GetFileName(upload.FileName));
        }

        private string GetUploadDirectory() => Path.Combine(_environment.ContentRootPath, "App_Data", "ProductFiles");
        private string GetUploadPath(string fileName) => Path.Combine(GetUploadDirectory(), Path.GetFileName(fileName));

        private void DeleteUpload(string? fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var path = GetUploadPath(fileName);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }
    }
}
