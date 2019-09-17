using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Party.Shared.Models.Registries;

namespace Party.Shared.Serializers
{
    public class RegistrySerializer
    {
        public string Serialize(Registry registry)
        {
            return SerializeInternal(registry) + Environment.NewLine;
        }

        public string Serialize(RegistryPackage registry)
        {
            return SerializeInternal(registry);
        }

        private static string SerializeInternal(object value)
        {
            var serializerSettings = new JsonSerializerSettings
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

            return JsonConvert.SerializeObject(value, serializerSettings);
        }

        public class StringArrayConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(IEnumerable<string>).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return existingValue;
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
                return existingValue ?? (RegistryVersionString)reader.ReadAsString();
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
                return Enum.TryParse<RegistryPackageType>(reader.ReadAsString(), out var value) ? value : RegistryPackageType.Unknown;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteRawValue($"\"{value.ToString()}\"");
            }
        }
    }
}
