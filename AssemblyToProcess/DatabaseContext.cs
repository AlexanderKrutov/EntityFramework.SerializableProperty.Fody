using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFramework.SerializableProperty;

namespace AssemblyToProcess
{
    public class DatabaseContext : DbContext
    {
        public virtual DbSet<PurchaseOrder> Orders { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DatabaseContext>());

            modelBuilder.Entity<PurchaseOrder>()
                .ToTable("Orders")
                .Serialized(t => t.Products)
                .HasKey(t => t.Id);
        }
    }
}
