using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task DoSomethingAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 1);
            command.Parameters.AddWithValue("@Name", "Animal1");
        
            await command.ExecuteNonQueryAsync();
        
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 2);
            command.Parameters.AddWithValue("@Name", "Animal2");
        
            await command.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task ProcedureAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        command.CommandText = "NazwaProcedury";
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.AddWithValue("@Id", 2);
        
        await command.ExecuteNonQueryAsync();
        
    }

    public async Task<int> RegisterProductAsync(RegisterProductDTO dto)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = (SqlTransaction)transaction;

        try
        {
            command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);

            var productExists = await command.ExecuteScalarAsync();
            if (productExists is null)
                throw new InvalidOperationException("Produkt nie istnieje.");

            command.Parameters.Clear();

            command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            command.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);

            var warehouseExists = await command.ExecuteScalarAsync();
            if (warehouseExists is null)
                throw new InvalidOperationException("Magazyn nie istnieje.");

            command.Parameters.Clear();

            if (dto.Amount <= 0)
                throw new ArgumentException("Amount musi być > 0.");

            command.CommandText = """
                                      SELECT IdOrder FROM [Order]
                                      WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt
                                  """;
            command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
            command.Parameters.AddWithValue("@Amount", dto.Amount);
            command.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);

            var idOrderObj = await command.ExecuteScalarAsync();
            if (idOrderObj is null)
                throw new InvalidOperationException("Zamówienie nie istnieje.");

            int idOrder = Convert.ToInt32(idOrderObj);
            command.Parameters.Clear();

            command.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", idOrder);

            var alreadyAssigned = await command.ExecuteScalarAsync();
            if (alreadyAssigned is not null)
                throw new InvalidOperationException("Zamówienie zostało już zrealizowane.");

            command.Parameters.Clear();

            command.CommandText = "UPDATE [Order] SET FulfilledAt = @CreatedAt WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            await command.ExecuteNonQueryAsync();
            command.Parameters.Clear();

            command.CommandText = """
                                      INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                                      OUTPUT INSERTED.IdProductWarehouse
                                      VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount,
                                      (SELECT Price FROM Product WHERE IdProduct = @IdProduct) * @Amount,
                                      @CreatedAt)
                                  """;
            command.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", idOrder);
            command.Parameters.AddWithValue("@Amount", dto.Amount);
            command.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);
            var newId = await command.ExecuteScalarAsync();

            await transaction.CommitAsync();

            return Convert.ToInt32(newId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<int> ProcedureAsync(RegisterProductDTO dto)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();

        await using var command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", dto.Amount);
        command.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);

        var result = await command.ExecuteReaderAsync();

        if (await result.ReadAsync())
        {
            return result.GetInt32(0);
        }

        throw new Exception("Procedura nie zwróciła żadnego wyniku.");
    }

}