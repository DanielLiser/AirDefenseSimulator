using AirDefenseSimulator;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            var activeThreats = new ConcurrentDictionary<String, Threat>();
            var arrowQueue = new ConcurrentQueue<Threat>();
            var ironDomeQueue = new ConcurrentQueue<Threat>();
            PhysicsEngine physics = new PhysicsEngine(activeThreats);
            BattleManager bmc=new BattleManager(arrowQueue,ironDomeQueue);

            Radar radar = new Radar(activeThreats);

            Arrow arrow1 = new Arrow("arrow1", 0, 0, 5, arrowQueue);
            Arrow arrow2 = new Arrow("arrow2", 2, 0, 5, arrowQueue);

            IronDome iron1 = new IronDome("iron1", 3, 0, 6, ironDomeQueue);
            IronDome iron2 = new IronDome("iron2", 3, 0, 6, ironDomeQueue);

            TcpBroadcaster tcpServer = new TcpBroadcaster();
            tcpServer.StartServer(5000);

            physics.OnPhysicsUpdate += (id, height) =>
            {
                tcpServer.SendEvent(new { TYPE = "PHYSICS", THREAT_ID = id, HEIGHT = height });
            };

            physics.OnGroundImpact += (id) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[🔥 IMPACT] {id} hit the ground! We failed.");
                // במקום לשלוח מחרוזת, אנחנו שולחים אובייקט עם שדות מדויקים
                tcpServer.SendEvent(new { TYPE = "IMPACT", THREAT_ID = id });
                Console.ResetColor();
            };

            radar.ThreatDetected += (threat) =>
            {
                bmc.RouteNewThreat(threat);
                Console.WriteLine($"DETECTED: {threat.id} at height: {threat.location.y}");
                tcpServer.SendEvent(new { TYPE = "DETECTED", THREAT_ID = threat.id, HEIGHT = threat.location.y });
            };

            arrow1.OnMissileFired += (arrowName, threatName) =>
            {
                tcpServer.SendEvent(new { TYPE = "FIRE_MISSLE", BATTERY_NAME = arrowName, THREAT_ID = threatName });
            };
            arrow2.OnMissileFired += (arrowName, threatName) =>
            {
                tcpServer.SendEvent(new { TYPE = "FIRE_MISSLE", BATTERY_NAME = arrowName, THREAT_ID = threatName });
            };

            arrow1.OnTargetDestroyed += (arrowName, threatName) =>
            {
                tcpServer.SendEvent(new { TYPE = "DESTROYED", BATTERY_NAME = arrowName, THREAT_ID = threatName });
            };
            arrow2.OnTargetDestroyed += (arrowName, threatName) =>
            {
                tcpServer.SendEvent(new { TYPE = "DESTROYED", BATTERY_NAME = arrowName, THREAT_ID = threatName });
            };

            arrow1.OnTargetMissed += (name, thread) =>
            {
                bmc.RouteMissedThreat(name, thread);
                tcpServer.SendEvent(new { TYPE = "MISSED", BATTERY_NAME = name, THREAT_ID = thread.id });
            };
            arrow2.OnTargetMissed += (name, thread) =>
            {
                bmc.RouteMissedThreat(name, thread);
                tcpServer.SendEvent(new { TYPE = "MISSED", BATTERY_NAME = name, THREAT_ID = thread.id });
            };

            iron1.OnMissileFired += (arrowName, threatName) =>
            {
                tcpServer.SendEvent(new { TYPE = "FIRE_MISSLE", BATTERY_NAME = arrowName, THREAT_ID = threatName });
            };
            iron2.OnMissileFired += (arrowName, threatName) =>
            {
                tcpServer.SendEvent(new { TYPE = "FIRE_MISSLE", BATTERY_NAME = arrowName, THREAT_ID = threatName });
            };

            iron1.OnTargetDestroyed += (arrowName, threatName) =>
            {
                tcpServer.SendEvent(new { TYPE = "DESTROYED", BATTERY_NAME = arrowName, THREAT_ID = threatName });
            };
            iron2.OnTargetDestroyed += (arrowName, threatName) =>
            {
                tcpServer.SendEvent(new { TYPE = "DESTROYED", BATTERY_NAME = arrowName, THREAT_ID = threatName });
            };

            iron1.OnCriticalMiss += (batteryName, threat) =>
            {
                tcpServer.SendEvent(new { TYPE = "IMPACT", THREAT_ID = threat.id });
            };
            iron2.OnCriticalMiss += (batteryName, threat) =>
            {
                tcpServer.SendEvent(new { TYPE = "IMPACT", THREAT_ID = threat.id });
            };

            bmc.OnCriticalMiss += (batteryName, threat) =>
            {
                tcpServer.SendEvent(new { TYPE = "IMPACT", THREAT_ID = threat.id });
            };

            physics.StartSimulation();
            radar.StartScanning();
            arrow1.StartDefending();
            arrow2.StartDefending();
            iron1.StartDefending();
            iron2.StartDefending();

            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(15000);
                    try
                    {
                        Console.WriteLine("\n[🚚 LOGISTICS] Refilling batteries: Arrow (+2), Iron Dome (+5)");
                        arrow1.ReloadBattery(2);
                        arrow2.ReloadBattery(2);
                        Console.WriteLine("Arrow reload complete.");

                        iron1.ReloadBattery(5);
                        iron2.ReloadBattery(5);
                        Console.WriteLine("Iron Dome reload complete.");
                    }
                    catch (Exception ex)
                    {
                        // אם משהו קורס, נראה את זה פה באדום בוהק!
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"\n[🔥 CRITICAL ERROR IN LOGISTICS] {ex.Message}");
                        Console.WriteLine(ex.StackTrace);
                        Console.ResetColor();
                    }
                }
            });
            Console.ReadLine();
        }

    }
}
