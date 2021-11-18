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
    /// ChooseText.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChooseText : Window
    {
        public EventHandler<string> TextAdded;

        public ChooseText()
        {
            InitializeComponent();

            this.KeyUp += ChooseText_KeyUp;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            TextAdded?.Invoke(this, textBox.Text);
            this.Close();
        }

        private void ChooseText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OK_Click(this, null);
            }
        }
    }
}