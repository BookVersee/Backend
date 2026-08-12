namespace BookManagement.Repository.Entities.Enums
{
    public enum ReturnStatus
    {
        NONE,
        REQUESTED,
        PENDING,
        REJECTED,
        PROCESSING,
        SHIPPED,
        DELIVERED,
        CANCELLED,
        COMPLETED,
        REFUNDED
    }
}
