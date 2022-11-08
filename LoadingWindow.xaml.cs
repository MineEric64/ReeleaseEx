using System;
using System.Collections.Generic;
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

namespace ReeleaseEx
{
    /// <summary>
    /// LoadingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoadingWindow : Window
    {
        private int _progress = 0;
        public bool IsShown { get; private set; } = false;

        public LoadingWindow()
        {
            InitializeComponent();
        }

        public void Initialize(int maximum, string text = "Loading...")
        {
            Dispatcher.Invoke(() =>
            {
                loadingBar.Value = 0;
                loadingBar.Maximum = maximum;
                loadingText.Content = text;
                loadingPercentage.Text = "0%";
            });
        }

        public void Increment()
        {
            if (_progress > loadingBar.Maximum)
            {
                return;
            }
            if (!IsShown)
            {
                _progress++;
                return;
            }

            Dispatcher.Invoke(() =>
            {
                loadingBar.Value = ++_progress;
                loadingPercentage.Text = $"{_progress / (int)loadingBar.Maximum}%";
            });
        }

        public new void Show()
        {
            base.Show();
            IsShown = true;
        }

        public new void Hide()
        {
            base.Hide();
            IsShown = false;
        }
    }
}
