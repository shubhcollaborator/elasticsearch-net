using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Nest
{
	internal class ReserializeJsonConverter<TReadAs, TInterface> : JsonConverter
		where TReadAs : class, TInterface
		where TInterface : class
	{
		protected ReadAsTypeJsonConverter<TReadAs> Reader { get; } = new ReadAsTypeJsonConverter<TReadAs>();

		public override bool CanRead => true;

		public override bool CanWrite => true;

		public override bool CanConvert(Type objectType) => typeof(TInterface).IsAssignableFrom(objectType);

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType != JsonToken.StartObject) return null;
			var depth = reader.Depth;
			var deserialized = this.DeserializeJson(reader, objectType, existingValue, serializer);
			return reader.ReadToEnd(depth, deserialized);
		}

		protected TReadAs ReadAs(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return this.Reader.ReadJson(reader, objectType, existingValue, serializer) as TReadAs;
		}

		protected virtual object DeserializeJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
			this.ReadAs(reader, objectType, existingValue, serializer);

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (!(value is TInterface v)) return;
			this.SerializeJson(writer, value, v, serializer);
		}

		protected virtual void SerializeJson(JsonWriter writer, object value, TInterface castValue, JsonSerializer serializer)
		{
			this.Reserialize(writer, value, serializer);
		}

		protected virtual bool SkipWriteProperty(string propertyName) => false;

		protected void Reserialize(JsonWriter writer, object value, JsonSerializer serializer, Action<JsonWriter> inlineWriter = null)
		{
			var properties = value.GetType().GetCachedObjectProperties();
			if (properties.Count == 0) return;
			writer.WriteStartObject();
			inlineWriter?.Invoke(writer);
			foreach (var p in properties)
			{
				if (p.Ignored || SkipWriteProperty(p.PropertyName)) continue;
				var vv = p.ValueProvider.GetValue(value);
				if (vv == null) continue;
				writer.WritePropertyName(p.PropertyName);
				if (p.Converter?.GetType() == typeof(SourceValueWriteConverter))
					SourceValueWriteConverter.Write(writer, vv, serializer);
				else
					serializer.Serialize(writer, vv);
			}
			writer.WriteEndObject();
		}
	}
}
