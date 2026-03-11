using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
namespace HaganaSimulator
{
    public abstract class InterceptorBattery
    {
        protected String name;
        protected Location location;
        protected SemaphoreSlim missileInventory;
        protected ConcurrentQueue<Threat> myQueue;
        protected int missles;

        public event Action<string, string> OnMissileFired;
        public event Action<string, string> OnTargetDestroyed;

        public InterceptorBattery(string name, double x, double y, int missles, ConcurrentQueue<Threat> queue)
        {
            this.name = name;
            this.location = new Location(x, y);
            this.missileInventory = new SemaphoreSlim(missles, missles);
            myQueue = queue;
            this.missles = missles;
        }
        public void StartDefending()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (myQueue.TryPeek(out Threat threat))
                    {
                        if (!threat.isAlive)
                        {
                            myQueue.TryDequeue(out _);
                            continue;
                        }
                        lock (threat.ThreatLock)
                        {
                            if (threat.isTargeted) continue;
                            myQueue.TryDequeue(out _);


                            threat.isTargeted = true;
                        }
                        missileInventory.Wait();
                        FireMissle(threat);

                    }
                    else 
                    {
                        Thread.Sleep(50); 
                    }

                }
            });
        }

        private void FireMissle(Threat threat)
        {
            OnMissileFired?.Invoke(name, threat.id);
            double distance = Location.distance(this.location, threat.location);
            double timeToTarget = distance / GetMissileSpeed();
            int sleepTimeMs = Math.Max(100, (int)(timeToTarget * 1000));

            Thread.Sleep(sleepTimeMs);
            if (!threat.isAlive) return;
            if (Random.Shared.NextDouble() <= GetHitProbability())
            {
                threat.isAlive = false;
                OnTargetDestroyed?.Invoke(name, threat.id);
            }
            else
            {
                HandleMiss(threat);
            }

        }
        public void ReloadBattery(int amount)
        {
            int currentAmmo = missileInventory.CurrentCount;
            int space = this.missles - currentAmmo;
            if (amount + currentAmmo > this.missles)
            {
                if (space > 0)
                {
                    missileInventory.Release(space);
                }
                return;
            }
            if (amount > 0)
            {
                missileInventory.Release(amount);
            }

        }
        protected abstract void HandleMiss(Threat threat);
        protected abstract double GetHitProbability();
        protected abstract double GetMissileSpeed();
    }



}