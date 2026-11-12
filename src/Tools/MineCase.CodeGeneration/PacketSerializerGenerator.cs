using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MineCase.CodeGeneration
{
    [Generator]
    public class PacketSerializerGenerator : IIncrementalGenerator
    {
        private const string ClassAttributeName = "MineCase.Serialization.GeneratePacketSerializerAttribute";
        private const string FieldAttributeName = "MineCase.Serialization.SerializeAsAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var sources = context.SyntaxProvider.ForAttributeWithMetadataName(
                ClassAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, _) => GenerateSource(ctx));

            context.RegisterSourceOutput(sources, static (spc, item) =>
                spc.AddSource(item.HintName, SourceText.From(item.Source, Encoding.UTF8)));
        }

        private static (string HintName, string Source) GenerateSource(GeneratorAttributeSyntaxContext ctx)
        {
            var cls = (ClassDeclarationSyntax)ctx.TargetNode;
            var clsSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
            var compilation = ctx.SemanticModel.Compilation;
            var fieldAttributeSymbol = compilation.GetTypeByMetadataName(FieldAttributeName)!;

            var clsAttribute = ctx.Attributes[0];
            var methods = clsAttribute.NamedArguments.FirstOrDefault(x => x.Key == "Methods").Value.Value;
            var mdValue = methods == null ? GenerateSerializerMethods.Both : (GenerateSerializerMethods)(int)methods;

            var codeWriter = new StringBuilder();
            codeWriter.Append(@"using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MineCase.Serialization;

");
            new PacketSerializerGenerator().ProcessClass(cls, clsSymbol, mdValue, codeWriter, compilation, fieldAttributeSymbol);

            var hintName = $"{clsSymbol.ContainingNamespace.ToDisplayString()}.{clsSymbol.MetadataName}.g.cs";
            return (hintName, codeWriter.ToString());
        }

        private void ProcessClass(ClassDeclarationSyntax cls, ITypeSymbol clsSymbol, GenerateSerializerMethods mdValue, StringBuilder codeWriter, Compilation compilation, INamedTypeSymbol fieldAttributeSymbol)
        {
            codeWriter.AppendLine($"namespace {clsSymbol.ContainingNamespace.ToDisplayString()}");
            codeWriter.AppendLine("{");
            codeWriter.Ident(1).AppendLine($"public partial class {cls.Identifier}{cls.TypeParameterList?.ToString()}");
            codeWriter.Ident(1).AppendLine("{");

            try
            {
                var fields = from fSymbol in clsSymbol.GetMembers().OfType<IFieldSymbol>()
                             let attr = fSymbol.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, fieldAttributeSymbol))
                             where attr != null
                             let dataType = (DataType)(int)attr.ConstructorArguments[0].Value!
                             select new
                             {
                                 Symbol = fSymbol,
                                 DataType = dataType,
                                 Arguments = attr.NamedArguments
                             };

                object? GetNamedArgument(ImmutableArray<KeyValuePair<string, TypedConstant>> args, string name)
                {
                    return args.FirstOrDefault(x => x.Key == name).Value.Value;
                }

                string GetArrayLengthMember(ImmutableArray<KeyValuePair<string, TypedConstant>> args)
                {
                    var arg = (string?)GetNamedArgument(args, "ArrayLengthMember");
                    if (arg != null)
                        return "(int)" + arg;
                    return string.Empty;
                }

                // 1. Serializer
                if (mdValue == GenerateSerializerMethods.Serialize || mdValue == GenerateSerializerMethods.Both)
                {
                    codeWriter.Ident(2).AppendLine("public void Serialize(BinaryWriter bw)");
                    codeWriter.Ident(2).AppendLine("{");

                    foreach (var field in fields)
                    {
                        var code = field.DataType switch
                        {
                            DataType.Angle => $"WriteAsAngle({field.Symbol.Name})",
                            DataType.Array => $"WriteAsArray({field.Symbol.Name})",
                            DataType.Boolean => $"WriteAsBoolean({field.Symbol.Name})",
                            DataType.Byte => $"WriteAsByte({OptionalEnumCastFrom(field.Symbol.Type, "byte")}{field.Symbol.Name})",
                            DataType.ByteArray => $"WriteAsByteArray({field.Symbol.Name})",
                            DataType.Chat => $"WriteAsChat({field.Symbol.Name})",
                            DataType.Double => $"WriteAsDouble({field.Symbol.Name})",
                            DataType.EntityMetadata => throw new NotSupportedException(),
                            DataType.Float => $"WriteAsFloat({field.Symbol.Name})",
                            DataType.Int => $"WriteAsInt({OptionalEnumCastFrom(field.Symbol.Type, "int")}{field.Symbol.Name})",
                            DataType.IntArray => $"WriteAsIntArray({field.Symbol.Name})",
                            DataType.Long => $"WriteAsLong({OptionalEnumCastFrom(field.Symbol.Type, "long")}{field.Symbol.Name})",
                            DataType.NbtArray => $"WriteAsNbtArray({field.Symbol.Name})",
                            DataType.NBTTag => $"WriteAsNBTTag({field.Symbol.Name})",
                            DataType.Position => $"WriteAsPosition({field.Symbol.Name})",
                            DataType.Short => $"WriteAsShort({OptionalEnumCastFrom(field.Symbol.Type, "short")}{field.Symbol.Name})",
                            DataType.Slot => $"WriteAsSlot({field.Symbol.Name})",
                            DataType.SlotArray => $"WriteAsSlotArray({field.Symbol.Name})",
                            DataType.String => $"WriteAsString({field.Symbol.Name})",
                            DataType.UnsignedByte => $"WriteAsUnsignedByte({OptionalEnumCastFrom(field.Symbol.Type, "byte")}{field.Symbol.Name})",
                            DataType.UnsignedShort => $"WriteAsUnsignedShort({OptionalEnumCastFrom(field.Symbol.Type, "ushort")}{field.Symbol.Name})",
                            DataType.UUID => $"WriteAsUUID({field.Symbol.Name})",
                            DataType.VarInt => $"WriteAsVarInt({OptionalEnumCastFrom(field.Symbol.Type, "uint")}{field.Symbol.Name}, out _)",
                            DataType.VarIntArray => $"WriteAsVarIntArray({field.Symbol.Name})",
                            DataType.VarLong => $"WriteAsVarLong({OptionalEnumCastFrom(field.Symbol.Type, "ulong")}{field.Symbol.Name}, out _)",
                            _ => ""
                        };

                        codeWriter.Ident(3).AppendLine($"bw.{code};");
                    }

                    codeWriter.Ident(2).AppendLine("}");
                }

                if (mdValue == GenerateSerializerMethods.Both)
                    codeWriter.AppendLine();

                // 2. Deserializer
                if (mdValue == GenerateSerializerMethods.Deserialize || mdValue == GenerateSerializerMethods.Both)
                {
                    codeWriter.Ident(2).AppendLine("public void Deserialize(ref SpanReader br)");
                    codeWriter.Ident(2).AppendLine("{");

                    foreach (var field in fields)
                    {
                        var code = field.DataType switch
                        {
                            DataType.Angle => $"ReadAsAngle()",
                            DataType.Array => $"ReadAsArray<{((IArrayTypeSymbol)field.Symbol.Type).ElementType.ToDisplayString()}>({GetArrayLengthMember(field.Arguments)})",
                            DataType.Boolean => $"ReadAsBoolean()",
                            DataType.Byte => $"ReadAsByte()",
                            DataType.ByteArray => $"ReadAsByteArray({GetArrayLengthMember(field.Arguments)})",
                            DataType.Chat => $"ReadAsChat()",
                            DataType.Double => $"ReadAsDouble()",
                            DataType.EntityMetadata => throw new NotSupportedException(),
                            DataType.Float => $"ReadAsFloat()",
                            DataType.Int => $"ReadAsInt()",
                            DataType.IntArray => $"ReadAsIntArray({GetArrayLengthMember(field.Arguments)})",
                            DataType.Long => $"ReadAsLong()",
                            DataType.NbtArray => $"ReadAsNbtArray({GetArrayLengthMember(field.Arguments)})",
                            DataType.NBTTag => $"ReadAsNBTTag()",
                            DataType.Position => $"ReadAsPosition()",
                            DataType.Short => $"ReadAsShort()",
                            DataType.Slot => $"ReadAsSlot()",
                            DataType.SlotArray => $"ReadAsSlotArray({GetArrayLengthMember(field.Arguments)})",
                            DataType.String => $"ReadAsString()",
                            DataType.UnsignedByte => $"ReadAsUnsignedByte()",
                            DataType.UnsignedShort => $"ReadAsUnsignedShort()",
                            DataType.UUID => $"ReadAsUUID()",
                            DataType.VarInt => $"ReadAsVarInt(out _)",
                            DataType.VarIntArray => $"ReadAsVarIntArray({GetArrayLengthMember(field.Arguments)})",
                            DataType.VarLong => $"ReadAsVarLong(out _)",
                            _ => ""
                        };

                        codeWriter.Ident(3).AppendLine($"{field.Symbol.Name} = {OptionalEnumCastTo(field.Symbol.Type)}br.{code};");
                    }

                    codeWriter.Ident(2).AppendLine("}");
                }
            }
            catch (Exception ex)
            {
                codeWriter.AppendLine(ex.ToString());
            }

            codeWriter.Ident(1).AppendLine("}");
            codeWriter.AppendLine("}");
            codeWriter.AppendLine();
        }

        private static string OptionalEnumCastFrom(ITypeSymbol type, string desiredType)
        {
            if (type.TypeKind == TypeKind.Enum || type.ToDisplayString() != desiredType)
                return $"({desiredType})";
            return string.Empty;
        }

        private static string OptionalEnumCastTo(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Enum || type.IsUnmanagedType)
                return $"({type.ToDisplayString()})";
            return string.Empty;
        }

        private enum GenerateSerializerMethods
        {
            Serialize = 0,
            Deserialize = 1,
            Both = 2
        }

        private enum DataType
        {
            Boolean,
            Byte,
            UnsignedByte,
            Short,
            UnsignedShort,
            Int,
            Long,
            Float,
            Double,
            String,
            Chat,
            VarInt,
            VarLong,
            EntityMetadata,
            Slot,
            NBTTag,
            Position,
            Angle,
            UUID,
            ByteArray,
            IntArray,
            NbtArray,
            VarIntArray,
            SlotArray,
            Array
        }
    }

    static class CodeWriterExtensions
    {
        public static StringBuilder Ident(this StringBuilder sb, int count)
        {
            for (int i = 0; i < count; i++)
                sb.Append("    ");
            return sb;
        }
    }
}
