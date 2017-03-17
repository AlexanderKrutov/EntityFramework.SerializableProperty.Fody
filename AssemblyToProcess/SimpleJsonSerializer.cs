using Newtonsoft.Json;
using EntityFramework.SerializableProperty;
using System;

namespace AssemblyToProcess
{
    /// <summary>
    /// Simple implementation of <see cref="ISerializer" />. 
    /// <see cref="Newtonsoft.Json.JsonConvert"/> is used here but this is not mandatory; you can use any serialization techic you prefer.
    /// The single requirement for the serializer is to implement <see cref="ISerializer" interface. /> and to have parameterless constructor.
    /// </summary>
    public class SimpleJsonSerializer : ISerializer
    {
        public SimpleJsonSerializer() { }

        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public T Deserialize<T>(string str) 
        {
            return JsonConvert.DeserializeObject<T>(str);
        }
    }
}
