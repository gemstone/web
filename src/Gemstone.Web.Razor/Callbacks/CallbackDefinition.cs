//******************************************************************************************************
//  CallbackDefinition.cs - Gbtc
//
//  Copyright © 2022, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  07/14/2020 - Stephen C. Wills
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace Gemstone.Web.Razor.Callbacks
{
    /// <summary>
    /// Defines a callback that can be invoked by the <see cref="IJSRuntime"/>.
    /// </summary>
    [JsonConverter(typeof(Converter))]
    public class CallbackDefinition : IDisposable
    {
        #region [ Members ]

        // Nested Types
        private class Converter : JsonConverter<CallbackDefinition>
        {
            // It's not possible to determine the type of the DotNetObjectReference<T> without some more metadata.
            // The callback is designed for sending callbacks to JavaScript so Read() implementation is not worth the effort.
            public override CallbackDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                throw new NotImplementedException();

            public override void Write(Utf8JsonWriter writer, CallbackDefinition value, JsonSerializerOptions options)
            {
                value.Initialize();

                writer.WriteStartObject();

                if (value is { ReferenceType: not null, Reference: not null })
                {
                    writer.WritePropertyName("instance");
                    JsonSerializer.Serialize(writer, value.Reference, value.ReferenceType, options);
                }

                if (value.AssemblyName != null)
                    writer.WriteString("assemblyName", value.AssemblyName);

                writer.WriteString("methodName", value.MethodName);
                writer.WriteEndObject();
            }
        }

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly in which the callback is defined.</param>
        /// <param name="methodName">The name of the method in which the </param>
        public CallbackDefinition(string assemblyName, string methodName)
        {
            AssemblyName = assemblyName;
            MethodName = methodName;
            Initialized = true;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="methodCapture">The method to be invoked from Javascript.</param>
        /// <exception cref="ArgumentException">The method to be invoked is not annotated with the <see cref="JSInvokableAttribute"/>.</exception>
        public CallbackDefinition(Delegate methodCapture)
        {
            MethodInfo method = methodCapture.Method;
            IEnumerable<JSInvokableAttribute> invokables = method.GetCustomAttributes<JSInvokableAttribute>();

            if (!invokables.Any())
                throw new ArgumentException("The method captured by the callback must be JS invokable.", nameof(methodCapture));

            MethodCapture = methodCapture;

            MethodName = invokables
                .Select(invokable => invokable.Identifier)
                .Where(name => name != null)
                .DefaultIfEmpty(method.Name)
                .First();
        }

        private CallbackDefinition(Type referenceType, object reference, string methodName, bool disposeReference = false)
        {
            ReferenceType = referenceType;
            Reference = reference;
            MethodName = methodName;
            DisposeReference = disposeReference;
            Initialized = true;
        }

        #endregion

        #region [ Properties ]

        private Delegate? MethodCapture { get; }
        private Type? ReferenceType { get; set; }
        private object? Reference { get; set; }
        private string? AssemblyName { get; set; }
        private string MethodName { get; }

        private bool Initialized { get; set; }
        private bool Disposed { get; set; }
        private bool DisposeReference { get; } = true;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases all unmanaged resources associated with this callback.
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                return;

            try
            {
                static void TryDispose(object? obj) =>
                    (obj as IDisposable)?.Dispose();

                if (DisposeReference)
                    TryDispose(Reference);
            }
            finally
            {
                Disposed = true;
            }
        }

        private void Initialize()
        {
            if (Initialized)
                return;

            if (MethodCapture == null)
                throw new Exception("Callback initialization requires that a method be captured.");

            object? CreateReference()
            {
                if (Disposed)
                    throw new ObjectDisposedException(nameof(CallbackDefinition));

                Type referenceType = MethodCapture.Method.DeclaringType;
                Type factoryType = typeof(DotNetObjectReference);
                string factoryMethodName = nameof(DotNetObjectReference.Create);

                MethodInfo factoryMethod = factoryType
                    .GetMethod(factoryMethodName)
                    .MakeGenericMethod(referenceType);

                ReferenceType = factoryMethod.ReturnType;
                Reference = factoryMethod.Invoke(null, new[] { MethodCapture.Target });
                return Reference;
            }

            if (MethodCapture.Target != null)
                Reference = CreateReference();
            else
                AssemblyName = MethodCapture.Method.DeclaringType.Assembly.GetName().Name;

            Initialized = true;
        }

        #endregion

        #region [ Static ]

        // Static Methods

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <typeparam name="T">The type containing the instance method to be captured in a callback.</typeparam>
        /// <param name="instance">The instance containing the method to be captured in a callback.</param>
        /// <param name="methodName">The name of the method to be captured in the callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition Create<T>(T instance, string methodName) where T : class =>
            Create(DotNetObjectReference.Create(instance), methodName, true);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <typeparam name="T">The type containing the instance method to be captured in a callback.</typeparam>
        /// <param name="reference">The reference to the instance containing the method to be captured in a callback.</param>
        /// <param name="methodName">The name of the method to be captured in the callback.</param>
        /// <param name="disposeReference">True if the callback should dispose the reference when it is disposed; otherwise false.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition Create<T>(DotNetObjectReference<T> reference, string methodName, bool disposeReference = false) where T : class =>
            new(typeof(DotNetObjectReference<T>), reference, methodName, disposeReference);

        #region [ Delegate Captures ]

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From(Action action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T>(Action<T> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2>(Action<T1, T2> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3>(Action<T1, T2, T3> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="action">The action to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action) => new(action);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<TResult>(Func<TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T, TResult>(Func<T, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, TResult>(Func<T1, T2, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> func) => new(func);

        /// <summary>
        /// Creates a new instance of the <see cref="CallbackDefinition"/> class.
        /// </summary>
        /// <param name="func">The function to be captured in a callback.</param>
        /// <returns>The definition for the callback to be used with JavaScript.</returns>
        public static CallbackDefinition From<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> func) => new(func);

        #endregion

        #endregion
    }
}
