using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfPaint.ShapesExtension;
using WpfPaint.Tools;

namespace WpfPaint
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DrawTool selectedTool;
               
        public MainWindow()
        {
            InitializeComponent();
            this.selectedTool = new DrawTool(this, this.workspace, this.border, this.canvas, DrawToolType.Pointer);
            this.selectedTool.SelectionChange += selectedTool_SelectionChange;
            this.UpdateButtons();
        }

        void selectedTool_SelectionChange(object sender, EventArgs e)
        {
            this.UpdateButtons();
            if (this.selectedTool.Selection.Count == 1)
            {
                Shape s = this.selectedTool.Selection.First();
                Brush b = this.selectedTool.CurrentBrush;
                this.updatePopup = false;
                this.colorButton.Background = b;
                this.lineWidthComboBox.SelectedIndex = (int)Math.Round(this.selectedTool.CurrentLineWidth) - 3;
                
                if (s is Polyline)
                {
                    this.isGradientCB.IsEnabled = false;
                    this.isGradientCB.IsChecked = false;
                }
                else
                {
                    this.isGradientCB.IsEnabled = true;
                }

                this.setColorPopupBrush(b);
            }
        }

        /// <summary>
        /// Обновляет состояние кнопок интерфейса
        /// </summary>
        private void UpdateButtons()
        {
            this.colorButton.IsEnabled = this.selectedTool.Selection.Count < 2 && this.selectedTool.ToolType != DrawToolType.Delete;
            this.lineWidthComboBox.IsEnabled = this.colorButton.IsEnabled;
            this.colorPopup.IsOpen = this.colorPopup.IsOpen && this.colorButton.IsEnabled;

            this.addPolylineButton.IsChecked = this.selectedTool.ToolType == DrawToolType.Polyline;
            this.addRectangleButton.IsChecked = this.selectedTool.ToolType == DrawToolType.Rectangle;
            this.deleteButton.IsChecked = this.selectedTool.ToolType == DrawToolType.Delete;
            this.pointerButton.IsChecked = this.selectedTool.ToolType == DrawToolType.Pointer;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Point p = new Point((this.ActualWidth - this.border.ActualWidth) / 2, (this.ActualHeight - this.border.ActualHeight) / 2);
            Canvas.SetLeft(this.border, p.X);
            Canvas.SetTop(this.border, p.Y);
            this.setColorPopupBrush(Brushes.Black);
            this.colorButton.Background = Brushes.Black;
            this.selectedTool.CurrentBrush = Brushes.Black;
            this.selectedTool.CurrentLineWidth = this.lineWidthComboBox.SelectedIndex + 3.0;

        }

        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            if (this.selectedTool.Selection.Count > 1)
                this.selectedTool.Selection.SetDeletingStyle(true);
        }

        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            this.selectedTool.Selection.SetDeletingStyle(false);
        }
    }
    
    
}