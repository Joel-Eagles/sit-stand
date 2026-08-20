// Sit/Stand Desk Timer - C# port
//
// Compiles to a genuine Win32 executable (no PowerShell, no script host
// involved at runtime), which avoids the "powershell.exe -WindowStyle
// Hidden" living-off-the-land pattern that EDR/ATP products commonly flag.
//
// Build (from a regular Command Prompt, no admin needed, no downloads):
//   "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:SitStandTimer.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.VisualBasic.dll Program.cs
//
// /target:winexe means Windows creates NO console window at all - not
// even briefly - because the process is marked as a GUI subsystem app
// from the start, unlike running a .ps1 under powershell.exe.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SitStandTimer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new TimerAppContext());
        }
    }

    internal struct Preset
    {
        public string Label;
        public int Sit;
        public int Stand;

        public Preset(string label, int sit, int stand)
        {
            Label = label;
            Sit = sit;
            Stand = stand;
        }
    }

    internal struct ColorTheme
    {
        public string Name;
        public Color SitColor;
        public Color StandColor;

        public ColorTheme(string name, Color sitColor, Color standColor)
        {
            Name = name;
            SitColor = sitColor;
            StandColor = standColor;
        }
    }

    internal class TimerAppContext : ApplicationContext
    {
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        private NotifyIcon _trayIcon;
        private System.Windows.Forms.Timer _timer;
        private ToolStripMenuItem _statusItem;
        private ToolStripMenuItem _pauseItem;

        private Icon _sitIcon;
        private Icon _standIcon;
        private readonly Icon _pauseIcon;
        private IntPtr _sitIconHandle = IntPtr.Zero;
        private IntPtr _standIconHandle = IntPtr.Zero;

        // Built-in defaults, also used by "Reset to defaults"
        private const int DefaultSitMinutes = 30;
        private const int DefaultStandMinutes = 15;
        private const string DefaultSitLabel = "D";
        private const string DefaultStandLabel = "U";
        private static readonly Color DefaultSitColor = Color.FromArgb(184, 14, 255);
        private static readonly Color DefaultStandColor = Color.FromArgb(16, 137, 62);

        private int _sitMinutes = DefaultSitMinutes;
        private int _standMinutes = DefaultStandMinutes;
        private string _state = "Sit";
        private bool _paused = false;
        private int _remainingSecs;

        private string _sitLabel = DefaultSitLabel;
        private string _standLabel = DefaultStandLabel;
        private Color _sitColor = DefaultSitColor;
        private Color _standColor = DefaultStandColor;

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SitStandTimer", "config.txt");

        public TimerAppContext()
        {
            LoadConfig();
            _remainingSecs = _sitMinutes * 60;

            _pauseIcon = CreateStateIcon("II", Color.FromArgb(150, 150, 150));
            RebuildStateIcons();

            var menu = new ContextMenuStrip();

            _statusItem = new ToolStripMenuItem("Sitting - 30:00 remaining") { Enabled = false };
            menu.Items.Add(_statusItem);
            menu.Items.Add(new ToolStripSeparator());

            var switchNowItem = new ToolStripMenuItem("Switch now");
            switchNowItem.Click += (s, e) => SwitchState();
            menu.Items.Add(switchNowItem);

            _pauseItem = new ToolStripMenuItem("Pause");
            _pauseItem.Click += (s, e) => { _paused = !_paused; UpdateDisplay(); };
            menu.Items.Add(_pauseItem);

            menu.Items.Add(new ToolStripSeparator());

            // --- Intervals submenu ---
            var intervalMenu = new ToolStripMenuItem("Intervals");
            var presets = new Preset[]
            {
                new Preset("20 min sit / 8 min stand", 20, 8),
                new Preset("30 min sit / 15 min stand", 30, 15),
                new Preset("45 min sit / 15 min stand", 45, 15),
                new Preset("60 min sit / 20 min stand", 60, 20),
            };
            foreach (var p in presets)
            {
                var preset = p;
                var item = new ToolStripMenuItem(preset.Label);
                item.Click += (s, e) =>
                {
                    _sitMinutes = preset.Sit;
                    _standMinutes = preset.Stand;
                    _remainingSecs = (_state == "Sit" ? _sitMinutes : _standMinutes) * 60;
                    SaveConfig();
                    UpdateDisplay();
                };
                intervalMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(intervalMenu);

            var customItem = new ToolStripMenuItem("Custom...");
            customItem.Click += (s, e) => ShowCustomIntervalDialog();
            menu.Items.Add(customItem);

            menu.Items.Add(new ToolStripSeparator());

            // --- Tray Icon submenu ---
            var iconMenu = new ToolStripMenuItem("Tray Icon");

            var colorSchemeMenu = new ToolStripMenuItem("Color scheme");
            var themes = new ColorTheme[]
            {
                new ColorTheme("Purple / Green (default)", Color.FromArgb(184, 14, 255), Color.FromArgb(16, 137, 62)),
                new ColorTheme("Blue / Green", Color.FromArgb(0, 120, 212), Color.FromArgb(16, 137, 62)),
                new ColorTheme("Orange / Teal", Color.FromArgb(230, 81, 0), Color.FromArgb(0, 121, 107)),
                new ColorTheme("Red / Indigo", Color.FromArgb(198, 40, 40), Color.FromArgb(48, 63, 159)),
            };
            foreach (var t in themes)
            {
                var theme = t;
                var item = new ToolStripMenuItem(theme.Name);
                item.Click += (s, e) =>
                {
                    _sitColor = theme.SitColor;
                    _standColor = theme.StandColor;
                    RebuildStateIcons();
                    SaveConfig();
                    UpdateDisplay();
                };
                colorSchemeMenu.DropDownItems.Add(item);
            }
            iconMenu.DropDownItems.Add(colorSchemeMenu);

            var customColorsItem = new ToolStripMenuItem("Custom colors...");
            customColorsItem.Click += (s, e) => ShowCustomColorDialog();
            iconMenu.DropDownItems.Add(customColorsItem);

            var customLettersItem = new ToolStripMenuItem("Custom letters...");
            customLettersItem.Click += (s, e) => ShowCustomLetterDialog();
            iconMenu.DropDownItems.Add(customLettersItem);

            menu.Items.Add(iconMenu);

            menu.Items.Add(new ToolStripSeparator());

            var resetItem = new ToolStripMenuItem("Reset to defaults...");
            resetItem.Click += (s, e) => ResetToDefaults();
            menu.Items.Add(resetItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) =>
            {
                _trayIcon.Visible = false;
                _timer.Stop();
                Application.Exit();
            };
            menu.Items.Add(exitItem);

            _trayIcon = new NotifyIcon
            {
                Icon = _sitIcon,
                Text = "Sit/Stand Timer - Sitting",
                Visible = true,
                ContextMenuStrip = menu
            };
            _trayIcon.MouseDoubleClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) SwitchState();
            };

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) =>
            {
                if (_paused) return;
                _remainingSecs--;
                if (_remainingSecs <= 0)
                    SwitchState();
                else
                    UpdateDisplay();
            };
            _timer.Start();

            UpdateDisplay();
            ShowNotification("Sit/Stand Timer running",
                string.Format("Sitting for {0} min, standing for {1} min.", _sitMinutes, _standMinutes));
        }

        private static Icon CreateStateIcon(string label, Color color)
        {
            IntPtr handle;
            return CreateStateIcon(label, color, out handle);
        }

        private static Icon CreateStateIcon(string label, Color color, out IntPtr hIconHandle)
        {
            using (var bmp = new Bitmap(32, 32))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    using (var brush = new SolidBrush(color))
                        g.FillEllipse(brush, 1, 1, 30, 30);

                    using (var font = new Font("Segoe UI", 14, FontStyle.Bold))
                    using (var fmt = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    })
                    {
                        g.DrawString(label, font, Brushes.White, 16, 17, fmt);
                    }
                }

                hIconHandle = bmp.GetHicon();
                return Icon.FromHandle(hIconHandle);
            }
        }

        private void RebuildStateIcons()
        {
            Icon oldSit = _sitIcon;
            IntPtr oldSitHandle = _sitIconHandle;
            Icon oldStand = _standIcon;
            IntPtr oldStandHandle = _standIconHandle;

            _sitIcon = CreateStateIcon(_sitLabel, _sitColor, out _sitIconHandle);
            _standIcon = CreateStateIcon(_standLabel, _standColor, out _standIconHandle);

            if (oldSit != null) oldSit.Dispose();
            if (oldSitHandle != IntPtr.Zero) DestroyIcon(oldSitHandle);
            if (oldStand != null) oldStand.Dispose();
            if (oldStandHandle != IntPtr.Zero) DestroyIcon(oldStandHandle);
        }

        private void UpdateDisplay()
        {
            int mins = _remainingSecs / 60;
            int secs = _remainingSecs % 60;
            string timeStr = string.Format("{0}:{1:D2}", mins, secs);

            if (_paused)
            {
                _trayIcon.Icon = _pauseIcon;
                _trayIcon.Text = string.Format("Sit/Stand Timer - Paused ({0})", _state);
                _statusItem.Text = string.Format("Paused - was {0}ting", _state.ToLower());
                _pauseItem.Text = "Resume";
                return;
            }

            _pauseItem.Text = "Pause";
            if (_state == "Sit")
            {
                _trayIcon.Icon = _sitIcon;
                _trayIcon.Text = string.Format("Sit/Stand Timer - Sitting ({0})", timeStr);
                _statusItem.Text = string.Format("Sitting - {0} remaining", timeStr);
            }
            else
            {
                _trayIcon.Icon = _standIcon;
                _trayIcon.Text = string.Format("Sit/Stand Timer - Standing ({0})", timeStr);
                _statusItem.Text = string.Format("Standing - {0} remaining", timeStr);
            }
        }

        private void ShowNotification(string title, string message)
        {
            // Toggling visibility forces Windows to actually re-fire the
            // balloon/toast instead of silently suppressing a repeat.
            _trayIcon.Visible = false;
            _trayIcon.Visible = true;
            _trayIcon.ShowBalloonTip(8000, title, message, ToolTipIcon.Info);
        }

        private void SwitchState()
        {
            if (_state == "Sit")
            {
                _state = "Stand";
                _remainingSecs = _standMinutes * 60;
                ShowNotification("Time to stand up", "Raise your desk and stand for a while.");
            }
            else
            {
                _state = "Sit";
                _remainingSecs = _sitMinutes * 60;
                ShowNotification("Time to sit down", "Lower your desk and take a seat.");
            }
            UpdateDisplay();
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return;

                foreach (string line in File.ReadAllLines(ConfigPath))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;

                    int eq = trimmed.IndexOf('=');
                    if (eq <= 0) continue;

                    string key = trimmed.Substring(0, eq).Trim();
                    string value = trimmed.Substring(eq + 1).Trim();
                    int parsed;

                    if (key == "SitMinutes" && int.TryParse(value, out parsed) && parsed > 0)
                        _sitMinutes = parsed;
                    else if (key == "StandMinutes" && int.TryParse(value, out parsed) && parsed > 0)
                        _standMinutes = parsed;
                    else if (key == "SitLabel" && value.Length > 0)
                        _sitLabel = value.Length > 2 ? value.Substring(0, 2) : value;
                    else if (key == "StandLabel" && value.Length > 0)
                        _standLabel = value.Length > 2 ? value.Substring(0, 2) : value;
                    else if (key == "SitColor")
                    {
                        Color c;
                        if (TryParseColor(value, out c)) _sitColor = c;
                    }
                    else if (key == "StandColor")
                    {
                        Color c;
                        if (TryParseColor(value, out c)) _standColor = c;
                    }
                }
            }
            catch
            {
                // If the config file is missing, unreadable, or corrupt, just
                // fall back to the built-in defaults rather than crashing.
            }
        }

        private void SaveConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                File.WriteAllLines(ConfigPath, new string[]
                {
                    "# Sit/Stand Timer settings - edit by hand or via the tray menu",
                    "SitMinutes=" + _sitMinutes,
                    "StandMinutes=" + _standMinutes,
                    "SitLabel=" + _sitLabel,
                    "StandLabel=" + _standLabel,
                    "SitColor=" + FormatColor(_sitColor),
                    "StandColor=" + FormatColor(_standColor)
                });
            }
            catch
            {
                // Non-fatal if we can't write the config - the app still
                // works, it just won't remember the setting next launch.
            }
        }

        private static string FormatColor(Color c)
        {
            return c.R + "," + c.G + "," + c.B;
        }

        private static bool TryParseColor(string value, out Color color)
        {
            color = Color.Black;
            string[] parts = value.Split(',');
            if (parts.Length != 3) return false;

            int r, g, b;
            if (int.TryParse(parts[0].Trim(), out r) &&
                int.TryParse(parts[1].Trim(), out g) &&
                int.TryParse(parts[2].Trim(), out b))
            {
                color = Color.FromArgb(r, g, b);
                return true;
            }
            return false;
        }

        private void ShowCustomIntervalDialog()
        {
            string sitInput = Microsoft.VisualBasic.Interaction.InputBox(
                "Minutes to sit:", "Sit/Stand Timer", _sitMinutes.ToString());
            if (string.IsNullOrEmpty(sitInput)) return;

            string standInput = Microsoft.VisualBasic.Interaction.InputBox(
                "Minutes to stand:", "Sit/Stand Timer", _standMinutes.ToString());
            if (string.IsNullOrEmpty(standInput)) return;

            int sitVal, standVal;
            if (int.TryParse(sitInput, out sitVal) && int.TryParse(standInput, out standVal)
                && sitVal > 0 && standVal > 0)
            {
                _sitMinutes = sitVal;
                _standMinutes = standVal;
                _remainingSecs = (_state == "Sit" ? _sitMinutes : _standMinutes) * 60;
                SaveConfig();
                UpdateDisplay();
            }
        }

        private void ShowCustomColorDialog()
        {
            using (var dlg = new ColorDialog())
            {
                dlg.Color = _sitColor;
                dlg.FullOpen = true;
                if (dlg.ShowDialog() != DialogResult.OK) return;
                Color newSitColor = dlg.Color;

                dlg.Color = _standColor;
                if (dlg.ShowDialog() != DialogResult.OK) return;
                Color newStandColor = dlg.Color;

                _sitColor = newSitColor;
                _standColor = newStandColor;
                RebuildStateIcons();
                SaveConfig();
                UpdateDisplay();
            }
        }

        private void ShowCustomLetterDialog()
        {
            string sitInput = Microsoft.VisualBasic.Interaction.InputBox(
                "Letter(s) for the Sitting icon (1-2 characters):", "Sit/Stand Timer", _sitLabel);
            if (string.IsNullOrEmpty(sitInput)) return;

            string standInput = Microsoft.VisualBasic.Interaction.InputBox(
                "Letter(s) for the Standing icon (1-2 characters):", "Sit/Stand Timer", _standLabel);
            if (string.IsNullOrEmpty(standInput)) return;

            _sitLabel = sitInput.Length > 2 ? sitInput.Substring(0, 2) : sitInput;
            _standLabel = standInput.Length > 2 ? standInput.Substring(0, 2) : standInput;
            RebuildStateIcons();
            SaveConfig();
            UpdateDisplay();
        }

        private void ResetToDefaults()
        {
            DialogResult confirm = MessageBox.Show(
                "Reset sit/stand minutes, icon colors, and letters back to the defaults?",
                "Sit/Stand Timer",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _sitMinutes = DefaultSitMinutes;
            _standMinutes = DefaultStandMinutes;
            _sitLabel = DefaultSitLabel;
            _standLabel = DefaultStandLabel;
            _sitColor = DefaultSitColor;
            _standColor = DefaultStandColor;
            _remainingSecs = (_state == "Sit" ? _sitMinutes : _standMinutes) * 60;

            RebuildStateIcons();
            SaveConfig();
            UpdateDisplay();
        }
    }
}
