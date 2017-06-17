using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace WpfPaint
{
    public partial class MainWindow
    {
        private void cmd_Open(object sender, ExecutedRoutedEventArgs e)
        {
            this.colorPopup.IsOpen = false;
            this.selectedTool.SetToolType(Tools.DrawToolType.Pointer);
            this.selectedTool.ClearAll();
            this.UpdateButtons();
            
            OpenFileDialog open = new OpenFileDialog();
            open.CheckFileExists = true;
            open.CheckPathExists = true;
            open.ValidateNames = true;
            open.Multiselect = false;
            open.DefaultExt = ".xml";
            open.Filter = "Файл WpfPaint|*.xml";
            open.FilterIndex = 0;

            if (open.ShowDialog().Value)
                this.Load(open.FileName);
        }

        private void cmd_Save(object sender, ExecutedRoutedEventArgs e)
        {
            this.colorPopup.IsOpen = false;
            this.selectedTool.SetToolType(Tools.DrawToolType.Pointer);
            this.UpdateButtons();

            SaveFileDialog save = new SaveFileDialog();
            save.CheckPathExists = true;
            save.ValidateNames = true;
            save.AddExtension = true;
            save.OverwritePrompt = true;
            save.DefaultExt = ".xml";
            save.Filter = "Файл WpfPaint|*.xml";
            save.FilterIndex = 0;

            if (save.ShowDialog().Value)
                this.Save(save.FileName);
        }

        private void cmd_Pointer(object sender, ExecutedRoutedEventArgs e)
        {
           this.selectedTool.SetToolType(Tools.DrawToolType.Pointer);
           this.isGradientCB.IsEnabled = true;
           this.UpdateButtons();
        }

        private void cmd_AddPolyline(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.selectedTool.ToolType == Tools.DrawToolType.Polyline)
                this.selectedTool.SetToolType(Tools.DrawToolType.Pointer);
            else
            {
                this.selectedTool.SetToolType(Tools.DrawToolType.Polyline);
                this.isGradientCB.IsEnabled = false;
                this.isGradientCB.IsChecked = false;
                Brush b = this.getColorPopupBrush();
                this.setColorPopupBrush(b);
                this.selectedTool.CurrentBrush = b;
            }

            this.UpdateButtons();
        }

        private void cmd_AddRectangle(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.selectedTool.ToolType == Tools.DrawToolType.Rectangle)
                this.selectedTool.SetToolType(Tools.DrawToolType.Pointer);
            else
            {
                this.selectedTool.SetToolType(Tools.DrawToolType.Rectangle);
                this.isGradientCB.IsEnabled = true;
            }

            this.UpdateButtons();
        }

        private void cmd_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.selectedTool.ToolType == Tools.DrawToolType.Delete)
                this.selectedTool.SetToolType(Tools.DrawToolType.Pointer);
            else if (this.selectedTool.Selection.Count > 1)
                this.selectedTool.DeleteSelected();
            else
                this.selectedTool.SetToolType(Tools.DrawToolType.Delete);
            this.UpdateButtons();
        }
    }

    public class Commands
    {
        /// <summary>
        /// Загрузить рисунок из файла
        /// </summary>
        public static RoutedCommand Open { get; set; } 
        /// <summary>
        /// Сохранить рисунок в файл
        /// </summary>
        public static RoutedCommand Save { get; set; }
        /// <summary>
        /// Трансформировать/переместить/повернуть/выделить фигуру
        /// </summary>
        public static RoutedCommand Pointer { get; set; }
        /// <summary>
        /// Добавить линию
        /// </summary>
        public static RoutedCommand AddPolyline { get; set; }
        /// <summary>
        /// Добавить 4х угольник
        /// </summary>
        public static RoutedCommand AddRectangle { get; set; }
        /// <summary>
        /// Удалить фигуру
        /// </summary>
        public static RoutedCommand Delete { get; set; }

        static Commands()
        {
            Commands.Open = new RoutedCommand("Open", typeof(Commands));
            Commands.Save = new RoutedCommand("Save", typeof(Commands));
            Commands.Pointer = new RoutedCommand("Pointer", typeof(Commands));
            Commands.AddPolyline = new RoutedCommand("AddPolyline", typeof(Commands));
            Commands.AddRectangle = new RoutedCommand("AddRectangle", typeof(Commands));
            Commands.Delete = new RoutedCommand("Delete", typeof(Commands));
        }
    }
}