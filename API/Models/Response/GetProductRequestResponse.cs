namespace API.Models.Response
{
    public class GetProductRequestResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public GetSectorReponse UserSector { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool Delivered { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
