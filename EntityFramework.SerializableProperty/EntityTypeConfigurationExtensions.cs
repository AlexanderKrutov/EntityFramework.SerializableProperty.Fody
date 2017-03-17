using System;
using System.Data.Entity.ModelConfiguration;
using System.Linq.Expressions;

namespace EntityFramework.SerializableProperty
{
    /// <summary>
    /// Set of extension helpers to configure entity type for serializing complex properties
    /// </summary>
    public static class EntityTypeConfigurationExtensions
    {
        /// <summary>
        /// Tells that the specified model property should be stored in serialized form.
        /// </summary>
        /// <typeparam name="TEntityType">The type of the model containing the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property to be serialized.</typeparam>
        /// <param name="configuration"><see cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{TEntityType}"/> instance to be configured.</param>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured to store in serialized form.</param>
        /// <param name="columnName">Database column name to store the serialized property value.</param>
        /// <returns>The same <see cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{TEntityType}"/> instance so that multiple calls can be chained.</returns>
        public static EntityTypeConfiguration<TEntityType> Serialized<TEntityType, TProperty>(this EntityTypeConfiguration<TEntityType> configuration, Expression<Func<TEntityType, TProperty>> propertyExpression, string columnName) where TEntityType : class
        {
            return Configure(configuration, propertyExpression, columnName);
        }

        /// <summary>
        /// Tells that the specified model property should be stored in serialized form.
        /// </summary>
        /// <typeparam name="TEntityType">The type of the model containing the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property to be serialized.</typeparam>
        /// <param name="configuration"><see cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{TEntityType}"/> instance to be configured.</param>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured to store in serialized form.</param>
        /// <returns>The same <see cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{TEntityType}"/> instance so that multiple calls can be chained.</returns>
        public static EntityTypeConfiguration<TEntityType> Serialized<TEntityType, TProperty>(this EntityTypeConfiguration<TEntityType> configuration, Expression<Func<TEntityType, TProperty>> propertyExpression) where TEntityType : class
        {
            return Configure(configuration, propertyExpression, null);
        }

        /// <summary>
        /// Internal implementation of configration logic.
        /// </summary>
        /// <typeparam name="TEntityType">The type of the model containing the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property to be serialized.</typeparam>
        /// <param name="configuration"><see cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{TEntityType}"/> instance to be configured.</param>
        /// <param name="propertyExpression">A lambda expression representing the property to be configured to store in serialized form.</param>
        /// <param name="column">Database column name to store the serialized property value.</param>
        /// <returns>The same <see cref="System.Data.Entity.ModelConfiguration.EntityTypeConfiguration{TEntityType}"/> instance so that multiple calls can be chained.</returns>
        private static EntityTypeConfiguration<TEntityType> Configure<TEntityType, TProperty>(EntityTypeConfiguration<TEntityType> configuration, Expression<Func<TEntityType, TProperty>> propertyExpression, string column) where TEntityType : class
        {
            // ignore the original property
            configuration.Ignore(propertyExpression);

            // obtain original property name
            string propertyName = GetPropertyName(propertyExpression);

            // generated property name
            string serializedPropertyName = $"{propertyName}_serialized";

            // determine column name. Be default it equals to original property name.
            string columnName = column ?? propertyName;

            // construct the expression for the generated property name
            var param = Expression.Parameter(typeof(TEntityType));
            var body = Expression.PropertyOrField(param, serializedPropertyName);
            var serializedPropertyExpression = Expression.Lambda<Func<TEntityType, string>>(body, param);

            // map serialized property to column with specified name
            configuration.Property(serializedPropertyExpression).HasColumnName(columnName);

            return configuration;
        }

        /// <summary>
        /// Gets name of the property from the expression
        /// </summary>
        private static string GetPropertyName<TModel, TProperty>(Expression<Func<TModel, TProperty>> propertyExpression)
        {
            var expression = propertyExpression.Body as MemberExpression;
            if (expression == null)
            {
                throw new ArgumentException("Expression is not a property.");
            }

            return expression.Member.Name;
        }
    }
}
