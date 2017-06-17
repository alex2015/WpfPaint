using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfPaint
{
    public partial class MainWindow
    {
        private bool updatePopup = true; // Следует ли обновлять стиль выделеной фигуры при изменении настроек кисти
        private bool closePopup = false; // Закрыть панель настройки кисти. Для правильной обработки щелчка на colorButton
       
        /// <summary>
        /// Установка контролов Popup в соотвествии с заданной кистью. Исользуется для обновления текущей кисти при выделении фигуры
        /// </summary>
        private void setColorPopupBrush(Brush brush)
        {
            this.updatePopup = false; // Обновление стиля фигуры не требуется
            if (brush == null)
            {
                this.startColorCB.SelectedIndex = -1;
                this.endColorCB.SelectedIndex = -1;
                this.gradientSlider.Value = 0;
                this.endColorCB.IsEnabled = false;
                this.gradientSlider.IsEnabled = false;
            }
            else if (brush is LinearGradientBrush)
            {
                LinearGradientBrush gbrush = brush as LinearGradientBrush;
                this.startColorCB.SelectedItem = this.getComboBoxColor(gbrush.GradientStops[0].Color);
                this.endColorCB.SelectedItem = this.getComboBoxColor(gbrush.GradientStops[1].Color);
                this.gradientSlider.Value = (gbrush.GradientStops[0].Offset + gbrush.GradientStops[1].Offset) * 50;
                this.endColorCB.IsEnabled = true;
                this.isGradientCB.IsChecked = true;
                this.gradientSlider.IsEnabled = true;
            }
            else if (brush is SolidColorBrush)
            {
                this.startColorCB.SelectedItem = this.getComboBoxColor((brush as SolidColorBrush).Color);
                this.endColorCB.IsEnabled = false;
                this.isGradientCB.IsChecked = false;
                this.gradientSlider.IsEnabled = false;
            }
            this.colorButton.Background = brush;
            this.updatePopup = true;
        }

        /// <summary>
        /// Хак для получения Color который есть в ComboBoxColor.Items
        /// </summary>
        private object getComboBoxColor(Color color)
        {
            foreach (PropertyInfo c in this.startColorCB.Items)
            {
                if (color.Equals(c.GetValue(null, null)))
                    return c;
            }

            return Colors.Transparent;
        }

        /// <summary>
        /// Расчет кисти из параметров контролов Popup
        /// </summary>
        private Brush getColorPopupBrush()
        {
            if (this.endColorCB == null || this.startColorCB == null || this.gradientSlider == null
                || (this.endColorCB.SelectedIndex == -1 && this.startColorCB.SelectedIndex == -1))
                return null;

            if (this.isGradientCB != null && (this.isGradientCB.IsChecked ?? false) && this.isGradientCB.IsEnabled)
            {
                LinearGradientBrush gradBrush = new LinearGradientBrush();
                gradBrush.StartPoint = new Point(0, 0.5);
                gradBrush.EndPoint = new Point(1, 0.5);
                Color c1 = (Color)(startColorCB.SelectedItem as PropertyInfo).GetValue(null, null);
                Color c2 = (Color)(endColorCB.SelectedItem as PropertyInfo).GetValue(null, null);

                gradBrush.GradientStops.Add(new GradientStop(c1, Math.Max(0, this.gradientSlider.Value / 100 - 0.3)));
                gradBrush.GradientStops.Add(new GradientStop(c2, Math.Min(1, this.gradientSlider.Value / 100 + 0.3)));

                return gradBrush;
            }
            else
                return new SolidColorBrush((Color)(startColorCB.SelectedItem as PropertyInfo).GetValue(null, null));
        }

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /* Необходимое условие при обработки действий интерфейса - проверить this.selectedTool != null
        *  при первой заргузке главного окна popup вызывает события изменения раньше чем будет создан selectedTool
        */
    
        private void colorButton_MouseEnter(object sender, MouseEventArgs e)
        {
            this.closePopup = this.colorPopup.IsOpen;
        }

        private void isGradientCB_Click(object sender, RoutedEventArgs e)
        {
            this.endColorCB.IsEnabled = this.isGradientCB.IsChecked.Value;
            this.gradientSlider.IsEnabled = this.endColorCB.IsEnabled;

            Brush b = this.getColorPopupBrush();
            this.setColorPopupBrush(b);

            if (this.selectedTool != null)
                this.selectedTool.CurrentBrush = b;
        }

        private void colorButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.closePopup)
            {
                this.colorPopup.IsOpen = false;
                this.closePopup = false;
            }
            else
            {
                this.colorPopup.IsOpen = true;
                this.closePopup = true;
            }
        }

        private void gradientSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.updatePopup)
            {
                Brush b = this.getColorPopupBrush();
                this.colorButton.Background = b;
                
                if (this.selectedTool != null)
                    this.selectedTool.CurrentBrush = b;
            }
        }

        private void startColorCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.updatePopup)
            {
                Brush b = this.getColorPopupBrush();
                this.colorButton.Background = b;

                if (this.selectedTool != null)
                    this.selectedTool.CurrentBrush = b;
            }
        }

        private void endColorCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.updatePopup)
            {
                Brush b = this.getColorPopupBrush();
                this.colorButton.Background = b;

                if (this.selectedTool != null)
                    this.selectedTool.CurrentBrush = b;
            }
        }

        private void clearBrushButton_Click(object sender, RoutedEventArgs e)
        {
            this.selectedTool.CurrentBrush = Brushes.Transparent;
            Brush b = this.selectedTool.CurrentBrush;
            this.setColorPopupBrush(b);
        }

        private void lineWidthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.updatePopup && this.selectedTool != null)
               this.selectedTool.CurrentLineWidth = this.lineWidthComboBox.SelectedIndex + 3.0;
        }
    }
}
