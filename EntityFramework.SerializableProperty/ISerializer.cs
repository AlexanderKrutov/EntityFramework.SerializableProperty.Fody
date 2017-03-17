namespace EntityFramework.SerializableProperty
{
    /// <summary>
    /// Interface of serializer to be used for persisting EF serializable property values.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes object to string.
        /// </summary>
        /// <param name="obj">Object to be serialized.</param>
        /// <returns>The object serialized as a string.</returns>
        string Serialize(object obj);

        /// <summary>
        /// Deserializes object from string.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="str">The object serialized as a string.</param>
        /// <returns>Instance of type <typeparamref name="T"/> deserialized from the string.</returns>
        T Deserialize<T>(string str);
    }
}
