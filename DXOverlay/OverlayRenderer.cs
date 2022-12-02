using Overlay.NET.Common;
using Overlay.NET.Directx;
using Process.NET.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DXOverlay
{
    [RegisterPlugin("Renderer", "LF1337", "Overlay Renderer", "1.0", "Easy to use overlay")]
    public class OverlayRenderer : DirectXOverlayPlugin
    {
        private readonly TickEngine _tickEngine = new TickEngine();
        public readonly ISettings<OverlaySettings> Settings = new SerializableSettings<OverlaySettings>();
        private int RenderedFrameInSeconds;
        public int FPS;
        private Stopwatch _watch;

        public List<OverlayVariables.DrawData> DrawList = new List<OverlayVariables.DrawData>();

        public override void Initialize(IWindow targetWindow)
        {
            base.Initialize(targetWindow);

            var current = Settings.Current;

            if (current.UpdateRate == 0)
                current.UpdateRate = 1000 / 60;

            OverlayWindow = new DirectXOverlayWindow(targetWindow.Handle, false);
            _watch = Stopwatch.StartNew();

            RenderedFrameInSeconds = 0;
            FPS = 0;
            // Set up update interval and register events for the tick engine.

            _tickEngine.PreTick += OnPreTick;
            _tickEngine.Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!OverlayWindow.IsVisible)
            {
                return;
            }

            OverlayWindow.Update();
            Render();
        }

        private void OnPreTick(object sender, EventArgs e)
        {
            var targetWindowIsActivated = TargetWindow.IsActivated;
            if (!targetWindowIsActivated && OverlayWindow.IsVisible)
            {
                _watch.Stop();
                ClearScreen();
                OverlayWindow.Hide();
            }
            else if (targetWindowIsActivated && !OverlayWindow.IsVisible)
            {
                OverlayWindow.Show();
            }
        }

        public override void Enable()
        {
            _tickEngine.Interval = Settings.Current.UpdateRate.Milliseconds();
            _tickEngine.IsTicking = true;
            base.Enable();
        }

        public override void Disable()
        {
            _tickEngine.IsTicking = false;
            base.Disable();
        }

        public override void Update() => _tickEngine.Pulse();

        public override void Dispose()
        {
            OverlayWindow.Dispose();
            base.Dispose();
        }

        private void ClearScreen()
        {
            OverlayWindow.Graphics.BeginScene();
            OverlayWindow.Graphics.ClearScene();
            OverlayWindow.Graphics.EndScene();
        }

        protected void Render()
        {
            if (!_watch.IsRunning)
            {
                _watch.Start();
            }

            OverlayWindow.Graphics.BeginScene();
            OverlayWindow.Graphics.ClearScene();
            #region FPS Counter
            if (_watch.ElapsedMilliseconds > 1000)
            {
                FPS = RenderedFrameInSeconds;
                RenderedFrameInSeconds = 0;
                _watch.Restart();
            }
            else
            {
                RenderedFrameInSeconds++;
            }
            #endregion

            BackgroundText("FPS : " + FPS, 10, 10, Color.White, Color.FromArgb(128, Color.Black));
            var gfx = OverlayWindow.Graphics;
            try
            {
                foreach(var draw in DrawList)
                {
                    switch (draw.Type)
                    {
                        case OverlayVariables.DrawType.Line:
                            if (draw.Outline)
                                gfx.DrawLine(draw.X1, draw.Y1, draw.X2, draw.Y2, draw.Stroke + 2, gfx.CreateBrush(Color.FromArgb(draw.Color.A, Color.Black)));

                            gfx.DrawLine(draw.X1, draw.Y1, draw.X2, draw.Y2, draw.Stroke, gfx.CreateBrush(draw.Color));
                            break;

                        case OverlayVariables.DrawType.Rectangle:
                            if (draw.Outline)
                                gfx.DrawRectangle(draw.X1, draw.Y1, draw.Width, draw.Height, draw.Fill ? draw.Stroke + 3 : draw.Stroke + 2, gfx.CreateBrush(Color.FromArgb(draw.Color.A, Color.Black)));

                            if (draw.Fill)
                                gfx.FillRectangle(draw.X1, draw.Y1, draw.Width, draw.Height, gfx.CreateBrush(draw.Color));
                            else
                                gfx.DrawRectangle(draw.X1, draw.Y1, draw.Width, draw.Height, draw.Stroke, gfx.CreateBrush(draw.Color));
                            break;

                        case OverlayVariables.DrawType.Circle:
                            if (draw.Outline)
                                gfx.DrawCircle(draw.X1, draw.Y1, draw.Radius, draw.Fill ? draw.Stroke + 3 : draw.Stroke + 2, gfx.CreateBrush(Color.FromArgb(draw.Color.A, Color.Black)));

                            if (draw.Fill)
                                gfx.FillCircle(draw.X1, draw.Y1, draw.Radius, gfx.CreateBrush(draw.Color));
                            else
                                gfx.DrawCircle(draw.X1, draw.Y1, draw.Radius, draw.Stroke, gfx.CreateBrush(draw.Color));
                            break;

                        case OverlayVariables.DrawType.Text:
                            gfx.DrawText(draw.Text, gfx.CreateFont(draw.FontFamily, draw.FontSize), gfx.CreateBrush(draw.Color), draw.X1, draw.Y1);
                            break;
                    }
                }
            } catch { }
            finally { DrawList.Clear(); }

            OverlayWindow.Graphics.EndScene();
        }

        public void Line(int x1, int y1, int x2, int y2, Color color, float stroke = 1f, bool outline = false)
        {
            OverlayVariables.DrawData drawData = new OverlayVariables.DrawData();
            drawData.Type = OverlayVariables.DrawType.Line;
            drawData.X1 = x1;
            drawData.Y1 = y1;
            drawData.X2 = x2;
            drawData.Y2 = y2;
            drawData.Color = color;
            drawData.Stroke = stroke;
            drawData.Outline = outline;
            DrawList.Add(drawData);
        }

        public void Rectangle(int x1, int y1, int width, int height, Color color, bool fill = false, bool outline = false, float stroke = 1f)
        {
            OverlayVariables.DrawData drawData = new OverlayVariables.DrawData();
            drawData.Type = OverlayVariables.DrawType.Rectangle;
            drawData.X1 = x1;
            drawData.Y1 = y1;
            drawData.Width = width;
            drawData.Height = height;
            drawData.Color = color;
            drawData.Fill = fill;
            drawData.Outline = outline;
            drawData.Stroke = stroke;
            DrawList.Add(drawData);
        }

        public void Circle(int x1, int y1, int radius, Color color, bool fill = false, bool outline = false, float stroke = 1f)
        {
            OverlayVariables.DrawData drawData = new OverlayVariables.DrawData();
            drawData.Type = OverlayVariables.DrawType.Circle;
            drawData.X1 = x1;
            drawData.Y1 = y1;
            drawData.Radius = radius;
            drawData.Color = color;
            drawData.Fill = fill;
            drawData.Outline = outline;
            drawData.Stroke = stroke;
            DrawList.Add(drawData);
        }

        public void Text(string text, int x1, int y1, Color color, bool centerX = false, bool centerY = false, string fontFamily = "Consolas", float fontSize = 11f)
        {
            OverlayVariables.DrawData drawData = new OverlayVariables.DrawData();
            drawData.Type = OverlayVariables.DrawType.Text;
            drawData.Text = text;
            drawData.X1 = centerX ? x1 - TextRenderer.MeasureText(text, new Font(fontFamily, fontSize)).Width / 3 : x1;
            drawData.Y1 = centerY ? y1 - TextRenderer.MeasureText(text, new Font(fontFamily, fontSize)).Height / 3 : y1;
            drawData.Color = color;
            drawData.FontFamily = fontFamily;
            drawData.FontSize = fontSize;
            DrawList.Add(drawData);
        }

        public void BackgroundText(string text, int x1, int y1, Color color, Color backgroundColor, bool centerX = false, bool centerY = false, string fontFamily = "Consolas", float fontSize = 11f)
        {
            Size textSize = MeasueText(text, fontFamily, fontSize);
            int x = centerX ? x1 - textSize.Width / 2 : x1;
            int y = centerY ? y1 - textSize.Height / 2 : y1;
            Rectangle(x, y, textSize.Width + 2, textSize.Height + 2, backgroundColor, true);
            Text(text, x1, y1, color, centerX, centerY, fontFamily, fontSize);
        }

        public Size MeasueText(string text, string fontFamily, float fontSize)
        {
            Size textSize = TextRenderer.MeasureText(text, new Font(fontFamily, fontSize));
            return new Size(textSize.Width - textSize.Width / 3, textSize.Height - textSize.Height / 3);
        }
    }

    public class OverlaySettings
    {
        public int UpdateRate { get; set; }
    }

    public class OverlayVariables
    {
        public struct DrawData
        {
            public int X1;
            public int Y1;
            public int X2;
            public int Y2;
            public DrawType Type;

            public int Width;
            public int Height;

            public Color Color;
            public float Stroke;
            public bool Outline;
            public bool Fill;

            public int Radius;

            public string Text;
            public string FontFamily;
            public float FontSize;
        }

        public enum DrawType
        {
            Line,
            Rectangle,
            Circle,
            Text
        }
    }
}
