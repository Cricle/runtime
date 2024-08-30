﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Private.CoreLib.Generators.Models;
using System.Text;

namespace Generators
{
    public partial class EventSourceEventGenerator
    {
        private sealed partial class Emiitter
        {
            private const string NonEventAttribute = "global::System.Diagnostics.Tracing.NonEventAttribute";
            private const string EventData = "global::System.Diagnostics.Tracing.EventSource.EventData";
            private const string IntPtrZero = "global::System.IntPtr.Zero";
            private const string AsPointer = "global::System.Runtime.CompilerServices.Unsafe.AsPointer";
            private const string GetReference = "global::System.Runtime.InteropServices.MemoryMarshal.GetReference";
            private const string AsSpan = "global::System.MemoryExtensions.AsSpan";

            public static void Emit(EventMethodsParsedResult result, StringBuilder stringBuilder)
            {
                var writer = new IndentedTextWriter(new StringWriter(stringBuilder));
                writer.WriteLine("// <auto-generated/>");

                if (!string.IsNullOrEmpty(result.Namespace))
                {
                    writer.WriteLine($"namespace {result.Namespace}");
                    writer.WriteLine('{');
                    writer.Indent++;
                }

                foreach (string classDeclaration in result.ContextClassDeclarations)
                {
                    writer.WriteLine(classDeclaration);
                    writer.WriteLine('{');
                    writer.Indent++;
                }

                EmitClass(result, writer);

                foreach (string _ in result.ContextClassDeclarations)
                {
                    writer.WriteLine('}');
                    writer.Indent--;
                }

                if (!string.IsNullOrEmpty(result.Namespace))
                {
                    writer.WriteLine('}');
                    writer.Indent--;
                }
            }

            private static void EmitClass(EventMethodsParsedResult result, IndentedTextWriter writer)
            {
                Debug.Assert(result != null);
                Debug.Assert(writer != null);

                if (result.Methods.Length == 0)
                {
                    return;
                }

                foreach (EventMethod method in result.Methods)
                {
                    EmitMethod(method, writer);
                }

            }

            ///<summary>Emits the event partial method</summary>
            private static void EmitMethod(EventMethod method, IndentedTextWriter writer)
            {
                Debug.Assert(method != null);
                Debug.Assert(writer != null);
                Debug.Assert(method.Arguments != null);

                //Write method header
                writer.WriteLine("");
                writer.WriteLine('{');
                writer.Indent++;

                if (method.Arguments.Count != 0)
                {
                    writer.WriteLine($"{EventData}* datas = stackalloc {EventData}[{method.Arguments.Count}]");

                    foreach (EventMethodArgument item in method.Arguments)
                    {
                        EmitArgument(item, writer);
                    }

                    writer.WriteLine($"WriteEventCore({method.EventId}, {method.Arguments.Count}, datas);");
                }
                else
                {
                    writer.WriteLine($"WriteEvent({method.EventId});");
                }

                writer.WriteLine($"On{method.Name}({string.Join(", ", method.Arguments.Select(x => x.Name))});");

                writer.Indent--;
                writer.WriteLine('}');
                writer.WriteLine();
                writer.WriteLine($"[{NonEventAttribute}]");
                writer.WriteLine($"partial void On{method.Name}({string.Join(", ", method.Arguments.Select(x => $"{x.TypeName} {x.Name}"))});");
                writer.WriteLine();
            }

            ///<summary>Emits the argument set the EventData to the `datas` variable</summary>
            private static void EmitArgument(EventMethodArgument argument, IndentedTextWriter writer)
            {
                writer.WriteLine($"datas[{argument.Index}] = new {EventData}");
                writer.WriteLine('{');
                writer.Indent++;
                writer.WriteLine($"DataPointer = {argument.Name} == null ? {IntPtrZero} : (nint){AsPointer}(ref {GetReference}({AsSpan}({argument.Name}))),");
                writer.WriteLine($"Size = {argument.Name} == null ? 0 : (({argument.Name}.Length + 1) * sizeof(char)),");
                writer.WriteLine("Reserved = 0");//I don't know if this is necessary or not
                writer.Indent--;
                writer.WriteLine('}');
            }

        }

    }
}
