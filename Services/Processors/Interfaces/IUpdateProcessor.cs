using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Processors
{
    public interface IUpdateProcessor
    {
        Task ProcessUpdatesAsync(JObject updates, string userId);
    }
} 