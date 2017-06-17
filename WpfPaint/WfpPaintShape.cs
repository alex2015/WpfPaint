using System.Collections.Generic;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WpfPaint
{
    public class MultiSlider : Slider
    {
        private List<Thumb> thumbs
        {
            get
            {
                List<Thumb> result = new List<Thumb>();

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(this); i++)
                {
                    var thumb = VisualTreeHelper.GetChild(this, i);

                    if (thumb is Thumb)
                        result.Add(thumb as Thumb);
                }

                return result;
            }
        }

        private Thumb GetThumb()
        {
            var track = this.Template.FindName("PART_Track", this) as Track;
            return track == null ? null : track.Thumb;
        }
    }

    internal class WfpPaintShape
    {
    }

    public interface IWfpPaintShape
    {
        void ClearSelection();

        bool IsIntersectWith(Rectangle r);
    }
}