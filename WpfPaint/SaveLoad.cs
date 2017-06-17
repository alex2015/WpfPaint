using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Schema;
using WpfPaint.ShapesExtension;

namespace WpfPaint
{
    public partial class MainWindow
    {
        /// <summary>
        /// Сохраняет рабочую область в файл
        /// </summary>
        private void Save(String filename)
        {
            using (XmlWriter writer = XmlWriter.Create(filename))
            {
                writer.WriteStartElement("File");
                foreach (Shape s in this.canvas.Children.OfType<Shape>())
                {
                    if (s is Path)
                    {
                        this.writeRectangleShape(writer, s as Path);
                    }
                    else if (s is Polyline)
                    {
                        this.writePolylineShape(writer, s as Polyline);
                    }
                }
                writer.WriteEndElement();
                writer.Flush();
            }

            if (GetXmlFromFile(filename, false) == null) // проверка получившегося xml по схеме
            {
                MessageBox.Show("Ошибка при формировании файла " + filename, "WpfPaint - Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Записывает в xml представление ломаной линии
        /// </summary>
        private void writePolylineShape(XmlWriter writer, Polyline line)
        {
            writer.WriteStartElement("Shape");
            writer.WriteAttributeString("Type", "Line");

            var sett = line.Style.Setters.OfType<Setter>().Where(ss => ss.Property == Polyline.StrokeThicknessProperty);
            if (sett != null && sett.Count() > 0)
                writer.WriteAttributeString("Stroke", Math.Round((double)sett.First().Value).ToString());

            sett = line.Style.Setters.OfType<Setter>().Where(ss => ss.Property == Polyline.StrokeProperty);
            if (sett != null && sett.Count() > 0)
            {
                Brush b = (sett.First().Value as Brush);
                writer.WriteStartElement("Brush");

                if (b is SolidColorBrush)
                    writer.WriteAttributeString("Color1", (b as SolidColorBrush).Color.ToString());
                else if (b is LinearGradientBrush)
                {
                    writer.WriteAttributeString("Color1", (b as LinearGradientBrush).GradientStops[0].Color.ToString());
                }
                writer.WriteEndElement(); // Brush
            }

            writer.WriteStartElement("Points");
            foreach (Point p in line.Points)
                this.WritePoint(p, writer);
            writer.WriteEndElement(); // Points

            writer.WriteEndElement(); // Shape
        }

        /// <summary>
        /// Записывает в xml представление 4х угольника
        /// </summary>
        private void writeRectangleShape(XmlWriter writer, Path rect)
        {
            writer.WriteStartElement("Shape");
            writer.WriteAttributeString("Type", "Rect");

            var sett = rect.Style.Setters.OfType<Setter>().Where(ss => ss.Property == Rectangle.FillProperty);
            if (sett != null && sett.Count() > 0)
            {
                Brush b = (sett.First().Value as Brush);
                writer.WriteStartElement("Brush");

                if (b is SolidColorBrush)
                    writer.WriteAttributeString("Color1", (b as SolidColorBrush).Color.ToString());
                else if (b is LinearGradientBrush)
                {
                    LinearGradientBrush lb = b as LinearGradientBrush;
                    writer.WriteAttributeString("Color1", lb.GradientStops[0].Color.ToString());
                    writer.WriteAttributeString("Color2", lb.GradientStops[1].Color.ToString());
                    writer.WriteAttributeString("Color1pos", lb.GradientStops[0].Offset.ToString());
                    writer.WriteAttributeString("Color2pos", lb.GradientStops[1].Offset.ToString());
                }
                writer.WriteEndElement(); // Brush
            }
            writer.WriteStartElement("Points");
            foreach (Point p in rect.GetSegment().Points)
                this.WritePoint(p, writer);
            writer.WriteEndElement(); //Points

            writer.WriteEndElement(); // Shape
        }

        /// <summary>
        /// Записывает в xml представление точки координат
        /// </summary>
        private void WritePoint(Point p, XmlWriter writer)
        {
            writer.WriteStartElement("Point");
            writer.WriteAttributeString("X", p.X.ToString());
            writer.WriteAttributeString("Y", p.Y.ToString());
            writer.WriteEndElement();
        }
        
        /// <summary>
        /// Загрузка рабочей области из файла
        /// </summary>
        private void Load(String filename)
        {
            XmlDocument doc = GetXmlFromFile(filename);
            if (doc != null) // провекра прошла успешно
            {
                foreach (var x in doc.DocumentElement.ChildNodes.OfType<XmlNode>())
                {
                    if (x.Attributes["Type"].Value == "Rect")
                    {
                        XmlNode size = x.ChildNodes[1];
                        if (size.ChildNodes.Count != 4) // 4х угольник но коордниат вершин не 4
                            continue;
                        else
                        {
                            //  координаты вершин
                            Point p1 = this.GetPoint(size.ChildNodes[0]);
                            Point p2 = this.GetPoint(size.ChildNodes[1]);
                            Point p3 = this.GetPoint(size.ChildNodes[2]);
                            Point p4 = this.GetPoint(size.ChildNodes[3]);

                            PolyLineSegment myPolyLineSegment = new PolyLineSegment();
                            myPolyLineSegment.Points = new PointCollection(new Point[] { p1, p2, p3, p4 });

                            PathFigure pathFigure = new PathFigure();
                            pathFigure.StartPoint = p1;
                            pathFigure.IsClosed = true;
                            pathFigure.Segments.Add(myPolyLineSegment);
                            
                            PathGeometry myPathGeometry = new PathGeometry();
                            myPathGeometry.Figures.Add(pathFigure);

                            Brush brush = this.GetShapeBrushFromFile(x.ChildNodes[0]);

                            Path rect = new Path();
                            rect.Style = Tools.DrawTool.CalculateRectangleStyle(brush);
                            rect.Tag = ShapeTag.None;
                            rect.Data = myPathGeometry;
                            this.canvas.Children.Add(rect);                        
                        }
                    }
                    else
                    {
                        XmlNode pts = x.ChildNodes[1];
                        if (pts.ChildNodes.Count < 2)
                            continue; // линия в которой меньше 2х точек
                        else
                        {
                            Brush brush = this.GetShapeBrushFromFile(x.ChildNodes[0]);

                            Polyline line = new Polyline();
                            line.Style = Tools.DrawTool.CalculatePolylineStyle(brush, double.Parse(x.Attributes["Stroke"].Value));
                            line.Tag = ShapeTag.None;    

                            foreach (XmlNode p in pts.ChildNodes)
                            {
                                Point pt = this.GetPoint(p);
                                line.Points.Add(pt);
                            }

                            this.canvas.Children.Add(line);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Получает Point из XmlNode
        /// </summary>
        private Point GetPoint(XmlNode x)
        {
            Point p = new Point();
            p.X = double.Parse(x.Attributes["X"].Value);
            p.Y = double.Parse(x.Attributes["Y"].Value);
            return p;
        }

        /// <summary>
        /// Получает Brush из XmlNode
        /// </summary>
        private Brush GetShapeBrushFromFile(XmlNode b)
        {
            if (b.Attributes.Count > 1)
            {
                LinearGradientBrush gb = new LinearGradientBrush();
                gb.StartPoint = new Point(0, 0.5);
                gb.EndPoint = new Point(1, 0.5);
                Color c1 = (Color)ColorConverter.ConvertFromString(b.Attributes["Color1"].Value);
                Color c2 = (Color)ColorConverter.ConvertFromString(b.Attributes["Color2"].Value);

                gb.GradientStops.Add(new GradientStop(c1, double.Parse(b.Attributes["Color1pos"].Value)));
                gb.GradientStops.Add(new GradientStop(c2, double.Parse(b.Attributes["Color2pos"].Value)));
                return gb;
            }
            else
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(b.Attributes["Color1"].Value));
        }

        /// <summary>
        /// Получаем XmlDocument из файла по указанному пути, одновремено проверяя файл на соответсвие схеме CanvasFileXMLSchema.XSD
        /// </summary>
        /// <param name="showErrors">Показввать сообщения об ошибках проверки</param>
        /// <returns>null - если ошибка при проверке, иначе валидный XmlDocument</returns>
        private XmlDocument GetXmlFromFile(String filename, bool showErrors = true)
        {
            XmlDocument validXml = new XmlDocument();
            XmlDocument xml = new XmlDocument();
            XmlDocument schema = new XmlDocument();
            schema.LoadXml(Properties.Resources.CanvasFileXMLSchema);

            XmlReaderSettings validateSettings = new XmlReaderSettings();
            validateSettings.Schemas.Add(null, new XmlNodeReader(schema));
            validateSettings.ValidationType = ValidationType.Schema;
            validateSettings.ConformanceLevel = ConformanceLevel.Auto;
            validateSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

            try
            {
                xml.Load(filename);
                using (XmlReader r = XmlReader.Create(new XmlNodeReader(xml), validateSettings))
                    validXml.Load(r);
                return validXml;
            }
            catch (Exception e)
            {
                if (showErrors)
                    MessageBox.Show(e.ToString());
                return null;
            }
        }
    }
}