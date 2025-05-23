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
                if (readerClone.TokenType == JsonTokenType.PropertyName && 
                    (readerClone.GetString().ToLowerInvariant() == "type")) // Case-insensitive check for "type"
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
            
            // Set PropertyNameCaseInsensitive to true to handle case variations in property names
            actualOptions.PropertyNameCaseInsensitive = true;
            
            // Normalize the type to uppercase for comparison
            string normalizedType = type.ToUpperInvariant();

            return normalizedType switch
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
                if (readerClone.TokenType == JsonTokenType.PropertyName && 
                    (readerClone.GetString().ToLowerInvariant() == "type")) // Case-insensitive check for "type"
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
            
            // Set PropertyNameCaseInsensitive to true to handle case variations in property names
            actualOptions.PropertyNameCaseInsensitive = true;
            
            // Normalize the type to uppercase for comparison
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
                reader.Read(); // Move to the value

                // Handle special cases for arrays: npcEntries and locationEntries
                if (propertyName == "npcEntries" || propertyName == "locationEntries")
                {
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        // Skip the array - we'll handle these separately
                        reader.Skip();
                        continue;
                    }
                }

                // Only try to deserialize as IUpdatePayload if we're at a StartObject
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var payload = payloadConverter.Read(ref reader, typeof(IUpdatePayload), options);
                    dictionary.Add(propertyName, payload);
                }
                else
                {
                    // Skip other types that aren't objects
                    reader.Skip();
                }
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

    // --- NPC List Converter for Location Update Payload ---
    public class NpcListConverter : JsonConverter<List<NpcListUpdateItem>>
    {
        public override List<NpcListUpdateItem> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Check if we're at the start of an array
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Expected StartArray token for NpcList.");
            }

            var result = new List<NpcListUpdateItem>();
            
            // Create new options to avoid infinite recursion
            var newOptions = new JsonSerializerOptions(options);
            var existingConverter = newOptions.Converters.FirstOrDefault(c => c.GetType() == typeof(NpcListConverter));
            if (existingConverter != null)
            {
                newOptions.Converters.Remove(existingConverter);
            }
            
            // Read until we reach the end of the array
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return result;
                }
                
                if (reader.TokenType == JsonTokenType.String)
                {
                    // Simple string format - convert to NpcListUpdateItem with Add action
                    string npcId = reader.GetString();
                    if (!string.IsNullOrEmpty(npcId))
                    {
                        result.Add(new NpcListUpdateItem { 
                            NpcId = npcId, 
                            Action = UpdateAction.Add 
                        });
                    }
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    // Object format - deserialize as NpcListUpdateItem
                    // We need to manually parse the object to handle case insensitivity
                    string npcId = null;
                    UpdateAction action = UpdateAction.Add; // Default action
                    
                    // Start reading the object
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }
                        
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string propertyName = reader.GetString().ToLowerInvariant(); // Make case-insensitive
                            reader.Read(); // Move to value
                            
                            if (propertyName == "npcid")
                            {
                                npcId = reader.GetString();
                            }
                            else if (propertyName == "action")
                            {
                                string actionStr = reader.GetString()?.ToUpperInvariant(); // Make case-insensitive
                                if (actionStr == "ADD")
                                {
                                    action = UpdateAction.Add;
                                }
                                else if (actionStr == "REMOVE")
                                {
                                    action = UpdateAction.Remove;
                                }
                                else
                                {
                                    // Default to Add if unrecognized
                                    action = UpdateAction.Add;
                                }
                            }
                            else
                            {
                                // Skip unknown properties
                                reader.Skip();
                            }
                        }
                    }
                    
                    // Add the item if we found an npcId
                    if (!string.IsNullOrEmpty(npcId))
                    {
                        result.Add(new NpcListUpdateItem { 
                            NpcId = npcId, 
                            Action = action 
                        });
                    }
                }
                else
                {
                    // Skip other tokens
                    reader.Skip();
                }
            }
            
            throw new JsonException("Unexpected end of JSON array in NpcList.");
        }

        public override void Write(Utf8JsonWriter writer, List<NpcListUpdateItem> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            
            if (value != null)
            {
                // Create new options to avoid infinite recursion
                var newOptions = new JsonSerializerOptions(options);
                var existingConverter = newOptions.Converters.FirstOrDefault(c => c.GetType() == typeof(NpcListConverter));
                if (existingConverter != null)
                {
                    newOptions.Converters.Remove(existingConverter);
                }
                
                foreach (var item in value)
                {
                    writer.WriteStartObject();
                    
                    // Write npcId
                    writer.WritePropertyName("npcId");
                    writer.WriteStringValue(item.NpcId);
                    
                    // Write action
                    writer.WritePropertyName("action");
                    writer.WriteStringValue(item.Action.ToString());
                    
                    writer.WriteEndObject();
                }
            }
            
            writer.WriteEndArray();
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

    // --- New Converter for Partial Updates --- 
    public class PartialUpdatesConverter : JsonConverter<PartialUpdates>
    {
        public override PartialUpdates Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for PartialUpdates.");
            }

            var result = new PartialUpdates
            {
                Player = null,
                World = null,
                NpcEntries = new List<NpcUpdatePayload>(),
                LocationEntries = new List<LocationUpdatePayload>()
            };

            // Get the specific converter for IUpdatePayload
            var payloadConverter = options.GetConverter(typeof(IUpdatePayload)) as UpdatePayloadConverter
                ?? throw new InvalidOperationException("UpdatePayloadConverter not registered or found in options.");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return result;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName token in PartialUpdates.");
                }

                string propertyName = reader.GetString();
                reader.Read(); // Move to the value

                switch (propertyName)
                {
                    case "player":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            result.Player = (PlayerUpdatePayload)payloadConverter.Read(ref reader, typeof(IUpdatePayload), options);
                        }
                        else
                        {
                            reader.Skip();
                        }
                        break;

                    case "world":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            result.World = (WorldUpdatePayload)payloadConverter.Read(ref reader, typeof(IUpdatePayload), options);
                        }
                        else
                        {
                            reader.Skip();
                        }
                        break;

                    case "npcEntries":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            result.NpcEntries = ReadNpcEntries(ref reader, options);
                        }
                        else
                        {
                            reader.Skip();
                        }
                        break;

                    case "locationEntries":
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            result.LocationEntries = ReadLocationEntries(ref reader, options);
                        }
                        else
                        {
                            reader.Skip();
                        }
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }

            throw new JsonException("Unexpected end of JSON data in PartialUpdates.");
        }

        private List<NpcUpdatePayload> ReadNpcEntries(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var entries = new List<NpcUpdatePayload>();
            
            // Create new options with case-insensitive property names
            var newOptions = new JsonSerializerOptions(options)
            {
                PropertyNameCaseInsensitive = true  // Ensure case-insensitive property name matching
            };
            
            // Remove this converter to avoid recursion
            var existingConverter = newOptions.Converters.FirstOrDefault(c => c.GetType() == typeof(PartialUpdatesConverter));
            if (existingConverter != null)
            {
                newOptions.Converters.Remove(existingConverter);
            }
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return entries;
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    try
                    {
                        var entry = JsonSerializer.Deserialize<NpcUpdatePayload>(ref reader, newOptions);
                        if (entry != null)
                        {
                            // Ensure ID is set - needed for further processing
                            if (string.IsNullOrEmpty(entry.Id))
                            {
                                // Skip this entry if ID is missing
                                reader.Skip(); // Skip to the end of the object
                                continue;
                            }
                            
                            entries.Add(entry);
                        }
                    }
                    catch (JsonException ex)
                    {
                        // Log and skip this entry if there's a deserialization error
                        reader.Skip(); // Skip to the end of the object
                    }
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Unexpected end of JSON array in npcEntries.");
        }

        private List<LocationUpdatePayload> ReadLocationEntries(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var entries = new List<LocationUpdatePayload>();
            
            // Create new options with case-insensitive property names
            var newOptions = new JsonSerializerOptions(options)
            {
                PropertyNameCaseInsensitive = true  // Ensure case-insensitive property name matching
            };
            
            // Remove this converter to avoid recursion
            var existingConverter = newOptions.Converters.FirstOrDefault(c => c.GetType() == typeof(PartialUpdatesConverter));
            if (existingConverter != null)
            {
                newOptions.Converters.Remove(existingConverter);
            }
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return entries;
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    try
                    {
                        var entry = JsonSerializer.Deserialize<LocationUpdatePayload>(ref reader, newOptions);
                        if (entry != null)
                        {
                            // Ensure ID is set - needed for further processing
                            if (string.IsNullOrEmpty(entry.Id))
                            {
                                // Try to find an id property with different casing if it's missing
                                reader.Skip(); // Skip to the end of the object
                                continue;  // Skip this entry if ID is missing
                            }
                            
                            entries.Add(entry);
                        }
                    }
                    catch (JsonException ex)
                    {
                        // Log and skip this entry if there's a deserialization error
                        reader.Skip(); // Skip to the end of the object
                    }
                }
                else
                {
                    reader.Skip();
                }
            }

            throw new JsonException("Unexpected end of JSON array in locationEntries.");
        }

        public override void Write(Utf8JsonWriter writer, PartialUpdates value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (value.Player != null)
            {
                writer.WritePropertyName("player");
                JsonSerializer.Serialize(writer, value.Player, options);
            }

            if (value.World != null)
            {
                writer.WritePropertyName("world");
                JsonSerializer.Serialize(writer, value.World, options);
            }

            if (value.NpcEntries != null && value.NpcEntries.Count > 0)
            {
                writer.WritePropertyName("npcEntries");
                writer.WriteStartArray();
                foreach (var entry in value.NpcEntries)
                {
                    JsonSerializer.Serialize(writer, entry, options);
                }
                writer.WriteEndArray();
            }

            if (value.LocationEntries != null && value.LocationEntries.Count > 0)
            {
                writer.WritePropertyName("locationEntries");
                writer.WriteStartArray();
                foreach (var entry in value.LocationEntries)
                {
                    JsonSerializer.Serialize(writer, entry, options);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }

    // --- Sanitized String Converter ---
    /// <summary>
    /// JsonConverter that sanitizes string values by removing or fixing problematic escape sequences.
    /// This helps handle malformed strings from LLM responses.
    /// </summary>
    public class SanitizedStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected string value");
            }

            // Get the raw string
            string value = reader.GetString();
            
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            
            try
            {
                // Replace problematic escape sequences
                // Convert double backslashes that aren't part of valid escape sequences to single backslashes
                value = System.Text.RegularExpressions.Regex.Replace(
                    value,
                    @"\\\\(?![""\\\/bfnrt]|u[0-9a-fA-F]{4})",  // Match \\ not followed by valid escape chars
                    @"\");                                      // Replace with single \
                
                return value;
            }
            catch (Exception)
            {
                // If any error occurs during sanitization, return the original string
                return value;
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            // For writing, just use the normal string serialization
            writer.WriteStringValue(value);
        }
    }
} 