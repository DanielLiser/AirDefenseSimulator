using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    internal class IronDome : InterceptorBattery
    {
        public event Action<string, Threat> OnCriticalMiss;

        public IronDome(string name, double x, double y, int missles, ConcurrentQueue<Threat> queue):
            base(name, x, y, missles, queue)
        {

        }
        protected override double GetHitProbability()
        {
            return 0.5;
        }

        protected override double GetMissileSpeed()
        {
            return 1500;
        }

        protected override void HandleMiss(Threat threat)
        {
            OnCriticalMiss?.Invoke(name, threat);
        }
    }
}
