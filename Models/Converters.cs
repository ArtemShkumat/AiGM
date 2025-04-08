using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic; // Required for Dictionary
using System.Linq; // Required for FirstOrDefault

namespace AiGMBackEnd.Models
{
    // Helper method made static within the namespace
    internal static class JsonConverterHelper
    {
        internal static JsonSerializerOptions GetOptionsWithoutConverter(JsonSerializerOptions options, JsonConverter converterToRemove)
        {
            var newOptions = new JsonSerializerOptions(options);
            // Check if the converter exists before trying to remove
            var existingConverter = newOptions.Converters.FirstOrDefault(c => c.GetType() == converterToRemove.GetType());
            if (existingConverter != null)
            {
                newOptions.Converters.Remove(existingConverter);
            }
            return newOptions;
        }
    }

    // --- Creation Hook List Converter ---
    public class CreationHookListConverter : JsonConverter<List<ICreationHook>>
    {
        public override List<ICreationHook> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected start of array when deserializing List<ICreationHook>");
            }

            var result = new List<ICreationHook>();
            // Get the item converter from options
            var itemConverter = options.GetConverter(typeof(ICreationHook)) as CreationHookConverter 
                ?? throw new InvalidOperationException("CreationHookConverter not registered or found in options");

            // Remove this converter to avoid infinite recursion
            var newOptions = JsonConverterHelper.GetOptionsWithoutConverter(options, this);

            // Read array contents
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return result;
                }

                // Deserialize each item using the item converter
                var item = itemConverter.Read(ref reader, typeof(ICreationHook), newOptions);
                result.Add(item);
            }

            throw new JsonException("Unexpected end of JSON array.");
        }

        public override void Write(Utf8JsonWriter writer, List<ICreationHook> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            
            var itemConverter = options.GetConverter(typeof(ICreationHook)) as CreationHookConverter
                ?? throw new InvalidOperationException("CreationHookConverter not registered or found in options");

            // Remove this converter to avoid infinite recursion
            var newOptions = JsonConverterHelper.GetOptionsWithoutConverter(options, this);

            foreach (var item in value)
            {
                itemConverter.Write(writer, item, newOptions);
            }
            
            writer.WriteEndArray();
        }
    }

    // --- Creation Hook Converter ---
    public class CreationHookConverter : JsonConverter<ICreationHook>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(ICreationHook).IsAssignableFrom(typeToConvert);

        public override ICreationHook Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Peek at the type property without consuming the reader state
            Utf8JsonReader readerClone = reader;
            if (readerClone.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for ICreationHook.");
            }

            string type = null;
            while (readerClone.Read())
            {
                if (readerClone.TokenType == JsonTokenType.PropertyName && readerClone.GetString() == "type")
                {
                    if (readerClone.Read() && readerClone.TokenType == JsonTokenType.String)
                    {
                        type = readerClone.GetString();
                        break;
                    }
                    else
                    {
                         throw new JsonException("Could not read 'type' property value.");
                    }
                }
                 // Skip nested objects/arrays if searching for type
                if (readerClone.TokenType == JsonTokenType.StartObject || readerClone.TokenType == JsonTokenType.StartArray)
                {
                    readerClone.Skip();
                }
                if (readerClone.TokenType == JsonTokenType.EndObject)
                {
                     // Reached end of object without finding type
                     break;
                }
            }

            if (type == null)
            {
                throw new JsonException("Creation Hook JSON must contain a 'type' property.");
            }

            // Now deserialize based on the determined type using the original reader
            var actualOptions = JsonConverterHelper.GetOptionsWithoutConverter(options, this); // Call static helper

            return type switch
            {
                "NPC" => JsonSerializer.Deserialize<NpcCreationHook>(ref reader, actualOptions),
                "LOCATION" => JsonSerializer.Deserialize<LocationCreationHook>(ref reader, actualOptions),
                "QUEST" => JsonSerializer.Deserialize<QuestCreationHook>(ref reader, actualOptions),
                _ => throw new JsonException($"Unknown creation hook type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, ICreationHook value, JsonSerializerOptions options)
        {
            var actualOptions = JsonConverterHelper.GetOptionsWithoutConverter(options, this); // Call static helper
            switch (value)
            {
                case NpcCreationHook npcHook: JsonSerializer.Serialize(writer, npcHook, actualOptions); break;
                case LocationCreationHook locHook: JsonSerializer.Serialize(writer, locHook, actualOptions); break;
                case QuestCreationHook questHook: JsonSerializer.Serialize(writer, questHook, actualOptions); break;
                default: throw new JsonException($"Cannot serialize unknown creation hook type: {value.GetType().Name}");
            }
        }
    }


    // --- Update Payload Converter ---
     public class UpdatePayloadConverter : JsonConverter<IUpdatePayload>
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(IUpdatePayload).IsAssignableFrom(typeToConvert);

        public override IUpdatePayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
             // Peek at the type property
            Utf8JsonReader readerClone = reader;
            if (readerClone.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for IUpdatePayload.");
            }
            string type = null;
            while (readerClone.Read())
            {
                if (readerClone.TokenType == JsonTokenType.PropertyName && readerClone.GetString() == "type")
                {
                    if (readerClone.Read() && readerClone.TokenType == JsonTokenType.String)
                    {
                        type = readerClone.GetString();
                        break;
                    }
                    else
                    {
                        throw new JsonException("Could not read 'type' property value.");
                    }
                }
                if (readerClone.TokenType == JsonTokenType.StartObject || readerClone.TokenType == JsonTokenType.StartArray)
                {
                    readerClone.Skip();
                }
                 if (readerClone.TokenType == JsonTokenType.EndObject)
                {
                     break;
                }
            }

            if (type == null)
            {
                throw new JsonException("Update Payload JSON must contain a 'type' property.");
            }

            var actualOptions = JsonConverterHelper.GetOptionsWithoutConverter(options, this); // Call static helper
            string normalizedType = type.ToUpperInvariant();

            return normalizedType switch
            {
                "PLAYER" => JsonSerializer.Deserialize<PlayerUpdatePayload>(ref reader, actualOptions),
                "WORLD" => JsonSerializer.Deserialize<WorldUpdatePayload>(ref reader, actualOptions),
                "NPC" => JsonSerializer.Deserialize<NpcUpdatePayload>(ref reader, actualOptions),
                "LOCATION" => JsonSerializer.Deserialize<LocationUpdatePayload>(ref reader, actualOptions),
                _ => throw new JsonException($"Unknown update payload type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, IUpdatePayload value, JsonSerializerOptions options)
        {
             var actualOptions = JsonConverterHelper.GetOptionsWithoutConverter(options, this); // Call static helper
            switch (value)
            {
                case PlayerUpdatePayload p: JsonSerializer.Serialize(writer, p, actualOptions); break;
                case WorldUpdatePayload w: JsonSerializer.Serialize(writer, w, actualOptions); break;
                case NpcUpdatePayload n: JsonSerializer.Serialize(writer, n, actualOptions); break;
                case LocationUpdatePayload l: JsonSerializer.Serialize(writer, l, actualOptions); break;
                default: throw new JsonException($"Cannot serialize unknown update payload type: {value.GetType().Name}");
            }
        }
    }

    // --- Dictionary Converter for Update Payloads ---
    public class UpdatePayloadDictionaryConverter : JsonConverter<Dictionary<string, IUpdatePayload>>
    {
        public override Dictionary<string, IUpdatePayload> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for UpdatePayload Dictionary.");
            }

            var dictionary = new Dictionary<string, IUpdatePayload>();
            // Get the specific converter for IUpdatePayload instead of relying on default finding
             var payloadConverter = options.GetConverter(typeof(IUpdatePayload)) as UpdatePayloadConverter
                ?? throw new InvalidOperationException("UpdatePayloadConverter not registered or found in options.");


            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName token.");
                }

                string propertyName = reader.GetString();
                reader.Read(); // Move to the value (StartObject)

                var payload = payloadConverter.Read(ref reader, typeof(IUpdatePayload), options);
                dictionary.Add(propertyName, payload);
            }

            throw new JsonException("Unexpected end of JSON data.");
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, IUpdatePayload> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            var payloadConverter = options.GetConverter(typeof(IUpdatePayload)) as UpdatePayloadConverter
                 ?? throw new InvalidOperationException("UpdatePayloadConverter not registered or found in options.");

            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                payloadConverter.Write(writer, kvp.Value, options);
            }

            writer.WriteEndObject();
        }
    }

    // --- List Converter for Creation Hooks ---

    // --- LLM-Safe Integer Converter ---
    public class LlmSafeIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                if (int.TryParse(reader.GetString(), out int value))
                {
                    return value;
                }
            }
            throw new JsonException($"Expected an integer or a string representing an integer, but got {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
} 