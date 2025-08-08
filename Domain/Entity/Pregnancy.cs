namespace SkyHorizont.Domain.Entity
{
    public enum PregnancyStatus { Active, Miscarriage, Stillbirth, Delivered }

    /// <summary>
    /// Pregnancy info lives inside the Mother's aggregate.
    /// Immutable value-object style with With* methods for state transitions.
    /// </summary>
    public sealed class Pregnancy
    {
        public Guid FatherId { get; }
        public int ConceptionYear { get; }
        public int ConceptionMonth { get; }
        public int GestationMonths { get; }
        public PregnancyStatus Status { get; }

        private Pregnancy(Guid fatherId, int year, int month, int gestationMonths, PregnancyStatus status)
        {
            FatherId = fatherId;
            ConceptionYear = year;
            ConceptionMonth = month;
            GestationMonths = gestationMonths;
            Status = status;
        }

        public static Pregnancy Start(Guid fatherId, int year, int month, int gestationMonths = 9)
        {
            if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
            return new Pregnancy(fatherId, year, month, gestationMonths, PregnancyStatus.Active);
        }

        public (int DueYear, int DueMonth) DueDate(int monthsPerYear)
        {
            var total = ConceptionYear * monthsPerYear + ConceptionMonth - 1 + GestationMonths;
            var dueYear = total / monthsPerYear;
            var dueMonth = (total % monthsPerYear) + 1;
            return (dueYear, dueMonth);
        }

        public bool IsDue(int currentYear, int currentMonth, int monthsPerYear)
        {
            var (y, m) = DueDate(monthsPerYear);
            return Status == PregnancyStatus.Active && currentYear == y && currentMonth == m;
        }

        public Pregnancy WithStatus(PregnancyStatus newStatus) =>
            new Pregnancy(FatherId, ConceptionYear, ConceptionMonth, GestationMonths, newStatus);
    }
}
