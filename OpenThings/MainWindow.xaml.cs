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
        private Dictionary<string, Shortcut> _applications;
        private ResultsWindow _resultsWindow;
        private string _currentPath;

        public MainWindow()
        {
            InitializeComponent();

            // Hold the list of applications/shortcuts in memory
            _applications = new Dictionary<string, Shortcut>();

            // Get a list of the paths to search for shortcuts and apps
            _currentPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            _currentPath = _currentPath.Substring(0, _currentPath.LastIndexOf("\\"));

            LoadApplicationList(_currentPath);

            _resultsWindow = new ResultsWindow();
        }

        // Given a text file with one path per line, retrieve all the executables 
        // and shortcuts below each path
        private void LoadApplicationList(string pathConfigFile)
        {
            _applications.Clear();

            var weightings = new Dictionary<string,int>();

            // If there's a weightings file, load the weightings into a dictionary
            // where key = app name and value = weighting
            if(File.Exists(_currentPath + "\\weighting.txt"))
            {
                weightings = (from line in File.ReadAllLines(_currentPath + "\\weighting.txt")
                              let item = line.Split('|')
                              select new { 
                                  k = item[0],
                                  v = Convert.ToInt32(item[1])
                              }).ToDictionary(x => x.k, x => x.v);
            }

            // Scan each folder specified in paths.txt
            using (var reader = File.OpenText(_currentPath + "\\paths.txt"))
            {
                while (reader.Peek() >= 0)
                {
                    var path = reader.ReadLine();
                    ScanFolder(path, weightings);
                }
            }
        }

        // Using Directory.GetFiles with the SearchOption.AllDirectories option doesn't 
        // allow the handling of access exceptions, so we're using our own recursive method
        private void ScanFolder(string path, Dictionary<string, int> weightings)
        {
            try
            {
                // Just get shortcuts and .exe files
                var files = Directory.GetFiles(path, "*.*")
                                     .Where(x => x.EndsWith(".exe") || x.EndsWith(".lnk"))
                                     .ToList();

                foreach (var item in files)
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(item);

                    var key = name.ToUpper();

                    // Don't add Uninstall shortcuts to the list
                    if (name != null && item != null && !key.Contains("UNINSTALL"))
                        _applications.Add(key, new Shortcut { 
                            // If there's a pre-existing weighting for this item, assign it
                            Weighting = (weightings.ContainsKey(key)) ? weightings[key] : 0,
                            Path = item,
                            Name = name
                        });
                }

                // Scan each subfolder of the current folder
                foreach (var folder in Directory.GetDirectories(path))
                {
                    ScanFolder(folder, weightings);
                }
            }
            catch (Exception ex)
            {
                // Ignore this folder, we don't have access
            }
        }

        private void PersistWeighting()
        {
            var weightingList = new List<string>();

            foreach (var app in _applications)
                weightingList.Add(app.Key + "|" + app.Value.Weighting);

            File.WriteAllText(_currentPath + "\\weighting.txt", string.Join(Environment.NewLine, weightingList.ToArray()));
        }

        private void window_OnKeyUp(object sender, KeyEventArgs e)
        {
            var current = _resultsWindow.listBox1.SelectedIndex;

            // Move the selection cursor up one position
            if (e.Key == Key.Up && current > 0)
            {
                _resultsWindow.listBox1.SelectedIndex = current - 1;
            }

            // Move the selection cursor down one position
            if (e.Key == Key.Down && current < _resultsWindow.listBox1.Items.Count)
            {
                _resultsWindow.listBox1.SelectedIndex = current + 1;
            }

            // When we hit enter
            if (e.Key == Key.Enter)
            {
                // Allow app exit (temporary, otherwise you can only quit using task manager...)
                if(textBox1.Text.ToUpper() == "QUIT")
                    Application.Current.Shutdown();

                // Allow rescanning of paths to add new apps without restarting
                if (textBox1.Text.ToUpper() == "RESCAN")
                {
                    this.Top = -100;
                    this.textBox1.Text = "";
                    this.textBox1.Focus();
                    _resultsWindow.Hide();
                    LoadApplicationList(_currentPath);
                }

                // If there's an item selected
                if (current != -1)
                {
                    // Get the selected item
                    var item = (ListBoxItem)_resultsWindow.listBox1.SelectedItem;

                    // Hide the text entry box and the results
                    this.Top = -100;
                    _resultsWindow.Hide();

                    // Get the Shortcut object associated with the selected item
                    var shortcut = (Shortcut)item.Tag;
                    // Each time an item is launched, add one to its weighting. This way,
                    // products which are launched more often gradually bubble up to the 
                    // top of the results lists
                    _applications[shortcut.Name.ToUpper()].Weighting += 1;
                    // Save the weighting values to a simple text file
                    PersistWeighting();

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

            // Escape just clears and closes the app
            if (e.Key == Key.Escape)
            {
                this.Top = -100;

                this.textBox1.Text = "";
                this.textBox1.Focus();

                _resultsWindow.Hide();
            }
        }

        // Handle text entry in the main text box control
        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the current value of the textbox (search text)
            var text = textBox1.Text.ToUpper();

            // Get all the apps from our in-memory list where the key contains our search text
            var matches = _applications.Where(x => x.Key.Contains(text)).ToDictionary(x => x.Key, x => x.Value);

            // Clear the current selection
            _resultsWindow.listBox1.SelectedItem = null;

            if (textBox1.Text != "" && matches.Count > 0)
            {
                _resultsWindow.listBox1.Items.Clear();

                // Order the matching applications alphabetically and then by their weighting value (see window_OnKeyUp)
                foreach (KeyValuePair<string, Shortcut> match in matches.OrderBy(x => x.Value.Name).OrderByDescending(x => x.Value.Weighting))
                    _resultsWindow.listBox1.Items.Add(new ListBoxItem { Tag = match.Value, Content = match.Value.Name });

                // Kill the selection
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

            // Set up some key codes
            const uint MOD_ALT = 0x1;     
            const uint MOD_CONTROL = 0x2; 
            const uint MOD_SHIFT = 0x4;   
            const uint MOD_WIN = 0x8;   

            const uint VK_F10 = 0x79;
            const uint VK_SPACE = 0x20;
            const uint VK_CAPITAL = 0x14;

            // Set third parameter to 0 for no modifier key
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_ALT, VK_SPACE))
            {
                // Don't handle error
                MessageBox.Show("Couldn't register hotkey");
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

        // Handle the global hotkey
        private void OnHotKeyPressed()
        {
            // Toggle the main window's visibility
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
