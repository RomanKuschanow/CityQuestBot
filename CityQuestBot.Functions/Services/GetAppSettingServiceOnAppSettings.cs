using System.Collections.Generic;

namespace CityQuestBot.Functions.Services
{
    public class GetAppSettingServiceOnAppSettings : IGetAppSettingService
    {
        public GetAppSettingServiceOnAppSettings(Options appSettings)
        {
            AppSettings = appSettings;
        }

        public Options AppSettings { get; }

        public string GetAppSettingOrThrow(string key) => TryGetAppSetting(key) ?? throw new KeyNotFoundException(key);

        public string? TryGetAppSetting(string key)
        {
            return key switch
            {
                nameof(AppSettings.TelegramBotApiKey) => AppSettings.TelegramBotApiKey,
                nameof(AppSettings.AzureWebJobsStorage) => AppSettings.AzureWebJobsStorage,
                _ => null
            };
        }
    }
}
