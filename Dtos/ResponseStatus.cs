using Newtonsoft.Json;

namespace QueenOfDreamer.API.Dtos
{
    public class ResponseStatus
    {
        [JsonIgnore]
        public int StatusCode { get; set; }

        public string Message { get; set; }
    }
}
