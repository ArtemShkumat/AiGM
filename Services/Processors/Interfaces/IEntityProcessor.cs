using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AiGMBackEnd.Services.Processors
{
    public interface IEntityProcessor
    {
        Task ProcessAsync(JObject data, string userId);
    }
} 