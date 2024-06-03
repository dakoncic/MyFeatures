using Core.DomainModels;
using Core.Enum;

namespace Core.Helpers
{
    public static class IntervalCalculator
    {
        public static void CalculateAndAssignDaysBetween(Item item)
        {
            if (item.IntervalType.HasValue && item.IntervalValue.HasValue)
            {
                if (item.IntervalType.Value == IntervalType.Months)
                {
                    item.DaysBetween = CalculateDaysBetweenForMonths(item.IntervalValue.Value);
                }
                else
                {
                    item.DaysBetween = item.IntervalValue;
                }
            }
        }

        private static int CalculateDaysBetweenForMonths(int months)
        {
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddMonths(months);
            return (endDate - startDate).Days;
        }
    }
}
