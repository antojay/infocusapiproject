using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEDITransactionFailures")]
    public partial class EdiTransactionFailures
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 TrxSeq
        {
            get;
            set;
        }
        public DateTime? RecordDate
        {
            get;
            set;
        }

        public Int32 EDIHeaderId
        {
            get;
            set;
        }

        public DateTime? TrxDateTime
        {
            get;
            set;
        }

        [MaxLength(30)]
        public String TrxType
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }

        [MaxLength(30)]
        public String CardCode
        {
            get;
            set;
        }

        [MaxLength(30)]
        public String SBOCardCode
        {
            get;
            set;
        }

        [MaxLength(100)]
        public String PurchaseOrderReference
        {
            get;
            set;
        }

    }
}
