using CounterStrikeSharp.API.Core;
using System.Text;

namespace WeaponPaints;

public static class PlayerExtensions
{
    public static void Print(this CCSPlayerController controller, string message)
    {
        if (WeaponPaints._localizer == null)
        {
            controller.PrintToChat(message);
        }
        else
        {
            StringBuilder _message = new(WeaponPaints._localizer["wp_prefix"]);
            _message.Append(message);
            controller.PrintToChat(_message.ToString());
        }
    }
}