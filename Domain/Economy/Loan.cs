using System;

namespace SkyHorizont.Domain.Economy
{
    public enum LoanAccountType { Planet, Faction, Character }

    public sealed class Loan
    {
        public Guid Id { get; }
        public LoanAccountType AccountType { get; }
        public Guid OwnerId { get; }
        public int Principal { get; }
        public double MonthlyInterestRate { get; } // e.g., 0.02 = 2% / turn (month)
        public int TermMonths { get; }
        public int RemainingPrincipal { get; private set; }
        public int StartYear { get; }
        public int StartMonth { get; }
        public bool IsDefaulted { get; private set; }

        public Loan(Guid id, LoanAccountType type, Guid ownerId, int principal,
                    double monthlyRate, int termMonths, int startYear, int startMonth)
        {
            Id = id;
            AccountType = type;
            OwnerId = ownerId;
            Principal = principal;
            MonthlyInterestRate = monthlyRate;
            TermMonths = termMonths;
            RemainingPrincipal = principal;
            StartYear = startYear;
            StartMonth = startMonth;
        }

        public int AccrueInterest()
        {
            if (IsDefaulted || RemainingPrincipal <= 0) return 0;
            // round up interest to integer credits
            int interest = (int)Math.Ceiling(RemainingPrincipal * MonthlyInterestRate);
            RemainingPrincipal += interest;
            return interest;
        }

        public int MakePayment(int amount)
        {
            if (IsDefaulted || RemainingPrincipal <= 0 || amount <= 0) return 0;
            int paid = Math.Min(amount, RemainingPrincipal);
            RemainingPrincipal -= paid;
            return paid;
        }

        public void MarkDefault() => IsDefaulted = true;
        public bool IsFullyRepaid => RemainingPrincipal <= 0;
    }
}
