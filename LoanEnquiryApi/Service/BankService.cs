using ClosedXML.Excel;
using LoanEnquiryApi.Constant;
using LoanEnquiryApi.Entity;
using LoanEnquiryApi.Model.Bank;
using Microsoft.EntityFrameworkCore;

namespace LoanEnquiryApi.Service
{
    public class BankService(DataContext dataContext)
    {
        private readonly DataContext _dataContext = dataContext;

        internal bool CreateBank(CreateBankModel model)
        {
            var entity = new BankEntity
            {
                Name = model.Name,
                ContactPersonName = model.ContactPersonName,
                ContactEmail = model.ContactEmail,
                ContactNo = model.ContactNo,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
            };

            _dataContext.Banks.Add(entity);

            return _dataContext.SaveChanges() > 0;
        }

        internal bool UpdateBank(UpdateBankModel model)
        {
            var entity = _dataContext.Banks.Find(model.Id);

            if (entity == null) return false;

            entity.Name = model.Name;
            entity.ContactPersonName = model.ContactPersonName;
            entity.ContactEmail = model.ContactEmail;
            entity.ContactNo = model.ContactNo;
            entity.UpdatedAt = DateTime.Now;

            return _dataContext.SaveChanges() > 0;
        }

        internal bool UpdateBankLogo(UpdateBankLogoModel model)
        {
            var entity = _dataContext.Banks.Find(model.Id);

            if (entity == null) return false;

            using var ms = new MemoryStream();
            model.Logo.CopyTo(ms);
            var fileBytes = ms.ToArray();
            string base64Logo = Convert.ToBase64String(fileBytes);

            entity.Logo = base64Logo;
            entity.UpdatedAt = DateTime.Now;

            return _dataContext.SaveChanges() > 0;
        }

        internal bool DeleteBank(Guid id)
        {
            var entity = _dataContext.Banks.Find(id);

            if (entity == null) return false;

            _dataContext.Banks.Remove(entity);

            return _dataContext.SaveChanges() > 0;
        }

        internal List<ListBankModel> GetBanks()
        {
            return _dataContext.Banks
                .Select(b => new ListBankModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    ContactPersonName = b.ContactPersonName,
                    ContactEmail = b.ContactEmail,
                    ContactNo = b.ContactNo,
                    BankLogo = b.Logo,
                }).ToList();
        }

        internal ViewBankModel GetBank(Guid id)
        {
            var entity = _dataContext.Banks
                .Include(b => b.BankRates)
                .FirstOrDefault(b => b.Id == id);

            if (entity == null) return null;

            var newPurchase = entity.BankRates
                .Where(b => b.LoanType == LoanType.NewPurchase)
                .Select(b => new BankPropertyTypeRate
                {
                    Year = b.Year,
                    PropertyType = b.PropertyType,
                    PropertyTypeName = b.PropertyType.ToString(),
                    RateType = b.RateType,
                    RateTypeName = b.RateType.ToString(),
                    InterestRate = b.InterestRate,
                    MonthlyInstallment = b.InterestRate / 100 / 12 * b.MinLoanAmount
                })
                .OrderBy(b => b.PropertyType).ThenBy(b => b.RateType).ThenBy(b => b.Year)
                .ToList();

            var refinance = entity.BankRates
                .Where(b => b.LoanType == LoanType.Refinance)
                .Select(b => new BankPropertyTypeRate
                {
                    Year = b.Year,
                    PropertyType = b.PropertyType,
                    PropertyTypeName = b.PropertyType.ToString(),
                    RateType = b.RateType,
                    RateTypeName = b.RateType.ToString(),
                    InterestRate = b.InterestRate,
                    MonthlyInstallment = b.InterestRate / 100 / 12 * b.MinLoanAmount
                })
                .OrderBy(b => b.PropertyType).ThenBy(b => b.RateType).ThenBy(b => b.Year)
                .ToList();

            return new ViewBankModel
            {
                Id = entity.Id,
                Name = entity.Name,
                ContactPersonName = entity.ContactPersonName,
                ContactEmail = entity.ContactEmail,
                ContactNo = entity.ContactNo,
                BankLogo = entity.Logo,
                NewPurchaseRates = newPurchase,
                RefinanceRates = refinance,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        internal List<BankDropdownModel> GetBankDropdown()
        {
            return _dataContext.Banks
                .Select(b => new BankDropdownModel
                {
                    Id = b.Id,
                    Name = b.Name
                }).ToList();
        }
        internal string? ImportBankRate(ImportBankRateModel model)
        {
            var bankEntities = _dataContext.Banks.ToList();

            var bankRateEntities = GetBankRate(bankEntities, model.NewPurchaseRateFile, LoanType.NewPurchase, out string? errorMessage);
            if (!string.IsNullOrEmpty(errorMessage)) return errorMessage;

            bankRateEntities.AddRange(GetBankRate(bankEntities, model.RefinanceRateFile, LoanType.Refinance, out errorMessage));

            if (!string.IsNullOrEmpty(errorMessage)) return errorMessage;

            _dataContext.Database.ExecuteSqlRaw("TRUNCATE TABLE BankRates");

            _dataContext.BankRates.AddRange(bankRateEntities);
            _dataContext.SaveChanges();

            return null;
        }

        private List<BankRateEntity> GetBankRate(List<BankEntity> bankEntities, IFormFile file, LoanType loanType, out string? errorMessage)
        {
            List<BankRateEntity> bankRateEntities = [];
            errorMessage = null;

            using var workbook = new XLWorkbook(file.OpenReadStream());

            var worksheet = workbook.Worksheet(1);

            int row = 2; // row 1 is header
            while (true)
            {
                var bankName = GetValue<string>(worksheet, row, "A");
                var propertyType = GetValue<string>(worksheet, row, "B");
                var rateType = GetValue<string>(worksheet, row, "C");
                var lockIn= GetValue<int>(worksheet, row, "D");
                var minLoanAmount = GetValue<int>(worksheet, row, "E");
                var sora = GetValue<int>(worksheet, row, "F");
                var year1 = GetValue<decimal>(worksheet, row, "G");
                var year2 = GetValue<decimal>(worksheet, row, "H");
                var year3 = GetValue<decimal>(worksheet, row, "I");
                var year4 = GetValue<decimal>(worksheet, row, "J");
                var year5 = GetValue<decimal>(worksheet, row, "K");

                if (string.IsNullOrEmpty(bankName) || string.IsNullOrEmpty(propertyType) || minLoanAmount < 0 || string.IsNullOrEmpty(rateType) || year1 < 0 ||
                    year2 < 0 || year3 < 0 || year4 < 0 || year5 < 0) break;

                var bankEntity = bankEntities.Where(b => b.Name == bankName).FirstOrDefault();
                if (bankEntity == null)
                {
                    errorMessage = $"Invalid Bank - {bankName} at row {row}";
                    return [];
                }
                var isValidPropertyType = Enum.TryParse<PropertyType>(propertyType, out var _propertyType);
                if (!isValidPropertyType)
                {
                    errorMessage = $"Invalid PropertyType - {propertyType} at row {row}";
                    return [];
                }

                var isValidRateType = Enum.TryParse<RateType>(rateType, out var _rateType);
                if (!isValidRateType)
                {
                    errorMessage = $"Invalid RateType - {rateType} at row {row}";
                    return [];
                }

                var latestRateEntry = _dataContext.SoraRates.OrderByDescending(a => a.CreatedAt).FirstOrDefault();

                decimal latestRate = 0;

                //Determine which SORA rate to use based on the value of sora
                switch (sora)
                {
                    case 1:
                        latestRate = latestRateEntry.SoraRate1M;
                        break;
                    case 2:
                        latestRate = latestRateEntry.SoraRate3M;
                        break;
                    case 3:
                        latestRate = latestRateEntry.SoraRate6M;
                        break;
                    default:
                        throw new ArgumentException("Invalid SORA value");
                }

                RateType rateTypeEnum;
                if (!Enum.TryParse(rateType, true, out rateTypeEnum))
                {
                    throw new ArgumentException("Invalid rateType value");
                }

                decimal year1Rate = 0, year2Rate = 0, year3Rate = 0, year4Rate = 0, year5Rate = 0;
                if (rateTypeEnum == RateType.Fixed)
                {
                    if (lockIn == 1)
                    {
                        year1Rate = year1;
                        year2Rate = year2 + latestRate;
                        year3Rate = year3 + latestRate;
                        year4Rate = year4 + latestRate;
                        year5Rate = year5 + latestRate;
                    } 
                    else if (lockIn == 2)
                    {
                        year1Rate = year1;
                        year2Rate = year2;
                        year3Rate = year3 + latestRate;
                        year4Rate = year4 + latestRate;
                        year5Rate = year5 + latestRate;
                    } 
                    else if (lockIn == 3)
                    {
                        year1Rate = year1;
                        year2Rate = year2;
                        year3Rate = year3;
                        year4Rate = year4 + latestRate;
                        year5Rate = year5 + latestRate;
                    }
                    else if (lockIn == 4)
                    {
                        year1Rate = year1;
                        year2Rate = year2;
                        year3Rate = year3;
                        year4Rate = year4;
                        year5Rate = year5 + latestRate;
                    } 
                    else if (lockIn == 5)
                    {
                        year1Rate = year1;
                        year2Rate = year2;
                        year3Rate = year3;
                        year4Rate = year4;
                        year5Rate = year5;
                    } 
                    else
                    {
                        year1Rate = year1 + latestRate;
                        year2Rate = year2 + latestRate;
                        year3Rate = year3 + latestRate;
                        year4Rate = year4 + latestRate;
                        year5Rate = year5 + latestRate;
                    }
                } else if (rateTypeEnum == RateType.Floating)
                {
                    year1Rate = year1 + latestRate;
                    year2Rate = year2 + latestRate;
                    year3Rate = year3 + latestRate;
                    year4Rate = year4 + latestRate;
                    year5Rate = year5 + latestRate;
                }
                else
                {
                    Console.WriteLine("This is for Both option.");
                }

                for (int year = 1; year < 37; year++)
                {
                    BankRateEntity bankRateEntity = new BankRateEntity
                    {
                        BankId = bankEntity.Id,
                        LoanType = loanType,
                        PropertyType = _propertyType,
                        RateType = _rateType,
                        MinLoanAmount = minLoanAmount,
                        LockIn = lockIn,
                        Year = year,
                        InterestRate = GetInterestRate(year, year1Rate, year2Rate, year3Rate, year4Rate, year5Rate),
                        MonthlyInstallment = GetMonthlyInstallment(minLoanAmount, GetInterestRate(year, year1Rate, year2Rate, year3Rate, year4Rate, year5Rate), year),
                        CreatedAt = DateTime.Now,
                    };

                    bankRateEntities.Add(bankRateEntity);
                }

                row++;
            }

            return bankRateEntities;
        }

        private decimal GetInterestRate(int year, decimal year1, decimal year2, decimal year3, decimal year4, decimal year5)
        {
            if (year == 1) return year1;
            if (year == 2) return year2;
            if (year == 3) return year3;
            if (year == 4) return year4;
            return year5;
        }

        private static decimal GetMonthlyInstallment(decimal _minLoanAmount, decimal annualInterestRate, int loanTerm)
        {
            decimal monthlyInterestRate = annualInterestRate / 100 / 12;
            int totalPayments = loanTerm * 12;

            decimal monthlyPayment = _minLoanAmount * (monthlyInterestRate * (decimal)Math.Pow((double)(1 + monthlyInterestRate), totalPayments))
                                    / ((decimal)Math.Pow((double)(1 + monthlyInterestRate), totalPayments) - 1);

            return monthlyPayment;
        }

        private static T? GetValue<T>(IXLWorksheet worksheet, int row, string cell)
        {
            var isValid = worksheet.Row(row).Cell(cell).TryGetValue<T>(out var value);

            if (!isValid) return default;

            return value;
        }
    }
}
