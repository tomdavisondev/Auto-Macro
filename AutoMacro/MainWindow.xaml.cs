using Dapplo.Windows.Input;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Input.Mouse;
using Dapplo.Windows.Input.Structs;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Cursor = System.Windows.Input.Cursor;
using Point = System.Windows.Point;
using Path = System.IO.Path;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Data;
using System.ComponentModel.Design;

namespace AutoMacro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ObservableCollection<Command> commands;
        private static EventWaitHandle _commandRunner;
        private DispatcherTimer dispatcherTimer;
        string settingsPath;
        private int pressHandlingTime = 500;
        bool running;
        VirtualKeyCode[] macroKeyCodes;
        EnterMacroWindow macroWindow;

        public MainWindow()
        {
            InitializeComponent();
            commands = new ObservableCollection<Command>();
            running = false;
            _commandRunner = new AutoResetEvent(false);
            settingsPath = Path.Combine(Path.GetDirectoryName(System.Environment.CurrentDirectory), "Settings");
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();

            macroKeyCodes = new VirtualKeyCode[] { };

            PopulateGrid();

            DelayLabel.Content = pressHandlingTime.ToString() + "ms";

            DelaySlider.Value = pressHandlingTime;
            InitializeHotkeys();
        }

        private void PopulateGrid()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Enabled", typeof(bool));
            dt.Columns.Add("X", typeof(int));
            dt.Columns.Add("Y", typeof(int));
            dt.Columns.Add("Keys", typeof(string));

            foreach(Command command in commands)
            {
                var row = dt.NewRow();
                row.SetField<bool>("Enabled", command.Enabled);
                row.SetField<int>("X", command.XPos);
                row.SetField<int>("Y", command.YPos);
                row.SetField<string>("Keys", command.MacroListToString());
                dt.Rows.Add(row);
            }

            CommandList.DataContext = dt;
            dt.DefaultView.RowFilter = "";
            CommandList.ItemsSource = dt.DefaultView;
        }

        private void InitializeHotkeys()
        {
            var recordClick = new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyR)
            {
                IgnoreInjected = true,
                IsPassThrough = false
            };
            KeyboardHook.KeyboardEvents.Where(recordClick).Subscribe(KeyboardHookEventArgs => OnRecordClick());

            var stopHotkey = new KeyCombinationHandler(VirtualKeyCode.Escape)
            {
                IgnoreInjected = true,
                IsPassThrough = false
            };
            KeyboardHook.KeyboardEvents.Where(stopHotkey).Subscribe(KeyboardHookEventArgs => { running = false; });

            var recordKeystroke = new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyE)
            {
                IgnoreInjected = true,
                IsPassThrough = false
            };
            KeyboardHook.KeyboardEvents.Where(recordKeystroke).Subscribe(KeyboardHookEventArgs =>
            {
                macroWindow = new EnterMacroWindow();
                macroWindow.Owner = this;
                macroWindow.ShowDialog();
                commands.Add(new Command(true, macroWindow.macroKeyCodes));
                PopulateGrid();
            });
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Point mousePos = GetCursorPosition();
            XPositionTextBox.Text = mousePos.X.ToString();
            YPositionTextBox.Text = mousePos.Y.ToString();
        }
        
        private void OnRecordClick()
        {
            Point point = GetCursorPosition();
            Trace.WriteLine(point.ToString());
            Command cmd = new Command(true, (int)point.X, (int)point.Y);
            commands.Add(cmd);
            PopulateGrid();
        }

        private void AddTargetbtn_Click(object sender, RoutedEventArgs e)
        {
            Command cmd = new Command(false, 0, 0);
            commands.Add(cmd);
            PopulateGrid();
        }

        private void DeleteTargetbtn_Click(object sender, RoutedEventArgs e)
        {
            if (commands.Count == 0)
                return;
            if (CommandList.SelectedIndex != -1)
            {
                commands.RemoveAt(CommandList.SelectedIndex);
            }
            else
                commands.RemoveAt(commands.Count - 1);

            PopulateGrid();
        }

        private void ClearTargetbtn_Click(object sender, RoutedEventArgs e)
        {
            commands.Clear();
            PopulateGrid();
        }

        private void Startbtn_Click(object sender, RoutedEventArgs e)
        {
            running = true;
            foreach (Command cmd in commands)
            {
                if (running && cmd.Enabled)
                {
                    if (cmd == null)
                        return;
                    if (cmd.Keys == null)
                    {
                        SetCursorPos(cmd.XPos, cmd.YPos);
                        //TODO: Add variance in mousedown/mouseup events
                        MouseInputGenerator.MouseClick(Dapplo.Windows.Input.Enums.MouseButtons.Left);
                        _commandRunner.WaitOne(pressHandlingTime);
                    }
                    else
                    {
                        KeyboardInputGenerator.KeyPresses(cmd.Keys.ToArray());
                    }
                }
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            pressHandlingTime = (int)DelaySlider.Value;
            DelayLabel.Content = pressHandlingTime.ToString() + "ms";
        }

        private void Importbtn_Click(object sender, RoutedEventArgs e)
        {
            string json = File.ReadAllText(settingsPath + "Settings.json");
            var obj = JsonConvert.DeserializeObject<ObservableCollection<Command>>(json);
            try
            {
                commands = (ObservableCollection<Command>)obj;
                PopulateGrid();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Import Failed: " + ex.Message);
            }
        }

        private void Exportbtn_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(settingsPath + "Settings.json", Newtonsoft.Json.JsonConvert.SerializeObject(commands));
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator System.Windows.Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);

        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);

            return lpPoint;
        }
    }
}
