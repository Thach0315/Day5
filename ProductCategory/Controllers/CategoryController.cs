using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductCategory.Data;
using ProductCategory.Models;

namespace ProductCategory.Controllers
{
    public class CategoryController : Controller
    {
        // GET: CategoryController
        public ActionResult Index()
        {
            return View(SeedData.Categories.OrderBy(c => c.CategoryId).ToList());
        }

        // GET: CategoryController/Details/5
        public ActionResult Details(int id)
        {
            var category = SeedData.Categories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null) return NotFound();
            ViewBag.ProductCount = SeedData.Products.Count(p => p.CategoryId == id);
            return View(category);
        }

        // GET: CategoryController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CategoryController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Category category)
        {
            if (!ModelState.IsValid) return View(category);
            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError(nameof(category.CategoryName), "Category name is required.");
                return View(category);
            }
            category.CategoryId = SeedData.Categories.Count == 0 ? 1 : SeedData.Categories.Max(c => c.CategoryId) + 1;
            category.Products = null;
            SeedData.Categories.Add(category);
            return RedirectToAction(nameof(Index));
        }

        // GET: CategoryController/Edit/5
        public ActionResult Edit(int id)
        {
            var category = SeedData.Categories.FirstOrDefault(c => c.CategoryId == id);
            return category == null ? NotFound() : View(category);
        }

        // POST: CategoryController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Category updatedCategory)
        {
            if (id != updatedCategory.CategoryId) return BadRequest();
            if (!ModelState.IsValid) return View(updatedCategory);
            if (string.IsNullOrWhiteSpace(updatedCategory.CategoryName))
            {
                ModelState.AddModelError(nameof(updatedCategory.CategoryName), "Category name is required.");
                return View(updatedCategory);
            }
            var category = SeedData.Categories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null) return NotFound();
            category.CategoryName = updatedCategory.CategoryName;
            return RedirectToAction(nameof(Index));
        }

        // GET: CategoryController/Delete/5
        public ActionResult Delete(int id)
        {
            var category = SeedData.Categories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null) return NotFound();
            ViewBag.ProductCount = SeedData.Products.Count(p => p.CategoryId == id);
            return View(category);
        }

        // POST: CategoryController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var category = SeedData.Categories.FirstOrDefault(c => c.CategoryId == id);
            if (category == null) return NotFound();
            if (SeedData.Products.Any(p => p.CategoryId == id))
            {
                ModelState.AddModelError(string.Empty, "Cannot delete a category that still contains products.");
                ViewBag.ProductCount = SeedData.Products.Count(p => p.CategoryId == id);
                return View("Delete", category);
            }
            SeedData.Categories.Remove(category);
            return RedirectToAction(nameof(Index));
        }
    }
}
