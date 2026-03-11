using HaganaSimulator;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AirDefenseSimulator
{
    public class PhysicsEngine
    {
        private ConcurrentDictionary<string, Threat> activeThreats;

        public event Action<string> OnGroundImpact;
        public event Action<string, double> OnPhysicsUpdate;

        public PhysicsEngine(ConcurrentDictionary<string, Threat> threats)
        {
            activeThreats = threats;
        }

        public void StartSimulation()
        {
            Task.Run(() =>
            {
                while (true)
                {

                   
                    Thread.Sleep(100);

                    foreach (var kvp in activeThreats)
                    {
                        Threat threat = kvp.Value;

                        if (!threat.isAlive)
                        {
                            activeThreats.TryRemove(threat.id, out _);
                            continue;
                        }

                       
                        double fallDistance = threat.speed * 0.1;
                       // threat.location.y -= fallDistance;
                        threat.location.y = threat.location.y - fallDistance < 0 ? 0 : threat.location.y - fallDistance;

                        OnPhysicsUpdate?.Invoke(threat.id, threat.location.y);

                        // --- בדיקת פגיעה בקרקע ---
                        if (threat.location.y <= 0)
                        {
                            threat.location.y = 0;   // מקבעים ל-0 כדי שלא נראה גובה שלילי ב-GUI
                            threat.isAlive = false; // מסמנים שהטיל סיים את חייו

                            // מוציאים אותו מהרשימה הפעילה
                            activeThreats.TryRemove(threat.id, out _);

                            // צועקים החוצה (ל-Main) שהייתה פגיעה בקרקע
                            OnGroundImpact?.Invoke(threat.id);
                        }
                    }
                }
            });
        }
    }
}