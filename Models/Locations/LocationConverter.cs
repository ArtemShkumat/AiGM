using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiGMBackEnd.Models.Locations
{
    public class LocationConverter : JsonConverter<Location>
    {
        public override Location Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // We need to look ahead to determine the location type
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var rootElement = jsonDoc.RootElement;
            
            // Try to get the type property
            if (rootElement.TryGetProperty("locationType", out var typeProperty))
            {
                string locationType = typeProperty.GetString();
                
                // Clone the JsonSerializerOptions but remove this converter to avoid infinite recursion
                var newOptions = new JsonSerializerOptions(options);
                foreach (var converter in options.Converters)
                {
                    if (converter is not LocationConverter)
                    {
                        newOptions.Converters.Add(converter);
                    }
                }
                
                // Based on the type, deserialize to the appropriate concrete class
                string json = rootElement.GetRawText();
                
                return locationType?.ToLower() switch
                {
                    "delve" => JsonSerializer.Deserialize<Delve>(json, newOptions),
                    "building" => JsonSerializer.Deserialize<Building>(json, newOptions),
                    "settlement" => JsonSerializer.Deserialize<Settlement>(json, newOptions),
                    "wilds" => JsonSerializer.Deserialize<Wilds>(json, newOptions),
                    _ => throw new JsonException($"Unknown location type: {locationType}")
                };
            }
            
            throw new JsonException("Could not determine location type");
        }

        public override void Write(Utf8JsonWriter writer, Location value, JsonSerializerOptions options)
        {
            // Create new options without this converter to avoid infinite recursion
            var newOptions = new JsonSerializerOptions(options);
            foreach (var converter in options.Converters)
            {
                if (converter is not LocationConverter)
                {
                    newOptions.Converters.Add(converter);
                }
            }
            
            // Serialize the concrete type
            switch (value)
            {
                case Delve delve:
                    JsonSerializer.Serialize(writer, delve, newOptions);
                    break;
                case Building building:
                    JsonSerializer.Serialize(writer, building, newOptions);
                    break;
                case Settlement settlement:
                    JsonSerializer.Serialize(writer, settlement, newOptions);
                    break;
                case Wilds wilds:
                    JsonSerializer.Serialize(writer, wilds, newOptions);
                    break;
                default:
                    throw new JsonException($"Unknown location type: {value.GetType().Name}");
            }
        }
    }
} 