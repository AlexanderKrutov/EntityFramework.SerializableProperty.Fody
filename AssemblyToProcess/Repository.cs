using System;
using System.Linq;
using System.Collections.Generic;
using EntityFramework.SerializableProperty;

namespace AssemblyToProcess
{
    /// <summary>
    /// Simple repository implementation.
    /// </summary>
    public static class Repository
    {
        /// <summary>
        /// Clears database
        /// </summary>
        public static void ClearOrders()
        {
            using (var ctx = new DatabaseContext())
            {
                ctx.Orders.RemoveRange(ctx.Orders.ToList());
                ctx.SaveChanges();
            }
        }

        /// <summary>
        /// Adds order to database
        /// </summary>
        public static void AddOrder(PurchaseOrder order)
        {
            using (var ctx = new DatabaseContext())
            {
                ctx.Orders.Add(order);
                ctx.SaveChanges();
            }
        }

        /// <summary>
        /// Gets all orders from database
        /// </summary>
        /// <returns></returns>
        public static List<PurchaseOrder> GetOrders()
        {
            using (var ctx = new DatabaseContext())
            {
                return ctx.Orders.ToList();
            }
        }
    }
}
