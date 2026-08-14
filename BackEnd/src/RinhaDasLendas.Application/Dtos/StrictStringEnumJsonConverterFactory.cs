using System.Text.Json;
using System.Text.Json.Serialization;

namespace RinhaDasLendas.Application.Dtos;

public sealed class StrictStringEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        var valueType = UnwrapOptional(typeToConvert);
        return (Nullable.GetUnderlyingType(valueType) ?? valueType).IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>))
        {
            var optionalValueType = typeToConvert.GetGenericArguments()[0];
            var enumType = Nullable.GetUnderlyingType(optionalValueType) ?? optionalValueType;
            var converterType = Nullable.GetUnderlyingType(optionalValueType) is null
                ? typeof(StrictOptionalEnumJsonConverter<>).MakeGenericType(enumType)
                : typeof(StrictOptionalNullableEnumJsonConverter<>).MakeGenericType(enumType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        var nullableEnumType = Nullable.GetUnderlyingType(typeToConvert);
        var type = nullableEnumType is null
            ? typeof(StrictEnumJsonConverter<>).MakeGenericType(typeToConvert)
            : typeof(StrictNullableEnumJsonConverter<>).MakeGenericType(nullableEnumType);
        return (JsonConverter)Activator.CreateInstance(type)!;
    }

    private static Type UnwrapOptional(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>)
            ? type.GetGenericArguments()[0]
            : type;

    private static TEnum ReadEnum<TEnum>(ref Utf8JsonReader reader)
        where TEnum : struct, Enum
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException();
        }

        var text = reader.GetString();
        if (text is null
            || !Enum.TryParse<TEnum>(text, ignoreCase: false, out var value)
            || !string.Equals(Enum.GetName(value), text, StringComparison.Ordinal))
        {
            throw new JsonException();
        }

        return value;
    }

    private static void WriteEnum<TEnum>(Utf8JsonWriter writer, TEnum value)
        where TEnum : struct, Enum =>
        writer.WriteStringValue(Enum.GetName(value) ?? throw new JsonException());

    private sealed class StrictEnumJsonConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            ReadEnum<TEnum>(ref reader);

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
            WriteEnum(writer, value);
    }

    private sealed class StrictNullableEnumJsonConverter<TEnum> : JsonConverter<TEnum?>
        where TEnum : struct, Enum
    {
        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null ? null : ReadEnum<TEnum>(ref reader);

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                WriteEnum(writer, value.Value);
                return;
            }

            writer.WriteNullValue();
        }
    }

    private sealed class StrictOptionalEnumJsonConverter<TEnum> : JsonConverter<Optional<TEnum>>
        where TEnum : struct, Enum
    {
        public override Optional<TEnum> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => new(ReadEnum<TEnum>(ref reader));

        public override void Write(
            Utf8JsonWriter writer,
            Optional<TEnum> value,
            JsonSerializerOptions options) => WriteEnum(writer, value.Value);
    }

    private sealed class StrictOptionalNullableEnumJsonConverter<TEnum> : JsonConverter<Optional<TEnum?>>
        where TEnum : struct, Enum
    {
        public override bool HandleNull => true;

        public override Optional<TEnum?> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            new(reader.TokenType == JsonTokenType.Null ? null : ReadEnum<TEnum>(ref reader));

        public override void Write(
            Utf8JsonWriter writer,
            Optional<TEnum?> value,
            JsonSerializerOptions options)
        {
            if (value.Value.HasValue)
            {
                WriteEnum(writer, value.Value.Value);
                return;
            }

            writer.WriteNullValue();
        }
    }
}
