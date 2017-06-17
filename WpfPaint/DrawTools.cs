using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfPaint.ShapesExtension;

namespace WpfPaint.Tools
{
    public enum DrawToolType : byte
    {
        None = 0,
        Pointer,
        Rectangle,
        Polyline,
        Delete
    }

    public partial class DrawTool
    {
        private Window window; // окно (для получения координат мыши)
        private Canvas workspace; //  для рамки выделения
        private Border canvasBorder; // рамки рабочей области рисования (для позиционирования)
        private Canvas canvas; // рабочая область для рисования

        private Border border; // рамка выделения
        private ItemsControl dotsControl; // точки трансформации
        private DrawToolDots dots; // источник данных точек трансформации

        private Point? mousePos = null; // rкоордината нажатия кнопки мыши
                
        private Brush currentBrush; // текущая кисть
        private double currentLineWidth; // текущая толщина линии
        
        
        private DrawToolDot selectedDot = null;

        public Brush CurrentBrush
        {
            get
            {
                return this.currentBrush;
            }
            set // для обновления текущей фигуры
            {
                if (value != this.currentBrush)
                {
                    this.currentBrush = value;
                    if (this.Selection.Count == 1)
                    {
                        Shape s = this.Selection.First();
                        if (s is Path)
                            s.Style = DrawTool.CalculateRectangleStyle(this.currentBrush);
                        else if (s is Polyline)
                            s.Style = DrawTool.CalculatePolylineStyle(this.currentBrush, this.currentLineWidth);
                    }
                }
            }
        }

        public double CurrentLineWidth
        {
            get
            {
                return this.currentLineWidth;
            }
            set // для обновления текущей фигуры
            {
                if (value != this.currentLineWidth)
                {
                    this.currentLineWidth = value;
                    if (this.Selection.Count == 1)
                    {
                        Shape s = this.Selection.First();
                        if (s is Path)
                            s.Style = DrawTool.CalculateRectangleStyle(this.currentBrush);
                        else if (s is Polyline)
                            s.Style = DrawTool.CalculatePolylineStyle(this.currentBrush, this.currentLineWidth);
                    }

                }
            }
        }

        /// <summary>
        ///  Выделенные фигуры
        /// </summary>
        public List<Shape> Selection { get; private set; }
        public DrawToolType ToolType { get; private set; }

        private Shape lastShape = null; // текущая обрабатываемая фигура
        private Path lastShapeAsRect
        {
            get
            {
                return this.lastShape as Path;
            }
        } // тоже но приведенная к Path (чтобы не приводить каждый раз в коде)
        private Polyline lastShapeAsLine
        {
            get
            {
                return this.lastShape as Polyline;
            }
        } // тоже но приведенная к Polyline (чтобы не приводить каждый раз в коде)
        
        /// <summary>
        /// Событие возникает когда выделюятся новые фигуры/пропадает выделение
        /// </summary>
        public event EventHandler SelectionChange;
        /// <summary>
        /// конструктор
        /// </summary>
        public DrawTool(Window Window, Canvas Workspace, Border CanvasBorder, Canvas Canvas, DrawToolType type)
        {
            this.canvas = Canvas;
            this.window = Window;
            this.workspace = Workspace;
            this.canvasBorder = CanvasBorder;
            this.Selection = new List<Shape>();

            this.border = (Border)XamlReader.Parse(DrawTool.BorderControlXaml);
            this.dotsControl = (ItemsControl)XamlReader.Parse(DrawTool.DotsItemsControlXaml);
            this.dots = new DrawToolDots();
            this.dotsControl.Tag = this.dots;
            this.dotsControl.ItemsSource = this.dots.Dots;
            this.border.Visibility = Visibility.Hidden;

            this.workspace.Children.Add(this.border);
            this.canvas.Children.Add(this.dotsControl);
            this.SetToolType(type);
        }
        /// <summary>
        /// Рассчитывает стиль линии в зависимости от предоставленной кисти и толщины линии
        /// </summary>
        public static Style CalculatePolylineStyle(Brush brush, double stroke)
        {
            Brush b = (VisualBrush)XamlReader.Parse(DrawTool.HatchBrushXaml);
            Style style = new Style();

            style.TargetType = typeof(Polyline);
            style.Setters.Add(new Setter(Polyline.StrokeProperty, brush));
            style.Setters.Add(new Setter(Polyline.StrokeThicknessProperty, stroke));
            style.Setters.Add(new Setter(Polyline.StrokeLineJoinProperty, PenLineJoin.Round));

            MultiTrigger mt = new MultiTrigger();
            mt.Conditions.Add(new Condition(Polyline.IsMouseOverProperty, true));
            mt.Conditions.Add(new Condition(Polyline.TagProperty, ShapeTag.None));
            mt.Setters.Add(new Setter(Polyline.StrokeProperty, Brushes.Red));
            style.Triggers.Add(mt);

            Trigger t = new Trigger() { Property = Polyline.TagProperty, Value = ShapeTag.Select };
            t.Setters.Add(new Setter(Polyline.StrokeProperty, Brushes.Red));
            style.Triggers.Add(t);

            t = new Trigger() { Property = Polyline.TagProperty, Value = ShapeTag.Select | ShapeTag.Deleting };
            t.Setters.Add(new Setter(Polyline.StrokeProperty, b));
            style.Triggers.Add(t);

            t = new Trigger() { Property = Polyline.TagProperty, Value = ShapeTag.Deleting };
            t.Setters.Add(new Setter(Polyline.StrokeProperty, b));
            style.Triggers.Add(t);

            return style;
        }

        /// <summary>
        /// Рассчитывает стиль линии в зависимости от предоставленной кисти
        /// </summary>
        public static Style CalculateRectangleStyle(Brush brush)
        {
            Brush b = (VisualBrush)XamlReader.Parse(DrawTool.HatchBrushXaml);
            Style style = new Style();

            style.TargetType = typeof(Path);
            style.Setters.Add(new Setter(Path.FillProperty, brush));

            Trigger t = new Trigger() { Property = Path.TagProperty, Value = ShapeTag.Select };
            t.Setters.Add(new Setter(Path.StrokeProperty, Brushes.Red));
            t.Setters.Add(new Setter(Path.StrokeThicknessProperty, 2.0));
            style.Triggers.Add(t);

            MultiTrigger mt = new MultiTrigger();
            mt.Conditions.Add(new Condition(Path.IsMouseOverProperty, true));
            mt.Conditions.Add(new Condition(Path.TagProperty, ShapeTag.None));
            mt.Setters.Add(new Setter(Path.StrokeProperty, Brushes.Red));
            mt.Setters.Add(new Setter(Path.StrokeDashArrayProperty, new DoubleCollection(new double[] { 2, 2 })));
            mt.Setters.Add(new Setter(Path.StrokeThicknessProperty, 2.0));
            style.Triggers.Add(mt);

            t = new Trigger() { Property = Path.TagProperty, Value = ShapeTag.Select | ShapeTag.Deleting };
            t.Setters.Add(new Setter(Path.StrokeProperty, Brushes.Red));
            t.Setters.Add(new Setter(Path.StrokeThicknessProperty, 2.0));
            t.Setters.Add(new Setter(Path.FillProperty, b));
            style.Triggers.Add(t);

            t = new Trigger() { Property = Path.TagProperty, Value = ShapeTag.Deleting };
            t.Setters.Add(new Setter(Path.StrokeProperty, Brushes.Red));
            t.Setters.Add(new Setter(Path.StrokeThicknessProperty, 2.0));
            t.Setters.Add(new Setter(Path.FillProperty, b));
            style.Triggers.Add(t);

            return style;
        }

        /// <summary>
        /// Удаляет все фигуры
        /// </summary>
        public void ClearAll()
        {
            this.selectShape(null);
            this.selectedDot = null;
            this.lastShape = null;
            var shapes = this.canvas.Children.OfType<Shape>();
            while (shapes.Count() > 0)
            {
                this.canvas.Children.Remove(shapes.First());
            }

        }

        /// <summary>
        /// Отпускание кнопки мыши на точке трансформации
        /// </summary>
        private void dotsControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if ((e == null || e.ChangedButton == MouseButton.Left) && this.selectedDot != null)
            {
                var intersectedDots = this.dots.Dots
                        .Where(d => Math.Abs(d.DotID - this.selectedDot.DotID) == 1 && DrawToolDot.IsDotsIntersect(this.selectedDot, d));
                if (this.selectedDot.Parent.Source is Polyline)
                {
                    if (intersectedDots.Count() > 0)
                    {
                        Polyline line = this.selectedDot.Parent.Source as Polyline;
                        if (intersectedDots.Count() == line.Points.Count - 1)
                        {
                            this.canvas.Children.Remove(line);
                            this.selectShape(null);
                        }
                        else
                        {
                            int i = 0;
                            foreach (var dot in intersectedDots.OrderBy(d => d.DotID))
                            {
                                line.Points.RemoveAt(dot.DotID + i);
                                i--;
                            }
                            this.dots.SetSource(line);
                        }
                    }
                }
                else if (this.dots.Dots.Count(d => d.Point == this.selectedDot.Point) == 9) // удаляем фигуру если все 9 точек СОВПАДАЮТ
                {
                    this.canvas.Children.Remove(this.selectedDot.Parent.Source);
                    this.selectShape(null);
                }
                this.mousePos = null;
                this.selectedDot = null;
            }
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Отвязка и привязка событий к мыше и клавиатуре в зависимости от выбранного типа интрумента
        /// </summary>
        public void SetToolType(DrawToolType type)
        {
            switch (this.ToolType)
            {
                case DrawToolType.Pointer:
                    Mouse.RemoveMouseDownHandler(this.window, this.toolMouseDown);
                    Mouse.RemoveMouseUpHandler(this.window, this.toolMouseUp);
                    Mouse.RemoveMouseMoveHandler(this.window, this.toolMouseMove);
                    Keyboard.RemoveKeyDownHandler(this.window, this.toolKeyDown);
                    Keyboard.RemoveKeyUpHandler(this.window, this.toolKeyUp);
                    Mouse.RemoveMouseDownHandler(this.dotsControl, this.dotsControl_MouseDown);
                    Mouse.RemoveMouseUpHandler(this.dotsControl, this.dotsControl_MouseUp);
                    Mouse.RemoveMouseMoveHandler(this.dotsControl, this.dotsControl_MouseMove);
                    this.selectShapes(null);
                    break;

                case DrawToolType.Polyline:
                    Mouse.RemoveMouseDownHandler(this.canvas, this.polylineToolMouseDown);
                    Mouse.RemoveMouseUpHandler(this.window, this.polylineToolMouseUp);
                    Mouse.RemoveMouseMoveHandler(this.window, this.polylineToolMouseMove);
                    this.canvas.Cursor = null;
                    this.lastShape = null;
                    break;
                case DrawToolType.Rectangle:
                    Mouse.RemoveMouseDownHandler(this.canvas, this.rectangleToolMouseDown);
                    Mouse.RemoveMouseUpHandler(this.window, this.rectangleToolMouseMouseUp);
                    Mouse.RemoveMouseMoveHandler(this.window, this.rectangleToolMouseMove);
                    this.canvas.Cursor = null;
                    this.lastShape = null;
                    break;
                case DrawToolType.Delete:
                    Mouse.RemoveMouseDownHandler(this.canvas, this.deleteToolMouseDown);
                    Mouse.RemoveMouseMoveHandler(this.window, this.deleteToolMouseMove);
                    if (this.lastShape != null)
                    {
                        this.lastShape.SetDeletingStyle(false);
                        this.lastShape = null;
                    }
                    break;
            }

            this.ToolType = type;

            switch (this.ToolType)
            {
                case DrawToolType.Pointer:
                    Mouse.AddMouseDownHandler(this.dotsControl, this.dotsControl_MouseDown);
                    Mouse.AddMouseUpHandler(this.dotsControl, this.dotsControl_MouseUp);
                    Mouse.AddMouseDownHandler(this.window, this.toolMouseDown);
                    Mouse.AddMouseUpHandler(this.window, this.toolMouseUp);
                    Mouse.AddMouseMoveHandler(this.window, this.toolMouseMove);
                    Mouse.AddMouseMoveHandler(this.dotsControl, this.dotsControl_MouseMove);
                    Keyboard.AddKeyDownHandler(this.window, this.toolKeyDown);
                    Keyboard.AddKeyUpHandler(this.window, this.toolKeyUp);
                    break;

                case DrawToolType.Polyline:
                    Mouse.AddMouseDownHandler(this.canvas, this.polylineToolMouseDown);
                    Mouse.AddMouseUpHandler(this.window, this.polylineToolMouseUp);
                    Mouse.AddMouseMoveHandler(this.window, this.polylineToolMouseMove);
                    this.canvas.Cursor = Cursors.Cross;
                    break;
                case DrawToolType.Rectangle:
                    Mouse.AddMouseDownHandler(this.canvas, this.rectangleToolMouseDown);
                    Mouse.AddMouseUpHandler(this.window, this.rectangleToolMouseMouseUp);
                    Mouse.AddMouseMoveHandler(this.window, this.rectangleToolMouseMove);
                    this.canvas.Cursor = Cursors.Cross;
                    break;
                case DrawToolType.Delete:
                    Mouse.AddMouseDownHandler(this.canvas, this.deleteToolMouseDown);
                    Mouse.AddMouseMoveHandler(this.window, this.deleteToolMouseMove);
                    break;
            }
        }

        /// <summary>
        /// Удаляет выбранные  фигуры
        /// </summary>
        public void DeleteSelected()
        {
            foreach (var s in this.Selection)
                this.canvas.Children.Remove(s);

            this.selectShapes(null);
        }

        /// <summary>
        /// Возращает элемент над которым находится мышь
        /// </summary>
        private UIElement GetCanvasHoveredElement()
        {
            var elems = this.canvas.Children.OfType<UIElement>().Where(e => e.Visibility == Visibility.Visible && e.IsMouseOver);
            return elems.DefaultIfEmpty(null).First();
        }

        /// <summary>
        /// Нажатие кнопки мыши инструмента удаления
        /// </summary>
        private void deleteToolMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (this.lastShape != null && this.lastShape.IsMouseOver)
                {
                    this.canvas.Children.Remove(this.lastShape);
                }
                else
                {
                    var s = this.GetCanvasHoveredElement() as Shape;
                    if (s != null)
                    {
                        this.canvas.Children.Remove(s);
                    }
                }
                this.lastShape = null;
            }
        }

        /// <summary>
        /// Движение кнопки мыши инструмента удаления
        /// </summary>
        private void deleteToolMouseMove(object sender, MouseEventArgs e)
        {
            if (this.lastShape != null)
            {
                if (!this.lastShape.IsMouseOver)
                {
                    this.lastShape.SetDeletingStyle(false);
                    this.lastShape = null;

                    if (!this.canvas.IsMouseOver)
                        return;
                }
                else
                    return;
            }
            if (this.canvas.IsMouseOver)
            {
                var hovershape = this.GetCanvasHoveredElement() as Shape;
                if (hovershape != null)
                    hovershape.SetDeletingStyle(true);

                this.lastShape = hovershape;
            }
        }

        /// <summary>
        /// Нажатие кнопки мыши на точке трансформации
        /// </summary>
        private void dotsControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var ht = VisualTreeHelper.HitTest(this.dotsControl, Mouse.GetPosition(this.dotsControl));
            if (ht != null)
            {
                this.selectedDot = (ht.VisualHit as Rectangle).Tag as DrawToolDot;
                if (this.selectedDot.Parent.Source is Path && this.selectedDot.RectPoint == RectPoints.Center)
                {
                    Mouse.OverrideCursor = Cursors.ScrollWE;
                    return;
                }

            }
            Mouse.OverrideCursor = null;
        }
        
        /// <summary>
        /// Перемещеине указателя мыши над 4х угольником точки трансформации
        /// </summary>
        private void dotsControl_MouseMove(object sender, MouseEventArgs e)
        {
            // грязный хак, ItemsControl не прорисовывает свои элементы во время обработки MouseEvent. Событие MouseUp перехватывается удаленым(не прорисованным?) елементом и не возникает в коде
            if (this.selectedDot != null && e.LeftButton == MouseButtonState.Released)
                this.dotsControl_MouseUp(sender, null);
        }

        /// <summary>
        /// Нажатие кнопки мыши инструмента рисования линии
        /// </summary>
        private void polylineToolMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.mousePos = Mouse.GetPosition(this.canvas);
        }

        /// <summary>
        /// Перемещение мыши инструмента рисования линии
        /// </summary>
        private void polylineToolMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && this.mousePos.HasValue)
            {
                Point pos = e.GetPosition(this.canvas);
                if (this.lastShapeAsLine == null)
                {
                    Polyline line = new Polyline();
                    line.Style = DrawTool.CalculatePolylineStyle(this.CurrentBrush, this.CurrentLineWidth);
                    line.Points.Add(new Point(this.mousePos.Value.X, this.mousePos.Value.Y));
                    line.Points.Add(line.Points[0]);
                    line.Tag = ShapeTag.None;
                    this.lastShape = line;
                    this.canvas.Children.Add(line);
                }

                this.lastShapeAsLine.Points[1] = new Point(pos.X, pos.Y);
            }
        }

        /// <summary>
        /// Нажатие кнопки мыши инструмента рисования линии
        /// </summary>
        private void polylineToolMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.mousePos = null;
                if (this.lastShapeAsLine != null)
                {
                    if ((lastShapeAsLine.ActualWidth * lastShapeAsLine.ActualWidth + lastShapeAsLine.ActualHeight * lastShapeAsLine.ActualHeight) <= 4)
                        this.canvas.Children.Remove(lastShapeAsLine);
                    this.lastShape = null;
                }
            }
        }

        /// <summary>
        /// Нажатие кнопки мыши инструмента рисования 4х угольников
        /// </summary>
        private void rectangleToolMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.mousePos = Mouse.GetPosition(this.canvas);
            }
        }

        /// <summary>
        /// Отпускание кнопки мыши инструмента рисования 4х угольников
        /// </summary>
        private void rectangleToolMouseMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.mousePos = null;
                if (this.lastShapeAsRect != null)
                {
                    if ((this.lastShapeAsRect.ActualWidth + this.lastShapeAsRect.ActualHeight) <= 4)
                        this.canvas.Children.Remove(this.lastShapeAsRect);

                    this.lastShape = null;
                }
            }
        }

        /// <summary>
        /// Перемещение мыши инструмента рисования 4х угольников
        /// </summary>
        private void rectangleToolMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && this.mousePos.HasValue)
            {
                Point pos = Mouse.GetPosition(this.canvas);
                if (this.lastShapeAsRect == null)
                {
                    PolyLineSegment myPolyLineSegment = new PolyLineSegment();
                    myPolyLineSegment.Points = new PointCollection(new Point[] { pos, pos, pos, pos });

                    PathFigure pathFigure = new PathFigure();
                    pathFigure.IsClosed = true;
                    pathFigure.StartPoint = pos;
                    pathFigure.Segments.Add(myPolyLineSegment);

                    PathGeometry myPathGeometry = new PathGeometry();
                    myPathGeometry.Figures.Add(pathFigure);

                    Path rect = new Path();
                    rect.Style = Tools.DrawTool.CalculateRectangleStyle(this.currentBrush);
                    rect.Tag = ShapeTag.None;
                    rect.Data = myPathGeometry;
                    this.canvas.Children.Add(rect);
                    this.lastShape = rect;
                }
                else
                {
                    PolyLineSegment line = this.lastShapeAsRect.GetSegment();

                    double w = pos.X - this.mousePos.Value.X;
                    double h = pos.Y - this.mousePos.Value.Y;
                    Point p1 = line.Points[1];
                    Point p2 = line.Points[2];
                    Point p3 = line.Points[3];

                    p1.Offset(w, 0);
                    p2.Offset(w, h);
                    p3.Offset(0, h);

                    line.Points[1] = p1;
                    line.Points[2] = p2;
                    line.Points[3] = p3;
                    this.mousePos = pos;
                }
            }
        }
        
        /// <summary>
        /// Выделение одной фигуры
        /// </summary>
        private void selectShape(Shape s)
        {
            this.Selection.ClearShapes();
            if (s != null)
            {
                this.Selection.AddShape(s);
                if (s is Path)
                {
                    Style style = s.Style;
                    var sett = style.Setters.OfType<Setter>().Where(ss => ss.Property == Path.FillProperty);
                    if (sett != null && sett.Count() > 0)
                        this.CurrentBrush = (sett.First().Value as Brush);
                }
                else if (s is Polyline)
                {
                    Style style = s.Style;
                    var sett = style.Setters.OfType<Setter>().Where(ss => ss.Property == Polyline.StrokeProperty);
                    if (sett != null && sett.Count() > 0)
                        this.CurrentBrush = (sett.First().Value as Brush);

                    sett = style.Setters.OfType<Setter>().Where(ss => ss.Property == Polyline.StrokeThicknessProperty);
                    if (sett != null && sett.Count() > 0)
                        this.CurrentLineWidth = (double)sett.First().Value;
                }
            }
            this.dots.SetSource(s);
            if (this.SelectionChange != null)
                this.SelectionChange(this, new EventArgs());
        }

        /// <summary>
        /// Выделение нескольких фигур
        /// </summary>
        private void selectShapes(IEnumerable<Shape> shapes)
        {
            if (shapes != null && shapes.Count() == 1)
                this.selectShape(shapes.First());
            else
            {
                this.Selection.ClearShapes();
                this.Selection.AddShapes(shapes);
                this.dots.SetSource(null);
                if (this.SelectionChange != null)
                    this.SelectionChange(this, new EventArgs());
            }
        }

        /// <summary>
        /// Нажатие кнопки Shift
        /// </summary>
        private void toolKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.LeftShift || e.Key == Key.RightShift) && !e.IsRepeat)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    this.mousePos = Mouse.GetPosition(this.window);
                    this.border.Width = 0;
                    this.border.Height = 0;

                    this.border.SetValue(Canvas.LeftProperty, this.mousePos.Value.X);
                    this.border.SetValue(Canvas.TopProperty, this.mousePos.Value.Y);

                    this.border.Visibility = Visibility.Visible;
                    this.dotsControl.Visibility = Visibility.Hidden;

                    this.selectShapes(null);
                }
            }
        }

        /// <summary>
        /// Отпускание кнопки Shift
        /// </summary>
        private void toolKeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.LeftShift || e.Key == Key.RightShift) && !e.IsRepeat)
            {
                this.border.Visibility = Visibility.Hidden;
                this.mousePos = null;
            }
        }

        /// <summary>
        /// Нажатие кнопки мыши инструмента трансформации
        /// </summary>
        private void toolMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.mousePos = Mouse.GetPosition(this.window);
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) // надатие мыши при нажатой кнопке shift
                {
                    this.border.SetValue(Canvas.LeftProperty, this.mousePos.Value.X);
                    this.border.SetValue(Canvas.TopProperty, this.mousePos.Value.Y);
                    this.border.Width = 0;
                    this.border.Height = 0;

                    this.border.Visibility = Visibility.Visible;

                    this.selectShapes(null);
                    Mouse.OverrideCursor = null;
                }
                else if (e.ClickCount > 1 && this.Selection.Count == 1) // двойной щелчок по линии
                {
                    Polyline line = this.Selection.First() as Polyline;
                    if (line != null)
                    {
                        Point topleft = e.GetPosition(this.canvas);
                        topleft.Offset(-2, -2); // немного расширяем область для более комфортного нажатия на линию
                        Point bottomright = new Point(topleft.X + 5, topleft.Y + 5);
                        for (int i = line.Points.Count - 1; i >= 1; i--)
                        {
                            if (ShapesHelper.CheckLineLineIntersection(topleft, bottomright, line.Points[i - 1], line.Points[i]))
                            {
                                line.Points.Insert(i, topleft);
                                this.dots.SetSource(line);
                                break;
                            }
                        }
                    }
                }
                else 
                {
                    var s = this.GetCanvasHoveredElement();
                    if (s != null) // выделение 1 фигуры
                    {
                        if (s != this.dotsControl) // Если не точка транфсормации
                        {
                            if (!this.Selection.Contains(s))
                                this.selectShape(s as Shape);
                            this.selectedDot = null;
                            Mouse.OverrideCursor = Cursors.SizeAll;
                        }
                    }
                    else // Щелчок по пустой области
                    {
                        this.selectShapes(null);
                        Mouse.OverrideCursor = Cursors.ScrollAll;
                    }
                }
            }
        }

        /// <summary>
        /// Перемещение кнопки мыши инструмента трансформации
        /// </summary>
        private void toolMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && this.mousePos != null)
            {
                Point lastPos = Mouse.GetPosition(this.window);
                if (lastPos == this.mousePos.Value)
                    return;

                if (this.border.Visibility == Visibility.Visible) // Выделение
                {
                    this.toolSelection(lastPos);
                }
                else if (this.selectedDot != null) // Трансформация
                {
                    if (this.selectedDot.Parent.Source is Polyline) // перемещеине узлов линий
                    {
                        this.toolPolylineTransform(lastPos);
                    }
                    else if (this.selectedDot.Parent.Source is Path) //трансформация 4х угольника
                    {
                        Path path = this.selectedDot.Parent.Source as Path;
                        if (this.selectedDot.RectPoint == RectPoints.Center) // поворот
                            this.toolRotateRect(lastPos, path);
                        else // изменение размера
                            this.toolResizeRect(lastPos, path);
                    }
                    // обновляем положеие точек после трансформации + сохраняем выделенной такую же точку что и в старой коллекции
                    int olddot = this.selectedDot.DotID;
                    this.dots.SetSource(this.selectedDot.Parent.Source);
                    if (this.selectedDot != this.dots.Dots[olddot])
                        this.selectedDot = this.dots.Dots[olddot];

                }
                else if (this.Selection.Count > 0) // Перемещение фигур
                {
                    this.toolMoveShapes(lastPos);
                }
                else if (this.canvas.IsMouseOver) // перемещение рабочей области рисования
                {
                    this.toolMoveCanvas(lastPos);
                }
            }
        }

        /// <summary>
        ///  Перемещение рабочей области рисования  при движении мыши
        /// </summary>
        private void toolMoveCanvas(Point lastPos)
        {
            Point borderPos = new Point(Canvas.GetLeft(this.canvasBorder), Canvas.GetTop(this.canvasBorder));

            double left = borderPos.X + lastPos.X - this.mousePos.Value.X;
            double top = borderPos.Y + lastPos.Y - this.mousePos.Value.Y;

            if (left + this.canvas.ActualWidth < 100)
                left = -this.canvas.ActualWidth + 100;
            else if (left > this.workspace.ActualWidth - 100)
                left = this.workspace.ActualWidth - 100;

            if (top + this.canvas.ActualHeight < 100)
                top = -this.canvas.ActualHeight + 100;
            else if (top > this.workspace.ActualHeight - 100)
                top = this.workspace.ActualHeight - 100;

            this.mousePos = lastPos;

            if (left != Canvas.GetLeft(this.canvasBorder))
                Canvas.SetLeft(this.canvasBorder, left);
            if (top != Canvas.GetTop(this.canvasBorder))
                Canvas.SetTop(this.canvasBorder, top);
        }

        /// <summary>
        ///  Перемещение фигур  при движении мыши
        /// </summary>
        private void toolMoveShapes(Point lastPos)
        {
            Point move = new Point(lastPos.X - this.mousePos.Value.X, lastPos.Y - this.mousePos.Value.Y);
            //some linq magic
            this.Selection.ForEach(s =>
            {
                if (s is Path)
                {
                    PolyLineSegment line = (s as Path).GetSegment();
                    for (int i = 0; i < line.Points.Count; i++)
                    {
                        Point pt = line.Points[i];
                        pt.Offset(move.X, move.Y);
                        if (i > 0)
                            line.Points[i] = pt;
                        else
                            (s as Path).SetTopLeft(pt);
                    }
                }
                else if (s is Polyline)
                {
                    Polyline line = (s as Polyline);
                    for (int i = 0; i < line.Points.Count; i++)
                    {
                        Point pt = line.Points[i];
                        pt.Offset(move.X, move.Y);
                        line.Points[i] = pt;
                    }
                }
            });

            if (this.Selection.Count() == 1)
                this.dots.SetSource(this.Selection.First());
            this.mousePos = lastPos;
        }

        /// <summary>
        ///  Изменение размеров  4х угольника  при движении мыши
        /// </summary>
        private void toolResizeRect(Point lastPos, Path path)
        {
            RotateTransform rt = null;
            Point move = new Point(this.mousePos.Value.X - lastPos.X, this.mousePos.Value.Y - lastPos.Y);

            Vector v = new Vector(1, 0);
            Point p1 = this.dots.Dots[RectPoints.TopLeft].Point;
            Point p2 = this.dots.Dots[RectPoints.TopRight].Point;
            p2.Offset(-p1.X, -p1.Y);
            Vector rotated = new Vector(p2.X, p2.Y);
            double angle = Vector.AngleBetween(v, rotated); // угол между нормальным вектором и вектором по верхней стороне 4х угольника
            if (angle != 0) // если 4х угольник повернут, то разворачиваем его назад и приводим к номральному виду
            {
                rt = new RotateTransform(-angle, this.dots.Dots[RectPoints.Center].X, this.dots.Dots[RectPoints.Center].Y);
                path.Transform(rt);

                angle *= Math.PI / 180; // в радианы

                // Если перемещаем одну из точек в вершине 4х угольника, то координаты перемещения также разворачиваем и приводим к нормальному виду
                if (this.selectedDot.RectPoint == RectPoints.TopLeft || this.selectedDot.RectPoint == RectPoints.TopRight || this.selectedDot.RectPoint == RectPoints.BottomLeft || this.selectedDot.RectPoint == RectPoints.BottomRight)
                {
                    Point rp = rt.Transform(lastPos);
                    Point rpos = rt.Transform(this.mousePos.Value);
                    move = new Point(rpos.X - rp.X, rpos.Y - rp.Y);
                }
            }
            var rect = path.GetSegment();
            var rectPoints = rect.Points.ToArray();
            Point size = new Point(rectPoints[1].X - rectPoints[0].X, rectPoints[2].Y - rectPoints[1].Y);

            ScaleTransform st = new ScaleTransform();
            // задаем параметры трансформации в зависимости от используемой точки
            switch (this.selectedDot.RectPoint)
            {
                case RectPoints.Left:

                    move.X = move.X * Math.Cos(angle) + move.Y * Math.Sin(angle); // проекция на ось Y разницы координат
                    if (size.X == 0) //  защита от схлапывания
                    {
                        path.SetTopLeft(new Point(rectPoints[0].X - move.X, rectPoints[0].Y));
                        rect.Points[3] = new Point(rectPoints[3].X - move.X, rectPoints[3].Y);
                        st = null;
                    }
                    else
                    {
                        st.CenterX = rectPoints[1].X;
                        st.CenterY = (rectPoints[2].Y - rectPoints[1].Y) / 2;
                        st.ScaleX = 1 + move.X / size.X;
                    }
                    break;
                case RectPoints.Right:
                    move.X = move.X * Math.Cos(angle) + move.Y * Math.Sin(angle); // проекция на ось Y разницы координат
                    if (size.X == 0) //  защита от схлапывания
                    {
                        rect.Points[1] = new Point(rectPoints[1].X - move.X, rectPoints[1].Y);
                        rect.Points[2] = new Point(rectPoints[2].X - move.X, rectPoints[2].Y);
                        st = null;
                    }
                    else
                    {
                        st.CenterX = rectPoints[0].X;
                        st.CenterY = (rectPoints[3].Y - rectPoints[0].Y) / 2;
                        st.ScaleX = 1 + move.X / -size.X;
                    }
                    break;
                case RectPoints.Top:
                    move.Y = move.X * Math.Sin(-angle) + move.Y * Math.Cos(-angle); // проекция на ось Х разницы координат
                    if (size.Y == 0) //  защита от схлапывания
                    {
                        path.SetTopLeft(new Point(rectPoints[0].X, rectPoints[0].Y - move.Y));
                        rect.Points[1] = new Point(rectPoints[1].X, rectPoints[1].Y - move.Y);
                        st = null;
                    }
                    else
                    {
                        st.CenterX = (rectPoints[3].X - rectPoints[2].X) / 2;
                        st.CenterY = rectPoints[2].Y;
                        st.ScaleY = 1 + move.Y / size.Y;
                    }
                    break;
                case RectPoints.Bottom:
                    move.Y = move.X * Math.Sin(-angle) + move.Y * Math.Cos(-angle); // проекция на ось Х разницы координат
                    if (size.Y == 0) //  защита от схлапывания
                    {
                        rect.Points[2] = new Point(rectPoints[2].X, rectPoints[2].Y - move.Y);
                        rect.Points[3] = new Point(rectPoints[3].X, rectPoints[3].Y - move.Y);
                        st = null;
                    }
                    else
                    {
                        st.CenterX = (rectPoints[1].X - rectPoints[0].X) / 2;
                        st.CenterY = rectPoints[0].Y;
                        st.ScaleY = 1 + move.Y / -size.Y;
                    }
                    break;
                case RectPoints.TopLeft:
                    if (size.Y == 0) //  защита от схлапывания
                    {
                        path.SetTopLeft(new Point(rectPoints[0].X, rectPoints[0].Y - move.Y));
                        rect.Points[1] = new Point(rectPoints[1].X, rectPoints[1].Y - move.Y);
                    }
                    else
                        st.ScaleY = 1 + move.Y / size.Y;
                    if (size.X == 0) //  защита от схлапывания
                    {
                        path.SetTopLeft(new Point(rectPoints[0].X - move.X, rect.Points[0].Y));
                        rect.Points[3] = new Point(rectPoints[3].X - move.X, rectPoints[3].Y);
                    }
                    else
                        st.ScaleX = 1 + move.X / size.X;
                    if (st.ScaleX != 0 || st.ScaleY != 0)
                    {
                        st.CenterX = rectPoints[2].X;
                        st.CenterY = rectPoints[2].Y;
                    }
                    else
                        st = null;
                    break;
                case RectPoints.TopRight:
                    if (size.Y == 0) //  защита от схлапывания
                    {
                        path.SetTopLeft(new Point(rectPoints[0].X, rectPoints[0].Y - move.Y));
                        rect.Points[1] = new Point(rectPoints[1].X, rectPoints[1].Y - move.Y);
                    }
                    else
                        st.ScaleY = 1 + move.Y / size.Y;
                    if (size.X == 0) //  защита от схлапывания
                    {
                        rect.Points[1] = new Point(rectPoints[1].X - move.X, rect.Points[1].Y);
                        rect.Points[2] = new Point(rectPoints[2].X - move.X, rectPoints[2].Y);
                    }
                    else
                        st.ScaleX = 1 + move.X / -size.X;
                    if (st.ScaleX != 0 || st.ScaleY != 0)
                    {
                        st.CenterX = rectPoints[3].X;
                        st.CenterY = rectPoints[3].Y;
                    }
                    else
                        st = null;
                    break;
                case RectPoints.BottomRight:
                    if (size.Y == 0) //  защита от схлапывания
                    {
                        rect.Points[2] = new Point(rectPoints[2].X, rectPoints[2].Y - move.Y);
                        rect.Points[3] = new Point(rectPoints[3].X, rectPoints[3].Y - move.Y);
                    }
                    else
                        st.ScaleY = 1 + move.Y / -size.Y;
                    if (size.X == 0) //  защита от схлапывания
                    {
                        rect.Points[1] = new Point(rectPoints[1].X - move.X, rectPoints[1].Y);
                        rect.Points[2] = new Point(rectPoints[2].X - move.X, rect.Points[2].Y);
                    }
                    else
                        st.ScaleX = 1 + move.X / -size.X;
                    if (st.ScaleX != 0 || st.ScaleY != 0)
                    {
                        st.CenterX = rectPoints[0].X;
                        st.CenterY = rectPoints[0].Y;
                    }
                    else
                        st = null;
                    break;
                case RectPoints.BottomLeft:
                    if (size.Y == 0) //  защита от схлапывания
                    {
                        rect.Points[2] = new Point(rectPoints[2].X, rectPoints[2].Y - move.Y);
                        rect.Points[3] = new Point(rectPoints[3].X, rectPoints[3].Y - move.Y);
                    }
                    else
                        st.ScaleY = 1 + move.Y / -size.Y;
                    if (size.X == 0) //  защита от схлапывания
                    {
                        path.SetTopLeft(new Point(rectPoints[0].X - move.X, rectPoints[0].Y));
                        rect.Points[3] = new Point(rectPoints[3].X - move.X, rect.Points[3].Y);
                    }
                    else
                        st.ScaleX = 1 + move.X / size.X;
                    if (st.ScaleX != 0 || st.ScaleY != 0)
                    {
                        st.CenterX = rectPoints[1].X;
                        st.CenterY = rectPoints[1].Y;
                    }
                    else
                        st = null;
                    break;
            }

            path.Transform(st);
            if (rt != null)
                path.Transform(rt.Inverse);
            this.mousePos = lastPos;
        }

        /// <summary>
        ///  Поворот 4х угольника  при движении мыши
        /// </summary>
        private void toolRotateRect(Point lastPos, Path path)
        {
            RotateTransform rt = new RotateTransform();
            rt.CenterX = this.selectedDot.X;
            rt.CenterY = this.selectedDot.Y;
            rt.Angle = lastPos.X - this.mousePos.Value.X;
            path.Transform(rt);

            this.mousePos = lastPos;
        }

        /// <summary>
        ///  Перемещение точек трансформации линии  при движении мыши
        /// </summary>
        private void toolPolylineTransform(Point lastPos)
        {
            Point move = new Point(lastPos.X - this.mousePos.Value.X, lastPos.Y - this.mousePos.Value.Y);

            Polyline line = this.selectedDot.Parent.Source as Polyline;

            Point pt = line.Points[this.selectedDot.DotID];
            pt.Offset(move.X, move.Y);
            line.Points[this.selectedDot.DotID] = pt;
            this.mousePos = lastPos;
        }

        /// <summary>
        ///  Изменение рамки выделения при движении мыши
        /// </summary>
        private void toolSelection(Point lastPos)
        {
            Rect selectionRect = new Rect(Math.Min(lastPos.X, this.mousePos.Value.X), Math.Min(lastPos.Y, this.mousePos.Value.Y), Math.Abs(lastPos.X - this.mousePos.Value.X), Math.Abs(lastPos.Y - this.mousePos.Value.Y));
            Rect canvasRect = new Rect(Canvas.GetLeft(this.canvasBorder), Canvas.GetTop(this.canvasBorder), this.canvas.ActualWidth, this.canvas.ActualHeight);

            if (lastPos.X < this.mousePos.Value.X)
                this.border.SetValue(Canvas.LeftProperty, selectionRect.X);
            if (lastPos.Y < this.mousePos.Value.Y)
                this.border.SetValue(Canvas.TopProperty, selectionRect.Y);
            this.border.Width = selectionRect.Width;
            this.border.Height = selectionRect.Height;

            selectionRect.Intersect(canvasRect);

            if (!selectionRect.IsEmpty)
            {
                selectionRect.Offset(-canvasRect.X, -canvasRect.Y);
                var ss = this.canvas.Children.OfType<Shape>().Where(s => s.IntersectWith(selectionRect));
                this.selectShapes(ss);
            }
            else
                this.selectShapes(null);
        }

        /// <summary>
        /// Отпускание кнопки мыши инструмента трансформации
        /// </summary>
        private void toolMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (this.border.Visibility == Visibility.Visible)
                    this.border.Visibility = Visibility.Hidden;

                this.mousePos = null;
                this.selectedDot = null;
                Mouse.OverrideCursor = null;
            }
        }
    }
}
