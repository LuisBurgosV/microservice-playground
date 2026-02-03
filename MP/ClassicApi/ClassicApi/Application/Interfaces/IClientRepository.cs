using ClassicApi.Application.DTOs;

namespace ClassicApi.Application.Interfaces
{
    public interface IClientRepository
    {
        // GET
        Task<ClientDto> GetClientByIdAsync(int clientId);

        Task<IEnumerable<ClientDto>> GetClientsByNameAsync(string clientName);

        Task<IEnumerable<ClientDto>> GetAllClientsAsync(int pageNumber = 1, int pageSize = 10);

        // POST
        Task<ClientDto> CreateClientAsync(BaseClientDto baseClientDto);

        // PUT
        Task<ClientDto> UpdateClientByIdAsync(int clientId, BaseClientDto baseClientDto);

        // DELETE
        Task<bool> DeleteClientByIdAsync(int clientId);
    }
}


