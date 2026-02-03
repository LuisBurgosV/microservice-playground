using ClassicApi.Application.DTOs;

namespace ClassicApi.Application.Interfaces
{
    public interface IClientService
    {
        Task<ClientDto> GetClientByIdAsync(int id);

        Task<IEnumerable<ClientDto>> GetClientsByNameAsync(string name);

        Task<IEnumerable<ClientDto>> GetAllClientsAsync(int pageNumber = 1, int pageSize = 10);

        Task<ClientDto> CreateClientAsync(BaseClientDto baseClientDto);

        Task<ClientDto> UpdateClientByIdAsync(int id, BaseClientDto baseClientDto);

        Task<bool> DeleteClientByIdAsync(int id);
    }
}



