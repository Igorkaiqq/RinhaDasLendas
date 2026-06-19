namespace RinhaDasLendas.Domain.Constants;

public static class MessageCodes
{
    public const string LoadingInformation = "MI001";
    public const string WaitingServerResponse = "MI002";
    public const string SyncingData = "MI003";
    public const string NoRecordsFound = "MI004";
    public const string FiltersApplied = "MI005";
    public const string PreferencesLoaded = "MI006";
    public const string DraftWaitingPlayers = "MI007";
    public const string QueueWaitingConfirmation = "MI008";
    public const string StatisticsUpdating = "MI009";

    public const string OperationSuccess = "MSIS001";
    public const string PlayerCreated = "MSIS002";
    public const string PlayerUpdated = "MSIS003";
    public const string PlayerDeactivated = "MSIS004";
    public const string RoutePreferencesUpdated = "MSIS005";
    public const string QueueCreated = "MSIS006";
    public const string AttendanceConfirmed = "MSIS007";
    public const string DraftSaved = "MSIS008";
    public const string ResultRegistered = "MSIS009";
    public const string TeamCreated = "MSIS010";
    public const string TeamUpdated = "MSIS011";
    public const string TeamDeactivated = "MSIS012";
    public const string TeamReactivated = "MSIS013";

    public const string UnexpectedError = "ME001";
    public const string ServerConnectionFailed = "ME002";
    public const string PlayerNotFound = "ME003";
    public const string PlayerSaveFailed = "ME004";
    public const string PlayerUpdateFailed = "ME005";
    public const string PlayerDeactivateFailed = "ME006";
    public const string RoutePreferencesNotFound = "ME007";
    public const string QueueNotFound = "ME008";
    public const string DraftNotFound = "ME009";
    public const string MatchNotFound = "ME010";
    public const string SeriesNotFound = "ME011";
    public const string StatisticsLoadFailed = "ME012";
    public const string ResultRegisterFailed = "ME013";
    public const string ExternalIntegrationUnavailable = "ME014";
    public const string UnauthorizedAccess = "ME015";
    public const string InsufficientPermission = "ME016";
    public const string ResourceTemporarilyUnavailable = "ME017";
    public const string InconsistentDataFound = "ME018";
    public const string RequestProcessingFailed = "ME019";
    public const string RequestTimedOut = "ME020";
    public const string TeamNotFound = "ME021";
    public const string TeamSaveFailed = "ME022";

    public const string FieldRequired = "MV001";
    public const string InvalidEmailFormat = "MV002";
    public const string PlayerNameRequired = "MV003";
    public const string LeagueNicknameRequired = "MV004";
    public const string PlayerRegionRequired = "MV005";
    public const string InvalidOpGgLink = "MV006";
    public const string InvalidDeeplolLink = "MV007";
    public const string RoutePrioritiesMustBeUnique = "MV008";
    public const string RoutePrioritiesRange = "MV009";
    public const string OnlyOneNeverPlayRole = "MV010";
    public const string PlayerAlreadyExists = "MV011";
    public const string InactivePlayerCannotJoinQueue = "MV012";
    public const string QueuePlayerLimitReached = "MV013";
    public const string PlayerAlreadyInQueue = "MV014";
    public const string TeamPlayerLimitReached = "MV015";
    public const string DuplicatePlayerInMatch = "MV016";
    public const string ChampionRequired = "MV017";
    public const string MatchResultRequired = "MV018";
    public const string SeriesMustBeBestOfThreeOrFive = "MV019";
    public const string TeamNameRequired = "MV020";
    public const string TeamTagRequired = "MV021";
    public const string TeamAlreadyExists = "MV022";
    public const string PlayerAlreadyInTeam = "MV023";
    public const string TeamCaptainMustBeMember = "MV024";
    public const string InactivePlayerCannotJoinTeam = "MV025";

    public const string ConfirmAction = "MC001";
    public const string ConfirmPlayerDeactivate = "MC002";
    public const string ConfirmRemovePlayerFromQueue = "MC003";
    public const string ConfirmCloseQueue = "MC004";
    public const string ConfirmSaveDraft = "MC005";
    public const string ConfirmRegisterResult = "MC006";
    public const string ConfirmOverwriteStatistics = "MC007";
    public const string ConfirmChangeCaptains = "MC008";
    public const string ConfirmDiscardChanges = "MC009";

    public const string UnsavedChanges = "MA001";
    public const string ExternalIntegrationSkipped = "MA002";
    public const string ManualDataNeedsReview = "MA003";
    public const string IncompleteRoutePreferences = "MA004";
    public const string QueueNearPlayerLimit = "MA005";
    public const string IncompleteDraft = "MA006";
    public const string StatisticsMayBeOutdated = "MA007";
    public const string AdminOnlyAction = "MA008";
    public const string ReviewDataBeforeContinue = "MA009";
}
