using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models
{
    public class TriggerValueConverter : JsonConverter<TriggerValue>
    {
        private const string TYPE_PROPERTY = "type";
        
        public override TriggerValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected start of object");
            }
            
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;
            
            if (!root.TryGetProperty(TYPE_PROPERTY, out var typeProperty))
            {
                throw new JsonException($"Missing '{TYPE_PROPERTY}' property");
            }
            
            var type = typeProperty.GetString();
            
            return type switch
            {
                "time" => JsonSerializer.Deserialize<TimeTriggerValue>(root.GetRawText(), options),
                "location" => JsonSerializer.Deserialize<LocationTriggerValue>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown trigger value type: {type}")
            };
        }
        
        public override void Write(Utf8JsonWriter writer, TriggerValue value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case TimeTriggerValue timeTrigger:
                    JsonSerializer.Serialize(writer, timeTrigger, options);
                    break;
                case LocationTriggerValue locationTrigger:
                    JsonSerializer.Serialize(writer, locationTrigger, options);
                    break;
                default:
                    throw new JsonException($"Unknown trigger value type: {value.GetType().Name}");
            }
        }
    }
} 