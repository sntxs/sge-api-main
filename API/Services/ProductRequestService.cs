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

                    string query = "SELECT a.*, " +
                        "b.Name AS UserName, " +
                        "c.Name AS ProductName, " +
                        "d.Id AS SectorId, " +
                        "d.Name AS SectorName, " +
                        "d.CreatedAt AS SectorCreatedAt, " +
                        "e.Id AS CategoryId, " +
                        "e.Name AS CategoryName " +
                        "FROM ProductRequest a " +
                        "JOIN User b ON a.UserId = b.Id " +
                        "JOIN Product c ON a.ProductId = c.Id " +
                        "JOIN Sector d ON b.SectorId = d.Id " +
                        "JOIN Category e ON c.CategoryId = e.Id";

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
                                CategoryId = reader.GetGuid("CategoryId"),
                                CategoryName = reader.GetString("CategoryName"),
                                UserSector = new GetSectorReponse()
                                {
                                    Id = reader.GetGuid("SectorId"),
                                    Name = reader.GetString("SectorName"),
                                    CreatedAt = reader.GetDateTime("SectorCreatedAt")
                                },
                                Delivered = reader.GetBoolean("Delivered"),
                                DeliveredAt = reader.IsDBNull(reader.GetOrdinal("DeliveredAt")) ? null : reader.GetDateTime("DeliveredAt")
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

        public async Task Update(Guid id, UpdateProductRequestRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();
                    
                    // Verificar se a requisição existe
                    string query = "SELECT ProductId, Quantity FROM ProductRequest WHERE Id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    
                    Guid currentProductId;
                    int currentRequestQuantity;
                    
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Requisição de produto não encontrada.");
                        
                        currentProductId = reader.GetGuid("ProductId");
                        currentRequestQuantity = reader.GetInt32("Quantity");
                    }
                    
                    // Verificar a quantidade disponível do produto
                    query = "SELECT Quantity FROM Product WHERE Id = @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", currentProductId);
                    
                    int productAvailableQuantity;
                    
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Produto não encontrado.");
                        
                        productAvailableQuantity = reader.GetInt32("Quantity");
                    }
                    
                    // Calcular a diferença de quantidade
                    int quantityDifference = request.Quantity - currentRequestQuantity;
                    
                    // Verificar se há quantidade suficiente disponível
                    if (quantityDifference > 0 && quantityDifference > productAvailableQuantity)
                        throw new Exception("Quantidade do produto excede o máximo disponível.");
                    
                    // Atualizar a requisição
                    query = "UPDATE ProductRequest SET Quantity = @quantity WHERE Id = @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@id", id);
                    
                    await cmd.ExecuteNonQueryAsync();
                    
                    // Atualizar a quantidade do produto
                    int newProductQuantity = productAvailableQuantity - quantityDifference;
                    
                    query = "UPDATE Product SET Quantity = @quantity WHERE Id = @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@quantity", newProductQuantity);
                    cmd.Parameters.AddWithValue("@id", currentProductId);
                    
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task Delete(Guid id)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();
                    
                    // Verificar se a requisição existe e obter detalhes
                    string query = "SELECT ProductId, Quantity FROM ProductRequest WHERE Id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    
                    Guid productId;
                    int requestQuantity;
                    
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Requisição de produto não encontrada.");
                        
                        productId = reader.GetGuid("ProductId");
                        requestQuantity = reader.GetInt32("Quantity");
                    }
                    
                    // Obter a quantidade atual do produto
                    query = "SELECT Quantity FROM Product WHERE Id = @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", productId);
                    
                    int currentProductQuantity;
                    
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Produto não encontrado.");
                        
                        currentProductQuantity = reader.GetInt32("Quantity");
                    }
                    
                    // Excluir a requisição
                    query = "DELETE FROM ProductRequest WHERE Id = @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    
                    await cmd.ExecuteNonQueryAsync();
                    
                    // Devolver a quantidade ao produto
                    int newProductQuantity = currentProductQuantity + requestQuantity;
                    
                    query = "UPDATE Product SET Quantity = @quantity WHERE Id = @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@quantity", newProductQuantity);
                    cmd.Parameters.AddWithValue("@id", productId);
                    
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task<GetProductRequestResponse> GetById(Guid id)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT a.*, " +
                        "b.Name AS UserName, " +
                        "c.Name AS ProductName, " +
                        "d.Id AS SectorId, " +
                        "d.Name AS SectorName, " +
                        "d.CreatedAt AS SectorCreatedAt, " +
                        "e.Id AS CategoryId, " +
                        "e.Name AS CategoryName " +
                        "FROM ProductRequest a " +
                        "JOIN User b ON a.UserId = b.Id " +
                        "JOIN Product c ON a.ProductId = c.Id " +
                        "JOIN Sector d ON b.SectorId = d.Id " +
                        "JOIN Category e ON c.CategoryId = e.Id " +
                        "WHERE a.Id = @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                        {
                            var item = new GetProductRequestResponse
                            {
                                Id = reader.GetGuid("Id"),
                                UserName = reader.GetString("UserName"),
                                ProductName = reader.GetString("ProductName"),
                                Quantity = reader.GetInt32("Quantity"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                CategoryId = reader.GetGuid("CategoryId"),
                                CategoryName = reader.GetString("CategoryName"),
                                UserSector = new GetSectorReponse()
                                {
                                    Id = reader.GetGuid("SectorId"),
                                    Name = reader.GetString("SectorName"),
                                    CreatedAt = reader.GetDateTime("SectorCreatedAt")
                                },
                                Delivered = reader.GetBoolean("Delivered"),
                                DeliveredAt = reader.IsDBNull(reader.GetOrdinal("DeliveredAt")) ? null : reader.GetDateTime("DeliveredAt")
                            };

                            return item;
                        }
                        
                        throw new Exception("Requisição de produto não encontrada.");
                    }
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task MarkAsDelivered(Guid id)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();
                    
                    // Verificar se a requisição existe
                    string query = "SELECT 1 FROM ProductRequest WHERE Id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Requisição de produto não encontrada.");
                    }
                    
                    // Atualizar a requisição como entregue
                    query = "UPDATE ProductRequest SET Delivered = 1, DeliveredAt = @deliveredAt WHERE Id = @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@deliveredAt", DateTime.UtcNow.AddHours(-3));
                    cmd.Parameters.AddWithValue("@id", id);
                    
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task CancelDelivery(Guid id)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();
                    
                    // Verificar se a requisição existe e está marcada como entregue
                    string query = "SELECT Delivered FROM ProductRequest WHERE Id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Requisição de produto não encontrada.");
                        
                        bool isDelivered = reader.GetBoolean("Delivered");
                        if (!isDelivered)
                            throw new Exception("Esta requisição não está marcada como entregue.");
                    }
                    
                    // Cancelar a entrega
                    query = "UPDATE ProductRequest SET Delivered = 0, DeliveredAt = NULL WHERE Id = @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);
                    
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }
    }
}
