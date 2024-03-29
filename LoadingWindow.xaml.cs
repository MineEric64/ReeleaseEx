﻿using System;
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
        private int _max = 0;
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
            _max = maximum;
        }

        public void Increment()
        {
            if (_progress > _max)
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
                loadingPercentage.Text = $"{(int)Math.Round(_progress / (double)_max * 100)}%";
            });
        }

        public new void Show()
        {
            Dispatcher.Invoke(base.Show);
            IsShown = true;
        }

        public new void Hide()
        {
            Dispatcher.Invoke(base.Hide);
            IsShown = false;
        }
    }
}
