using API.Models.Request;
using API.Models.Response;
using MySql.Data.MySqlClient;

namespace API.Services
{
    public class ProductRequestService
    {
        private readonly IConfiguration _configuration;

        public ProductRequestService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Create(CreateProductRequestRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT Quantity FROM Product WHERE Id = @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", request.ProductId);

                    int currentQuantity = 0;

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                        {
                            currentQuantity = reader.GetInt32("Quantity");
                            if (request.Quantity > currentQuantity)
                                throw new Exception("Quantidade do produto excede o máximo disponível.");
                        }
                        else
                            throw new Exception("Produto não encontrado.");
                    }

                    query = "SELECT 1 FROM User where Id = @id";

                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", request.UserId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Usuário não encontrado.");
                    }

                    query = "INSERT INTO ProductRequest (Id, UserId, ProductId, Quantity, CreatedAt) VALUES (UUID(), @userId, @productId, @quantity, @date)";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@userId", request.UserId);
                    cmd.Parameters.AddWithValue("@productId", request.ProductId);
                    cmd.Parameters.AddWithValue("@quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@date", DateTime.UtcNow.AddHours(-3));

                    await cmd.ExecuteNonQueryAsync();

                    int newQuantity = currentQuantity - request.Quantity;

                    query = "UPDATE Product SET Quantity = @quantity where Id = @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@quantity", newQuantity);
                    cmd.Parameters.AddWithValue("@id", request.ProductId);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task<List<GetProductRequestResponse>> Get()
        {
            var items = new List<GetProductRequestResponse>();

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                //    string query = @"
                //SELECT 
                //    a.Id, 
                //    a.Quantity, 
                //    a.CreatedAt, 
                //    b.Name as UserName, 
                //    b.SectorName, 
                //    c.Name as ProductName 
                //FROM 
                //    ProductRequest a
                //JOIN 
                //    User b ON a.UserId = b.Id
                //JOIN 
                //    Product c ON a.ProductId = c.Id";

                    string query = "SELECT a.*, b.Name AS UserName, c.Name AS ProductName, d.Id AS SectorId, d.Name AS SectorName, d.CreatedAt AS SectorCreatedAt " +
                        "FROM ProductRequest a, User b, Product c, Sector d WHERE a.UserId = b.Id AND c.Id = a.ProductId AND b.SectorId = d.Id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new GetProductRequestResponse
                            {
                                Id = reader.GetGuid("Id"),
                                UserName = reader.GetString("UserName"),
                                ProductName = reader.GetString("ProductName"),
                                Quantity = reader.GetInt32("Quantity"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                UserSector = new GetSectorReponse()
                                {
                                    Id = reader.GetGuid("SectorId"),
                                    Name = reader.GetString("SectorName"),
                                    CreatedAt = reader.GetDateTime("SectorCreatedAt")
                                },
                            };

                            items.Add(item);
                        }
                    }

                    return items;
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }
    }
}
