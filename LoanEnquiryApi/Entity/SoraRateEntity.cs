﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LoanEnquiryApi.Entity
{
    public class SoraRateEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SoraRate1M { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SoraRate3M { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal SoraRate6M { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
