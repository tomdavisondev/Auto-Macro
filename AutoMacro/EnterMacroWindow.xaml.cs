using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Reactive.Disposables;
using Dapplo.Windows.Input.Keyboard;
using System.Runtime.CompilerServices;
using Dapplo.Windows.Messages;

namespace AutoMacro
{
    /// <summary>
    /// Interaction logic for EnterMacroWindow.xaml
    /// </summary>
    public partial class EnterMacroWindow : Window
    {
        public List<VirtualKeyCode> macroKeyCodes;
        IDisposable rawInputObservable;
        IDisposable enterHotKeyDisposable;

        public EnterMacroWindow()
        {
            InitializeComponent();
            macroKeyCodes = new List<VirtualKeyCode>();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            var stopHotkey = new KeyCombinationHandler(VirtualKeyCode.Return)
            {
                IgnoreInjected = true,
                IsPassThrough = false
            };
            enterHotKeyDisposable = KeyboardHook.KeyboardEvents.Where(stopHotkey).Subscribe(KeyboardHookEventArgs => {
                StopRecording();
            });
            base.OnContentRendered(e);
            StartRecording();
        }

        public void StartRecording()
        {
            rawInputObservable = RawInputMonitor.MonitorRawInput(RawInputDevices.Keyboard).Subscribe(ri =>
            {
                if (ri.RawInput.Device.Keyboard.Flags == RawKeyboardFlags.Break)
                {
                    if (ri.RawInput.Device.Keyboard.VirtualKey != VirtualKeyCode.Return)
                    {
                        if (ri.RawInput.Device.Keyboard.VirtualKey.IsModifier())
                        {
                            macroKeyCodes.Clear();
                            macroKeyCodes.Add(ri.RawInput.Device.Keyboard.VirtualKey);
                            KeyCombo.Text = ri.RawInput.Device.Keyboard.VirtualKey.ToString() + " + ";
                        }
                        else
                        {
                            macroKeyCodes.Add(ri.RawInput.Device.Keyboard.VirtualKey);
                            KeyCombo.Text += ri.RawInput.Device.Keyboard.VirtualKey.ToString();
                            Trace.WriteLine("Key added to macro: {0}", ri.RawInput.Device.Keyboard.VirtualKey.ToString());
                        }
                    }
                    else
                    {
                        StopRecording();
                    }
                }
            });
        }

        public void StopRecording()
        {
            try
            {
                RawInputApi.RegisterRawInput(WinProcHandler.Instance.Handle, RawInputDeviceFlags.Remove | RawInputDeviceFlags.DeviceNotify, RawInputDevices.Keyboard);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
            enterHotKeyDisposable.Dispose();
            rawInputObservable.Dispose();
            this.Close();
        }
    }
}
