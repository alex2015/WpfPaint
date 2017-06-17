using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Shapes;
using WpfPaint.ShapesExtension;

namespace WpfPaint.Tools
{
    public class DrawToolDots
    {
        /// <summary>
        /// Фигура которой соответсвуют  точки
        /// </summary>
        public Shape Source {get; private set;}
        
        /// <summary>
        /// Размер стороны 4х угольника точки
        /// </summary>
        public double DotSize { get; private set; }

        public DrawToolDotsCollection Dots { get; private set; }
       
        /// <summary>
        /// Конструктор без привязки к источнику
        /// </summary>
        public DrawToolDots()
        {
            this.Dots = new DrawToolDotsCollection();
        }

        /// <summary>
        /// Конструктор. Создает DrawToolDots и привязывает к источнику
        /// </summary>
        public DrawToolDots(Shape s)
            : this()
        {
            this.SetSource(s);
        }

        /// <summary>
        /// Устанавливаем новый источник координат, обновляем существующий или удаляем источник
        /// </summary>
        public void SetSource(Shape shape)
        {
            this.Source = shape;
            this.DotSize = 9;
            this.Dots.Clear();

            if (shape != null)
            {
                var sett = shape.Style.Setters.OfType<Setter>().Where(ss => ss.Property == Polyline.StrokeThicknessProperty);
                if (sett != null && sett.Count() > 0)
                    this.DotSize = 9 + (double)sett.First().Value;

                if (shape is Path)
                    this.setRectangleSource(shape as Path);
                else if (shape is Polyline)
                    this.setPolylineSource(shape as Polyline);
            }
        }

        private void setRectangleSource(Path r)
        {
            var rectDots = r.GetSegment().Points.ToArray(); // координаты углов четырехугольника
            var rectSides = new Point[] {
                new Point(Math.Min(rectDots[0].X, rectDots[1].X) + Math.Abs(rectDots[0].X - rectDots[1].X)/2, Math.Min(rectDots[0].Y, rectDots[1].Y) + Math.Abs(rectDots[0].Y - rectDots[1].Y)/2), // topleft
                new Point(Math.Min(rectDots[1].X, rectDots[2].X) + Math.Abs(rectDots[1].X - rectDots[2].X)/2, Math.Min(rectDots[1].Y, rectDots[2].Y) + Math.Abs(rectDots[1].Y - rectDots[2].Y)/2), // topright
                new Point(Math.Min(rectDots[2].X, rectDots[3].X) + Math.Abs(rectDots[2].X - rectDots[3].X)/2, Math.Min(rectDots[2].Y, rectDots[3].Y) + Math.Abs(rectDots[2].Y - rectDots[3].Y)/2), // bottomright
                new Point(Math.Min(rectDots[3].X, rectDots[0].X) + Math.Abs(rectDots[3].X - rectDots[0].X)/2, Math.Min(rectDots[3].Y, rectDots[0].Y) + Math.Abs(rectDots[3].Y - rectDots[0].Y)/2), //  bottomleft
                new Point(Math.Min(rectDots[0].X, rectDots[2].X) + Math.Abs(rectDots[0].X - rectDots[2].X)/2, Math.Min(rectDots[3].Y, rectDots[1].Y) + Math.Abs(rectDots[3].Y - rectDots[1].Y)/2)  // center
            }; // координаты точек на гранях четырехугольника и центральной точки
            
            Point[] result = new Point[] {
                rectSides[4],
                rectDots[0],
                rectSides[0],
                rectDots[1],
                rectSides[1],
                rectDots[2],
                rectSides[2],
                rectDots[3],
                rectSides[3]
            };
            this.Dots.AddPoints(this, result);
        }

        private void setPolylineSource(Polyline p)
        {
            this.Dots.AddPoints(this, p.Points);
        }
               
    }

    public class DrawToolDotsCollection : ObservableCollection<DrawToolDot>
    {
        /// <summary>
        /// Добавляет точки в коллекцию. DotID каждой новой точки увеличивается на еденицу
        /// </summary>
        public void AddPoints(DrawToolDots parent, IEnumerable<Point> points)
        {
            int max = this.DefaultIfEmpty(DrawToolDot.Empty).Max(p => p.DotID) + 1;

            foreach (Point p in points)
            {
                base.Add(new DrawToolDot(parent, max, p.X, p.Y));
                max++;
            }
        }

        /// <summary>
        /// Индекстатор
        /// </summary>
        public new DrawToolDot this[int ID]
        {
            get
            {
                return this.Where(p => p.DotID == ID).DefaultIfEmpty(DrawToolDot.Empty).First();
            }
        }

        /// <summary>
        /// Индексатор для четырехугольника
        /// </summary>
        public DrawToolDot this[RectPoints pt]
        {
            get
            {
                return this[(int)pt];
            }
        }
    }

    public class DrawToolDot
    {
        /// <summary>
        /// Родитель  к которому принадлежит точка. Используется для доступа к соседним точкам, фигуре-источнике координат и DotSize в IsDotsIntersect
        /// </summary>
        public DrawToolDots Parent { get; private set; }

        /// <summary>
        /// Пустая точка, без родителя и источника. Используется в лямда-выражениях linq
        /// </summary>
        public static DrawToolDot Empty
        {
            get
            {
                return new DrawToolDot(null, -1);
            }
        }

        /// <summary>
        /// Порядковый номер точки
        /// </summary>
        public int DotID { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>
        /// X, Y в виде Point
        /// </summary>
        public Point Point
        {
            get
            {
                return new Point(this.X, this.Y);
            }
        }

        /// <summary>
        /// DotID в виде RectPoint
        /// </summary>
        public RectPoints RectPoint
        {
            get
            {
                if (this.DotID <= 8)
                    return (RectPoints)this.DotID;
                else
                    return RectPoints.None;
            }
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public DrawToolDot(DrawToolDots parent, int id, double x = 0, double y = 0)
        {
            this.Parent = parent;
            this.DotID = id;
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Проверяет 2 точки на пересечение. Точки считаются пересекающимеся когда их прямоугольники пересекаются
        /// </summary>
        public static bool IsDotsIntersect(DrawToolDot d1, DrawToolDot d2)
        {
            double size = d1.Parent.DotSize;
            return Math.Abs(d1.X - d2.X) <= size && Math.Abs(d1.Y - d2.Y) <= size;
        }
    }

    /// <summary>
    /// Название точек прямоугольника для упрощения доступа
    /// </summary>
    public enum RectPoints : int
    {
        None = -1,
        TopLeft = 1,
        Top = 2,
        TopRight = 3,
        Right = 4,
        BottomRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        Left = 8,
        Center = 0
    }
    
    /// <summary>
    /// Конвертер для привязки координат TopLeft прямоугольников DrawToolDots к координатам Path или Polyline. Сдвигает координаты прямоугольника, так чтобы центр приходился на координаты точки фигуры.
    /// </summary>
    public class PointsToDotsCoorsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue || !(values[0] is double) || !(values[1] is double))
                return DependencyProperty.UnsetValue;

            return (double)values[0] - (double)values[1] / 2;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null; // обратное преобразование нигде не используется
        }
    }
}