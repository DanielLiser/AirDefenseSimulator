import socket
import json
import threading
import time
import os
import tkinter as tk
from tkinter import ttk

state_lock = threading.Lock()
activeThreats={}

batteriesStatus={
    "arrow1":"idle",
    "arrow2":"idle",
    "iron1":"idle",
    "iron2":"idle"
}

battle_stats = {
    "detected":0,
    "intercepted": 0,
    "impacts": 0
}


def delayed_delete(threat_id, delay_seconds):
    """פונקציה שרצה ברקע, מחכה כמה שניות, ואז מוחקת את הטיל מהזיכרון"""
    time.sleep(delay_seconds)
    with state_lock:
        if threat_id in activeThreats:
            del activeThreats[threat_id]

def tcp_listener(host='127.0.0.1', port=5000):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        try:
            print(f"Waiting for C# server on {host}:{port}...")
            s.connect((host, port))
            print("Connected! Listening for data...")
            stream_reader = s.makefile('r', encoding='utf-8')

            for line in stream_reader:
                if not line:
                    print("Connection closed by server.")
                    break

                # הדפסה קריטית: נראה מה C# באמת שולח!
                print(f"RAW DATA: {line.strip()}")

                try:
                    event = json.loads(line.strip())
                    update_state(event)
                except json.JSONDecodeError:
                    print(f"JSON Error on line: {line}")
        except Exception as e:
            print(f"Network thread fatal error: {e}")


def update_state(event):
    with state_lock:
        e_type = event.get("TYPE")
        t_id = event.get("THREAT_ID")

        if not t_id:
            return

        if t_id not in activeThreats:
            activeThreats[t_id] = {"height": 0, "status": "ACTIVE"}
            battle_stats["detected"] += 1

        if e_type in ["PHYSICS", "DETECTED"]:
            activeThreats[t_id]["height"] = event.get("HEIGHT", 0)

            # אנחנו לא נוגעים בסטטוס, כדי לא לדרוס אותו אם הוא כבר LOCKED

        elif e_type == "FIRE_MISSLE":
            activeThreats[t_id]["status"] = "LOCKED"
            bat_name = event.get("BATTERY_NAME")
            if bat_name in batteriesStatus:
                batteriesStatus[bat_name] = f"🔴 Locked on {t_id}"

        elif e_type == "DESTROYED":
            if activeThreats[t_id]["status"] != "DESTROYED":
                battle_stats["intercepted"] += 1
            activeThreats[t_id]["status"] = "DESTROYED"
            # מוחקים בעוד 2 שניות (כדי שנראה את הירוק)
            threading.Thread(target=delayed_delete, args=(t_id, 2.0), daemon=True).start()

            bat_name = event.get("BATTERY_NAME")
            if bat_name in batteriesStatus:
                batteriesStatus[bat_name] = "🟢 IDLE"



        elif e_type == "IMPACT":
            if activeThreats[t_id]["status"] != "IMPACT":
                battle_stats["impacts"] += 1
            activeThreats[t_id]["status"] = "IMPACT"
            # מוחקים בעוד 3 שניות (כדי שנראה את האדום)
            threading.Thread(target=delayed_delete, args=(t_id, 3.0), daemon=True).start()

        elif e_type == "MISSED":
            if activeThreats[t_id]["status"] == "LOCKED":
                activeThreats[t_id]["status"] = "ACTIVE"

            bat_name = event.get("BATTERY_NAME")
            if bat_name in batteriesStatus:
                batteriesStatus[bat_name] = "🟢 IDLE"


class DashboardApp:
    def __init__(self, root):
        self.root = root
        self.root.title("🛡️ Hagana Air Defense Dashboard")
        self.root.geometry("900x750")  # הגדלנו קצת את החלון
        self.root.configure(bg="#1e1e1e")

        style = ttk.Style()
        style.theme_use("clam")
        style.configure("TFrame", background="#1e1e1e")
        style.configure("TLabelFrame", background="#1e1e1e", foreground="#00ff00", font=("Consolas", 12, "bold"))
        style.configure("TLabel", background="#1e1e1e", foreground="white", font=("Consolas", 12))

        style.configure("Treeview", background="#2d2d2d", foreground="white", fieldbackground="#2d2d2d",
                        font=("Consolas", 11))
        style.configure("Treeview.Heading", font=("Consolas", 12, "bold"), background="#444444", foreground="white")

        # --- אזור המשגרים ---
        self.launchers_frame = ttk.LabelFrame(self.root, text=" [ LAUNCHERS STATUS ] ", padding=15)
        self.launchers_frame.pack(fill="x", padx=20, pady=10)

        self.battery_labels = {}
        for bat in batteriesStatus.keys():
            lbl = ttk.Label(self.launchers_frame, text=f"{bat:<15} | 🟢 IDLE")
            lbl.pack(anchor="w", pady=2)
            self.battery_labels[bat] = lbl

        # --- אזור האיומים באוויר ---
        self.threats_frame = ttk.LabelFrame(self.root, text=" [ ACTIVE THREATS IN AIR ] ", padding=15)
        self.threats_frame.pack(fill="both", expand=True, padx=20, pady=5)

        columns = ("ID", "Altitude")
        self.tree = ttk.Treeview(self.threats_frame, columns=columns, show="headings", height=10)
        self.tree.heading("ID", text="🚀 Threat ID")
        self.tree.heading("Altitude", text="Altitude (m)")
        self.tree.column("ID", width=200, anchor="center")
        self.tree.column("Altitude", width=200, anchor="center")

        self.tree.tag_configure("ACTIVE", foreground="white")
        self.tree.tag_configure("LOCKED", foreground="yellow")
        self.tree.tag_configure("DESTROYED", foreground="#00ff00")
        self.tree.tag_configure("IMPACT", foreground="#ff4444")

        self.tree.pack(fill="both", expand=True)

        # ==========================================
        # --- אזור הסטטיסטיקות (החדש!) ---
        # ==========================================
        self.stats_frame = ttk.LabelFrame(self.root, text=" [ BATTLE STATISTICS ] ", padding=15)
        self.stats_frame.pack(fill="x", side="bottom", padx=20, pady=15)

        self.lbl_detected = ttk.Label(self.stats_frame, text="📡 Detected: 0", font=("Consolas", 13, "bold"),
                                      foreground="#00ccff")
        self.lbl_detected.pack(side="left", padx=10, expand=True)

        self.lbl_intercepted = ttk.Label(self.stats_frame, text="✅ Intercepted: 0", font=("Consolas", 13, "bold"),
                                         foreground="#00ff00")
        self.lbl_intercepted.pack(side="left", padx=10, expand=True)

        self.lbl_impacts = ttk.Label(self.stats_frame, text="💥 Impacts: 0", font=("Consolas", 13, "bold"),
                                     foreground="#ff4444")
        self.lbl_impacts.pack(side="left", padx=10, expand=True)



        self.lbl_rate = ttk.Label(self.stats_frame, text="📊 Success Rate: 0%", font=("Consolas", 13, "bold"),
                                  foreground="white")
        self.lbl_rate.pack(side="left", padx=10, expand=True)

        self.update_gui()

    def update_gui(self):
        with state_lock:
            # 1. עדכון משגרים
            for bat, status in batteriesStatus.items():
                color = "#ff4444" if "Locked" in status else "#00cc00"
                self.battery_labels[bat].config(text=f"{bat:<15} | {status}", foreground=color)

            # 2. עדכון טבלת טילים
            for item in self.tree.get_children():
                self.tree.delete(item)

            for t_id, data in sorted(activeThreats.items(), key=lambda x: x[1]["height"], reverse=True):
                self.tree.insert("", "end", values=(t_id, f"{data['height']:,.0f}"), tags=(data["status"],))

                # 3. עדכון סטטיסטיקות
                detected = battle_stats["detected"]
                hits = battle_stats["intercepted"]
                misses = battle_stats["impacts"]
                total = hits + misses

                # חישוב אחוזים (עם הגנה מחלוקה באפס)
                rate = (hits / total * 100) if total > 0 else 0

                # 🔥 הנה השורה שהייתה חסרה לך! 🔥
                self.lbl_detected.config(text=f"📡 Detected: {detected}")

                self.lbl_intercepted.config(text=f"✅ Intercepted: {hits}")
                self.lbl_impacts.config(text=f"💥 Impacts: {misses}")

                # צבע דינמי לאחוזים: ירוק (טוב), צהוב (בינוני), אדום (גרוע)
                if total == 0:
                    rate_color = "white"
                elif rate >= 70:
                    rate_color = "#00ff00"
                elif rate >= 40:
                    rate_color = "yellow"
                else:
                    rate_color = "#ff4444"

                self.lbl_rate.config(text=f"📊 Success Rate: {rate:.1f}%", foreground=rate_color)

            self.root.after(100, self.update_gui)


if __name__ == "__main__":
    # מתחילים את ההאזנה ברשת בתהליכון רקע (daemon=True מבטיח שהוא ימות כשהחלון ייסגר)
    listener_thread = threading.Thread(target=tcp_listener, daemon=True)
    listener_thread.start()

    # הפעלת חלון ה-GUI (זה תוקע את ה-Main Thread פה עד שהמשתמש סוגר את החלון)
    root = tk.Tk()
    app = DashboardApp(root)
    root.mainloop()