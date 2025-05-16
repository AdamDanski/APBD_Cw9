using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IDbService
{
    Task DoSomethingAsync();
    Task ProcedureAsync();
    Task<int> RegisterProductAsync(RegisterProductDTO dto);
    Task<int> ProcedureAsync(RegisterProductDTO dto);

}