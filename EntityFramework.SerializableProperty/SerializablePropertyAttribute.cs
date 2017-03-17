using System;

namespace EntityFramework.SerializableProperty
{
    /// <summary>
    /// Indicates that the model property should be stored in database in serialized form.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class SerializablePropertyAttribute : Attribute
    {
        /// <summary>
        /// Creates new instance of SerializablePropertyAttribute.
        /// </summary>
        /// <param name="serializer">Type of ISerializer implementation used for serializing/deserializing property.</param>
        public SerializablePropertyAttribute(Type serializer)
        {
            Serializer = serializer;
        }

        /// <summary>
        /// Gets type of ISerializer implementation used for serializing/deserializing property.
        /// </summary>
        public Type Serializer { get; private set; }
    }
}
