// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Git.CredentialManager
{
    /// <summary>
    /// Represents the application's tracing system.
    /// </summary>
    public interface ITrace
    {
        /// <summary>
        /// True if any listeners have been added to the tracing system.
        /// </summary>
        bool HasListeners { get; }

        /// <summary>
        /// Get or set whether or not sensitive information such as secrets and credentials should be
        /// output to attached trace listeners.
        /// </summary>
        bool IsSecretTracingEnabled { get; set; }

        /// <summary>
        /// Add a listener to the trace writer.
        /// </summary>
        /// <param name="listener">The listener to add.</param>
        void AddListener(TextWriter listener);

        /// <summary>
        /// Forces any pending trace messages to be written to any listeners.
        /// </summary>
        void Flush();

        /// <summary>
        /// Writes an exception as a message to the trace writer.
        /// <para/>
        /// Expands exceptions' inner exceptions into additional trace lines.
        /// </summary>
        /// <param name="exception">The exception to write.</param>
        /// <param name="filePath">Path of the file this method is called from.</param>
        /// <param name="lineNumber">Line number of file this method is called from.</param>
        /// <param name="memberName">Name of the member in which this method is called.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        void WriteException(
            Exception exception,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");

        /// <summary>
        /// Write the contents of a dictionary to the trace writer.
        /// <para/>
        /// Calls <see cref="object.ToString"/> on all keys and values.
        /// </summary>
        /// <param name="dictionary">The dictionary to write.</param>
        /// <param name="filePath">Path of the file this method is called from.</param>
        /// <param name="lineNumber">Line number of file this method is called from.</param>
        /// <param name="memberName">Name of the member in which this method is called.</param>
        void WriteDictionary<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");

        /// <summary>
        /// Write the contents of a dictionary that contains sensitive information to the trace writer.
        /// <para/>
        /// Calls <see cref="object.ToString"/> on all keys and values, except keys specified as secret.
        /// </summary>
        /// <param name="dictionary">The dictionary to write.</param>
        /// <param name="secretKeys">Dictionary keys that contain secrets/sensitive information.</param>
        /// <param name="keyComparer">Comparer to use for <paramref name="secretKeys"/>.</param>
        /// <param name="filePath">Path of the file this method is called from.</param>
        /// <param name="lineNumber">Line number of file this method is called from.</param>
        /// <param name="memberName">Name of the member in which this method is called.</param>
        void WriteDictionarySecrets<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary,
            TKey[] secretKeys,
            IEqualityComparer<TKey> keyComparer = null,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");

        /// <summary>
        /// Writes a message to the trace writer followed by a line terminator.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="filePath">Path of the file this method is called from.</param>
        /// <param name="lineNumber">Line number of file this method is called from.</param>
        /// <param name="memberName">Name of the member in which this method is called.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        void WriteLine(
            string message,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");

        /// <summary>
        /// Writes a message containing sensitive information to the trace writer followed by a line terminator.
        /// <para/>
        /// Attached listeners will only receive the fully formatted message if <see cref="IsSecretTracingEnabled"/> is set
        /// to true, otherwise the secret arguments will be masked.
        /// </summary>
        /// <param name="format">The format string to write.</param>
        /// <param name="secrets">Sensitive/secret arguments for the format string.</param>
        /// <param name="filePath">Path of the file this method is called from.</param>
        /// <param name="lineNumber">Line number of file this method is called from.</param>
        /// <param name="memberName">Name of the member in which this method is called.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        void WriteLineSecrets(
            string format,
            object[] secrets,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");
    }

    public class Trace : ITrace, IDisposable
    {
        private const string SecretMask = "********";

        private readonly object _writersLock = new object();
        private readonly List<TextWriter> _writers = new List<TextWriter>();

        public bool HasListeners
        {
            get
            {
                lock (_writersLock)
                {
                    return _writers.Any();
                }
            }
        }

        public bool IsSecretTracingEnabled { get; set; }

        public void AddListener(TextWriter listener)
        {
            lock (_writersLock)
            {
                // Try not to add the same listener more than once
                if (_writers.Contains(listener))
                    return;

                _writers.Add(listener);
            }
        }

        ~Trace()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        public void Flush()
        {
            lock (_writersLock)
            {
                foreach (var writer in _writers)
                {
                    try
                    {
                        writer?.Flush();
                    }
                    catch
                    { /* squelch */ }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void WriteException(
            Exception exception,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            // Exception being null probably won't happen, but we shouldn't die because we failed to trace it.
            if (exception is null)
                return;

            WriteLine($"! error: '{exception.Message}'.", filePath, lineNumber, memberName);

            while ((exception = exception.InnerException) != null)
            {
                WriteLine($"       > '{exception.Message}'.", filePath, lineNumber, memberName);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void WriteDictionary<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            foreach (KeyValuePair<TKey, TValue> entry in dictionary)
            {
                WriteLine($"\t{entry.Key}={entry.Value}", filePath, lineNumber, memberName);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void WriteDictionarySecrets<TKey, TValue>(
            IDictionary<TKey, TValue> dictionary,
            TKey[] secretKeys,
            IEqualityComparer<TKey> keyComparer = null,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            foreach (KeyValuePair<TKey, TValue> entry in dictionary)
            {
                bool isSecretEntry = !(secretKeys is null) &&
                                     secretKeys.Contains(entry.Key, keyComparer ?? EqualityComparer<TKey>.Default);
                if (isSecretEntry && !this.IsSecretTracingEnabled)
                {
                    WriteLine($"\t{entry.Key}={SecretMask}", filePath, lineNumber, memberName);
                }
                else
                {
                    WriteLine($"\t{entry.Key}={entry.Value}", filePath, lineNumber, memberName);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void WriteLine(
            string message,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            lock (_writersLock)
            {
                if (_writers.Count == 0)
                {
                    return;
                }

                string text = FormatText(message, filePath, lineNumber, memberName);

                foreach (var writer in _writers)
                {
                    try
                    {
                        writer?.Write(text);
                        writer?.Write('\n');
                        writer?.Flush();
                    }
                    catch { /* squelch */ }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void WriteLineSecrets(
            string format,
            object[] secrets,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            string message = this.IsSecretTracingEnabled
                           ? string.Format(format, secrets)
                           : string.Format(format, secrets.Select(_ => (object)SecretMask).ToArray());

            WriteLine(message, filePath, lineNumber, memberName);
        }

        private void Dispose(bool finalizing)
        {
            if (!finalizing)
            {
                lock (_writersLock)
                {
                    try
                    {
                        for (int i = 0; i < _writers.Count; i += 1)
                        {
                            using (var writer = _writers[i])
                            {
                                _writers.Remove(writer);
                            }
                        }
                    }
                    catch
                    { /* squelch */ }
                }
            }
        }

        private static string FormatText(string message, string filePath, int lineNumber, string memberName)
        {
            const int sourceColumnMaxWidth = 23;

            EnsureArgument.NotNull(message, nameof(message));
            EnsureArgument.NotNull(filePath, nameof(filePath));
            EnsureArgument.PositiveOrZero(lineNumber, nameof(lineNumber));
            EnsureArgument.NotNull(memberName, nameof(memberName));

            // Source column format is file:line
            string source = $"{filePath}:{lineNumber}";

            if (source.Length > sourceColumnMaxWidth)
            {
                int idx = 0;
                int maxlen = sourceColumnMaxWidth - 3;
                int srclen = source.Length;

                while (idx >= 0 && (srclen - idx) > maxlen)
                {
                    idx = source.IndexOf('\\', idx + 1);
                }

                // If we cannot find a path separator which allows the path to be long enough, just truncate the file name
                if (idx < 0)
                {
                    idx = srclen - maxlen;
                }

                source = "..." + source.Substring(idx);
            }

            // Git's trace format is "{timestamp,-15} {source,-23} trace: {details}"
            string text = $"{DateTime.Now:HH:mm:ss.ffffff} {source,-23} trace: [{memberName}] {message}";

            return text;
        }
    }
}
