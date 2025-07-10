using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SaleManagement.Entities.Enums;
using SaleManagement.Services;
using SaleManagement.Schemas; 

namespace SaleManagement.Controllers;
[ApiController]
[Route("api/[controller]")]

public class CategoryController:ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryService.GetAllAsTreeAsync();
        return Ok(categories);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var category = await _categoryService.GetCategoryAsync(id);
        return category == null ? NotFound() : Ok(category);
    }
    
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))] // Chỉ Admin được tạo Category
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var newCategory = await _categoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = newCategory.Id }, newCategory);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))] // Chỉ Admin được sửa Category
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _categoryService.UpdateAsync(id, request);
        return result ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = nameof(UserRole.Admin))] // Chỉ Admin được xóa Category
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _categoryService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

}