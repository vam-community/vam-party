using System;
using System.IO;
using Newtonsoft.Json;

namespace Party.Shared.Serializers
{
    public class SceneJsonTextWriter : JsonWriter
    {
        private readonly StreamWriter _writer;

        public SceneJsonTextWriter(StreamWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            Formatting = Formatting.Indented;
        }

        public override void WritePropertyName(string name)
        {
            base.WritePropertyName(name);
            _writer.Write(JsonConvert.ToString(name));
            // For some reason, VaM serializes with a space before the colon
            _writer.Write(" : ");
        }

        public override void WriteNull()
        {
            base.WriteNull();
            _writer.Write("null");
        }

        public override void WriteValue(string value)
        {
            base.WriteValue(value);
            _writer.Write(JsonConvert.ToString(value));
        }

        public override void WriteValue(int value)
        {
            base.WriteValue(value);
            _writer.Write(value);
        }

        public override void WriteValue(long value)
        {
            base.WriteValue(value);
            _writer.Write(value);
        }

        public override void WriteValue(bool value)
        {
            base.WriteValue(value);
            _writer.Write(value);
        }

        public override void WriteValue(double value)
        {
            base.WriteValue(value);
            _writer.Write(value);
        }

        public override void WriteValue(float value)
        {
            base.WriteValue(value);
            _writer.Write(value);
        }

        public override void WriteValue(decimal value)
        {
            base.WriteValue(value);
            _writer.Write(value);
        }

        public override void WriteStartArray()
        {
            base.WriteStartArray();
            _writer.Write("[ ");
        }

        public override void WriteStartObject()
        {
            base.WriteStartObject();
            _writer.Write("{ ");
        }

        public override void WriteEndArray()
        {
            base.WriteEndArray();
            _writer.Write(']');
        }

        public override void WriteEndObject()
        {
            base.WriteEndObject();
            _writer.Write('}');
        }

        public override void Flush()
        {
            _writer.Flush();
        }

        protected override void WriteValueDelimiter()
        {
            base.WriteValueDelimiter();
            _writer.Write(", ");
        }

        protected override void WriteIndent()
        {
            _writer.Write(Environment.NewLine);

            int currentIndentCount = Top * 3;

            if (currentIndentCount == 0) return;

            while (currentIndentCount > 0)
            {
                int writeCount = Math.Min(currentIndentCount, 10);
                _writer.Write(new string(' ', writeCount));
                currentIndentCount -= writeCount;
            }
        }
    }
}
