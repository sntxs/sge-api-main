using API.Models.Request;
using API.Models.Response;
using MySql.Data.MySqlClient;

namespace API.Services
{
    public class ProductService
    {
        private readonly IConfiguration _configuration;

        public ProductService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Create(CreateUpdateProductRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT 1 FROM Product WHERE Name = @name";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                            throw new Exception("Nome de produto já está sendo usado.");
                    }

                    query = "SELECT 1 FROM User where Id = @id";

                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", request.UserId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Usuário não encontrado.");
                    }
                    
                    query = "SELECT 1 FROM Category where Id = @id";

                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", request.CategoryId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Categoria não encontrada.");
                    }

                    query = "INSERT INTO Product (Id, Name, Description, UserId, Quantity, CategoryId, CreatedAt) VALUES (UUID(), @name, @description, @userId, @quantity, @categoryId, @date)";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);
                    cmd.Parameters.AddWithValue("@description", request.Description);
                    cmd.Parameters.AddWithValue("@userId", request.UserId);
                    cmd.Parameters.AddWithValue("@quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@categoryId", request.CategoryId);
                    cmd.Parameters.AddWithValue("@date", DateTime.UtcNow.AddHours(-3));

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task<List<GetProductResponse>> Get()
        {
            var items = new List<GetProductResponse>();

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT a.*, b.Name as UserName, c.Name as CategoryName FROM Product a " +
                                  "JOIN User b ON a.UserId = b.Id " +
                                  "JOIN Category c ON a.CategoryId = c.Id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new GetProductResponse
                            {
                                Id = reader.GetGuid("Id"),
                                Name = reader.GetString("Name"),
                                Description = reader.GetString("Description"),
                                Quantity = reader.GetInt32("Quantity"),
                                UserName = reader.GetString("UserName"),
                                CategoryId = reader.GetGuid("CategoryId"),
                                CategoryName = reader.GetString("CategoryName"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
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

        public async Task<GetProductResponse> GetById(Guid id)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT a.*, b.Name as UserName, c.Name as CategoryName FROM Product a " +
                                  "JOIN User b ON a.UserId = b.Id " +
                                  "JOIN Category c ON a.CategoryId = c.Id " +
                                  "WHERE a.id = @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            var product = new GetProductResponse
                            {
                                Id = reader.GetGuid("Id"),
                                Name = reader.GetString("Name"),
                                Description = reader.GetString("Description"),
                                UserName = reader.GetString("UserName"),
                                Quantity = reader.GetInt32("Quantity"),
                                CategoryId = reader.GetGuid("CategoryId"),
                                CategoryName = reader.GetString("CategoryName"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                            };

                            return product;
                        }

                        throw new Exception("Não foi possível encontrar o item.");
                    }
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task Update(Guid id, CreateUpdateProductRequest request)
        {
            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT 1 FROM Product WHERE Name = @name AND Id <> @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);
                    cmd.Parameters.AddWithValue("@id", id);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (await reader.ReadAsync())
                            throw new Exception("Nome de produto já está sendo usado.");
                    }
                    
                    query = "SELECT 1 FROM Category where Id = @id";

                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", request.CategoryId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!await reader.ReadAsync())
                            throw new Exception("Categoria não encontrada.");
                    }

                    query = "UPDATE Product SET Name = @name, Description = @description, Quantity = @quantity, CategoryId = @categoryId WHERE Id = @id";

                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", request.Name);
                    cmd.Parameters.AddWithValue("@description", request.Description);
                    cmd.Parameters.AddWithValue("@quantity", request.Quantity);
                    cmd.Parameters.AddWithValue("@categoryId", request.CategoryId);
                    cmd.Parameters.AddWithValue("@id", id);

                    int result = await cmd.ExecuteNonQueryAsync();

                    if (result != 1)
                        throw new Exception("Não foi possível atualizar o produto. Caso o problema persistir, contate o administrador.");
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }

        public async Task Delete(Guid id)
        {
            //pegar usuário que está DELETANDO e ver se é admin

            using (MySqlConnection connection = new MySqlConnection(_configuration["ConnectionString"]))
            {
                try
                {
                    connection.Open();

                    string query = "DELETE FROM Product WHERE Id = @id";

                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", id);

                    var result = await cmd.ExecuteNonQueryAsync();

                    if (result != 1)
                        throw new Exception("Não foi possível excluir o produto. Caso o problema persistir, contate o administrador.");
                }
                catch (MySqlException ex)
                {
                    throw new Exception("Erro ao conectar com o banco de dados: " + ex.Message);
                }
            }
        }
    }
}
