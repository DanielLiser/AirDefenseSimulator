using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
namespace HaganaSimulator
{
    public class Threat
    {
        public String id { get; set; }
        public Location location { get; set; }
        public int speed { get; set; }
        public bool isTargeted { get; set; } = false;
        public bool isAlive { get; set; } = true;

        public readonly object ThreatLock = new object();
        public Threat(String id, int startX, int startY, int speed)
        {
            this.id = id;
            location = new Location(startX, startY);
            this.speed = speed;
        }
    }
}
