using System;
using System.Collections.Generic;

namespace CityQuestBot.Functions.Services
{
    public class GetAppSettingService : IGetAppSettingService
    {
        public string GetAppSettingOrThrow(string key) => TryGetAppSetting(key) ?? throw new KeyNotFoundException(key);

        public string? TryGetAppSetting(string key) =>
            Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
    }
}
