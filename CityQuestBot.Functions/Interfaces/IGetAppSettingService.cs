namespace CityQuestBot.Functions
{
    public interface IGetAppSettingService
    {
        string? TryGetAppSetting(string key);
        string GetAppSettingOrThrow(string key);
    }
}
