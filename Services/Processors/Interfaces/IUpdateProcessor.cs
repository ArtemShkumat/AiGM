using System.Threading.Tasks;
using AiGMBackEnd.Models;
using System.Collections.Generic;

namespace AiGMBackEnd.Services.Processors
{
    public interface IUpdateProcessor
    {
        Task ProcessUpdatesAsync(List<ICreationHook> newEntities, Dictionary<string, IUpdatePayload> partialUpdates, string userId);
    }
} 