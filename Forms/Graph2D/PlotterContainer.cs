using System;
using System.Drawing;
using SummerGUI.DataGrid;
using SummerGUI.Charting.Graph2D;

namespace SummerGUI.Charting
{
	public class PlotterContainer : SplitContainer
	{
		public Graph2DPlotter Plotter { get; private set; }
		public DataGridView GRD { get; private set; }

		public GraphList Graphs  { get; private set; }

		public PlotterContainer (string name)			
			: base(name, SplitOrientation.Vertical, -240f)
		{
			Plotter = new Graph2DPlotter ("plotter");
			Panel1.AddChild(Plotter);

			GRD = new DataGridView ("dgv");
			GRD.RowHeaderWidth = 0;
			GRD.AlternatingRowColor = Color.FromArgb (50, Theme.Colors.Cyan);
			Panel2.AddChild(GRD);

			Graphs = new GraphList ();
			GraphBase GB = new GraphBase (null, GRD);
			Graphs.Add (GB);
			GRD.SetDataProvider(GB);
			GB.OnDataLoaded ();

			Plotter.Graphs = Graphs;
			Plotter.Graph = GB;

			GB.GraphColor = Theme.Colors.Orange;
		}

		public override void Focus ()
		{
			GRD.Focus ();
		}
	}		
}

