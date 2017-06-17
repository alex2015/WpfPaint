using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfPaint.ShapesExtension
{
    /// <summary>
    /// Класс-расширение для упрощения работы с фигурами и коллекциями фигур
    /// </summary>
    public static class ShapesHelper
    {
        /// <summary>
        /// Устанавливаем признак удаления фигуры. Фигуры у которых Tag содержит ShapeTag.Deleting отрисовываются специальной кистью согласно стилю фигур
        /// </summary>
        public static void SetDeletingStyle(this Shape s, bool set)
        {
            if (set)
            {
                s.Tag = s.Tag == null ? // Если Tag раньше не устанавливался то ставим признак сразу, в проитвном случае просто затираем бит признака
                    ShapeTag.Deleting : ((ShapeTag)s.Tag | ShapeTag.Deleting); 
                s.Cursor = System.Windows.Input.Cursors.Hand;
            }
            else
            {
                s.Tag = s.Tag == null ? ShapeTag.None : (((ShapeTag)s.Tag | ShapeTag.Deleting) ^ ShapeTag.Deleting);
                s.Cursor = null;
            }
            
        }

        public static void SetDeletingStyle(this List<Shape> list, bool set)
        {
            if (set) // чтобы не выполнять проверку на каждой итерации немного увеличим код
                foreach (var s in list)
                {
                    s.Tag = s.Tag == null ? ShapeTag.Deleting : ((ShapeTag)s.Tag | ShapeTag.Deleting);
                    s.Cursor = System.Windows.Input.Cursors.Hand;
                }
            else
                foreach (var s in list)
                {
                    s.Tag = s.Tag == null ? ShapeTag.None : (((ShapeTag)s.Tag | ShapeTag.Deleting) ^ ShapeTag.Deleting);
                    s.Cursor = null;
                }
        }

        /// <summary>
        ///  добавляем в список еще одну фигуру, устанавливаем ей признак ShapeTag.Select, убираем ShapeTag.Deleting
        /// </summary>
        public static void AddShape(this List<Shape> list, Shape shape)
        {
            if (shape != null)
            {
                if (!list.Contains(shape))
                    list.Add(shape);
                shape.Tag = shape.Tag == null ? ShapeTag.Select : ((ShapeTag)shape.Tag | ShapeTag.Select);
                shape.Tag = ((ShapeTag)shape.Tag | ShapeTag.Deleting) ^ ShapeTag.Deleting;
            }
        }

        /// <summary>
        /// Добавляем нескоько фигур 
        /// </summary>
        public static void AddShapes(this List<Shape> list, IEnumerable<Shape> shapes)
        {
            if (shapes != null)
                foreach (var s in shapes)
                    list.AddShape(s);
        }

        /// <summary>
        /// Убираем фигуру из списка и удаляем ее признак ShapeTag.Select
        /// </summary>
        public static void RemoveShape(this List<Shape> list, Shape shape)
        {
            if (shape != null)
            {
                list.Remove(shape);
                shape.Tag = shape.Tag == null ? ShapeTag.None : (((ShapeTag)shape.Tag | ShapeTag.Select) ^ ShapeTag.Select);
            }
        }

        /// <summary>
        /// Чистим список фигур и отчищаем признаки ShapeTag.Select
        /// </summary>
        public static void ClearShapes(this List<Shape> list)
        {
            list.ForEach(s => s.Tag = s.Tag == null ? ShapeTag.None : (((ShapeTag)s.Tag | ShapeTag.Select) ^ ShapeTag.Select));
            list.Clear();
        }

        /// <summary>
        ///  Проверяем пересекается ли фигура с прямоугольником выделения
        /// </summary>
        public static bool IntersectWith(this Shape shape, Rect rect)
        {
            if (shape == null)
                return false;

            if (shape is Path) // если фигура  - четырехугольник
            {
                var shapePoints = (shape as Path).GetSegment().Points.ToArray(); // координаты верщин 4х угольника
                var rectPoints = new Point[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft }; // координаты верщин 4х угольника выделения

                // проверяем на пересечение каждой из сторон обоих фигур
                return !(DoAxisSeparationTest(shapePoints[0], shapePoints[1], shapePoints[2], rectPoints) ||
                    DoAxisSeparationTest(shapePoints[0], shapePoints[3], shapePoints[2], rectPoints) ||
                    DoAxisSeparationTest(shapePoints[3], shapePoints[2], shapePoints[0], rectPoints) ||
                    DoAxisSeparationTest(shapePoints[2], shapePoints[1], shapePoints[0], rectPoints) ||
                    DoAxisSeparationTest(rectPoints[0], rectPoints[1], rectPoints[2], shapePoints) ||
                    DoAxisSeparationTest(rectPoints[0], rectPoints[3], rectPoints[2], shapePoints) ||
                    DoAxisSeparationTest(rectPoints[3], rectPoints[2], rectPoints[0], shapePoints) ||
                    DoAxisSeparationTest(rectPoints[2], rectPoints[1], rectPoints[0], shapePoints));
            }
            else if (shape is Polyline) //если фигура - линия
            {
                Polyline line = shape as Polyline;
                for (int i = 1; i < line.Points.Count; i++) // проверяем каждый отрезок линии
                {
                    Point pt1 = line.Points[i - 1];
                    Point pt2 = line.Points[i];
                    if  ((rect.Contains(pt1) || rect.Contains(pt2)) //координаты отрезка совпадают с координатами вершин выделения
                        || (pt1.X == pt2.X && pt1.X >= rect.Left && pt1.X <= rect.Right) // то же, но отрезок вертикален
                        || (pt1.Y == pt2.Y && pt1.Y >= rect.Top && pt1.Y <= rect.Bottom) // то же, но отрезок горизонтален
                        || CheckLineLineIntersection(rect.TopLeft, rect.BottomLeft, pt1, pt2) // дальше проверка на пересечений отрезка с любой из сторон выделения
                        || CheckLineLineIntersection(rect.TopLeft, rect.TopRight, pt1, pt2)
                        || CheckLineLineIntersection(rect.TopRight, rect.BottomRight, pt1, pt2)
                        || CheckLineLineIntersection(rect.BottomLeft, rect.BottomRight, pt1, pt2))
                        return true;
                }
                return false;
            }
            else
                return false;
        }

        /// <summary>
        /// Возвращает Polyline. образуюущую 4х угольник
        /// </summary>
        public static PolyLineSegment GetSegment(this Path p)
        {
            return ((p.Data as PathGeometry).Figures[0] as PathFigure).Segments[0] as PolyLineSegment;
        }

        /// <summary>
        /// Устанавливает TopLeft линии 4х угольника и TopLeft фигуры
        /// </summary>
        public static void SetTopLeft(this Path p, Point pt)
        {
            ((p.Data as PathGeometry).Figures[0] as PathFigure).StartPoint = pt;
            GetSegment(p).Points[0] = pt;
        }

        /// <summary>
        /// Применяет трансформацию к Path
        /// </summary>
        public static void Transform(this Path p, GeneralTransform t)
        {
            if (t != null)
            {
                var line = p.GetSegment();

                for (int i = 0; i < line.Points.Count; i++)
                {
                    line.Points[i] = t.Transform(line.Points[i]);
                }
                ((p.Data as PathGeometry).Figures[0] as PathFigure).StartPoint = line.Points[0]; // Устанавливает TopLeft фигуры
            }
        }

        // за основу взят http://manski.net/2011/05/rectangle-intersection-test-with-csharp/
        /// <summary>
        /// Выполняем тест на наличие оси-разделителя. Если между двумя фигурам иможно провести прямую, то фигуры не пересекаются
        /// </summary>
        /// <param name="x1">Коорднита начала стороны</param>
        /// <param name="x2">Координата конца стороны</param>
        /// <param name="x3">Точка напротив для определения направления</param>
        /// <param name="otherQuadPoints">Координаты вершин 4х угольника выделения</param>
        /// <returns>true - фигуры не пересекаются, false - пересекаются</returns>
        private static bool DoAxisSeparationTest(Point x1, Point x2, Point x3, Point[] otherQuadPoints)
        {
            Vector vec = x2 - x1;// вектор по стороне
            Vector rotated = new Vector(-vec.Y, vec.X); // перпендикулярный вектор

            // положение выделения отностиетльно фигуры
            bool refSide = (rotated.X * (x3.X - x1.X)
                          + rotated.Y * (x3.Y - x1.Y)) >= 0;
            
            foreach (Point pt in otherQuadPoints)
            {
                bool side = (rotated.X * (pt.X - x1.X)
                           + rotated.Y * (pt.Y - x1.Y)) >= 0;
                if (side == refSide) // точка выделения внутри фигуры
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Проверяем 2 линии на пересечение
        /// </summary>
        /// <param name="line1Pt1">координата начала 1 линии</param>
        /// <param name="line1Pt2">координата конца 1 линии</param>
        /// <param name="line2Pt1">координата начала 2 линии</param>
        /// <param name="line2Pt2">координата конца 2 линии</param>
        /// <returns>true - пересекаются, false - не пересекаются</returns>
        public static bool CheckLineLineIntersection(Point line1Pt1, Point line1Pt2, Point line2Pt1, Point line2Pt2)
        {
            //вектора касательных
            Vector v1 = line1Pt2 - line1Pt1;
            Vector v2 = line2Pt2 - line2Pt1;

            double bDotDPerp = v1.X * v2.Y - v1.Y * v2.X; // коэффициент наклона

            if (bDotDPerp == 0) // параллельны
                return false;

            Vector c = line2Pt1 - line1Pt1; // общий вектор 2 концов разных линий
            double lineFactor = (c.X * v2.Y - c.Y * v2.X) / bDotDPerp; // коэффициент наклона
            if (lineFactor < 0 || lineFactor > 1) 
                return false;

            lineFactor = (c.X * v1.Y - c.Y * v1.X) / bDotDPerp; // коэффициент наклона общего вектора 2х других вершин
            if (lineFactor < 0 || lineFactor > 1)
                return false;

            return true;
        }
 
    }

    [FlagsAttribute]
    public enum ShapeTag : byte
    {
        None = 0,
        Select = 1,
        Deleting = 4,
    }
   
}