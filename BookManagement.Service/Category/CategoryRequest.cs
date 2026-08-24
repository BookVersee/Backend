namespace BookManagement.Service.Category;

public class CreateCategoryRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? Status { get; set; }
}
