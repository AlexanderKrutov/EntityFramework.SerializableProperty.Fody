using System;
using System.Collections.Generic;
using EntityFramework.SerializableProperty;

namespace AssemblyToProcess
{
    /// <summary>
    /// Simple model class describing a purchase order in a shop.
    /// </summary>
    public class PurchaseOrder
    {
        /// <summary>
        /// Unique order id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Order's total amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// This is a property that will be persisted in serialized form.
        /// </summary>
        [SerializableProperty(typeof(SimpleJsonSerializer))]
        public List<Product> Products { get; set; }

        /// <summary>
        /// Creates new instance of PurchaseOrder.
        /// </summary>
        public PurchaseOrder()
        {
            Products = new List<Product>();
        }
    }

    /// <summary>
    /// Product included in order
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Product price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Product name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Product quantity
        /// </summary>
        public int Quantity { get; set; }
    }
}
