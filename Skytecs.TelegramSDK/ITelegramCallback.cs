using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Skytecs.TelegramSDK
{
    public interface ITelegramCallback
    {
        Task OnUpdate(Update payload);
    }
}
