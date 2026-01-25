using ClassicApi.Application.DTOs;
using ClassicApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClassicApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private const string routeName = "{name}";
        private const string routeId = "{id}";
        // TODO: first call client service
        private readonly IClientRepository _clientRepository;

        public ClientsController(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        [HttpGet]
        [Route(routeId)]
        public async Task<IActionResult> GetClientById(int id)
        {
            var client = await _clientRepository.GetClientByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            return Ok(client);
        }

        [HttpGet]
        [Route(routeName)]
        public async Task<IActionResult> GetClientByName(string name)
        {
            var clients = await _clientRepository.GetClientsByNameAsync(name);
            if (clients == null || clients.Count() == 0)
            {
                return NotFound();
            }
            return Ok(clients);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllClients()
        {
            var clients = await _clientRepository.GetAllClientsAsync();

            if (clients == null || clients.Count() == 0)
            {
                return NotFound();
            }

            return Ok(clients);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] ClientCreateDto clientCreateDto)
        {
            var createdClient = await _clientRepository.CreateClientAsync(clientCreateDto);

            if (createdClient == null) {
                return BadRequest("Client could not be created.");
            }

            return Ok(createdClient);
        }

        [HttpPut]
        [Route(routeId)]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] ClientUpdateDto clientUpdateDto)
        {
            var updatedClient = await _clientRepository.UpdateClientByIdAsync(id, clientUpdateDto);
            if (updatedClient == null)
            {
                return NotFound();
            }
            return Ok(updatedClient);
        }

        [HttpDelete]
        [Route(routeId)]
        public async Task<IActionResult> DeleteClient(int id)
        {
            var isDeleted = await _clientRepository.DeleteClientByIdAsync(id);
            if (!isDeleted)
            {
                return NotFound();
            }
            return Ok("client deleted successfully");
        }
    }
}
