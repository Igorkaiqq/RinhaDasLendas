namespace RinhaDasLendas.Domain.Constants;

public static class ValidationConstants
{
    public const int RequiredRoutePreferencesCount = 5;
    public const int MinRoutePriority = 1;
    public const int MaxRoutePriority = 5;
    public const int MaxBlockedRoutePreferences = 1;
    public const int MessageCodeNumericSequenceLength = 3;
    public const string MessageCodePattern = "^[A-Z]{2,4}[0-9]{3}$";
}
