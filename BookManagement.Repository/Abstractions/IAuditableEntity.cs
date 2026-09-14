using System;

namespace BookManagement.Repository.Abstractions
{
    public interface IAuditableEntity
    {
        DateTimeOffset CreatedAt { get; set; }
        DateTimeOffset? UpdatedAt { get; set; }
    }
}
