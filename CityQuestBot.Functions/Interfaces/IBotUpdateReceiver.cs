using System.Threading.Tasks;

namespace CityQuestBot.Functions
{
    public interface IBotUpdateReceiver
    {
        Task<string> HandleUpdate();
    }
}