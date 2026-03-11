using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    public class Arrow: InterceptorBattery
    {
        public event Action<string, Threat> OnTargetMissed;
        public Arrow(string name, double x, double y, int missles, ConcurrentQueue<Threat> primaryQueue)
            : base(name, x, y, missles, primaryQueue)
        {
            
        }

        protected override double GetHitProbability()
        {
            return 0.6;
        }

        protected override double GetMissileSpeed()
        {
            return 1500;
        }

        protected override void HandleMiss(Threat threat)
        {
            OnTargetMissed?.Invoke(name, threat);
        }
    }
}
