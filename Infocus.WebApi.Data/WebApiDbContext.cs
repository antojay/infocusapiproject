using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data
{
    public partial class WebApiDbContext : DbContext
    {

        public WebApiDbContext()
            : base("WebApiDbContext")
        {

        }
        public WebApiDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public WebApiDbContext(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
        }

        public WebApiDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
        }

        public WebApiDbContext(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
        }

        public WebApiDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
        }

        public List<Edi850HeaderRecord> Get856sToProcess()
        {
            return (from record in Edi850HeaderRecords
                    where record.Processed856 == false 
                    // 01-25-2023 begin
                    &&  (from v in this.DeliveryLines.Include("Delivery").Include("Delivery.DeliveryLines")
                                             where v.Delivery.U_Info850HdrId == record.HeaderId 
                                                       && v.Delivery.Canceled == "N"
                                                 && (v.TargetType == null || v.TargetType != 16)
                                                 && v.Delivery.U_Info_BOL != null
                                             select v).FirstOrDefault().DocEntry > 0
                    // 01-25-2023 end
                    select record).ToList();
        }

        public DbSet<Delivery> Deliveries
        {
            get;
            set;
        }
        public DbSet<DeliveryLine> DeliveryLines
        {
            get;
            set;
        }
        public DbSet<Invoice> Invoices
        {
            get;
            set;
        }
        public DbSet<InvoiceLine> InvoiceLines
        {
            get;
            set;
        }

        public DbSet<SOrder> SOrders
        {
            get;
            set;
        }
        public DbSet<SOLine> SOLines
        {
            get;
            set;
        }
        // 05-31-2017 end

        // 08-20-2017 begin
        public DbSet<CreditMemo> CreditMemos
        {
            get;
            set;
        }
        public DbSet<CreditMemoLine> CreditMemoLines
        {
            get;
            set;
        }
        // 08-20-2017 end

        // 07-23-2019 begin
        public DbSet<SReturn> SReturns
        {
            get;
            set;
        }
        public DbSet<SRLine> SRLines
        {
            get;
            set;
        }
        // 07-23-2019 end

        // 02-25-2019 begin
        public DbSet<Edi860HeaderRecord> Edi860HeaderRecords
        {
            get;
            set;
        }
        public DbSet<Edi860DetailRecord> Edi860DetailRecords
        {
            get;
            set;
        }

        public DbSet<Edi180HeaderRecord> Edi180HeaderRecords
        {
            get;
            set;
        }
        public DbSet<Edi180DetailRecord> Edi180DetailRecords
        {
            get;
            set;
        }
        // 02-25-2019 end

        // 04-26-2019 begin
        public DbSet<Edi820HeaderRecord> Edi820HeaderRecords
        {
            get;
            set;
        }
        public DbSet<Edi820DetailRecord> Edi820DetailRecords
        {
            get;
            set;
        }
        // 04-26-2019 end
        public DbSet<Edi850HeaderRecord> Edi850HeaderRecords
        {
            get;
            set;
        }
        public DbSet<Edi850DetailRecord> Edi850DetailRecords
        {
            get;
            set;
        }

        // 06-03-2022 begin
        public DbSet<EdiTransactionFailures> EdiTransactionFailures
        {
            get;
            set;
        }
        // 06-03-2022 end

        // 07-21-2023 begin
        public DbSet<Edi940HeaderRecord> Edi940HeaderRecords
        {
            get;
            set;
        }
        public DbSet<Edi940DetailRecord> Edi940DetailRecords
        {
            get;
            set;
        }
        // 07-21-2023 end

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Edi850DetailRecord>()
   .HasRequired<Edi850HeaderRecord>(h => h.Edi850HeaderRecord)
   .WithMany(d => d.Details)
   .HasForeignKey(d => d.HeaderId);
            // 12-28-2022 begin
            modelBuilder.Entity<Delivery>()
   .HasRequired<Edi850HeaderRecord>(h => h.Edi850HeaderRecord)
   .WithMany(d => d.Deliveries)
   .HasForeignKey(d => d.U_Info850HdrId);
            // 12-28-2022 end

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

        }
    }
}
