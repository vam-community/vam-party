using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Party.Shared.Models.Registries;

namespace Party.Shared.Serializers
{
    public class RegistrySerializer : IRegistrySerializer
    {
        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateFormatString = "yyyy-MM-dd",
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters =
                {
                    new StringArrayConverter(),
                    new VersionConverter(),
                    new PackageTypeConverter(),
                }
        };

        public string Serialize(Registry registry)
        {
            return SerializeInternal(registry) + Environment.NewLine;
        }

        public string Serialize(RegistryPackage registry)
        {
            return SerializeInternal(registry);
        }

        public Registry Deserialize(Stream stream)
        {
            using var streamReader = new StreamReader(stream);
            using var jsonTestReader = new JsonTextReader(streamReader);
            var jsonSerializer = JsonSerializer.Create(_settings);
            return jsonSerializer.Deserialize<Registry>(jsonTestReader);
        }

        private string SerializeInternal(object value)
        {
            return JsonConvert.SerializeObject(value, _settings);
        }

        public class StringArrayConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(IEnumerable<string>).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingvalue, JsonSerializer serializer)
            {
                var list = new List<string>();
                while (reader.TokenType != JsonToken.EndArray)
                {
                    var value = reader.ReadAsString();
                    if (reader.TokenType != JsonToken.EndArray)
                        list.Add(value);
                }
                return list;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteRawValue(JsonConvert.SerializeObject(value, Formatting.None).Replace(",\"", ", \""));
            }
        }

        public class VersionConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(RegistryVersionString);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return (RegistryVersionString)(string)reader.Value;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteRawValue($"\"{value.ToString()}\"");
            }
        }

        public class PackageTypeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(RegistryPackageType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return Enum.TryParse<RegistryPackageType>(reader.Value as string, true, out var value) ? value : RegistryPackageType.Unknown;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteRawValue($"\"{value.ToString().ToLowerInvariant()}\"");
            }
        }
    }

    public interface IRegistrySerializer
    {
        string Serialize(Registry registry);
        string Serialize(RegistryPackage registry);
        Registry Deserialize(Stream stream);
    }
}
