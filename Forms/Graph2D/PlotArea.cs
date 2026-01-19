using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Data;
using System.Text;
using System.Threading;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework; 

using KS.Foundation;
using SummerGUI.Charting.Graph2D;

namespace SummerGUI.Charting
{    
	public class Graph2DPlotterStyle : WidgetStyle
	{
		public override void InitStyle ()
		{
			SetBackColor (Theme.Colors.Base3);
			SetForeColor (Theme.Colors.Base03);
			SetBorderColor (Color.Empty);
		}
	}

    public class Graph2DPlotter : Widget, ISupportsZoom
    {
        public event EventHandler BeforeItemChange;
        public void OnBeforeItemChange()
        {
            if (BeforeItemChange != null)
                BeforeItemChange(this, EventArgs.Empty);
        }

        public event EventHandler ItemChanged;
        public void OnItemChanged()
        {
            if (ItemChanged != null)
                ItemChanged(this, EventArgs.Empty);
        }

        public enum PlotterEditModes
        {
            editNone,
            editPaint,
            editErase,
            editMove
        }        

        public delegate void MouseCoordChangedEventHandler(string X, string Y);
        private MouseCoordChangedEventHandler MouseCoordChangedEvent;
        public event MouseCoordChangedEventHandler MouseCoordChanged
        {
            add
            {
                MouseCoordChangedEvent = (MouseCoordChangedEventHandler)System.Delegate.Combine(MouseCoordChangedEvent, value);
            }
            remove
            {
                MouseCoordChangedEvent = (MouseCoordChangedEventHandler)System.Delegate.Remove(MouseCoordChangedEvent, value);
            }
        }

        public delegate void PointCountChangedEventHandler();
        private PointCountChangedEventHandler PointCountChangedEvent;
        public event PointCountChangedEventHandler PointCountChanged
        {
            add
            {
                PointCountChangedEvent = (PointCountChangedEventHandler)System.Delegate.Combine(PointCountChangedEvent, value);
            }
            remove
            {
                PointCountChangedEvent = (PointCountChangedEventHandler)System.Delegate.Remove(PointCountChangedEvent, value);
            }
        }
			
        public PlotterEditModes EditMode = PlotterEditModes.editMove;

        public GraphList Graphs = null;
		public GraphBase Graph { get; set; }

        public AxisDataTypes XAxisDataType = AxisDataTypes.axNumeric;
                
		public PointF m_CenterPoint;
		public PointF CenterPoint 
		{ 
			get {
				return m_CenterPoint;
			}
		}        
        public RectangleF OldBounds = new RectangleF(0, 0, 0, 0);

        public double xRange;
        public double yRange;
        public double ZoomX;
        public double ZoomY;
        
		public bool LimitToView = false;
        public bool DrawGrid = true;        
		public Color GridColor = Theme.Colors.Magenta;   // Rose
        public bool ShowCross = false;

		[DpiScalable]
		public int PlotMargin { get; set; }

		[DpiScalable]
		public float DefaultCurveWidth { get; set; }

		[DpiScalable]
		public float PointRadius { get; set; }

		[DpiScalable]
		public float AxisWidth { get; set; }

        public int CurrentNumberOfPoints = -1;               

		public string StringFormatX = "g";
		public string StringFormatY = "g";
        
        // MouseMoving        
        private bool bMouseMoving = false;
        private bool bMouseHover = false;
        private float MoveStartX;
        private float MoveStartY;
		private PointF MoveStartCenterPoint = new PointF();
		private PointF CurrentMousePosition = new PointF();

		// TODO: Timer
        //private System.Windows.Forms.Timer timer_catch;        
        
		private GraphPoint m_PointCatched = null;
        public GraphPoint PointCatched
        {
            get
            {				
                return m_PointCatched;
            }
            set
            {
                if (m_PointCatched != value)
                {
                    m_PointCatched = value;
                    ShowCross = m_PointCatched != null;
					Invalidate ();
					UpdateCursor ();
                }
            }
        }

		private void UpdateCursor()
		{
			if (m_PointCatched != null || bMouseMoving)
				RefreshCursor ();
			else
				ParentWindow.SetCustomCursor ("CrossHairs");
		}

		public override void Initialize ()
		{
			base.Initialize ();            
			WindowResourceManager.Manager.LoadCursorFromFile ("Assets/Cursors/CrossHairs.png", "CrossHairs");
		}

		public override void OnMouseEnter (IGUIContext ctx)
		{
			base.OnMouseEnter (ctx);
			UpdateCursor ();
		}
        
        private PointF NodeCatchedStart;

        public void ResetCatchedNode()
        {            
            PointCatched = null;
            NodeCatchedStart = Point.Empty;
        }
                
		public IGUIFont Font { get; private set; }

		public Graph2DPlotter(string name)
			: base(name, Docking.Fill, new Graph2DPlotterStyle())
        {                        
            // this.Cursor = Cursors.Cross;
			Font = FontManager.Manager.DefaultFont;

            m_CenterPoint.X = 0;
			m_CenterPoint.Y = 0;

            PlotMargin = 8;
			PointRadius = 6f;
			DefaultCurveWidth = 3f;
			AxisWidth = 1.5f;

            xRange = 20;
            yRange = 20;
            ZoomX = 1.0;
            ZoomY = 1.0;

            Graphs = new GraphList();		
			CanFocus = true;

            //timer_catch = new System.Windows.Forms.Timer();
            //timer_catch.Interval = 50;            
            //timer_catch.Tick += new EventHandler(timer_catch_Tick);

            //Thread caretThread = new Thread(new ThreadStart(ShowCaret));
            //caretThread.Start();
        }        
			
		protected override void CleanupManagedResources ()
		{
			if (Graphs != null)
			{
				Graphs.Clear();
				Graphs = null;
			}  		
			base.CleanupManagedResources();
		}

        public void AddGraph(GraphBase graph, bool UseSameName)
        {
            GraphBase g = null;

            if (UseSameName)
            {
                foreach (string s in Graphs.Keys)
                {
                    g = Graphs[s];

                    if (g.GraphDescription.ToUpper() == graph.GraphDescription.ToUpper())
                    {
                        Graphs.Remove(s);
                        break;
                    }
                }
            }
            
            this.Graphs.Add(graph.Key, graph);            

            Invalidate();
            OnItemChanged();
        }        

        public void ClearGraphs()
        {            
            Graphs.Clear();
            Invalidate();
            OnItemChanged();
        }               

        private double PixelWidth(RectangleF bounds)
        {
            return xRange / (bounds.Width - PlotMargin * 6d);
        }

        private double PixelHeight(RectangleF bounds)
        {
            return yRange / (bounds.Height - PlotMargin * 6d);
        }

        public double AspectRatio(RectangleF bounds)
        {
            return bounds.Width / bounds.Height;
        }

        public double Point2ClientX(double X, RectangleF bounds)
        {
            return bounds.Width - Math.Round((bounds.Width / 2 - X * ZoomX / PixelWidth(bounds)) - m_CenterPoint.X);
        }

		public double Point2ClientY(double Y, RectangleF bounds)
        {
            return Math.Round(bounds.Height / 2 - Y * ZoomY / PixelHeight(bounds) * AspectRatio(bounds)) + m_CenterPoint.Y;
        }

        public PointF Point2Client(double X, double Y, RectangleF bounds)
        {
			return new PointF((float)Point2ClientX(X, bounds), (float)Point2ClientY(Y, bounds));            
        }

        // Point2Client R�ckw�rts
		public double Client2PointX(double X, RectangleF bounds)
        {            
            double dx = PixelWidth(bounds);
			return 0 + ((X - m_CenterPoint.X) * dx / ZoomX - bounds.Width / 2.0 * dx / ZoomX);
        }

        // Point2Client R�ckw�rts
        public double Client2PointY(double Y, RectangleF bounds)
        {            
            double dy = PixelHeight(bounds) / AspectRatio(Bounds);
			return 0 - ((Y - m_CenterPoint.Y) * dy / ZoomY - bounds.Height / 2.0 * dy / ZoomY);
        }        

		public void DrawGraphics(IGUIContext ctx)
        {            
			DrawGraphics(ctx, Rectangle.Ceiling(this.Bounds));
        }        
					        
		//public void DrawGraphics(IKsOpenGLContext ctx, Rectangle bounds, bool TransformLocation)
		public void DrawGraphics(IGUIContext ctx, Rectangle bounds)
        {                                    
            try
            {
                DrawAxis(ctx, bounds);

                if (Graphs != null)
                {
                    foreach (GraphBase graph in Graphs.Values)
                    {
                        if (graph.Visible)
						{
                            //DrawGraph(ctx, graph, bounds);
							;
						}
                    }
                }
            }
            catch (Exception)
            {                    
            }           
			            
            if (ShowCross && bMouseHover && m_PointCatched != null)
            {
                try
                {
                    double X = m_PointCatched.X.SafeDouble();
                    double Y = m_PointCatched.Y.SafeDouble();

					// ToDo: There is a 1 pixel offset that shouldn't be there
					int xValue = (int)(bounds.X + Point2ClientX(X, bounds) + 1.5);
					int yValue = (int)(bounds.Y + Point2ClientY(Y, bounds) + 1.5);

					ctx.DrawLine(Theme.Pens.Blue, xValue, 0, xValue, bounds.Bottom);
					ctx.DrawLine(Theme.Pens.Blue, 0, yValue, bounds.Right, yValue);
                }
                catch (Exception)
                {
                }
            }

            try
            {
                DrawNodes(ctx, bounds);
            }
            catch (Exception)
            {             
            }
        }

        
		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			DrawGraphics(ctx, Rectangle.Ceiling(bounds));
		}

		/**
        public double XValue(GraphPoint point)
        {
            if (this.XAxisDataType == AxisDataTypes.axNumeric)
				return (double)point.X;
            else
				return DoubleConvert.Date2Double((DateTime)(point.X));
        }
        **/


		private void DrawNodes(IGUIContext ctx, RectangleF bounds)
        {
            int OldNumberOfPoints = CurrentNumberOfPoints;
            CurrentNumberOfPoints = 0;

            if (Graph == null)
            {
                if (OldNumberOfPoints != 0)
                {
                    if (PointCountChangedEvent != null)
                        PointCountChangedEvent();
                }

                return;
            }

            PointF p;            
            bool bInterpolated = false;
            // int ListID;
            Color C;            

			float offsetX = bounds.X;
			float offsetY = bounds.Y;

			if (Graph.Visible)
			{	
				if (Graph.Points.Count > 1)
					DrawGraphLine (ctx, Graph, bounds);

				foreach(GraphPoint gp in Graph.Points)
	            {                
					p = Point2Client((double)gp.X, (double)gp.Y, bounds).Add(offsetX, offsetY);
					if (!Bounds.IntersectsWith (new RectangleF (p.X, p.Y, 1, 1)))
						continue;

					bInterpolated = Graph.GraphType == GraphTypes.gtInterpolation;
					C = Graph.GraphColor;

					Pen pen = new Pen(Color.White);
					SolidBrush brush = new SolidBrush(C);
					SolidBrush whitebrush = new SolidBrush(Color.White);

                    try
                    {
                        if (bInterpolated)
						{
							float PointWidth = PointRadius * 2f;
							ctx.DrawRectangle(pen, p.X - PointRadius, p.Y - PointRadius, PointWidth, PointWidth);
							if (gp == PointCatched)
								ctx.DrawRectangle(pen, p.X - PointRadius - 1, p.Y - PointRadius - 1, PointWidth + 2, PointWidth + 2);
						}
                        else
						{
							float Radius = PointRadius / 2f;;

							float width;
							if (gp == PointCatched)			
								width = 4f.Scale(ScaleFactor);
							else
								width = 3f.Scale(ScaleFactor);
								
							ctx.FillEllipse(brush, p.X, p.Y, Radius + width, Radius + width);

							width = 1.5f.Scale(ScaleFactor);
							ctx.FillEllipse(whitebrush, p.X, p.Y, Radius + width, Radius + width);
							ctx.FillEllipse(brush, p.X, p.Y, Radius, Radius);
						}							
                    }
                    catch { }

                    pen.Dispose();
                    brush.Dispose();

                    CurrentNumberOfPoints++;            
            	}					
			}                

            if (CurrentNumberOfPoints != OldNumberOfPoints)
            {
                if (PointCountChangedEvent != null)
                    PointCountChangedEvent();
            }
        }

		private void DrawGraphLine(IGUIContext ctx, GraphBase graph, RectangleF bounds)
		{
			PointF PPaint;
			PointF PPaintLast = new PointF();

			Pen pen = null;
			bool PlotFlag = true;

			float offsetX = bounds.X;
			float offsetY = bounds.Y;

			try {
				//pen = new Pen(graph.GraphColor);
				pen = new Pen(Color.FromArgb(200, Theme.Colors.Blue));
				pen.Width = DefaultCurveWidth;

				bool bFirstPoint = true;

				foreach(GraphPoint gp in Graph.Points)
				{
					if (bFirstPoint)
					{						
						bFirstPoint = false;
						PPaintLast = Point2Client((double)gp.X, (double)gp.Y, bounds);
						continue;
					}

					//double x = Client2PointX(i, Bounds);
					//double y = (double)graph.FX(x);
					//double y = Client2PointY(i, Bounds);

					PPaint = Point2Client((double)gp.X, (double)gp.Y, bounds);

					if (PlotFlag && (PPaint.Y >= 0 || PPaintLast.Y >= 0) && (PPaint.Y <= Bounds.Height || PPaintLast.Y <= bounds.Height))
					{
						try
						{
							ctx.DrawLine(pen, PPaintLast.X + offsetX, PPaintLast.Y + offsetY, PPaint.X + offsetX, PPaint.Y + offsetY);
						}
						catch { }
					}

					PPaintLast = PPaint;

					PlotFlag = true;
				}
			} catch (Exception ex) {				
				ex.LogError ();
			}
			finally {
				pen.Dispose ();
			}
		}

		/***

		private void DrawGraph(IKsOpenGLContext ctx, GraphBase graph, Rectangle Bounds)
        {
            double x;
            double y;            

            Point PPaint;
            Point PPaintLast = new Point(); ;            

            bool PlotFlag = false;

            Pen pen = null;

            try
            {
                pen = new Pen(graph.GraphColor);
                pen.Width = DefaultCurveWidth;

                switch (graph.GraphType)
                {
                    case GraphTypes.gtIntegral:

                        IntegralBase integral = (IntegralBase)graph;

                        pen.Color = System.Drawing.Color.FromArgb(128, pen.Color);

                        for (int i = 0; i < Bounds.Width; i++)
                        {
                            x = Client2PointX(i, Bounds);

                            if (x >= integral.MinX.X && x <= integral.MaxX.X)
                            {
                                PPaintLast = Point2Client(x, (double)integral.FXYMin(x), Bounds);
                                PPaint = Point2Client(x, (double)integral.FXYMax(x), Bounds);

                                try
                                {
                                    g.DrawLine(pen, PPaintLast, PPaint);
                                }
                                catch { }
                            }
                        }

                        break;

                    case GraphTypes.gtSpecialPoints:
                        // wird separat gezeichnet, s.u.
                        break;

                    case GraphTypes.gtStatsMean:

                        pen.Color = System.Drawing.Color.FromArgb(172, pen.Color);
                        pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                        switch (graph.GraphSubType)
                        {
                            case GraphSubTypes.gstMedian:
                                int iMedianX = Point2ClientX((double)graph.FX(0.0), Bounds);

                                try
                                {
                                    g.DrawLine(pen, iMedianX, 0, iMedianX, Bounds.Height);
                                }
                                catch { }

                                break;

                            case GraphSubTypes.gstMeanAverage:

                                int iMeanAverageY = Point2ClientY((double)graph.FX(0.0), Bounds);

                                try
                                {
                                    g.DrawLine(pen, 0, iMeanAverageY, Bounds.Width, iMeanAverageY);
                                }
                                catch { }

                                break;
                        }

                        break;

                    case GraphTypes.gtInterpolation:

                        SplineBase spline = (SplineBase)graph;

                        int iMin;
                        int iMax;
                        if (spline.DrawExtrapolate)
                        {
                            iMin = 0;
                            iMax = Bounds.Width;
                        }
                        else
                        {
                            iMin = Math.Max(0, Point2ClientX((double)spline.XMin, Bounds));
                            iMax = Math.Min(Bounds.Width, Point2ClientX((double)spline.XMax, Bounds));
                        }

                        for (int i = iMin; i <= iMax; i++)
                        {
                            x = Client2PointX(i, Bounds);
                            y = (double)graph.FX(x);

                            PPaint = Point2Client(x, y, Bounds);

                            if (PlotFlag && (PPaint.Y >= 0 || PPaintLast.Y >= 0) && (PPaint.Y <= Bounds.Height || PPaintLast.Y <= Bounds.Height))
                            {
                                try
                                {
                                    g.DrawLine(pen, PPaintLast, PPaint);
                                }
                                catch { }
                            }

                            PPaintLast = PPaint;

                            PlotFlag = true;
                        }

                        break;

                    default:

                        for (int i = 0; i < Bounds.Width; i++)
                        {
                            x = Client2PointX(i, Bounds);
                            y = (double)graph.FX(x);

                            PPaint = Point2Client(x, y, Bounds);

                            if (PlotFlag && (PPaint.Y >= 0 || PPaintLast.Y >= 0) && (PPaint.Y <= Bounds.Height || PPaintLast.Y <= Bounds.Height))
                            {
                                try
                                {
                                    g.DrawLine(pen, PPaintLast, PPaint);
                                }
                                catch { }
                            }

                            PPaintLast = PPaint;

                            PlotFlag = true;
                        }

                        break;
                }

                if (graph.Points != null)
                {
                    int Diag = 5;
                    Point PC;

                    pen.Color = System.Drawing.Color.FromArgb(200, pen.Color);

                    foreach (GraphPoint p in graph.Points)
                    {
                        PC = Point2Client(p.X.DoubleValue, p.Y.DoubleValue, Bounds);

                        try
                        {
                            g.DrawLine(pen, PC.X - Diag, PC.Y - Diag, PC.X + Diag, PC.Y + Diag);
                            g.DrawLine(pen, PC.X - Diag, PC.Y + Diag, PC.X + Diag, PC.Y - Diag);
                        }
                        catch { }
                    }
                }                
            }
            catch (Exception)
            {
            }
            finally
            {
                if (pen != null)
                {
                    pen.Dispose();
                    pen = null;
                }
            }

            //if (ShowCross && bMouseHover)
            //{
            //    try
            //    {
            //        int yValue = Point2ClientY((double)graph.FX(Client2PointX(CurrentMousePosition.X, Bounds)), Bounds);

            //        g.DrawLine(Pens.Cyan, CurrentMousePosition.X, 0, CurrentMousePosition.X, Bounds.Height);

            //        if (yValue >= 0 && yValue <= Bounds.Height)
            //            g.DrawLine(Pens.Cyan, 0, yValue, Bounds.Width, yValue);
            //    }
            //    catch (Exception)
            //    {
            //    }
            //}
        }
        ***/


        // 0 = X; 1 = Y
        private string FormatLabel(double value, int Axis)
        {
            // auf signifikante stellen runden

            if (Axis == 0 && StringFormatX.Length > 0)
            {
                try
                {
                    if (XAxisDataType == AxisDataTypes.axDateTime)
                    {                        
                        return DoubleConvert.Double2Date(value).ToString(StringFormatX);
                    }

                    return value.ToString(StringFormatX);
                }
                catch (Exception)
                {
                    return "format-error";
                }                                
            }
            else if (Axis == 1 && StringFormatY.Length > 0)
            {
                try
                {
                    return value.ToString(StringFormatY);
                }
                catch (Exception)
                {
                    return "format-error";
                }                
            }
            else
            {
                string S = value.ToString();

                if (value % 10 != 0)
                {

                    while (S.Substring(S.Length - 1, 1) == "0")
                        S = S.Substring(0, S.Length - 1);

                    string c = S.Substring(S.Length - 1, 1);
                    if (c == "." || c == ",")
                        S = S.Substring(0, S.Length - 1);
                }

                return S;
            }            
        }        

		private Pen AxisPen;

		FontFormat CenterFontFormat = new FontFormat(Alignment.Center, Alignment.Center, FontFormatFlags.None);
		FontFormat BelowAxisFontFormat = new FontFormat(Alignment.Far, Alignment.Center, FontFormatFlags.None);
		FontFormat AboveAxisFontFormat = new FontFormat(Alignment.Near, Alignment.Center, FontFormatFlags.None);

		private void DrawAxis(IGUIContext ctx, RectangleF bounds)
        {
			if (bounds.Width <= 0 || bounds.Height <= 0)
				return;

			if (AxisPen == null)
				AxisPen = new Pen (Theme.Colors.Base03, AxisWidth);
			else
				AxisPen.Width = AxisWidth;

			Brush TextBrush = Theme.Brushes.Base03;

            // Size-Change
            bool bSizeXChange = OldBounds.Width > 0 && OldBounds.Width != bounds.Width;
            bool bSizeYChange = OldBounds.Height > 0 && OldBounds.Height != bounds.Height;

            if (bSizeXChange)
            {
                double ratioX = (double)(bounds.Width - PlotMargin * 6) / (double)(OldBounds.Width - PlotMargin * 6);
                m_CenterPoint.X = (int)Math.Round((double)m_CenterPoint.X * ratioX);

                if (!bSizeYChange)
                {
                    double ratioY = (double)(Bounds.Height - PlotMargin * 6) / (double)(OldBounds.Height - PlotMargin * 6) * (AspectRatio(Bounds) / AspectRatio(OldBounds));
                    m_CenterPoint.Y = (int)Math.Round((double)m_CenterPoint.Y * ratioY);
                }
            }

            if (bSizeYChange)
            {                
                double ratioY = (double)(Bounds.Height - PlotMargin * 6) / (double)(OldBounds.Height - PlotMargin * 6) * (AspectRatio(Bounds) / AspectRatio(OldBounds));
                m_CenterPoint.Y = (int)Math.Round((double)m_CenterPoint.Y * ratioY);
            }

            OldBounds = bounds;

			float offsetX = bounds.X;
			float offsetY = bounds.Y;

            // Zero Point
			PointF Zero = Point2Client(0, 0, bounds).Add(offsetX, offsetY);
            
            // Pfeile
            PointF EndX = new PointF(bounds.Right - PlotMargin, Zero.Y);
            PointF EndY = new PointF(Zero.X, PlotMargin + offsetY);

            bool bXAxisPainted = false;
            bool bYAxisPainted = false;

            // Achsen
            if (Zero.Y < Bounds.Bottom - PlotMargin && Zero.Y > PlotMargin + offsetY)
            {
                bXAxisPainted = true;
				ctx.DrawLine(AxisPen, PlotMargin + offsetX, Zero.Y, Bounds.Right - PlotMargin, Zero.Y);
                // Y-Achse Pfeil
				ctx.DrawLine(AxisPen, EndY.X, EndY.Y, EndY.X - 3, EndY.Y + 6);
				ctx.DrawLine(AxisPen, EndY.X, EndY.Y, EndY.X + 3, EndY.Y + 6);                       
            }

            if (Zero.X < Bounds.Right - PlotMargin && Zero.X > PlotMargin + offsetX)
            {
                bYAxisPainted = true;
				ctx.DrawLine(AxisPen, Zero.X, PlotMargin + offsetY, Zero.X, Bounds.Bottom - PlotMargin);
                // X-Achse Pfeil
				ctx.DrawLine(AxisPen, EndX.X, EndX.Y, EndX.X - 6, EndX.Y - 3);
				ctx.DrawLine(AxisPen, EndX.X, EndX.Y, EndX.X - 6, EndX.Y + 3);
            }
				            
			// giving some extra margin (PlotMargin * 1.5) 
			// should look more beautiful at the borders.
			double MinX = Client2PointX(PlotMargin * 1.5, Bounds);
			double MaxX = Client2PointX(Bounds.Width - PlotMargin * 1.5, Bounds);
			double MinY = Client2PointY(Bounds.Height - PlotMargin * 1.5, Bounds);
			double MaxY = Client2PointY(PlotMargin * 1.5, Bounds);

            double dx = MaxX - MinX;
            double dy = MaxY - MinY;

            if (dx <= 0d || dy <= 0d)
                return;

            double stepX = 1;
            double stepY = 1;
            
            // http://en.wikipedia.org/wiki/Power_of_two

            if (dx / 40.0 > 1)
            {
                stepX = Math.Pow(2, Math.Floor(Math.Log(dx / 20, 2)));
            }
            else if (dx / 20.0 < 1)
            {
                stepX = Math.Pow(2, Math.Ceiling(Math.Log(dx / 40, 2)));
            }            

            if (XAxisDataType == AxisDataTypes.axNumeric)
            {
                stepY = stepX * (yRange / xRange);
                //stepY = stepX;
            }
            else
            {
                if (dy / 40.0 > 1)
                {
                    stepY = Math.Pow(2, Math.Floor(Math.Log(dy / 20, 2)));
                }
                else if (dy / 20.0 < 1)
                {
                    stepY = Math.Pow(2, Math.Ceiling(Math.Log(dy / 40, 2)));
                }
            }

			if (stepX <= 0 || stepY <= 0)
                return;

            PointF pStep;
            double iStart = 0;

            // ************** Linien ***************

            Pen penGrid = new Pen(Color.FromArgb(64, GridColor));
            Pen penGridBold = new Pen(GridColor);

            // Vertikale Linien
            // Links

            int iBold = 0;
            Pen LinePen;

			iStart = ((int)(MaxX / stepX)) * stepX;
            iStart = Math.Min(iStart, 0);
            iBold = (int)Math.Abs(iStart / stepX);            

			float tickLen = 3f.Scale (ScaleFactor);

			for (double i = iStart - stepX; i >= MinX; i -= stepX)
            {
			    iBold++;
                if (iBold % 5 == 0)
                    LinePen = penGridBold;
                else
                    LinePen = penGrid;

				pStep = Point2Client(i, 0, Bounds).Add(offsetX, offsetY);

                if (DrawGrid)
					ctx.DrawLine(LinePen, pStep.X, PlotMargin + offsetY, pStep.X, Bounds.Bottom - PlotMargin);

                if (bXAxisPainted)
					ctx.DrawLine(AxisPen, pStep.X, pStep.Y - tickLen, pStep.X, pStep.Y + tickLen);
            }

            // Rechts            
           
            iStart = ((int)(MinX / stepX)) * stepX;
            iStart = Math.Max(iStart, 0);
            iBold = (int)Math.Abs(iStart / stepX);

			for (double i = iStart + stepX; i <= MaxX; i += stepX)
            {
			    iBold++;
                if (iBold % 5 == 0)
                    LinePen = penGridBold;
                else
                    LinePen = penGrid;

				pStep = Point2Client(i, 0, Bounds).Add(offsetX, offsetY);

                if (DrawGrid)
					ctx.DrawLine(LinePen, pStep.X, PlotMargin + offsetY, pStep.X, bounds.Bottom - PlotMargin);

                if (bXAxisPainted)
					ctx.DrawLine(AxisPen, pStep.X, pStep.Y - tickLen, pStep.X, pStep.Y + tickLen);
            }

            // Horizontale Linien
            // Untere H�lfte            

            iStart = ((int)(MaxY / stepY)) * stepY;
            iStart = Math.Min(iStart, 0);
            iBold = (int)Math.Abs(iStart / stepY);
            
            for (double i = iStart - stepY; i >= MinY; i -= stepY)
            {
			    iBold++;
                if (iBold % 5 == 0)
                    LinePen = penGridBold;
                else
                    LinePen = penGrid;

				pStep = Point2Client(0, i, Bounds).Add(offsetX, offsetY);

                if (DrawGrid)
					ctx.DrawLine(LinePen, PlotMargin + offsetX, pStep.Y, Bounds.Right - PlotMargin, pStep.Y);

                if (bYAxisPainted)
					ctx.DrawLine(AxisPen, pStep.X - tickLen, pStep.Y, pStep.X + tickLen, pStep.Y);
            }

            // Obere H�lfte

            iStart = ((int)(MinY / stepY)) * stepY;
            iStart = Math.Max(iStart, 0);
            iBold = (int)Math.Abs(iStart / stepY);

			for (double i = iStart + stepY; i <= MaxY; i += stepY)
            {
			    iBold++;
                if (iBold % 5 == 0)
                    LinePen = penGridBold;
                else
                    LinePen = penGrid;

				pStep = Point2Client(0, i, Bounds).Add(offsetX, offsetY);

                if (DrawGrid)
					ctx.DrawLine(LinePen, PlotMargin + offsetX, pStep.Y, Bounds.Right - PlotMargin, pStep.Y);

                if (bYAxisPainted)
					ctx.DrawLine(AxisPen, pStep.X - tickLen, pStep.Y, pStep.X + tickLen, pStep.Y);
            }				

            penGrid.Dispose();
            penGridBold.Dispose();

            // ************************ Labels **********************
			//int lineheight = (int)Font.LineSpacing + 2;

            stepX *= 5;
            stepY *= 5;

            // Links            
            iStart = ((int)(MaxX / stepX)) * stepX;
            iStart = Math.Min(iStart, 0);

			float fontHeight = Font.Height;
			int fontHalfHeight = (int)(Font.Height / 2f + 0.5f);

            for (double i = iStart - stepX; i >= MinX; i -= stepX)
            {
				pStep = Point2Client(i, 0, Bounds).Add(offsetX, offsetY);
				ctx.DrawString(FormatLabel(i, 0), Font, TextBrush, pStep.X, pStep.Y - (fontHeight * 2f), CenterFontFormat);                
            }

            // Rechts
            iStart = ((int)(MinX / stepX)) * stepX;
            iStart = Math.Max(iStart, 0);

            for (double i = iStart + stepX; i <= MaxX; i += stepX)
            {
				pStep = Point2Client(i, 0, Bounds).Add(offsetX, offsetY);
				ctx.DrawString(FormatLabel(i, 0), Font, TextBrush, pStep.X, pStep.Y, CenterFontFormat);
            }				

            // Unten
            iStart = ((int)(MaxY / stepY)) * stepY;
            iStart = Math.Min(iStart, 0);

            for (double i = iStart - stepY; i >= MinY; i -= stepY)
            {
				pStep = Point2Client(0, i, Bounds).Add(offsetX, offsetY);
				ctx.DrawString(FormatLabel(i, 1), Font, TextBrush, pStep.X - fontHalfHeight, pStep.Y - fontHeight, BelowAxisFontFormat);
            }				

            // Oben
            iStart = ((int)(MinY / stepY)) * stepY;
            iStart = Math.Max(iStart, 0);

            for (double i = iStart + stepY; i <= MaxY; i += stepY)
            {
				pStep = Point2Client(0, i, Bounds).Add(offsetX, offsetY);
				ctx.DrawString(FormatLabel(i, 1), Font, TextBrush, pStep.X + fontHalfHeight, pStep.Y - fontHeight, AboveAxisFontFormat);
            }
        }

		public GraphPoint AddPoint(float ScreenX, float ScreenY)
        {
            OnBeforeItemChange();

			if (Graph == null || !Graph.Visible)
				return null;

			Rectangle bounds = Rectangle.Ceiling (Bounds);
			GraphPoint pt = new GraphPoint (Client2PointX (ScreenX, bounds), Client2PointY (ScreenY, bounds));
			Graph.AddPoint (pt);

            PointCatched = null;
            //this.Cursor = Cursors.Cross;
            Invalidate();
            OnItemChanged();
			return pt;
        }

        public void DeletePoint()
        {
			if (Graph == null || !Graph.Visible)
				return;

            if (PointCatched != null)
            {
                OnBeforeItemChange();
				Graph.DeletePoint (PointCatched);
            }

            PointCatched = null;
            //this.Cursor = Cursors.Cross;
            Invalidate();
            OnItemChanged();
        }

        private bool bMouseDownFlag = false;
        private bool LastMouseDownWasItem = false;

		public override void OnMouseDown (MouseButtonEventArgs e)
        {            
            base.OnMouseDown(e);
            bMouseDownFlag = true;

            if (!this.IsFocused && this.CanFocus)
            {
                this.Focus();
            }

            //System.Diagnostics.Debug.WriteLine("MouseDown " + e.X.ToString() + "," + e.Y.ToString());

            LastMouseDownWasItem = PointCatched != null;            


			bool bControlPressed = ModifierKeys.ControlPressed;
			if (PointCatched != null && (EditMode == PlotterEditModes.editErase || (e.Button == MouseButton.Left && bControlPressed) || e.Button == MouseButton.Right))
            {
                DeletePoint();                
                return;
            }

			if ((EditMode == PlotterEditModes.editPaint && e.Button == MouseButton.Left) || bControlPressed)
            {
                AddPoint(e.X, e.Y);
                LastMouseDownWasItem = true;
                return;
            }
            
            bMouseMoving = true;
            MoveStartX = e.X;
            MoveStartY = e.Y;            

            if (PointCatched != null)
            {
                OnBeforeItemChange();
				NodeCatchedStart = Point2Client((double)PointCatched.X, (double)PointCatched.Y, Bounds);
				Graph.SelectPoint (PointCatched);
            }
            else
            {                
                //this.Cursor = Cursors.SizeAll;
                MoveStartCenterPoint.X = m_CenterPoint.X;
                MoveStartCenterPoint.Y = m_CenterPoint.Y;
            }

			UpdateCursor ();
        }
        
		public override void OnDoubleClick (MouseButtonEventArgs e)
		{
			if (e.Button != MouseButton.Left)
				return;

			if (PointCatched != null)
			{
				if (!bMouseDownFlag)
					return;
			}
			else
			{
				if (LastMouseDownWasItem)   // Yeah, das geht gut.                
					return;

				bool bControlPressed = ModifierKeys.ControlPressed;
				if ((EditMode == PlotterEditModes.editMove || EditMode == PlotterEditModes.editErase) && !bControlPressed)
				{                					
					AddPoint (e.X - Bounds.Left, e.Y - Bounds.Top);

					// this causes problems with doubleclick
					//m_PointCatched = AddPoint (e.X - Bounds.Left, e.Y - Bounds.Top);
					//Invalidate ();
				}
			}
		}

		public override void OnMouseUp (MouseButtonEventArgs e)
        {            
            base.OnMouseUp(e);
            bMouseMoving = false;
			UpdateCursor ();
            //this.Cursor = Cursors.Cross;
        }
			
		public override void OnMouseMove (MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);            

            CurrentMousePosition.X = e.X;
            CurrentMousePosition.Y = e.Y;
            bMouseHover = true;

            //System.Diagnostics.Debug.WriteLine("MouseMove " + e.X.ToString() + "," + e.Y.ToString());

            if (MouseCoordChangedEvent != null)
                MouseCoordChangedEvent(this.FormatLabel(Client2PointX(e.X, Bounds), 0), this.FormatLabel(Client2PointY(e.Y, Bounds), 1));

            if (bMouseMoving)
            {
                if (EditMode == PlotterEditModes.editMove && PointCatched != null)
                {
                    bMouseDownFlag = false;

                    if (PointCatched != null)
                    {
                        try
                        {
							if (Graph.Points.Remove(PointCatched))
							{
								PointCatched.X = (decimal)Client2PointX(NodeCatchedStart.X - (MoveStartX - e.X), Bounds);
								PointCatched.Y = (decimal)Client2PointY(NodeCatchedStart.Y - (MoveStartY - e.Y), Bounds);
								Graph.Points.Add(PointCatched);
								Graph.SelectPoint(PointCatched);
							}
                        }
                        catch { }
                    }
                }
                else
                {
                    m_CenterPoint.X = MoveStartCenterPoint.X - (MoveStartX - e.X);
                    m_CenterPoint.Y = MoveStartCenterPoint.Y - (MoveStartY - e.Y);

                    if (LimitToView)
                    {
                        m_CenterPoint.X = Math.Min((0 + Bounds.Width) / 2 - PlotMargin, m_CenterPoint.X);
                        m_CenterPoint.X = Math.Max((0 - Bounds.Width) / 2 + PlotMargin, m_CenterPoint.X);

                        m_CenterPoint.Y = Math.Min((0 + Bounds.Height) / 2 - PlotMargin, m_CenterPoint.Y);
                        m_CenterPoint.Y = Math.Max((0 - Bounds.Height) / 2 + PlotMargin, m_CenterPoint.Y);
                    }
                }

                Invalidate();
            }
            else
            {
                if (EditMode == PlotterEditModes.editMove || EditMode == PlotterEditModes.editErase)
                {
					GraphPoint p = CatchNode ((float)(e.X - Bounds.Left), (float)(e.Y - Bounds.Top));
					if (p != m_PointCatched) {
						PointCatched = p;
					}					 
                }
            }            
        }
        
		public override void OnMouseLeave (IGUIContext ctx)
        {
            base.OnMouseLeave(ctx);
            bMouseHover = false;

            if (ShowCross)
                Invalidate();
        }

		public override bool OnMouseWheel (MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (e.OffsetY > 0)
                ZoomIn();
			else if (e.OffsetY < 0)
                ZoomOut();

			return true;
        }
			
        private GraphPoint CatchNode(float X, float Y)
        {
            if (this.IsDisposed || !Enabled || Graph == null)
                return null;
            
            double xd1 = this.Client2PointX(X - 5, Bounds);
			double xd2 = this.Client2PointX(X + 5, Bounds);
			double yd2 = this.Client2PointY(Y - 5, Bounds);
			double yd1 = this.Client2PointY(Y + 5, Bounds);

			if (Double.IsNaN(xd1) || Double.IsNaN(xd2) || Double.IsNaN(yd1) || Double.IsNaN(yd2))
                return null;

			try {
				return Graph.FindPoint ((decimal)xd1, (decimal)yd1, (decimal)xd2, (decimal)yd2).FirstOrDefault();
			} catch (Exception ex) {
				ex.LogWarning ();
				return null;
			}
        }

        private void InitializeComponent()
        {
			/**
			this.timer_catch = new System.Windows.Forms.Timer(this.components);
			this.timer_catch.Tick += new System.EventHandler(this.timer_catch_Tick);
			**/
        }

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{
			base.OnLayout (ctx, bounds);

			if (ZoomToFitXPending) {				
				ZoomToFitX ();
				ZoomToFitXPending = false;
			}

			if (ZoomToFitYPending) {				
				ZoomToFitY ();
				ZoomToFitYPending = false;
			}
		}

		private bool ZoomToFitXPending = false;
		private bool ZoomToFitYPending = false;

        // *** Zoomen
		public bool CanZoomIn
		{
			get{
				return (ZoomX * ZoomFactor < maxZoom && ZoomY * ZoomFactor < maxZoom);
			}
		}

		public bool CanZoomOut
		{
			get{
				return (ZoomX * ZoomFactor > minZoom && ZoomY * ZoomFactor > minZoom);
			}
		}

		public bool CanZoomToFit
		{
			get{
				return CurrentNumberOfPoints > 0;
			}
		}

		public bool CanZoomOriginal
		{
			get{
				return Math.Abs(ZoomX - 1d) > 0.00001 && Math.Abs(ZoomY - 1d) > 0.00001;
			}
		}


        public void ZoomCenter()
        {
            this.m_CenterPoint.X = 0;
            this.m_CenterPoint.Y = 0;
            this.Invalidate();
        }

		const double maxZoom = 45000000.0;
		const double minZoom = 1.0 / 45000000.0;
		const double ZoomFactor = 1.25;

        public void ZoomIn()
        {           
            //const double Factor = 2.0;			           

            if (this.ZoomX * ZoomFactor > maxZoom)
                return;

            if (this.ZoomY * ZoomFactor > maxZoom)
                return;

            this.ZoomX *= ZoomFactor;
            this.ZoomY *= ZoomFactor;
            m_CenterPoint.X = (int)Math.Round((double)m_CenterPoint.X * ZoomFactor);
            m_CenterPoint.Y = (int)Math.Round((double)m_CenterPoint.Y * ZoomFactor);
            this.Invalidate();
        }

        public void ZoomOut()
        {
            //const double Factor = 1.25;
            //const double Factor = 2.0;

            this.ZoomX /= ZoomFactor;
            this.ZoomY /= ZoomFactor;
            m_CenterPoint.X = (int)Math.Round((double)m_CenterPoint.X / ZoomFactor);
            m_CenterPoint.Y = (int)Math.Round((double)m_CenterPoint.Y / ZoomFactor);
            this.Invalidate();
        }

        public void ZoomOriginal()
        {
            m_CenterPoint.X = (int)Math.Round((double)m_CenterPoint.X / ZoomX);
            m_CenterPoint.Y = (int)Math.Round((double)m_CenterPoint.Y / ZoomY);
            this.ZoomX = 1.0;
            this.ZoomY = 1.0;

            if (m_CenterPoint.X == Int32.MaxValue || m_CenterPoint.X == Int32.MinValue
                || m_CenterPoint.Y == Int32.MaxValue || m_CenterPoint.Y == Int32.MinValue)
            {
                m_CenterPoint.X = 0;
                m_CenterPoint.Y = 0;
            }

            this.Invalidate();
        }

        public void ZoomToFitX()
        {
			if (Graph == null || Graph.Points.Count < 2)
				return;

			// TODO:
			//double MinX = Graph.MinX;
			//double MaxX = Graph.MaxX;

			double MinX = (double)Graph.Points.Min(p => p.X);
			double MaxX = (double)Graph.Points.Max(p => p.X);

			double RangeX = MaxX - MinX;
			double pwb = PixelWidth (Bounds);
			if (RangeX <= 0 || pwb <= 0) {
				ZoomToFitXPending = true;
				return;
			}

			//this.xRange = RangeX;

			ZoomX = (double)(Bounds.Width - 6 * PlotMargin) / RangeX * pwb;
            //ZoomY = ZoomX;

            // Mitte verschieben:
            this.m_CenterPoint.X = 0;
			this.m_CenterPoint.X = (float)((Bounds.Width / 2) - Point2ClientX(RangeX / 2 + MinX, Bounds) + 0.5f);

            this.Invalidate();            
        }

        public void ZoomToFitY()
        {
			if (Graph == null || Graph.Points.Count < 2)
				return;

			// TODO:
			//double MinY = Graph.MinY;
			//double MaxY = Graph.MaxY;

			double MinY = (double)Graph.Points.Min(p => p.Y);
			double MaxY = (double)Graph.Points.Max(p => p.Y);

			if (Math.Abs(MinY) < double.Epsilon && Math.Abs(MaxY) < double.Epsilon)
                return;

			if (Math.Abs(MaxY - MinY) < double.Epsilon)
            {
				Graph.MaxY *= 1.5;
				Graph.MinY /= 2;
            }

			double phb = PixelHeight (Bounds);
            double RangeY = MaxY - MinY;

			if (RangeY <= 0 || phb <= 0) {
				ZoomToFitYPending = true;
				return;
			}

			this.yRange = RangeY;

            ZoomY = (double)(Bounds.Height - 6 * PlotMargin) / RangeY * PixelHeight(Bounds) / AspectRatio(Bounds);

            // Mitte verschieben:
            this.m_CenterPoint.Y = 0;
			this.m_CenterPoint.Y = (float)((Bounds.Height / 2) - Point2ClientY(RangeY / 2 + MinY, Bounds) + 0.5f);

            this.Invalidate();
        }

        public void ZoomToFit()
        {
            ZoomToFitX();
            ZoomToFitY();
        }
    }
}
