using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Data;
using System.ComponentModel;
using System.Xml.Serialization;
using KS.Foundation;
using SummerGUI.DataGrid;

namespace SummerGUI.Charting.Graph2D
{
    [Serializable]    
	public class GraphPoint : IComparable<GraphPoint>
    {		
        public Decimal X { get; set; }
		public Decimal Y { get; set; }
                        
        public string Key { get; set; }

        public GraphPoint()
        {
            Key = System.Guid.NewGuid().ToString();
        }

        public GraphPoint(double x, double y)
        {
            Key = System.Guid.NewGuid().ToString();
			this.X = (decimal)x;
			this.Y = (decimal)y;
        }

		public GraphPoint(decimal x, decimal y)
        {
            Key = System.Guid.NewGuid().ToString();
            this.X = x;
            this.Y = y;
        }

		public int CompareTo (GraphPoint other)
		{
			if (other == null)
				return 0;
			return this.X == other.X ? this.Y.CompareTo (other.Y) : this.X.CompareTo (other.X);
		}
    }

    [Serializable]    
	public class GraphBase : Controller, IDataProvider
    {
		public IDataProviderOwner Owner { get; set; }
		public IColumnManager ColumnManager { get; private set; }
		public IRowManager RowManager { get; private set; }
		public ISelectionManager SelectionManager { get; private set; }

		public event EventHandler<EventArgs> DataLoaded;
		public void OnDataLoaded()
		{
			Owner.OnDataLoaded ();
			if (DataLoaded != null)
				DataLoaded (this, EventArgs.Empty);
		}

		public void Clear()
		{
            Points.Clear();
        }

		public void Remove(int row)  
		{
			throw new NotImplementedException ();
		}

		public void InitializeColumns ()
		{
			ColumnManager.Columns.Add (new DataGridColumn {
				Text = "Y",
				Width = 0.5f,
				MinWidth = 60,
				AutoMinWidth = true,
				AllowSort = false,
				DisplayFormatString = "n9"
			});

			ColumnManager.Columns.Add (new DataGridColumn {
				Text = "X",
				Width = 0.5f,
				MinWidth = 60,
				AutoMinWidth = true,
				AllowSort = false,
				DisplayFormatString = "n9"
			});

			Owner.ScrollBars = ScrollBars.Vertical;
		}

		public void InitializeRows ()
		{
			RowManager.RowCount = Points.Count;
			RowManager.MoveFirst ();
		}

		public string GetValue (int row, int col)
		{
			if (row >= Points.Count)
				return String.Empty;

			if (col == 0)
				return Points [row].X.ToString ("n9").StripTrailingZeros();
			else
				return Points [row].Y.ToString ("n9").StripTrailingZeros();
		}

		public int GroupLevel (int row)
		{
			return 0;
		}

		public async Task ApplySort ()
		{			
		}			

		public IEnumerable<GraphPoint> FindPoint(decimal x1, decimal y1, decimal x2, decimal y2)
		{
			//if (Points.Count == 0)

			int index = Points.IndexOfElementOrSuccessor (new GraphPoint(x1, y1));
			while (index >= 0 && index < Points.Count) 			
			{
				GraphPoint p = Points [index];
				if (p.X > x2)
					yield break;

				if (p.Y >= y1 && p.Y <= y2)
					yield return p;

				index++;
			}
		}

		public void SelectPoint(GraphPoint point)
		{
			int index = Points.IndexOf (point);
			if (index >= 0 && index < Points.Count) {
                SelectionManager.SelectNone();                
				RowManager.CurrentRowIndex = index;
				RowManager.EnsureRowindexVisible (index);
			}
		}

		public bool AddPoint(GraphPoint p)
		{
			if (p == null)
				return false;

			// check if a point with the same X-Value exists
			int index = Points.IndexOfElementOrSuccessor (p);
			if (index >= 0 && index < Points.Count) {
				GraphPoint pp = Points [index];
				if ((double)Math.Abs (pp.X - p.X) < Double.Epsilon)
					return false;
			}
				
			Points.Add (p);
			RowManager.RowCount = Points.Count;
			RowManager.CurrentRowIndex = Points.IndexOf (p);
			return true;
		}

		public bool DeletePoint(GraphPoint p)
		{
			if (p == null)
				return false;
			
			if (Points.Remove (p)) {
				RowManager.RowCount = Points.Count;
				return true;
			}

			return false;
		}


        protected bool m_SupportsThreaded = false;
        public bool SupportsThreaded
        {
            get
            {
                return m_SupportsThreaded;
            }
        }

        protected Decimal[] m_Coefficients = null;
		public Decimal[] Coefficients
        {
            get
            {
                return m_Coefficients;
            }
            set
            {
                m_Coefficients = value;
            }
        }

        public int ListID { get; set; }

		/**
		protected Decimal[] pointsX = null;
		protected Decimal[] pointsY = null;
        protected int rowcount = 0;
        **/


        private Color m_GraphColor = Color.Blue;
        [XmlIgnore]
        public Color GraphColor
        {
            get
            {
                return m_GraphColor;
            }
            set
            {
                m_GraphColor = value;
            }
        }

		/**
        [XmlElement("GraphColor")]
        public string GraphColorXml
        {
            get
            {
                return XmlHelpers.SerializeColor(m_GraphColor);
            }
            set
            {
                m_GraphColor = XmlHelpers.DeserializeColor(value, Color.Blue);
            }
        }
        **/
        
        public GraphTypes GraphType   { get; set; }
        public GraphSubTypes GraphSubType  { get; set; }
        public string GraphDescription { get; set; }
        public string GraphFunctionString  { get; set; }
        public string Key { get; set; }
        public bool Visible { get; set; }
        public bool HasResult { get; set; }
        public bool CanEvaluate { get; set; }
		        
		public BinarySortedList<GraphPoint> Points { get; private set; }
        
        public AxisDataTypes XAxisDataType { get; set; }
        public string StringFormatX { get; set; }
        public string StringFormatY { get; set; }

        [XmlIgnore]
        internal bool bCancel = false;

		public double MinX { get; set; }
		public double MaxX { get; set; }
		public double MinY { get; set; }
		public double MaxY { get; set; }

		public GraphBase(IController parent, IDataProviderOwner owner)
			: base(parent)
        {
			Owner = owner;

            Key = System.Guid.NewGuid().ToString();
            GraphType = GraphTypes.gtFunction;
            GraphSubType = GraphSubTypes.gstLinear;
            GraphDescription = "unknown";
            GraphFunctionString = "";

            XAxisDataType = AxisDataTypes.axNumeric;
            StringFormatX = "";
            StringFormatY = "";

            Visible = true;
            HasResult = true;
            CanEvaluate = true;

			Points = new BinarySortedList<GraphPoint>();
            ListID = 0;

			ColumnManager = this.AddSubController(new ColumnManager (this, Owner));
			RowManager = this.AddSubController(new RowManager (this, Owner));
			SelectionManager = this.AddSubController(new SelectionManager(this, Owner));

			InitializeColumns ();
			InitializeRows ();

			MinX = -10.0;
			MaxX = 10.0;
			MinY = -10.0;
			MaxY = 10.0;
        }

        public virtual void NamePoints()
        {
            if (Points == null)
                return;

            if (Points.Count > 0)
            {
                Points[0].Key = this.GraphDescription + " {First}";
            }

			if (Points.Count > 1)
            {
				Points[Points.Count - 1].Key = this.GraphDescription + " {Last}";
            }

			if (Points.Count > 2)
            {
				for (int i = 1; i < Points.Count - 1; i++)
                {
                    Points[i].Key = this.GraphDescription + " {" + (i + 1).ToString() + "}";
                }
            }
        }

        public virtual void FinalizeDeserialization(GraphList graphlist)
        {
        }
			        
        public virtual GraphBase FindGraphByKey(string key, GraphList graphlist)
        {            
            if (graphlist == null)
                return null;

            if (graphlist.ContainsKey(key))            
                return graphlist[key];
                        
            return null;            
        }

        public virtual GraphPoint GetPointByKey(string key)
        {
            if (Points == null || Points.Count == 0 || String.IsNullOrEmpty(key))
                return null;

            if (key.IndexOf(this.GraphDescription) < 0)
                return null;

            int i1 = key.LastIndexOf('{');
            int i2 = key.LastIndexOf('}');

            if (i1 < 0 || i2 < 0 || i2 < i1)
                return null;

            string strPointIndex = key.Substring(i1 + 1, i2 - i1 - 1);

            if (strPointIndex == "First")
            {
                return Points[0];
            }
            else if (strPointIndex == "Last")
            {
                return Points[Points.Count - 1];
            }
            else if (Strings.IsNumeric(strPointIndex))
            {
                int pointIndex = strPointIndex.SafeInt() - 1;
                
                if (pointIndex < 0)
                    return Points[0];

                if (pointIndex >= Points.Count)
                    return Points[Points.Count - 1];

                return Points[pointIndex];
            }

            return null;
        }			        

        public virtual GraphPoint FindPointByKey(string key, GraphList graphlist)
        {            
            if (graphlist == null)
                return null;
            
			GraphPoint p = null;
            foreach (GraphBase g in graphlist.Values)
            {
                p = g.GetPointByKey(key);
                if (p != null)
                    return p;
            }

            return null;
        }

        public virtual void Init(DataTable DT, int listID)
        {
            ListID = listID;
        }

		public virtual Decimal FX(Decimal X)
        {
            return 0.0m;
        }

        public virtual string FXTherm()
        {
            return "";
        }        

        public void Cancel()
        {
            bCancel = true;
        }

        public bool Canceled
        {
            get
            {
                return bCancel;
            }
        }
    }
}
