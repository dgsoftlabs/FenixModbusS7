using Newtonsoft.Json;
using System;

namespace ProjectDataLib
{
    /// <summary>
    /// Provides deep cloning via JSON serialization (replacement for deprecated BinaryFormatter).
    /// </summary>
    internal static class ObjectCloner
    {
        private static readonly JsonSerializerSettings CloneSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.None,
            Error = (sender, args) =>
            {
                args.ErrorContext.Handled = true;
            }
        };

        /// <summary>
        /// Deep-clones an object by serializing to JSON and deserializing back.
        /// Only [Serializable] / JSON-visible properties are copied.
        /// </summary>
        public static T DeepClone<T>(T source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var json = JsonConvert.SerializeObject(source, CloneSettings);
            return JsonConvert.DeserializeObject<T>(json, CloneSettings);
        }
    }
}