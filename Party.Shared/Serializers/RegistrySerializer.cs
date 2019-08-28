using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Party.Shared.Results;

namespace Party.Shared.Serializers
{
    public class RegistrySerializer
    {
        public string Serialize(Registry registry)
        {
            return SerializeInternal(registry) + Environment.NewLine;
        }

        public string Serialize(RegistryScript registry)
        {
            return SerializeInternal(registry);
        }

        private static string SerializeInternal(object value)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = {
                    new StringArrayConverter()
                }
            };

            return JsonConvert.SerializeObject(value, serializerSettings);
        }

        class StringArrayConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(IEnumerable<string>).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteRawValue(JsonConvert.SerializeObject(value, Formatting.None).Replace(",\"", ", \""));
            }
        }
    }
}
