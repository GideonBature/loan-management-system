
namespace FirstLend.Domain.Enums
{
    public enum PerformanceStatus
    {
        Performing,
        NonPerforming,
        Delinquent30,
        Delinquent60,
        Delinquent90,
        Default,
        Restructured,
        Rehabilitated,
        Closed,
        WrittenOff,
        UnderWatch,
        Forbearance,
        InCollections,
        Recovered,
        Dormant,
        Unknown
    }
}