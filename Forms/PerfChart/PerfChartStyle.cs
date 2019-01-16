using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Drawing;

namespace SummerGUI.Charting.PerfCharts
{    
    public class PerfChartStyle
    {        
        public PerfChartStyle() {
            VerticalGridPen = new ChartPen();
            HorizontalGridPen = new ChartPen();
			AvgLinePen = new ChartPen(Theme.Colors.Orange, 1.5f);
			ChartLinePen = new ChartPen(Theme.Colors.Base3, 1.5f);

			ShowVerticalGridLines = true;
			ShowHorizontalGridLines = true;
			ShowAverageLine = true;

			CaptionForegroundBrush = new SolidBrush (Theme.Colors.Base02);
			CaptionBrush = new LinearGradientBrush (Theme.Colors.Base00, Theme.Colors.Base01, GradientDirections.Vertical);
			GradientBrush = new LinearGradientBrush (Theme.Colors.Base02, Theme.Colors.Base03, GradientDirections.Vertical);
        }

		public bool ShowVerticalGridLines { get; set; }
		public bool ShowHorizontalGridLines { get; set; }
		public bool ShowAverageLine { get; set; }

		public ChartPen VerticalGridPen { get; set; }
		public ChartPen HorizontalGridPen { get; set; }
		public ChartPen AvgLinePen { get; set; }
		public ChartPen ChartLinePen { get; set; }

		public SummerGUI.Brush CaptionForegroundBrush  { get; private set; }
		public SummerGUI.LinearGradientBrush CaptionBrush  { get; private set; }
		public SummerGUI.LinearGradientBrush GradientBrush  { get; private set; }

		public Color BackgroundColorTop 
		{ 
			get 
			{ 
				return GradientBrush.Color;
			}
			set {
				GradientBrush.Color = value;
			}
		}

		public Color BackgroundColorBottom
		{ 
			get 
			{ 
				return GradientBrush.GradientColor;
			}
			set {
				GradientBrush.GradientColor = value;
			}
		}
    }
		
    public class ChartPen
    {
		public SummerGUI.Pen Pen { get; private set; }

		public ChartPen() : this(Color.Black, 1f) {}

		public ChartPen(Color color, float width) {
			this.Pen = new Pen(color, width);
        }

        public Color Color {
			get { return this.Pen.Color; }
			set { this.Pen.Color = value; }
        }

		/**
        public System.Drawing.Drawing2D.DashStyle DashStyle {
            get { return pen.DashStyle; }
            set { pen.DashStyle = value; }
        }
        **/

        public float Width {
            get { return this.Pen.Width; }
			set { this.Pen.Width = value; }
        }
    }
}
