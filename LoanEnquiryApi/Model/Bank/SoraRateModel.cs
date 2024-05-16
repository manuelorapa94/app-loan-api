using System.ComponentModel.DataAnnotations.Schema;

namespace LoanEnquiryApi.Model.Bank
{
    public class ViewSoraRateModel
    {
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
