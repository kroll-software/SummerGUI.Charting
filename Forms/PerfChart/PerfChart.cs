using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using SummerGUI.Charting.PerfCharts;

namespace SummerGUI.Charting
{    
    public class PerfChart : Widget
    {
        // Keep only a maximum MAX_VALUE_COUNT amount of values; This will allow
        //private const int MAX_VALUE_COUNT = 512;
		private int MAX_VALUE_COUNT = 240;
        // Draw a background grid with a fixed line spacing
        private const int GRID_SPACING = 32;

        // Amount of currently visible values (calculated from control width and value spacing)
        private int visibleValues = 0;
        // Horizontal value space in Pixels
		[DpiScalable]
		public float ValueSpacing { get; set; }
        // The currently highest displayed value, required for Relative Scale Mode
		public decimal CurrentMaxValue { get; private set; }
        // Offset value for the scrolling grid
        private float gridScrollOffset = 0;
        // Border Style
        // private Border3DStyle b3dstyle = Border3DStyle.Sunken;
        // List of stored values
		private LinkedList<decimal> drawValues;
        // Value queue for Timer Modes
		private ConcurrentQueue<decimal> waitingValues;
        // Style and Design

		public IGUIFont Font { get; set; }

		/// <summary>
		/// Gets or sets the display frequency in MilliSeconds.
		/// </summary>
		/// <value>The display frequency.</value>
		public int DisplayFrequency { get; set; }

		[DpiScalable]
		public int GridSpacing { get; set; }

		public string Caption { get; set; }
		int CaptionHeight { 
			get {
				if (Font == null || String.IsNullOrWhiteSpace (Caption))
					return 0;				
				return (int)(Font.CaptionHeight);
			}
		}			

		public override void OnResize ()
		{
			base.OnResize ();
			MAX_VALUE_COUNT = (Width / ValueSpacing + 2f).Ceil();
		}
        
        public PerfChart(string name)
			: base(name, Docking.Fill, new WidgetStyle())
		{   
			GridSpacing = GRID_SPACING;
			//ValueSpacing = GridSpacing;
			ValueSpacing = GridSpacing / 4;

			try {
				MAX_VALUE_COUNT = (OpenTK.DisplayDevice.Default.Bounds.Width / ValueSpacing).Ceil();	
			} catch (Exception ex) {
				ex.LogError ();
				MAX_VALUE_COUNT = 240;
			}				

            // Initialize Variables
            ChartStyle = new PerfChartStyle();
			        
			this.Font = SummerGUIWindow.CurrentContext.FontManager.CaptionFont;
			Caption = name.FormatForDisplay();

			ScaleMode = ScaleModes.Absolute;
			TimerMode = TimerModes.Simple;

			DisplayFrequency = 0;
			DemoSpeed = 1;

			//drawValues = new List<decimal>(MAX_VALUE_COUNT);
			drawValues = new LinkedList<decimal>();
			waitingValues = new ConcurrentQueue<decimal>();
        }			
			        
        public PerfChartStyle ChartStyle { get; set; }

		public Color AverageLineColor
		{
			get{
				return ChartStyle.AvgLinePen.Color;
			}
			set{
				ChartStyle.AvgLinePen.Color = value;
			}
		}

		public float AverageLineWidth
		{
			get{
				return ChartStyle.AvgLinePen.Width;
			}
			set{
				ChartStyle.AvgLinePen.Width = value;
			}
		}

		public Color LineColor
		{
			get{
				return ChartStyle.ChartLinePen.Color;
			}
			set{
				ChartStyle.ChartLinePen.Color = value;
			}
		}

		public float LineWidth
		{
			get{
				return ChartStyle.ChartLinePen.Width;
			}
			set{
				ChartStyle.ChartLinePen.Width = value;
			}
		}
				        
		public ScaleModes ScaleMode { get; set; }
        
		private TimerModes m_TimerMode;
        public TimerModes TimerMode 
		{
			get 
			{ 
				return m_TimerMode; 
			}
            set 
			{
                if (value == TimerModes.Disabled) {
                    // Stop and append only when changed
                    if (TimerMode != TimerModes.Disabled) {
						m_TimerMode = value;

                        // tmrRefresh.Stop();
                        // If there are any values in the queue, append them
                        ChartAppendFromQueue();
                    }
                }
                else {
					m_TimerMode = value;
                    // tmrRefresh.Start();
                }
            }
        }
			        
        /// <summary>
        /// Clears the whole chart
        /// </summary>
        public void Clear() {
            drawValues.Clear();
			ValuesSum = 0;
            Invalidate();
        }

		public enum FlowDirections
		{
			LeftToRight,
			RightToLeft
		}

		public FlowDirections FlowDirection { get; set; }
		public bool GridScrolling { get; set; }

        /// <summary>
        /// Adds a value to the Chart Line
        /// </summary>
        /// <param name="value">progress value</param>
		public void AddValue(decimal value) {
            if (ScaleMode == ScaleModes.Absolute && value > 100m)
                value = 100m;
                //throw new Exception(String.Format("Values greater then 100 not allowed in ScaleMode: Absolute ({0})", value));

            switch (TimerMode) {
                case TimerModes.Disabled:
                    ChartAppend(value);
                    Invalidate();
                    break;
                case TimerModes.Simple:
                case TimerModes.SynchronizedAverage:
                case TimerModes.SynchronizedSum:
                    // For all Timer Modes, the Values are stored in the Queue
                    AddValueToQueue(value);
                    break;
                default:
                    throw new Exception(String.Format("Unsupported TimerMode: {0}", TimerMode));
            }
        }
			        
        /// <summary>
        /// Add value to the queue for a timed refresh
        /// </summary>
        /// <param name="value"></param>
		private void AddValueToQueue(decimal value) 
        {
            waitingValues.Enqueue(value);
        }

		// The current average value
		public decimal AverageValue 
		{ 
			get {
				if (drawValues.Count == 0) {
					if (ScaleMode == ScaleModes.Absolute)
						return 50m;
					else
						return 0;
				}
				return ValuesSum / drawValues.Count;
			}
		}

		private decimal ValuesSum = 0m;

        /// <summary>
        /// Appends value <paramref name="value"/> to the chart (without redrawing)
        /// </summary>
        /// <param name="value">performance value</param>
		private void ChartAppend(decimal value) 
		{
			// flatten negative values to zero
			if (value < 0)
				value = 0;

            // Insert at first position
			drawValues.AddFirst(value);
			ValuesSum += value;

            // Remove last item if maximum value count is reached
			if (drawValues.Count > MAX_VALUE_COUNT) {
				ValuesSum -= drawValues.Last.Value;
				drawValues.RemoveLast ();
			}

            // Calculate horizontal grid offset for "scrolling" effect
			if (GridScrolling) {
				if (FlowDirection == FlowDirections.LeftToRight) {
					gridScrollOffset += ValueSpacing;
					if (gridScrollOffset > GridSpacing)
						gridScrollOffset = gridScrollOffset % GridSpacing;
				} else {
					gridScrollOffset -= ValueSpacing;
					if (gridScrollOffset < 0)
						gridScrollOffset = gridScrollOffset % GridSpacing;
				}
			}
        }


        /// <summary>
        /// Appends Values from queue
        /// </summary>
        private void ChartAppendFromQueue() {
            // Proceed only if there are values at all
            if (waitingValues.Count > 0) {
                if (TimerMode == TimerModes.Simple) 
                {                    
                    while (waitingValues.Count > 0)
                    {
						decimal d;                            
						if (waitingValues.TryDequeue (out d))
							ChartAppend (d);
						else
							break;
                    }
                }
                else if (TimerMode == TimerModes.SynchronizedAverage ||
                         TimerMode == TimerModes.SynchronizedSum) {
                    // appendValue variable is used for calculating the average or sum value
					decimal appendValue = 0;
                    int valueCount = waitingValues.Count;
					                    
                    while (waitingValues.Count > 0)
                    {
						decimal d;
						if (waitingValues.TryDequeue (out d))
							appendValue += d;
						else
							break;
                    }

                    // Calculate Average value in SynchronizedAverage Mode
                    if (TimerMode == TimerModes.SynchronizedAverage)
						appendValue = appendValue / valueCount;

                    // Finally append the value
                    ChartAppend(appendValue);
                }
            }
            else {
                // Always add 0 (Zero) if there are no values in the queue
				//ChartAppend(0);    // Kroll: gestrichen...
            }

            // Refresh the Chart
            //Invalidate();            
        }

        /// <summary>
        /// Calculates the vertical Position of a value in relation the chart size,
        /// Scale Mode and, if ScaleMode is Relative, to the current maximum value
        /// </summary>
        /// <param name="value">performance value</param>
        /// <returns>vertical Point position in Pixels</returns>
		private float CalcVerticalPosition(decimal value) 
		{
			float result = 0;
			float height = this.Height - CaptionHeight;
            if (ScaleMode == ScaleModes.Absolute)
				result = ((float)value * height) / 100f;
            else if (ScaleMode == ScaleModes.Relative)
				result = (CurrentMaxValue > 0) ? height * (float)(value / CurrentMaxValue) : 0f;
			return height - result;
        }


        /// <summary>
        /// Returns the currently highest (displayed) value, for Relative ScaleMode
        /// </summary>
        /// <returns></returns>
		private decimal GetHighestValueForRelativeMode() {
			if (drawValues.Count == 0)
				return 0m;

			// TODO: this should be optimized
			return drawValues.Max ();
        }			        

        /// <summary>
        /// Draws the chart (w/o background or grid, but with border) to the Graphics canvas
        /// </summary>        
		private void DrawChart(IGUIContext ctx, RectangleF bounds) {
            
			if (drawValues.Count == 0)
				return;

			float captionHeight = CaptionHeight;
			float offsetY = bounds.Top + captionHeight;

			visibleValues = (Math.Min(bounds.Width / ValueSpacing, drawValues.Count) + 2).Ceil();

            if (ScaleMode == ScaleModes.Relative)
                CurrentMaxValue = GetHighestValueForRelativeMode();				            

            // Only draw average line when possible (visibleValues) and needed (style setting)
            if (visibleValues > 0 && ChartStyle.ShowAverageLine)
				DrawAverageLine(ctx, bounds, captionHeight);
				            
			// Connect all visible values with lines
			PointF previousPoint;
			float deltaX;

			if (FlowDirection == FlowDirections.LeftToRight) {
				previousPoint = new PointF (bounds.Left - ValueSpacing, offsetY + ((bounds.Height - captionHeight) / 2));
				deltaX = ValueSpacing;
			} else {
				previousPoint = new PointF (bounds.Right + ValueSpacing, offsetY + ((bounds.Height - captionHeight) / 2));
				deltaX = -ValueSpacing;
			}

			PointF currentPoint = new Point();

			using (new PaintWrapper (RenderingFlags.HighQuality)) {
				GL.Color4 (LineColor);
				GL.LineWidth (LineWidth);
				GL.Begin (PrimitiveType.Lines);

				LinkedListNode<decimal> node = drawValues.First;

				for (int i = 0; i < visibleValues; i++) {

					if (node == null)	// that's true, it always happens. Wher have I got a loop ?
						break;

					currentPoint.X = previousPoint.X + deltaX;
					currentPoint.Y = (float)(CalcVerticalPosition (node.Value) + offsetY);

					GL.Vertex2 (currentPoint.X, currentPoint.Y);
					GL.Vertex2 (previousPoint.X, previousPoint.Y);

					previousPoint = currentPoint;
					node = node.Next;
				}

				GL.End ();
			}

            // Draw current relative maximum value string
            if (ScaleMode == ScaleModes.Relative) {
                SolidBrush sb = new SolidBrush(ChartStyle.ChartLinePen.Color);
				ctx.DrawString(CurrentMaxValue.ToString(), this.Font, sb, new PointF(4.0f, 2.0f), 
					FontFormat.DefaultSingleLine);
            }

            // Draw Border on top
            // ControlPaint.DrawBorder3D(g, 0, 0, Width, Height, b3dstyle);
        }			

		private void DrawAverageLine(IGUIContext ctx, RectangleF bounds, float captHeight) {            
			float verticalPosition = (float)CalcVerticalPosition(AverageValue) + bounds.Top + captHeight;
			ctx.DrawLine(ChartStyle.AvgLinePen.Pen, bounds.Left, verticalPosition, bounds.Right, verticalPosition);
        }
			
		private decimal demovalue = 0;
		private long lastTick = 0;

		public override void OnLayout (IGUIContext ctx, RectangleF bounds)
		{			
			long currentTicks = DateTime.Now.Ticks;
			if (currentTicks - lastTick >= this.DisplayFrequency) {				

				switch (DemoMode) {
				case DemoModes.Sinus:
					if (demovalue > 99999m)
						demovalue = 0m;
					demovalue += 0.1m * DemoSpeed;
					AddValue ((decimal)((Math.Sin ((double)demovalue) + 1) * 50d));
					break;
				case DemoModes.Random:
					demovalue = (currentTicks - lastTick) * 0.15m * DemoSpeed;
					AddValue ((decimal)((Math.Sin ((double)demovalue) + 1) * 50d));
					break;
				default:
					break;
				}

				lastTick = currentTicks;
				if (DemoMode != DemoModes.None)
					Invalidate ();
			}

			ChartAppendFromQueue();
			base.OnLayout (ctx, bounds);

			//Rectangle test = this.Bounds;
		}			

		public override void OnPaintBackground (IGUIContext ctx, RectangleF bounds)
		{
			//base.OnPaintBackground (ctx, bounds);

			int captionHeight = CaptionHeight;
			if (captionHeight > 0) {
				ctx.FillRectangle(ChartStyle.CaptionBrush, bounds.Left, bounds.Top, bounds.Width, captionHeight);

				bounds.Y += captionHeight;
				bounds.Height -= captionHeight;
			}

			// Draw the background Gradient rectangle
			ctx.FillRectangle(ChartStyle.GradientBrush, bounds);

			// Grid-lines should be optimizd by GL.CallList or a Vertex-Buffer

			// Draw all visible, vertical gridlines (if wanted)
			if (ChartStyle.ShowVerticalGridLines) {				
				using (new PaintWrapper (RenderingFlags.HighQuality)) {					
					GL.LineWidth (ChartStyle.VerticalGridPen.Pen.Width);
					GL.Color4 (ChartStyle.VerticalGridPen.Pen.Color);
					GL.Begin (PrimitiveType.Lines);

					float i = bounds.Right + gridScrollOffset;
					while (i > bounds.Left) {
						GL.Vertex2 (i, bounds.Bottom);
						GL.Vertex2 (i, bounds.Top);
						i -= GridSpacing;
					}						

					GL.End ();
				}
			}

			// Draw all visible, horizontal gridlines (if wanted)
			if (ChartStyle.ShowHorizontalGridLines) {
				using (new PaintWrapper (RenderingFlags.HighQuality)) {
					GL.Color4 (ChartStyle.HorizontalGridPen.Pen.Color);
					GL.LineWidth (ChartStyle.HorizontalGridPen.Pen.Width);
					GL.Begin (PrimitiveType.Lines);

					for (int i = bounds.Bottom.Ceil(); i >= bounds.Top; i -= GridSpacing) {
						GL.Vertex2 (bounds.Right, i + 0.5f);
						GL.Vertex2 (bounds.Left, i + 0.5f);
					}

					GL.End ();
				}
			}
		}

		protected virtual void DrawCaption(IGUIContext ctx, RectangleF bounds)
		{
			if (String.IsNullOrWhiteSpace (Caption))
				return;

			bounds = new RectangleF (bounds.Left+ Padding.Left, bounds.Top, bounds.Width - Padding.Width, CaptionHeight);			
			if (ctx.DrawString (Caption, Font, ChartStyle.CaptionForegroundBrush, bounds, FontFormat.DefaultSingleLineCentered).Width.Ceil () >= bounds.Width)
				Tooltip = Caption;
			else
				Tooltip = null;
		}			

		public override void OnPaint (IGUIContext ctx, RectangleF bounds)
		{
			base.OnPaint (ctx, bounds);
			DrawCaption(ctx, bounds);
			DrawChart(ctx, bounds);
		}

		public enum DemoModes
		{
			None,
			Random,
			Sinus
		}

		public DemoModes DemoMode { get; set; }
		public decimal DemoSpeed { get; set; } 			        
    }
}
