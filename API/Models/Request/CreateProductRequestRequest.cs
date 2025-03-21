namespace API.Models.Request
{
    public class CreateProductRequestRequest
    {
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
