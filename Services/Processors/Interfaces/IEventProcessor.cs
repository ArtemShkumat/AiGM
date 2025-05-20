using System.Collections.Generic;
using System.Threading.Tasks;
using AiGMBackEnd.Models;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Processors
{
    public interface IEventProcessor : IEntityProcessor
    {
        Task<string> ValidateEventCreationData(JObject data);
    }
} 