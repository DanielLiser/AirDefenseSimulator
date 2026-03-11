using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    internal class Radar
    {
        private ConcurrentDictionary<string, Threat> activeThreats;
        private int threatCounter = 0;

        public event Action<Threat> ThreatDetected;

        public Radar(ConcurrentDictionary<string, Threat> activeList)
        {
            this.activeThreats = activeList;
        }

        public void StartScanning()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(Random.Shared.Next(1000, 3000));
                    threatCounter++;
                    String id = $"Threat{threatCounter:D3}";
                    int randomX = Random.Shared.Next(-5000, 5000); 
                    int startY = Random.Shared.Next(2000, 5500);     
                    int speed = Random.Shared.Next(500, 900);    

                    Threat newThreat = new Threat(id, randomX, startY, speed);
                    activeThreats.TryAdd(id, newThreat);
                    ThreatDetected?.Invoke(newThreat);

                }
            });
        }
    }
}