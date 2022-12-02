using DXOverlay.Classes;
using DXOverlay.Classes.SDK;
using DXOverlay.Classes.SDK.Entities;
using DXOverlay.Classes.SDK.Managment;
using DXOverlay.Classes.SDK.Variables;
using ExternalUserCMD;
using Overlay.NET;
using Process.NET;
using Process.NET.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Memory.Memorys;

namespace DXOverlay
{
    class Overlay
    {
        private OverlayPlugin overlayPlugin;
        private ProcessSharp _processSharp;
        public bool Shutdown = false;

        public void Init(string processName)
        {
            var process = System.Diagnostics.Process.GetProcessesByName(processName).FirstOrDefault();

            overlayPlugin = new OverlayRenderer();
            _processSharp = new ProcessSharp(process, MemoryType.Remote);

            var FPS = 1000;
            var fpsValid = int.TryParse(Convert.ToString(FPS, CultureInfo.InvariantCulture), NumberStyles.Any,
                NumberFormatInfo.InvariantInfo, out int fps);

            var d3DOverlay = (OverlayRenderer)overlayPlugin;
            d3DOverlay.Settings.Current.UpdateRate = 1000 / fps;
            overlayPlugin.Initialize(_processSharp.WindowFactory.MainWindow);
            overlayPlugin.Enable();

            // Initalize Section
            Game.Initalize();
            while (!Shutdown)
            {
                Render(d3DOverlay);
                overlayPlugin.Update();
                Thread.Sleep(1);
            }

            UserCMD.Execute("crosshair 1");
            UserCMD.Execute("hideconsole");
            UserCMD.Execute("clear");
            Environment.Exit(0);
        }

        Vector3 CurrentViewAngles;
        Vector3 vPunch;
        Vector3 NewViewAngles;
        Vector3 OldAimPunch;
        bool Menu = false;
        bool ESP = false, Snaplines = false, HeadESP = false, Name = false, RCS = false, RecoilCrosshair = false;
        bool RecoilCrosshairUpdate = false;
        public void Render(OverlayRenderer render)
        {
            if(render.TargetWindow == null)
            {
                Shutdown = true;
                return;
            }
            Size WindowSize = new Size(render.OverlayWindow.Width, render.OverlayWindow.Height);
            Point MousePos = new Point(Cursor.Position.X - render.OverlayWindow.X, Cursor.Position.Y - render.OverlayWindow.Y);

            //render.Line(WindowSize.Width / 2 - 10 + 1, WindowSize.Height / 2 + 1, WindowSize.Width / 2 + 10 + 1, WindowSize.Height / 2 + 1, Color.Green, 1, true);
            //render.Line(WindowSize.Width / 2 + 1, WindowSize.Height / 2 - 10 + 1, WindowSize.Width / 2 + 1, WindowSize.Height / 2 + 10 + 1, Color.Green, 1, true);
            if (EngineClient.IsInGame)
            {
                Entity LocalPlayer = new Entity(Read<int>(Game.Client + Game.Offsets.signatures.dwLocalPlayer));
                if (LocalPlayer.Address == 0) return;
                var items = Read<int>(Read<int>(Read<int>(EngineClient.ClientState + Game.Offsets.signatures.dwClientState_PlayerInfo) + 0x40) + 0x0C);

                if(RCS)
                {
                    vPunch = new Vector3(LocalPlayer.PunchAngle.X, LocalPlayer.PunchAngle.Y, 0);
                    if (LocalPlayer.ShotsFired > 1)
                    {
                        CurrentViewAngles = EngineClient.ViewAngles;
                        NewViewAngles.X = ((CurrentViewAngles.X + OldAimPunch.X) - (vPunch.X * 2f));
                        NewViewAngles.Y = ((CurrentViewAngles.Y + OldAimPunch.Y) - (vPunch.Y * 2f));
                        NewViewAngles.Z = 0;

                        NewViewAngles.Z = 0;

                        OldAimPunch.X = vPunch.X * 2f;
                        OldAimPunch.Y = vPunch.Y * 2f;
                        OldAimPunch.Z = 0;

                    }
                    else { OldAimPunch.X = OldAimPunch.Y = OldAimPunch.Z = 0; }
                }
                else { OldAimPunch.X = OldAimPunch.Y = OldAimPunch.Z = 0; }

                for (int i = 1; i < EngineClient.MaxPlayer; i++)
                {
                    Entity entity = new Entity(Read<int>(Game.Client + Game.Offsets.signatures.dwEntityList + (i * 0x10)));
                    if (entity.Address == 0 && entity.Address == LocalPlayer.Address && entity.Address == LocalPlayer.ObserverTarget) continue;
                    if (entity.Dormant) continue;
                    if (entity.Health == 0) continue;
                    if (entity.Team == Classes.SDK.Variables.Enums.Team.None || entity.Team == Classes.SDK.Variables.Enums.Team.Spectator) continue;

                    Classes.SDK.Variables.Structs.player_info_s Entity_Info = Read<Classes.SDK.Variables.Structs.player_info_s>(Read<int>(items + 0x28 + ((i) * 0x34)));
                    string Entity_Name = new string(Entity_Info.m_szPlayerName);
                    
                    if (WorldToScreen(entity.Origin, out Vector2 Entity_Origin2D, EngineClient.ViewMatrix, WindowSize.Width, WindowSize.Height) && WorldToScreen(entity.Bone(8), out Vector2 Entity_Head2D, EngineClient.ViewMatrix, WindowSize.Width, WindowSize.Height))
                    {
                        float BoxHeight = Entity_Origin2D.Y - Entity_Head2D.Y;
                        float BoxWidth = BoxHeight / 2.4f;

                        float x1 = Entity_Head2D.X - (BoxWidth / 2f);
                        float y1 = Entity_Head2D.Y;

                        if(ESP)
                        {
                            render.Rectangle((int)x1, (int)y1, (int)BoxWidth, (int)BoxHeight, Color.FromArgb(128, Color.Black), true, false);
                            render.Rectangle((int)x1, (int)y1, (int)BoxWidth, (int)BoxHeight, LocalPlayer.Team == entity.Team ? Color.Blue : entity.Spotted ? Color.Green : Color.Red, false, true);
                        }
                        if(Snaplines)
                            render.Line(WindowSize.Width / 2, WindowSize.Height, (int)x1 + (int)(BoxWidth / 2), (int)Entity_Origin2D.Y, LocalPlayer.Team == entity.Team ? Color.Blue : LocalPlayer.Team == entity.Team ? Color.Blue : entity.Spotted ? Color.Green : Color.Red, 1f, true);
                        if(HeadESP)
                        {
                            render.Circle((int)Entity_Head2D.X, (int)Entity_Head2D.Y, (int)BoxWidth / 11 * 3, Color.FromArgb(128, Color.Black), true, false);
                            render.Circle((int)Entity_Head2D.X, (int)Entity_Head2D.Y, (int)BoxWidth / 11 * 3, LocalPlayer.Team == entity.Team ? Color.Blue : LocalPlayer.Team == entity.Team ? Color.Blue : entity.Spotted ? Color.Green : Color.Red, false, true);
                        }
                        if(Name)
                            render.BackgroundText(Entity_Name.Substring(0, 8), (int)(x1 + BoxWidth / 2), (int)Entity_Origin2D.Y, Color.White, Color.FromArgb(128, Color.Black), true);
                    }
                }
            }

            if(Win32.IsKeyPushedDown(Keys.Insert))
            {
                Menu = !Menu;
                if(Menu)
                {
                    UserCMD.Execute("clear");
                    UserCMD.Execute("echo This cheat coded by Lufzys1337");
                    UserCMD.Execute("echo And Edited by CrackerRatra");
                    UserCMD.Execute("showconsole");
                }
                else
                {
                    UserCMD.Execute("hideconsole");
                    UserCMD.Execute("clear");
                }
                Thread.Sleep(120);
            }

            if(Menu)
            {
                render.Text("DX Overlay - Example Cheat", 8, WindowSize.Height / 2 - 30, Color.White);
                Checkbox(render, 10, WindowSize.Height / 2, "ESP Box", ref ESP);
                Checkbox(render, 10, WindowSize.Height / 2 + 20, "Snaplines", ref Snaplines);
                Checkbox(render, 10, WindowSize.Height / 2 + 40, "Head", ref HeadESP);
                Checkbox(render, 10, WindowSize.Height / 2 + 60, "Name", ref Name);
            }
        }

        public void Checkbox(OverlayRenderer render, int x1, int y1,string text, ref bool check)
        {
            render.Rectangle(x1, y1, 10, 10, MouseInHover(render, x1, y1, 10, 10) ? Color.White : Color.Gray, true, true);
            render.BackgroundText(text, x1 + 15, y1 - 2, Color.White, Color.FromArgb(128, Color.Black));

            if (MouseInHover(render, x1, y1, 10, 10) && Win32.IsKeyPushedDown(Keys.LButton))
            {
                check = !check;
                Thread.Sleep(120);
            }

            if(check)
                render.Rectangle(x1 + 2, y1 + 2, 6, 6, Color.Black, true);
        }

        bool MouseInHover(OverlayRenderer render, int x, int y, int width, int height)
        {
            if (!Win32.GetCursorPos(out Win32.POINT cursor))
                return false;

            Point WindowPos = new Point(render.OverlayWindow.X, render.OverlayWindow.Y);
            if(cursor.X > WindowPos.X + x && cursor.X < WindowPos.X + x + width)
            {
                if (cursor.Y > WindowPos.Y + y && cursor.Y < WindowPos.Y + y + height)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
