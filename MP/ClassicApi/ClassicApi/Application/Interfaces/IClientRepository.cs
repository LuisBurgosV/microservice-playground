using ClassicApi.Application.DTOs;

namespace ClassicApi.Application.Interfaces
{
    public interface IClientRepository
    {
        // GET
        Task<ClientDto> GetClientByIdAsync(int clientId);

        Task<IEnumerable<ClientDto>> GetClientsByNameAsync(string clientName);

        Task<IEnumerable<ClientDto>> GetAllClientsAsync();

        // POST
        Task<ClientDto> CreateClientAsync(BaseClientDto BaseClientDto);

        // PUT
        Task<ClientDto> UpdateClientByIdAsync(int clientId, BaseClientDto BaseClientDto);

        // DELETE
        Task<bool> DeleteClientByIdAsync(int clientId);
    }
}
