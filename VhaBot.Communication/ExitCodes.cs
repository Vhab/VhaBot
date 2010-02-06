using System;
using System.Collections.Generic;
using System.Text;

namespace VhaBot.Communication
{
    public static class ExitCodes
    {
        public const int SHUTDOWN = 0;
        public const int RESTART = 1;
        public const int NO_CONFIGURATION = 2;
        public const int COMMUNICATION_LOST = 4;
        public const int REMOTING_FAILED = 8;
        public const int INVALID_ARGUMENT = 16;

        public const int UNKNOWN_ERROR = 1024;
    }
}
