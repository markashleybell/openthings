using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace OpenThings
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, Shortcut> _paths;
        private ResultsWindow _resultsWindow;

        public MainWindow()
        {
            InitializeComponent();

            _paths = new Dictionary<string, Shortcut>();

            var config = System.Reflection.Assembly.GetExecutingAssembly().Location;

            config = config.Substring(0, config.LastIndexOf("\\")) + "\\paths.txt";


            using (var reader = File.OpenText(config))
            {
                while (reader.Peek() >= 0)
                {
                    var path = reader.ReadLine();

                    ScanFolder(path);
                }
            }

            _resultsWindow = new ResultsWindow();
            //_resultsWindow.Show();
            //_resultsWindow.listView1.ItemsSource = _matches;

            //this.Activate();
            //this.Top = 0;
            //textBox1.Focus();
        }

        private void ScanFolder(string path)
        {
            try
            {
                var files = Directory.GetFiles(path, "*.*").Where(x => x.EndsWith(".exe") || x.EndsWith(".lnk")).ToList();

                foreach (var item in files)
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(item);

                    if (name != null && item != null)
                        _paths.Add(name.ToUpper(), new Shortcut { 
                            Weighting = 0,
                            Path = item,
                            Name = name
                        });
                }

                foreach (var folder in Directory.GetDirectories(path))
                {
                    ScanFolder(folder);
                }
            }
            catch (Exception ex)
            {
                // Ignore the folder, we don't have access
            }
        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {
            // MessageBox.Show(e.Key.ToString());

            var current = _resultsWindow.listBox1.SelectedIndex;

            if (e.Key == Key.Up)
            {
                _resultsWindow.listBox1.SelectedIndex = current + 1;
            }

            if (e.Key == Key.Down)
            {
                _resultsWindow.listBox1.SelectedIndex = current + 1;
            }

            if (e.Key == Key.Enter)
            {
                if(textBox1.Text.ToUpper() == "QUIT")
                    Application.Current.Shutdown();

                if (current != -1)
                {
                    var item = (ListBoxItem)_resultsWindow.listBox1.SelectedItem;

                    this.Top = -100;
                    _resultsWindow.Hide();

                    var shortcut = (Shortcut)item.Tag;

                    _paths[shortcut.Name.ToUpper()].Weighting += 1;

                    try
                    {
                        Process.Start(shortcut.Path.ToString());
                    }
                    catch (Exception ex)
                    {
                        // Something went wrong launching the program
                    }
                }
            }

            if (e.Key == Key.Escape)
            {
                this.Top = -100;

                textBox1.Text = "";
                this.textBox1.Focus();

                _resultsWindow.Hide();
            }
        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = textBox1.Text.ToUpper();
            var matches = _paths.Where(x => x.Key.Contains(text)).ToDictionary(x => x.Key, x => x.Value);

            //_matches.Clear();

            //foreach (var match in matches)
            //    _matches.Add(match);

            _resultsWindow.listBox1.SelectedItem = null;

            if (textBox1.Text != "" && matches.Count > 0)
            {
                _resultsWindow.listBox1.Items.Clear();

                foreach (KeyValuePair<string, Shortcut> match in matches.OrderBy(x => x.Value.Name).OrderByDescending(x => x.Value.Weighting))
                    _resultsWindow.listBox1.Items.Add(new ListBoxItem { Tag = match.Value, Content = match.Value.Name });

                _resultsWindow.listBox1.SelectedIndex = 0;

                _resultsWindow.Show();
            }
            else
            {
                _resultsWindow.Hide();
            }

            this.Activate();
        }

        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
            [In] IntPtr hWnd,
            [In] int id,
            [In] uint fsModifiers,
            [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        private HwndSource _source;
        private const int HOTKEY_ID = 9000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey();
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey();
            base.OnClosed(e);
        }

        private void RegisterHotKey()
        {
            var helper = new WindowInteropHelper(this);

            const uint MOD_ALT = 0x1;     // If bit 0 is set, Alt is pressed
            const uint MOD_CONTROL = 0x2; // If bit 1 is set, Ctrl is pressed
            const uint MOD_SHIFT = 0x4;   // If bit 2 is set, Shift is pressed 
            const uint MOD_WIN = 0x8;     // If bit 3 is set, Win is pressed

            const uint VK_F10 = 0x79;
            const uint VK_SPACE = 0x20;
            const uint VK_CAPITAL = 0x14;

            const uint VK_LSHIFT = 0xA0;
            const uint VK_RSHIFT = 0xA1;

            
            
            //if (!RegisterHotKey(helper.Handle, HOTKEY_ID, 0, VK_CAPITAL))
            //{
            //    // handle error
            //}

            if (!RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_ALT, VK_SPACE))
            {
                // handle error
            }
        }

        private void UnregisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            OnHotKeyPressed();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void OnHotKeyPressed()
        {
            if(this.Top < 0)
            {
                this.Top = 0;
                this.textBox1.Text = "";
                this.Activate();
                this.textBox1.Focus();
            }
            else
            {
                this.Top = -100;
            }
        }
    }
}
