using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT
{
    public sealed class Waiter
    {
        int timeout;
        int sleep;
        int waited = 0;
        Waiter(int timeout, int sleep)
        {
            this.timeout = timeout;
            this.sleep = sleep;
        }

        public static Waiter Create(int timeout, int sleep)
        {
            return new Waiter(timeout, sleep);
        }

        bool WaitUntil()
        {
            if (timeout > 0 && waited >= timeout)
                return false;
            try
            {
                Thread.Sleep(sleep * 1000);
            }
            catch (InterruptedException e)
            {
            }

            waited += sleep;
            return true;
        }
    }
}