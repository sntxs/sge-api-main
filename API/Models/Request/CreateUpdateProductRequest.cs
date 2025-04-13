namespace API.Models.Request
{
    public class CreateUpdateProductRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid UserId { get; set; }
        public int Quantity { get; set; }
        public Guid CategoryId { get; set; }
    }
}
