using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookManagement.Repository.Abstractions
{
    public class BaseEntity <T>
    {
        public T Id { get; set; } = default!;
        public bool IsDeleted { get; set; }
    }
}